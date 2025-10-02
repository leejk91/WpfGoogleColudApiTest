using Google.Cloud.Speech.V1;
using System.Threading.Channels;
using NAudio.Wave;
using System.IO;
using Microsoft.Extensions.Options;
using Google.Apis.Auth.OAuth2;

namespace GoogleCluodApiTest.Services
{
    public sealed class GoogleSttService : ISttService
    {
        private SpeechClient? _client;
        private SpeechClient.StreamingRecognizeStream? _stream;
        private Task? _readLoopTask;
        private readonly Channel<byte[]> _audioChan = Channel.CreateUnbounded<byte[]>();
        private CancellationTokenSource? _cts;
        private readonly GoogleCredential _credential;

        public event EventHandler<string>? PartialRecognized;
        public event EventHandler<string>? FinalRecognized;

        public GoogleSttService(IOptions<GoogleCloudOptions> googleOptions)
        {
            var googleOpt = googleOptions.Value;
            
            // JSON 파일에서 직접 인증 정보 읽기
            _credential = GoogleCredential.FromFile(googleOpt.ServiceAccountKeyPath);
        }

        /// <summary>
        /// STT 세션을 시작합니다. 내부적으로 SpeechClient 및 스트리밍을 초기화하고, 설정을 전송합니다.
        /// </summary>
        /// <param name="sampleRate"></param>
        /// <param name="languageCode"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task StartAsync(int sampleRate = 16000, string languageCode = "ko-KR", CancellationToken ct = default)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var builder = new SpeechClientBuilder
            {
                Credential = _credential
            };
            _client = await builder.BuildAsync(_cts.Token);
            _stream = _client.StreamingRecognize();

            // 첫 요청: 설정 전송
            await _stream.WriteAsync(new StreamingRecognizeRequest
            {
                StreamingConfig = new StreamingRecognitionConfig
                {
                    Config = new RecognitionConfig
                    {
                        Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                        SampleRateHertz = sampleRate,
                        LanguageCode = languageCode,
                        EnableAutomaticPunctuation = true,
                    },
                    InterimResults = true,
                    SingleUtterance = false
                }
            });

            // 응답 읽기 루프 (ReadAllAsync + await foreach)
            _readLoopTask = Task.Run(async () =>
            {

                try
                {
                    var responseStream = _stream!.GetResponseStream();
                    System.Diagnostics.Debug.WriteLine("[STT] Response loop started");
                    // 타입은 Google.Api.Gax.Grpc.AsyncResponseStream<StreamingRecognizeResponse>

                    while (await responseStream.MoveNextAsync(_cts!.Token).ConfigureAwait(false))
                    {
                        var response = responseStream.Current;
                        System.Diagnostics.Debug.WriteLine($"[STT] Got response: results={response.Results.Count}");
                        foreach (var result in response.Results)
                        {
                            var alt = result.Alternatives.FirstOrDefault();
                            if (alt is null) continue;

                            if (result.IsFinal)
                                FinalRecognized?.Invoke(this, alt.Transcript);
                            else
                                PartialRecognized?.Invoke(this, alt.Transcript);
                        }
                    }
                    System.Diagnostics.Debug.WriteLine("[STT] Response loop ended");
                }
                catch (Grpc.Core.RpcException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[STT] RpcException: {ex.Status} {ex.Message}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[STT] Loop error: {ex}");
                }
            }, _cts.Token);

            // 오디오 쓰기 루프
            _ = Task.Run(async () =>
            {
                while (await _audioChan.Reader.WaitToReadAsync(_cts!.Token))
                {
                    while (_audioChan.Reader.TryRead(out var chunk))
                    {
                        await _stream!.WriteAsync(new StreamingRecognizeRequest
                        {
                            AudioContent = Google.Protobuf.ByteString.CopyFrom(chunk)
                        });
                    }
                }
            }, _cts.Token);
            System.Diagnostics.Debug.WriteLine("[STT] StartAsync OK: config sent");
        }

        /// <summary>
        /// 오디오 데이터를 전송합니다. 내부적으로 채널에 데이터를 넣고, 별도의 쓰기 루프에서 스트림에 씁니다.
        /// </summary>
        /// <param name="pcm16"></param>
        /// <param name="count"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task SendAudioAsync(byte[] pcm16, int count, CancellationToken ct = default)
        {
            var buf = new byte[count];
            Buffer.BlockCopy(pcm16, 0, buf, 0, count);
            _audioChan.Writer.TryWrite(buf);
            System.Diagnostics.Debug.WriteLine($"[STT] SendAudio {count} bytes");
            return Task.CompletedTask;
        }

        /// <summary>
        /// STT 세션을 종료합니다. 내부적으로 스트림을 닫고, 읽기/쓰기 루프를 정리합니다.
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            System.Diagnostics.Debug.WriteLine("[STT] StopAsync called");
            try
            {
                _audioChan.Writer.Complete();
                if (_stream != null)
                    await _stream.WriteCompleteAsync();
            }
            catch { /* ignore */ }

            if (_readLoopTask != null)
            {
                try { await _readLoopTask; } catch { /* ignore */ }
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _stream = null;
            _client = null;
        }

        public async ValueTask DisposeAsync() => await StopAsync();

        /// <summary>
        /// 파일 기반 STT (비동기). 내부적으로 파일을 16kHz/Mono/WAV로 변환 후, Recognize 또는 LongRunningRecognize를 호출합니다.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="languageCode"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<string> TranscribeFileAsync(string filePath, string languageCode = "ko-KR", CancellationToken ct = default)
        {
            var client = _client;
            if (client == null)
            {
                var builder = new SpeechClientBuilder
                {
                    Credential = _credential
                };
                client = await builder.BuildAsync(ct);
            }

            // 1) 파일을 16kHz/Mono/PCM 으로 보장 (WAV 아닌 경우/샘플레이트 다른 경우 변환)
            string wav16000Path = await EnsureWav16kMonoAsync(filePath, ct);

            // 2) WAV 파일 내용을 바이트로 읽기
            var bytes = await File.ReadAllBytesAsync(wav16000Path, ct);
            var audio = RecognitionAudio.FromBytes(bytes);

            // 3) 짧은 파일은 Recognize, 긴 파일은 LongRunningRecognize
            double durationSec = GetWavDurationSec(wav16000Path);
            var config = new RecognitionConfig
            {
                Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                SampleRateHertz = 16000,
                LanguageCode = languageCode,
                EnableAutomaticPunctuation = true
            };

            if (durationSec <= 60) // ~1분 이내
            {
                var resp = await client.RecognizeAsync(config, audio, cancellationToken: ct);
                return string.Join(Environment.NewLine, resp.Results.SelectMany(r => r.Alternatives).Select(a => a.Transcript));
            }
            else
            {
                var op = await client.LongRunningRecognizeAsync(config, audio, cancellationToken: ct);
                var completed = await op.PollUntilCompletedAsync();
                var result = completed.Result;
                return string.Join(Environment.NewLine, result.Results.SelectMany(r => r.Alternatives).Select(a => a.Transcript));
            }
        }

        /// <summary>
        /// 파일을 16kHz/Mono/WAV 형식으로 변환합니다. 이미 해당 형식이면 그대로 반환합니다.
        /// </summary>
        /// <param name="srcPath"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private static async Task<string> EnsureWav16kMonoAsync(string srcPath, CancellationToken ct)
        {
            string ext = Path.GetExtension(srcPath).ToLowerInvariant();
            if (ext == ".wav")
            {
                using var r = new WaveFileReader(srcPath);
                bool need = !(r.WaveFormat.SampleRate == 16000 && r.WaveFormat.Channels == 1 && r.WaveFormat.Encoding == WaveFormatEncoding.Pcm);
            }

            // 공통 변환 경로
            string dstPath = Path.Combine(Path.GetTempPath(), $"stt_{Guid.NewGuid():N}.wav");

            await Task.Run(() =>
            {
                using var reader = ext == ".wav" ? (WaveStream)new WaveFileReader(srcPath) : new AudioFileReader(srcPath); // mp3/m4a 등도 처리
                                                                                                                           // 16k/Mono/16bit
                using var resampler = new MediaFoundationResampler(reader, new WaveFormat(16000, 16, 1))
                { ResamplerQuality = 60 };
                WaveFileWriter.CreateWaveFile(dstPath, resampler);
            }, ct);

            return dstPath;
        }

        private static double GetWavDurationSec(string wavPath)
        {
            using var r = new WaveFileReader(wavPath);
            return r.TotalTime.TotalSeconds;
        }
    }
}

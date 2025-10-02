using System.Diagnostics;
using NAudio.Wave;

namespace GoogleCluodApiTest.Services
{
    public sealed class NAudioCaptureService : IAudioCaptureService
    {
        private WaveInEvent? _waveIn;            // WASAPI (공유모드)
        private WaveFormat? _format;
        private BufferedWaveProvider? _buffered;    // 필요시 버퍼링
        private bool _running;

        public event EventHandler<byte[]>? AudioAvailable;

        /// <summary>
        /// 마이크 캡처 시작.
        /// </summary>
        /// <param name="sampleRate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task StartAsync(int sampleRate = 16000, CancellationToken ct = default)
        {
            if (_running) return Task.CompletedTask;
            _running = true;

            // 장치가 16kHz/Mono를 지원하지 않으면 예외가 날 수 있습니다.
            // 이 경우 옵션 B(리샘플)로 전환하거나 장치 포맷을 바꾸세요.
            _waveIn = new WaveInEvent
            {
                // 100ms면 16kHz에서 3200바이트(= 16000 * 2 * 0.1) 정도가 한 번에 들어옵니다.
                BufferMilliseconds = 100,
                NumberOfBuffers = 4,
                WaveFormat = new WaveFormat(sampleRate, 16, 1) // ★ 핵심: 16k/Mono/16bit
            };

            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;

            try
            {
                _waveIn.StartRecording();
                Debug.WriteLine($"[AUDIO] StartRecording: {sampleRate}Hz, {_waveIn.WaveFormat.BitsPerSample}bit, ch={_waveIn.WaveFormat.Channels}");
            }
            catch
            {
                // 시작 실패 시 정리
                _waveIn.DataAvailable -= OnDataAvailable;
                _waveIn.RecordingStopped -= OnRecordingStopped;
                _waveIn.Dispose();
                _waveIn = null;
                _running = false;
                throw;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 마이크 캡처 중지 및 정리.
        /// </summary>
        public Task StopAsync()
        {
            if (_waveIn != null)
            {
                try
                {
                    _waveIn.DataAvailable -= OnDataAvailable;
                    _waveIn.RecordingStopped -= OnRecordingStopped;
                    if (_running) _waveIn.StopRecording();
                }
                catch { /* ignore */ }
                finally
                {
                    _waveIn.Dispose();
                    _waveIn = null;
                }
            }
            _running = false;
            Debug.WriteLine("[AUDIO] StopRecording");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 비동기 정리.
        /// </summary>
        /// <returns></returns>
        public ValueTask DisposeAsync()
        {
            StopAsync();
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// 마이크에서 오디오 데이터가 들어올 때마다 호출됩니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (!_running) return;

            // e.Buffer에는 RAW PCM(헤더 없음)이 들어있습니다. 그대로 STT로 보낼 수 있습니다.
            // 보통 100ms 설정이면 e.BytesRecorded ≈ 3200 (16k/Mono/16bit 기준)
            if (e.BytesRecorded <= 0) return;

            // 버퍼 재사용 이슈 방지: 필요한 길이만 복사해서 전달
            var outBuf = new byte[e.BytesRecorded];
            Buffer.BlockCopy(e.Buffer, 0, outBuf, 0, e.BytesRecorded);

            Debug.WriteLine($"[AUDIO] In: {e.BytesRecorded} bytes");
            AudioAvailable?.Invoke(this, outBuf);
        }

        /// <summary>
        /// 녹음이 중지되었을 때 호출됩니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
                Debug.WriteLine($"[AUDIO] RecordingStopped with error: {e.Exception.Message}");
            else
                Debug.WriteLine("[AUDIO] RecordingStopped");
        }
    }
}

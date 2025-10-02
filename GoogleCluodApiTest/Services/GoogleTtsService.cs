using Google.Cloud.TextToSpeech.V1;
using GoogleCluodApiTest.Models;
using Microsoft.Extensions.Options;
using Google.Apis.Auth.OAuth2;

namespace GoogleCluodApiTest.Services
{
    /// <summary>
    /// Google Cloud Text-to-Speech API 를 사용하는 구현체입니다.
    /// </summary>
    class GoogleTtsService : ITtsService
    {
        private readonly TextToSpeechClient _client;

        /// <summary>
        /// 생성자. GoogleCloudOptions 를 통해 인증 정보를 설정하고 클라이언트를 초기화합니다.
        /// </summary>
        /// <param name="googleOptions"></param>
        public GoogleTtsService(IOptions<GoogleCloudOptions> googleOptions)
        {
            var googleOpt = googleOptions.Value;
            
            // JSON 파일에서 직접 인증 정보 읽기
            var credential = GoogleCredential.FromFile(googleOpt.ServiceAccountKeyPath);
            var builder = new TextToSpeechClientBuilder
            {
                Credential = credential
            };
            _client = builder.Build();
        }

        /// <summary>
        /// 사용 가능한 음성 목록을 가져옵니다.
        /// </summary>
        /// <param name="languageFilter"></param>
        /// <returns></returns>
        public async Task<IReadOnlyList<TtsVoice>> ListVoicesAsync(string? languageFilter = null)
        {
            var resp = await _client.ListVoicesAsync(new ListVoicesRequest { LanguageCode = languageFilter ?? "" });
            var list = new List<TtsVoice>();
            foreach (var v in resp.Voices)
            {
                foreach (var lang in v.LanguageCodes)
                {
                    list.Add(new TtsVoice(v.Name, lang, v.SsmlGender.ToString()));
                }
            }
            // 중복 제거 후 정렬
            return list
                .GroupBy(v => (v.Name, v.LanguageCode))
                .Select(g => g.First())
                .OrderBy(v => v.LanguageCode).ThenBy(v => v.Name)
                .ToList();
        }

        /// <summary>
        /// 텍스트를 음성으로 합성합니다.
        /// </summary>
        /// <param name="opt"></param>
        /// <returns></returns>
        public async Task<byte[]> SynthesizeAsync(TtsOptions opt)
        {
            var input = new SynthesisInput { Text = opt.Text };
            var voice = new VoiceSelectionParams
            {
                LanguageCode = opt.LanguageCode,
                Name = string.IsNullOrWhiteSpace(opt.VoiceName) ? null : opt.VoiceName
            };

            var config = new AudioConfig
            {
                SpeakingRate = opt.SpeakingRate,
                Pitch = opt.Pitch,
                VolumeGainDb = opt.VolumeGainDb,
                AudioEncoding = opt.AudioEncoding?.ToUpperInvariant() switch
                {
                    "MP3" => AudioEncoding.Mp3,
                    "OGG_OPUS" => AudioEncoding.OggOpus,
                    "LINEAR16" => AudioEncoding.Linear16, // RAW PCM
                    _ => AudioEncoding.Mp3
                }
            };

            var resp = await _client.SynthesizeSpeechAsync(new SynthesizeSpeechRequest
            {
                Input = input,
                Voice = voice,
                AudioConfig = config
            });

            return resp.AudioContent.ToByteArray();
        }
    }
}

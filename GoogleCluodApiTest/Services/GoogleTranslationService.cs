using Google.Cloud.Translate.V3;
using GoogleCluodApiTest.Models;
using Microsoft.Extensions.Options;
using Google.Apis.Auth.OAuth2;

namespace GoogleCluodApiTest.Services
{
    public class LanguageItem
    {
        public string Code { get; set; } = "";
        public string Display { get; set; } = "";
    }

    public sealed class GoogleTranslationService : ITranslationService
    {
        private readonly TranslationServiceClient _client;
        private readonly string _parent;

        public GoogleTranslationService(IOptions<GoogleCloudOptions> googleOptions)
        {
            var googleOpt = googleOptions.Value;

            // JSON 파일에서 직접 인증 정보 읽기
            var credential = GoogleCredential.FromFile(googleOpt.ServiceAccountKeyPath);
            var builder = new TranslationServiceClientBuilder
            {
                Credential = credential
            };
            _client = builder.Build();
            _parent = $"projects/{googleOpt.ProjectId}/locations/{googleOpt.Location}";
        }

        /// <summary>
        /// 텍스트를 번역합니다.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="targetLang"></param>
        /// <param name="sourceLang"></param>
        /// <returns></returns>
        public async Task<string> TranslateAsync(string text, string targetLang, string? sourceLang = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var request = new TranslateTextRequest
            {
                Parent = _parent,
                TargetLanguageCode = targetLang
            };
            request.Contents.Add(text);

            if(!string.IsNullOrWhiteSpace(sourceLang)) 
                request.SourceLanguageCode = sourceLang; 

            var response = await _client.TranslateTextAsync(request); 
            return response.Translations.FirstOrDefault()?.TranslatedText ?? string.Empty; 

        }

        /// <summary>
        /// 텍스트의 언어를 감지합니다.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task<IReadOnlyList<string>> DetectLanguageAsync(string text)
        {
            var req = new DetectLanguageRequest
            {
                Parent = _parent,
                Content = text
            };
            var resp = await _client.DetectLanguageAsync(req);
            return resp.Languages
                       .OrderByDescending(l => l.Confidence)
                       .Select(l => l.LanguageCode)
                       .ToList();
        }

        /// <summary>
        /// 지원하는 언어 목록을 가져옵니다.
        /// </summary>
        /// <param name="displayLang"></param>
        /// <returns></returns>
        public async Task<IReadOnlyList<(string Code, string DisplayName)>> GetSupportedLanguagesAsync(string displayLang = "en")
        {
            var req = new GetSupportedLanguagesRequest
            {
                Parent = _parent,
                DisplayLanguageCode = displayLang
            };
            var resp = await _client.GetSupportedLanguagesAsync(req);
            return resp.Languages
                       .Select(l => (l.LanguageCode, l.DisplayName))
                       .OrderBy(l => l.LanguageCode)
                       .ToList();
        }
    }
}

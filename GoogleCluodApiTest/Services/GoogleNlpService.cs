using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Language.V1;
using Google.Cloud.Translate.V3;
using Microsoft.Extensions.Options;

namespace GoogleCluodApiTest.Services
{
    public class GoogleNlpService : INlpService
    {
        private readonly LanguageServiceClient _client;

        public GoogleNlpService(IOptions<GoogleCloudOptions> googleOptions)
        {
            var googleOpt = googleOptions.Value;

            // JSON 파일에서 직접 인증 정보 읽기
            var credential = GoogleCredential.FromFile(googleOpt.ServiceAccountKeyPath);
            var builder = new LanguageServiceClientBuilder
            {
                Credential = credential
            };
            _client = builder.Build();
        }

        /// <summary>
        /// 텍스트에서 엔티티(사람, 장소, 날짜 등)를 추출합니다.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IReadOnlyList<EntityResult>> AnalyzeEntitiesAsync(string text, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<EntityResult>();

            var doc = Document.FromPlainText(text);
            var eRes = await _client.AnalyzeEntitiesAsync(doc, cancellationToken: ct);

            return eRes.Entities
                       .Select(e => new EntityResult(
                           e.Name,
                           e.Type.ToString(),
                           e.Salience))
                       .ToList();
        }

        /// <summary>
        /// 텍스트의 감정(긍정/부정)을 분석합니다.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<SentimentResult> AnalyzeSentimentAsync(string text, CancellationToken ct = default)
        {
            var doc = Document.FromPlainText(text);
            var sRes = await _client.AnalyzeSentimentAsync(doc, cancellationToken: ct);
            return new SentimentResult(sRes.DocumentSentiment.Score, sRes.DocumentSentiment.Magnitude);
        }

        /// <summary>
        /// 텍스트의 카테고리를 분류합니다.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IReadOnlyList<CategoryResult>> ClassifyTextAsync(string text, CancellationToken ct = default)
        {
            try
            {
                var doc = Document.FromPlainText(text);
                var res = await _client.ClassifyTextAsync(doc, cancellationToken: ct);
                return res.Categories.Select(c => new CategoryResult(c.Name, c.Confidence)).ToList();
            }
            catch (Exception)
            {
                return Array.Empty<CategoryResult>();
            }
        }
    }
}

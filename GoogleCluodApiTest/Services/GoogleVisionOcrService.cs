using System.Text;
using Google.Cloud.Vision.V1;
using Microsoft.Extensions.Options;
using Google.Apis.Auth.OAuth2;

namespace GoogleCluodApiTest.Services
{
    internal class GoogleVisionOcrService : IVisionOcrService
    {
        private readonly ImageAnnotatorClient _client;

        /// <summary>
        /// 생성자. GoogleCloudOptions 를 통해 인증 정보를 설정하고 클라이언트를 초기화합니다.
        /// </summary>
        /// <param name="googleOptions"></param>
        public GoogleVisionOcrService(IOptions<GoogleCloudOptions> googleOptions)
        {
            var googleOpt = googleOptions.Value;
            
            // JSON 파일에서 직접 인증 정보 읽기
            var credential = GoogleCredential.FromFile(googleOpt.ServiceAccountKeyPath);
            var builder = new ImageAnnotatorClientBuilder
            {
                Credential = credential
            };
            _client = builder.Build();
        }

        /// <summary>
        /// 이미지 파일에서 텍스트를 추출합니다.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public async Task<string> ExtractTextFromImageAsync(string filePath)
        {
            var image = await Image.FromFileAsync(filePath);
            var response = await _client.DetectTextAsync(image);

            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(response[0].Description))
            {
                sb.AppendLine(response[0].Description);
            }
            return sb.ToString().Trim();
        }
    }
}


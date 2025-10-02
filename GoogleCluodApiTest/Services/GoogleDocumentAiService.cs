using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.DocumentAI.V1;
using Google.Cloud.Language.V1;
using GoogleCluodApiTest.Models;
using Microsoft.Extensions.Options;

namespace GoogleCluodApiTest.Services
{
    public class GoogleDocumentAiService : IDocumentAiService
    {
        private readonly DocumentProcessorServiceClient _client;

        private readonly string _projectId;
        private readonly string _location;
        private readonly string _processorId;
        private readonly string _credentialFile;

        public GoogleDocumentAiService(IOptions<GoogleCloudOptions> googleOptions)
        {
            var googleOpt = googleOptions.Value;

            _processorId = googleOpt.DocumentProcessorId;
            _projectId = googleOpt.ProjectId;
            _location = googleOpt.DocumentLocation;

            var credential = GoogleCredential.FromFile(googleOpt.ServiceAccountKeyPath);
            var builder = new DocumentProcessorServiceClientBuilder
            {
                Credential = credential,

            };
            _client = builder.Build();
        }
        public async Task<DocAiResult> ProcessAsync(string filePath, CancellationToken ct = default)
        {
            var name = ProcessorName.FromProjectLocationProcessor(_projectId, _location, _processorId);
            var bytes = System.IO.File.ReadAllBytesAsync(filePath, ct);

            var mime = GuessMime(Path.GetExtension(filePath));

            var request = new ProcessRequest
            {
                Name = name.ToString(),
                RawDocument = new RawDocument
                {
                    Content = Google.Protobuf.ByteString.CopyFrom(bytes.Result),
                    MimeType = mime
                }
            };

            var response = _client.ProcessDocumentAsync(request, cancellationToken: ct);
            var doc = response.Result.Document;
            var fullText = doc.Text ?? string.Empty;
            int pageCount = doc.Pages.Count;

            var rawJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            return new DocAiResult
            {
                Text = fullText,
                PageCount = pageCount,
                RawJson = rawJson,
                MimeType = mime,
                FileName = Path.GetFileName(filePath),
                Summary = $"페이지 수: {pageCount}, Entities: {doc.Entities.Count}, Revisions={doc.Revisions.Count}"
            };
        }

        private static string GuessMime(string v)
        {
            v = v.ToLowerInvariant();
            return v switch
            {
                ".pdf" => "application/pdf",
                ".tif" or ".tiff" => "image/tiff",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/pdf"
            };
        }
    }
}

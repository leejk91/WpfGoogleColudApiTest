using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCluodApiTest.Services
{
    public interface INlpService
    {
        Task<SentimentResult> AnalyzeSentimentAsync(string text, CancellationToken ct = default);
        Task<IReadOnlyList<EntityResult>> AnalyzeEntitiesAsync(string text, CancellationToken ct = default);
        Task<IReadOnlyList<CategoryResult>> ClassifyTextAsync(string text, CancellationToken ct = default);
    }

    public record SentimentResult(float Score, float Magnitude);
    public record EntityResult(string Name, string Type, float Salience);
    public record CategoryResult(string Name,float Confidence);
}

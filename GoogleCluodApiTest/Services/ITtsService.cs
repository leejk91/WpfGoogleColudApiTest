using GoogleCluodApiTest.Models;

namespace GoogleCluodApiTest.Services
{
    public record TtsVoice(string Name, string LanguageCode, string Gender);

    public interface ITtsService
    {
        Task<byte[]> SynthesizeAsync(TtsOptions options);
        Task<IReadOnlyList<TtsVoice>> ListVoicesAsync(string? languageFilter = null);
    }
}

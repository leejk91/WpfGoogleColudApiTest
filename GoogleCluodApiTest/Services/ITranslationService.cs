namespace GoogleCluodApiTest.Services
{
    public interface ITranslationService
    {
        Task<string> TranslateAsync(string text, string targetLang, string? sourceLang = null);
        Task<IReadOnlyList<string>> DetectLanguageAsync(string text);
        Task<IReadOnlyList<(string Code, string DisplayName)>> GetSupportedLanguagesAsync(string displayLang = "en");
    }
}

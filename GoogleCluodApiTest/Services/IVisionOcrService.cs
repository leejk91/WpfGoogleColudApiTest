namespace GoogleCluodApiTest.Services
{
    public interface IVisionOcrService
    {
        Task<string> ExtractTextFromImageAsync(string filePath);
    }
}

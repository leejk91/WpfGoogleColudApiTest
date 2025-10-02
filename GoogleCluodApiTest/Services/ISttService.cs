namespace GoogleCluodApiTest.Services
{
    public interface ISttService : IAsyncDisposable
    {
        event EventHandler<string>? PartialRecognized;
        event EventHandler<string>? FinalRecognized;

        Task StartAsync(int sampleRate = 16000, string languageCode = "ko-KR", CancellationToken ct = default);
        Task SendAudioAsync(byte[] pcm16, int count, CancellationToken ct = default);
        Task StopAsync(); // 종료(스트림 닫기)

        Task<string> TranscribeFileAsync(string filePath, string languageCode = "ko-KR", CancellationToken ct = default);
    }
}

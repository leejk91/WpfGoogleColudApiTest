namespace GoogleCluodApiTest.Services
{
    public interface IAudioCaptureService : IAsyncDisposable
    {
        event EventHandler<byte[]>? AudioAvailable; // PCM16 청크
        Task StartAsync(int sampleRate = 16000, CancellationToken ct = default);
        Task StopAsync();
    }
}

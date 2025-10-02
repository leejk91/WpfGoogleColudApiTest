using System.Diagnostics;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoogleCluodApiTest.Services;

namespace GoogleCluodApiTest.ViewModels
{
    public partial class SttViewModel : ObservableObject
    {
        private readonly IAudioCaptureService _audio;
        private readonly ISttService _stt;
        private CancellationTokenSource? _cts;

        [ObservableProperty] private string _partialText = "";
        [ObservableProperty] private string _finalText = "";
        [ObservableProperty] private bool _isListening;
        [ObservableProperty] private bool _isBusy;

        public SttViewModel(IAudioCaptureService audio, ISttService stt)
        {
            _audio = audio;
            _stt = stt;

            _stt.PartialRecognized += (_, text) => PartialText = text;
            _stt.FinalRecognized += (_, text) =>
            {
                if (!string.IsNullOrWhiteSpace(text))
                    FinalText += (string.IsNullOrWhiteSpace(FinalText) ? "" : "\n") + text;
            };

            _audio.AudioAvailable += async (_, chunk) =>
            {
                if (IsListening)
                    await _stt.SendAudioAsync(chunk, chunk.Length);
            };
        }

        [RelayCommand]
        private async Task StartMicAsync()
        {
            Debug.WriteLine("StartMicAsync called");
            if (IsListening || IsBusy) return;
            IsListening = true;

            Debug.WriteLine("IsListening : " + IsListening);
            _cts = new CancellationTokenSource();

            await _stt.StartAsync(16000, "ko-KR", _cts.Token);
            await _audio.StartAsync(16000, _cts.Token);
        }

        [RelayCommand]
        private async Task StopMicAsync()
        {
            if (!IsListening) return;
            IsListening = false;

            await _audio.StopAsync();
            await _stt.StopAsync();

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        [RelayCommand]
        private void ClearText()
        {
            PartialText = "";
            FinalText = "";
        }

        [RelayCommand]
        private async Task TranscribeFileAsync()
        {
            if (IsBusy || IsListening) return;
            var dlg = new OpenFileDialog
            {
                Title = "오디오 파일 선택",
                Filter = "오디오 파일|*.wav;*.mp3;*.m4a;*.flac;*.aac;*.wma|모든 파일|*.*",
                Multiselect = false
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;



            IsBusy = true;
            try
            {
                using var cts = new CancellationTokenSource();
                var text = await _stt.TranscribeFileAsync(dlg.FileName, "ko-KR", cts.Token);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    if (!string.IsNullOrWhiteSpace(FinalText)) FinalText += "\n";
                    FinalText += text;
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}

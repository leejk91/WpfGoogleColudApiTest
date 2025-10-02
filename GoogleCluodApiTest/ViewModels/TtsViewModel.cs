using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoogleCluodApiTest.Models;
using GoogleCluodApiTest.Services;
using System.IO;

namespace GoogleCluodApiTest.ViewModels
{
    /// <summary>
    /// Google TTS 데모를 위한 뷰모델. 보이스 로드, 합성, 재생/저장을 담당합니다.
    /// </summary>
    public partial class TtsViewModel : ObservableObject
    {
        private readonly ITtsService _tts;
        private readonly MediaPlayer _player = new MediaPlayer();

        /// <summary>사용 가능한 보이스 목록</summary>
        public ObservableCollection<TtsVoice> Voices { get; } = new();
        /// <summary>사용 가능한 언어 코드 목록</summary>
        public ObservableCollection<string> Languages { get; } = new();

        /// <summary>합성할 텍스트</summary>
        [ObservableProperty] private string text = "안녕하세요! TTS 데모";
        /// <summary>선택된 언어 코드</summary>
        [ObservableProperty] private string languageCode = "ko-KR";
        /// <summary>선택된 보이스</summary>
        [ObservableProperty] private TtsVoice? selectedVoice;
        /// <summary>말하기 속도</summary>
        [ObservableProperty] private double rate = 1.0;
        /// <summary>피치</summary>
        [ObservableProperty] private double pitch = 0.0;
        /// <summary>볼륨 게인 dB</summary>
        [ObservableProperty] private double gain = 0.0;
        /// <summary>작업 중 여부</summary>
        [ObservableProperty] private bool isBusy;

        private byte[]? _lastAudio;
        /// <summary>마지막 합성 오디오 바이트</summary>
        public byte[]? LastAudio
        {
            get => _lastAudio;
            private set
            {
                SetProperty(ref _lastAudio, value);
                PlayCommand.NotifyCanExecuteChanged();
                SaveCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// 서비스 주입 및 초기 보이스 로드
        /// </summary>
        public TtsViewModel(ITtsService tts)
        {
            _tts = tts;
            _ = LoadVoicesAsync(); // 초기 로드
        }

        // XAML: Command="{Binding RefreshVoicesCommand}"
        [RelayCommand(CanExecute = nameof(CanRunWhenIdle))]
        private async Task RefreshVoicesAsync() => await LoadVoicesAsync();

        // XAML: Command="{Binding SynthesizeCommand}"
        [RelayCommand(CanExecute = nameof(CanSynthesize))]
        private async Task SynthesizeAsync()
        {
            try
            {
                IsBusy = true;
                var options = new TtsOptions
                {
                    Text = Text,
                    LanguageCode = SelectedVoice?.LanguageCode ?? LanguageCode,
                    VoiceName = SelectedVoice?.Name ?? "",
                    SpeakingRate = Rate,
                    Pitch = Pitch,
                    VolumeGainDb = Gain,
                    AudioEncoding = "MP3"
                };
                LastAudio = await _tts.SynthesizeAsync(options);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"합성 실패: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        // XAML: Command="{Binding PlayCommand}"
        [RelayCommand(CanExecute = nameof(CanPlay))]
        private void Play()
        {
            if (LastAudio == null) return;
            var temp = Path.Combine(Path.GetTempPath(), $"tts_{Guid.NewGuid():N}.mp3");
            File.WriteAllBytes(temp, LastAudio);
            _player.Open(new Uri(temp));
            _player.Play();
        }

        // XAML: Command="{Binding SaveCommand}"
        [RelayCommand(CanExecute = nameof(CanPlay))]
        private void Save()
        {
            if (LastAudio == null) return;
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "MP3 파일 (*.mp3)|*.mp3|모든 파일 (*.*)|*.*",
                FileName = "tts.mp3"
            };
            if (dlg.ShowDialog() == true)
                File.WriteAllBytes(dlg.FileName, LastAudio);
        }

        // ---- CanExecute 들
        private bool CanRunWhenIdle() => !IsBusy;
        private bool CanSynthesize() => !IsBusy && !string.IsNullOrWhiteSpace(Text);
        private bool CanPlay() => LastAudio != null;

        /// <summary>
        /// 공급자로부터 보이스 목록을 가져와 바인딩 컬렉션을 갱신합니다.
        /// </summary>
        private async Task LoadVoicesAsync()
        {
            try
            {
                IsBusy = true;
                Voices.Clear();
                Languages.Clear();

                var list = await _tts.ListVoicesAsync(LanguageCode);
                foreach (var v in list) Voices.Add(v);
                foreach (var g in System.Linq.Enumerable.DistinctBy(list, v => v.LanguageCode))
                    Languages.Add(g.LanguageCode);

                if (Languages.Count > 0 && !Languages.Contains(LanguageCode))
                    LanguageCode = Languages[0];

                SelectedVoice = Array.Find(Voices.ToArray(), v => v.LanguageCode == LanguageCode)
                                 ?? (Voices.Count > 0 ? Voices[0] : null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"보이스 목록을 가져오지 못했습니다.\n{ex.Message}");
            }
            finally
            {
                IsBusy = false;
                // Busy 상태 변경 시 버튼들 갱신
                RefreshVoicesCommand.NotifyCanExecuteChanged();
                SynthesizeCommand.NotifyCanExecuteChanged();
            }
        }

        // ObservableProperty 변경 시 추가 작업이 필요하면 partial 메서드 사용 가능
        /// <summary>
        /// IsBusy 변경 시 커맨드 재평가
        /// </summary>
        partial void OnIsBusyChanged(bool value)
        {
            RefreshVoicesCommand.NotifyCanExecuteChanged();
            SynthesizeCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Text 변경 시 합성 가능 여부 갱신
        /// </summary>
        partial void OnTextChanged(string value)
        {
            SynthesizeCommand.NotifyCanExecuteChanged();
        }
    }
}

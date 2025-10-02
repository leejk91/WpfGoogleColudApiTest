using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoogleCluodApiTest.Infrastructure;

namespace GoogleCluodApiTest.ViewModels
{
    /// <summary>
    /// 메인 창의 뷰모델. TTS 데모 창을 여는 동작을 제공합니다.
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IWindowService _windowService;
        private readonly System.IServiceProvider _sp;

        /// <summary>
        /// DI 로 창 서비스와 서비스 공급자를 주입받습니다.
        /// </summary>
        public MainWindowViewModel(IWindowService windowService, System.IServiceProvider sp)
        {
            _windowService = windowService;
            _sp = sp;
        }

        // XAML: Command="{Binding OpenTtsCommand}"
        [RelayCommand]
        private void OpenTts()
        {
            var vm = (TtsViewModel)_sp.GetService(typeof(TtsViewModel))!;
            _windowService.ShowWindow<Views.TtsWindow>(vm);
        }

        [RelayCommand]
        private void OpenStt()
        {
            var vm = (SttViewModel)_sp.GetService(typeof(SttViewModel))!;
            _windowService.ShowWindow<Views.SttWindow>(vm);
        }

        [RelayCommand]
        private void OpenOcr()
        {
            var vm = (OcrViewModel)_sp.GetService(typeof(OcrViewModel))!;
            _windowService.ShowWindow<Views.OcrWindow>(vm);
        }

        [RelayCommand]
        private void OpenTranslation()
        {
            var vm = (TranslationViewModel)_sp.GetService(typeof(TranslationViewModel))!;
            _windowService.ShowWindow<Views.TranslationWindow>(vm);
        }

        [RelayCommand]
        private void OpenNlp()
        {
            var vm = (NlpViewModel)_sp.GetService(typeof(NlpViewModel))!;
            _windowService.ShowWindow<Views.NlpWindow>(vm);
        }

        [RelayCommand]
        private void OpenDocument()
        {
            var vm = (DocumentAiViewModel)_sp.GetService(typeof(DocumentAiViewModel))!;
            _windowService.ShowWindow<Views.DocumentAiWindow>(vm);
        }

    }
}

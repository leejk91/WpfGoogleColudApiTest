using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoogleCluodApiTest.Services;

namespace GoogleCluodApiTest.ViewModels
{
    public partial class TranslationViewModel : ObservableObject
    {
        private readonly ITranslationService _translation;

        [ObservableProperty] private string? _projectId;
        [ObservableProperty] private string _inputText = "";
        [ObservableProperty] private string _outputText = "";
        [ObservableProperty] private string _targetLang = "en";
        [ObservableProperty] private string? _sourceLang;
        [ObservableProperty] private bool _isBusy;

        public ObservableCollection<LanguageItem> Languages { get; } = new();

        public TranslationViewModel(ITranslationService translation)
        {
            _translation = translation;
            _ = LoadLanguagesAsync();
        }

        private async Task LoadLanguagesAsync()
        {
            try
            {
                _isBusy = true;
                Languages.Clear();
                var langs = await _translation.GetSupportedLanguagesAsync("ko");
                foreach (var l in langs)
                    Languages.Add(new LanguageItem { Code = l.Code, Display = l.DisplayName });
            }
            catch (Exception)
            {

            }
            finally
            {
                _isBusy = false;
                OnPropertyChanged("Languages");
            }
        }

        [RelayCommand]
        private async Task DetectAsync()
        {
            if (string.IsNullOrWhiteSpace(InputText)) return;
            try
            {
                IsBusy = true;
                var detected = await _translation.DetectLanguageAsync(InputText);
                SourceLang = detected.FirstOrDefault();
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task TranslateAsync()
        {
            if (string.IsNullOrWhiteSpace(InputText)) return;
            try
            {
                IsBusy = true;
                OutputText = await _translation.TranslateAsync(InputText, TargetLang, SourceLang);
            }
            catch (Exception)
            {

            }
            finally { IsBusy = false; }
        }
    }
}

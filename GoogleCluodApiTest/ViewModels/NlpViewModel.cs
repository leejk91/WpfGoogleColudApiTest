using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoogleCluodApiTest.Services;

namespace GoogleCluodApiTest.ViewModels
{
    public partial class NlpViewModel : ObservableObject
    {
        private readonly INlpService _nlp;

        [ObservableProperty] private string? inputText;
        [ObservableProperty] private string? sentimentSummary = "";
        public ObservableCollection<EntityResult> Entities { get; } = new();
        public ObservableCollection<CategoryResult> Categories { get; } = new();

        public NlpViewModel(INlpService nlp)
        {
            _nlp = nlp;
        }

        [RelayCommand]
        public async Task AnalyzeAsync()
        {
            var text = InputText?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            var ct = CancellationToken.None;

            var s = await _nlp.AnalyzeSentimentAsync(text, ct);
            SentimentSummary = $"감정 점수: {s.Score}, 감정 크기: {s.Magnitude:0.00}";

            Entities.Clear();
            foreach (var e in await _nlp.AnalyzeEntitiesAsync(text, ct))
                Entities.Add(e);

            Categories.Clear();
            foreach (var c in await _nlp.ClassifyTextAsync(text, ct))
                Categories.Add(c);
        }

    }
}

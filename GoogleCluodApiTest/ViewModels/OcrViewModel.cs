using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoogleCluodApiTest.Services;

namespace GoogleCluodApiTest.ViewModels
{
    public partial class OcrViewModel : ObservableObject
    {
        private readonly IVisionOcrService _ocr;
        [ObservableProperty] private string resultText = "추출된 텍스트";

        public OcrViewModel(IVisionOcrService ocr)
        {
            _ocr = ocr;
        }

        [RelayCommand]
        private async Task SelectImageAsync()
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp|모든 파일|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    ResultText = "텍스트 추출 중...";
                    var text = await _ocr.ExtractTextFromImageAsync(dlg.FileName);
                    ResultText = string.IsNullOrWhiteSpace(text) ? "(텍스트 없음)" : text;
                }
                catch (Exception ex)
                {
                    ResultText = $"오류: {ex.Message}";
                }
            }
        }
    }
}

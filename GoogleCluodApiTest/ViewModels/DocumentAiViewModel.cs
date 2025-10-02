using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Google.Rpc;
using GoogleCluodApiTest.Models;
using GoogleCluodApiTest.Services;
using static Google.Cloud.Vision.V1.ProductSearchResults.Types;

namespace GoogleCluodApiTest.ViewModels
{
    public partial class DocumentAiViewModel : ObservableObject
    {
        private readonly IDocumentAiService _docAi;
        private CancellationTokenSource? _cts;

        public DocumentAiViewModel(IDocumentAiService docAi)
        {
            _docAi = docAi;
            Status = "준비 완료";

        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ProcessCommand))]
        private string? selectedFile;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ProcessCommand))]
        private bool isBusy;

        [ObservableProperty]
        private DocAiResult? result;

        [ObservableProperty]
        private string status = string.Empty;


        private bool CanProcess() => !IsBusy && !string.IsNullOrWhiteSpace(SelectedFile);


        /// <summary>
        /// 파일 선택 (PDF/JPG/PNG/TIFF)
        /// </summary>
        [RelayCommand]
        private void SelectFile()
        {
            var dlg = new OpenFileDialog
            {
                Title = "문서/이미지 선택",
                Filter = "문서/이미지|*.pdf;*.png;*.jpg;*.jpeg;*.tif;*.tiff"
            };
            if (dlg.ShowDialog() == true)
            {
                SelectedFile = dlg.FileName;
                Status = $"선택됨: {Path.GetFileName(SelectedFile)}";
            }
        }

        /// <summary>
        /// 문서 처리 시작
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanProcess))]
        private async Task ProcessAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedFile)) return;

            IsBusy = true;
            Status = "처리 중...";
            Result = null;
            _cts = new CancellationTokenSource();

            try
            {
                var r = await _docAi.ProcessAsync(SelectedFile, _cts.Token);
                Result = r;
                Status = "완료";
            }
            catch (OperationCanceledException)
            {
                Status = "취소됨";
            }
            catch (Exception ex)
            {
                Status = "에러";
                Result = new DocAiResult
                {
                    FileName = Path.GetFileName(SelectedFile),
                    Text = string.Empty,
                    RawJson = ex.ToString(),
                    Summary = "처리 실패"
                };
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                IsBusy = false;
            }
        }

        /// <summary>
        /// 처리 취소
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            if (IsBusy && _cts is not null)
            {
                _cts.Cancel();
            }
        }

        /// <summary>
        /// Raw JSON 복사 (디버깅/공유용)
        /// </summary>
        [RelayCommand]
        private void CopyRawJson()
        {
            try
            {
                var json = Result?.RawJson;
                if (!string.IsNullOrEmpty(json))
                {
                    Clipboard.SetText(json);
                    Status = "Raw JSON 복사 완료";
                }
            }
            catch
            {
                // Clipboard 예외 등은 상태만 갱신
                Status = "클립보드 복사 실패";
            }
        }

        /// <summary>
        /// 결과 텍스트 저장 (선택)
        /// </summary>
        [RelayCommand]
        private void SaveText()
        {
            if (string.IsNullOrEmpty(Result?.Text))
            {
                Status = "저장할 텍스트가 없습니다.";
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title = "텍스트 저장",
                FileName = $"{Path.GetFileNameWithoutExtension(SelectedFile)}_text.txt",
                Filter = "텍스트 파일|*.txt"
            };
            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, Result!.Text);
                Status = $"저장됨: {Path.GetFileName(dlg.FileName)}";
            }
        }
    }
}

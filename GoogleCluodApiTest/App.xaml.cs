
using System.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using GoogleCluodApiTest.ViewModels;
using GoogleCluodApiTest.Services;
using GoogleCluodApiTest.Infrastructure;
using GoogleCluodApiTest.Models;

namespace GoogleCluodApiTest
{
    /// <summary>
    /// WPF 애플리케이션의 진입점입니다. DI 컨테이너(Host)를 구성하고 수명주기를 관리합니다.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 애플리케이션 전역 DI 컨테이너입니다. 윈도우, 뷰모델, 서비스 등을 해석합니다.
        /// </summary>
        public static IHost AppHost { get; private set; } = null!;

        /// <summary>
        /// 기본 생성자. Microsoft.Extensions.Hosting 을 이용해 서비스 컬렉션을 구성합니다.
        /// </summary>
        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    // Services 등록
                    services.AddSingleton<ITtsService, GoogleTtsService>();
                    services.AddSingleton<ISttService, GoogleSttService>();
                    services.AddSingleton<IVisionOcrService, GoogleVisionOcrService>();
                    services.AddSingleton<IWindowService, WindowService>();
                    services.AddSingleton<IAudioCaptureService, NAudioCaptureService>();
                    services.AddSingleton<ITranslationService, GoogleTranslationService>();
                    services.AddSingleton<INlpService, GoogleNlpService>();
                    services.AddSingleton<IDocumentAiService, GoogleDocumentAiService>();

                    // ViewModels 등록
                    services.AddSingleton<MainWindowViewModel>();
                    services.AddTransient<TtsViewModel>();
                    services.AddTransient<SttViewModel>();
                    services.AddTransient<OcrViewModel>();
                    services.AddTransient<TranslationViewModel>();
                    services.AddSingleton<NlpViewModel>();
                    services.AddSingleton<DocumentAiViewModel>();

                    // Views 등록
                    services.AddSingleton<Views.MainWindow>();
                    services.AddTransient<Views.TtsWindow>();
                    services.AddTransient<Views.SttWindow>();
                    services.AddTransient<Views.OcrWindow>();
                    services.AddTransient<Views.TranslationWindow>();
                    services.AddTransient<Views.NlpWindow>();
                    services.AddTransient<Views.DocumentAiWindow>();
                })
                .Build();
        }

        /// <summary>
        /// 애플리케이션 시작 시 호출됩니다. Host 를 시작하고 메인 윈도우를 표시합니다.
        /// </summary>
        /// <param name="e">시작 인자</param>
        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost.StartAsync();
            var main = AppHost.Services.GetRequiredService<Views.MainWindow>();
            main.DataContext = AppHost.Services.GetRequiredService<MainWindowViewModel>();
            main.Show();
            base.OnStartup(e);
        }

        /// <summary>
        /// 애플리케이션 종료 시 호출됩니다. Host 를 정상적으로 종료합니다.
        /// </summary>
        /// <param name="e">종료 인자</param>
        protected override async void OnExit(ExitEventArgs e)
        {
            await AppHost.StopAsync(TimeSpan.FromSeconds(2));
            base.OnExit(e);
        }
    }
}

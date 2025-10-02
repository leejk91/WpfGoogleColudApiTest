using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace GoogleCluodApiTest.Infrastructure
{
    /// <summary>
    /// 창 표시를 캡슐화하는 서비스 인터페이스입니다.
    /// </summary>
    public interface IWindowService
    {
        /// <summary>
        /// 지정한 윈도우 타입을 DI 로 해석해 표시합니다.
        /// </summary>
        /// <typeparam name="TWindow">표시할 윈도우 타입</typeparam>
        /// <param name="viewModel">선택적 DataContext</param>
        void ShowWindow<TWindow>(object? viewModel = null) where TWindow : Window;
    }

    /// <summary>
    /// <see cref="IWindowService"/> 구현체. 메인 윈도우를 Owner 로 설정하고 Show 합니다.
    /// </summary>
    public class WindowService : IWindowService
    {
        /// <inheritdoc />
        public void ShowWindow<TWindow>(object? viewModel = null) where TWindow : Window
        {
            var window = App.AppHost.Services.GetRequiredService<TWindow>();
            if (viewModel != null) window.DataContext = viewModel;
            window.Owner = Application.Current.MainWindow;
            window.Show();
        }
    }
}

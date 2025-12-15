using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using FufuLauncher.ViewModels;

namespace FufuLauncher.Views;

public sealed partial class MainPage
{
    public MainViewModel ViewModel { get; }
    public XamlUICommand OpenLinkCommand { get; }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.BackgroundVideoPlayer))
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    BackgroundVideo.SetMediaPlayer(ViewModel.BackgroundVideoPlayer);
                    Debug.WriteLine($"MainPage: MediaPlayer 已设置");
                });
            }
        };

        OpenLinkCommand = new XamlUICommand();
        OpenLinkCommand.ExecuteRequested += (sender, args) =>
        {
            if (args.Parameter is string url)
            {
                OpenLink(url);
            }
        };

        Unloaded += (s, e) => ViewModel.Cleanup();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private void BannerImage_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("轮播图加载成功");
    }

    private void BannerImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        Debug.WriteLine($"轮播图加载失败: {e.ErrorMessage}");
    }

    private async void OpenLink(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            try
            {
                var uri = new Uri(url);
                await Windows.System.Launcher.LaunchUriAsync(uri);
                Debug.WriteLine($"打开链接: {url}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"打开链接失败: {ex.Message}");
            }
        }
    }
}
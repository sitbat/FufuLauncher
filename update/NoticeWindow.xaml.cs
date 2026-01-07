using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WinRT.Interop;

namespace update
{
    public sealed partial class NoticeWindow : Window
    {
        public NoticeWindow(string url)
        {
            this.InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            SetWindowSize(900, 600);
            
            InitializeWebView(url);
        }

        private async void InitializeWebView(string url)
        {
            try
            {
                await NoticeWebView.EnsureCoreWebView2Async();
                NoticeWebView.Source = new Uri(url);
                NoticeWebView.NavigationCompleted += (s, e) => 
                {
                    LoadingRing.IsActive = false;
                    LoadingRing.Visibility = Visibility.Collapsed;
                };
            }
            catch (Exception)
            {
                LoadingRing.IsActive = false;
            }
        }

        private void SetWindowSize(int width, int height)
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = width, Height = height });
        }
    }
}
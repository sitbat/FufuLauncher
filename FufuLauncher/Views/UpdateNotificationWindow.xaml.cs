using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace FufuLauncher.Views;

public sealed partial class UpdateNotificationWindow : WindowEx
{
    // 修改构造函数，接受更新公告URL参数
    public UpdateNotificationWindow(string updateInfoUrl)
    {
        InitializeComponent();

        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(AppTitleBar);

        // 使用传入的动态URL加载公告
        UpdateWebView.Source = new Uri(updateInfoUrl);

        this.CenterOnScreen();
        this.SystemBackdrop = new DesktopAcrylicBackdrop();
        this.IsShownInSwitchers = true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
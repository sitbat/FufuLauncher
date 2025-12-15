using Microsoft.UI.Xaml.Media;

namespace FufuLauncher.Views;

public sealed partial class UpdateNotificationWindow
{
    public UpdateNotificationWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        UpdateWebView.Source = new Uri("https://philia093.cyou/Update.html");

        this.CenterOnScreen();

        SystemBackdrop = new DesktopAcrylicBackdrop();

        IsShownInSwitchers = true;
    }
}
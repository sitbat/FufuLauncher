using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace FufuLauncher.Views;

public sealed partial class UpdateNotificationWindow : WindowEx
{
    public UpdateNotificationWindow()
    {
        InitializeComponent();

        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(AppTitleBar);

        UpdateWebView.Source = new Uri("https://philia093.cyou/Update.html");

        this.CenterOnScreen();

        this.SystemBackdrop = new DesktopAcrylicBackdrop();

        this.IsShownInSwitchers = true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
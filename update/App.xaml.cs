using System;
using Microsoft.UI.Xaml;

namespace update;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private MainWindow? _window;

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        
        string[] cmdArgs = Environment.GetCommandLineArgs();

        if (cmdArgs.Length > 1)
        {
            string localVersion = cmdArgs[1];
            await _window.CheckUpdateSilentAsync(localVersion);
        }
        else
        {
            _window.Activate();
        }
    }
}
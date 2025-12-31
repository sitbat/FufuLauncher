using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using CommunityToolkit.Mvvm.Messaging;
using FufuLauncher.Contracts.Services;
using FufuLauncher.Helpers;
using FufuLauncher.Messages;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI.ViewManagement;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FufuLauncher.ViewModels;
using FufuLauncher.Services.Background;
using Windows.Media.Playback;
using FufuLauncher.Services;
using FufuLauncher.Models;
using CommunityToolkit.Mvvm.Input;

namespace FufuLauncher;

public sealed partial class MainWindow : WindowEx
{
    private Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue;
    private UISettings settings;
    private readonly IBackgroundRenderer _backgroundRenderer;
    private readonly ILocalSettingsService _localSettingsService;
    private MediaPlayer? _globalBackgroundPlayer;
    private double _frameBackgroundOpacity = 0.5;
    private bool _minimizeToTray;
    private bool _isExit;
    
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private enum TOKEN_INFORMATION_CLASS { TokenElevationType = 18 }
    
    private enum TOKEN_ELEVATION_TYPE
    {
        TokenElevationTypeDefault = 1,
        TokenElevationTypeFull = 2,
        TokenElevationTypeLimited = 3
    }

    public IRelayCommand ShowWindowCommand { get; }
    public IRelayCommand ExitApplicationCommand { get; }

    private Task RunOnUIThreadAsync(Action action)
    {
        var tcs = new TaskCompletionSource();
        dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                action();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    public MainWindow()
    {
        InitializeComponent();
        
        ShowWindowCommand = new RelayCommand(ShowWindow);
        ExitApplicationCommand = new RelayCommand(ExitApplication);

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        
        Title = "芙芙启动器";

        ExtendsContentIntoTitleBar = true;
        
        AppWindow.Closing += AppWindow_Closing;

        dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged;
        _backgroundRenderer = App.GetService<IBackgroundRenderer>();
        _localSettingsService = App.GetService<ILocalSettingsService>();

        WeakReferenceMessenger.Default.Register<AgreementAcceptedMessage>(this, (r, m) =>
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                try { ShowMainContent(); }
                catch (Exception ex) { Debug.WriteLine($"消息处理异常: {ex.Message}"); }
            });
        });

        if (Content is FrameworkElement rootElement)
        {
            rootElement.ActualThemeChanged += (s, e) => UpdateBackgroundOverlayTheme();
            UpdateBackgroundOverlayTheme();
        }
        
        Debug.WriteLine("[MainWindow] 开始注册消息监听...");
        WeakReferenceMessenger.Default.Register<ValueChangedMessage<WindowBackdropType>>(this, (r, m) =>
        {
            Debug.WriteLine($"[MainWindow] 收到消息! 准备切换背景为: {m.Value}");
            dispatcherQueue.TryEnqueue(() =>
            {
                ApplyBackdrop(m.Value);
            });
        });

        WeakReferenceMessenger.Default.Register<NotificationMessage>(this, (r, m) =>
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                try { ShowNotification(m); }
                catch (Exception ex) { Debug.WriteLine($"显示通知异常: {ex.Message}"); }
            });
        });

        WeakReferenceMessenger.Default.Register<BackgroundRefreshMessage>(this, (r, m) =>
        {
            dispatcherQueue.TryEnqueue(async () => { await LoadGlobalBackgroundAsync(); });
        });

        WeakReferenceMessenger.Default.Register<BackgroundOverlayOpacityChangedMessage>(this, (r, m) =>
        {
            dispatcherQueue.TryEnqueue(() => ApplyOverlayOpacity(m.Value));
        });
 
         WeakReferenceMessenger.Default.Register<FrameBackgroundOpacityChangedMessage>(this, (r, m) =>
         {
             dispatcherQueue.TryEnqueue(() => ApplyFrameBackgroundOpacity(m.Value));
         });

        WeakReferenceMessenger.Default.Register<MinimizeToTrayChangedMessage>(this, (r, m) =>
        {
            _minimizeToTray = m.Value;
        });
        WeakReferenceMessenger.Default.Register<BackgroundImageOpacityChangedMessage>(this, (r, m) =>
        {
            dispatcherQueue.TryEnqueue(() => ApplyBackgroundImageOpacity(m.Value));
        });
        
        dispatcherQueue.TryEnqueue(async () => await LoadBackgroundImageOpacityAsync());
        Activated += OnWindowActivated;
        
        dispatcherQueue.TryEnqueue(() => 
        {
            CheckAndWarnUacElevation();
        });

        Activated += OnWindowActivated;
        
    }
    
    private async void CheckAndWarnUacElevation()
    {
        if (IsUacElevatedWithConsent())
        {
            if (this.Content is FrameworkElement rootElement)
            {
                if (rootElement.XamlRoot == null)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    void OnLoaded(object sender, RoutedEventArgs e)
                    {
                        rootElement.Loaded -= OnLoaded;
                        tcs.TrySetResult(true);
                    }
                    rootElement.Loaded += OnLoaded;
                    if (rootElement.XamlRoot == null) await tcs.Task;
                }
                
                ContentDialog dialog = new ContentDialog();
            
                // 必须设置 XamlRoot
                dialog.XamlRoot = rootElement.XamlRoot;
            
                dialog.Title = "权限警告";
                dialog.Content = "程序正以管理员身份运行。\n这会导致无法使用文件选择器。\n建议直接正常启动程序，不要使用“以管理员身份运行”。";
                dialog.CloseButtonText = "我知道了";
                dialog.DefaultButton = ContentDialogButton.Close;

                try
                {
                    await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MainWindow] 弹窗显示失败: {ex.Message}");
                }
            }
        }
    }

    private bool IsUacElevatedWithConsent()
    {
        try
        {
            if (!IsRunningAsAdministrator()) return false;

            IntPtr tokenHandle = IntPtr.Zero;
            try
            {
                if (OpenProcessToken(GetCurrentProcess(), 0x0008, out tokenHandle))
                {
                    int size = Marshal.SizeOf(typeof(int));
                    IntPtr ptr = Marshal.AllocHGlobal(size);
                    try
                    {
                        if (GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType, ptr, (uint)size, out _))
                        {
                            var type = (TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(ptr);
                            return type == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull;
                        }
                    }
                    finally { Marshal.FreeHGlobal(ptr); }
                }
            }
            finally { if (tokenHandle != IntPtr.Zero) CloseHandle(tokenHandle); }
        }
        catch { }
        return false;
    }
    private async Task LoadBackgroundImageOpacityAsync()
    {
        try
        {
            var valueObj = await _localSettingsService.ReadSettingAsync("GlobalBackgroundImageOpacity");
            double opacity = 1.0;
            if (valueObj != null && double.TryParse(valueObj.ToString(), out var parsed))
            {
                opacity = parsed;
            }
            ApplyBackgroundImageOpacity(opacity);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainWindow] 加载背景图片透明度失败: {ex.Message}");
            ApplyBackgroundImageOpacity(1.0);
        }
    }

    private void ApplyBackgroundImageOpacity(double value)
    {
        var clamped = Math.Clamp(value, 0.0, 1.0);
        
        if (GlobalBackgroundImage != null)
        {
            GlobalBackgroundImage.Opacity = clamped;
        }
    
        if (GlobalBackgroundVideo != null)
        {
            GlobalBackgroundVideo.Opacity = clamped;
        }
    }
    
    
    private void ShowWindow()
    {
        this.Show();
        this.BringToFront();
    }

    private async void ExitApplication()
    {
        await SaveWindowSizeAsync();
        _isExit = true;
        TrayIcon.Dispose();
        this.Close();
    }

    private async void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (_isExit) return;

        args.Cancel = true;

        if (_minimizeToTray)
        {
            this.Hide();
        }
        else
        {
            await SaveWindowSizeAsync();
            _isExit = true;
            this.Close();
        }
    }

    private async Task SaveWindowSizeAsync()
    {
        try
        {
            var localSettings = App.GetService<ILocalSettingsService>();
            var saveEnabledObj = await localSettings.ReadSettingAsync("IsSaveWindowSizeEnabled");
            if (saveEnabledObj != null && Convert.ToBoolean(saveEnabledObj))
            {
                // 保存当前窗口大小
                await localSettings.SaveSettingAsync("SavedWindowWidth", this.Width);
                await localSettings.SaveSettingAsync("SavedWindowHeight", this.Height);
                Debug.WriteLine($"[MainWindow] 窗口大小已保存: {this.Width}x{this.Height}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainWindow] 保存窗口大小失败: {ex.Message}");
        }
    }

    private void UpdateBackgroundOverlayTheme()
    {
        if (Content is FrameworkElement rootElement)
        {
            var currentTheme = rootElement.ActualTheme;
            
            if (currentTheme == ElementTheme.Default)
            {
                currentTheme = Application.Current.RequestedTheme == ApplicationTheme.Dark 
                    ? ElementTheme.Dark 
                    : ElementTheme.Light;
            }
            
            if (currentTheme == ElementTheme.Dark)
            {
                GlobalBackgroundOverlay.Fill = new SolidColorBrush(Colors.Black);
            }
            else
            {
                GlobalBackgroundOverlay.Fill = new SolidColorBrush(Colors.White);
            }

            ApplyFrameBackgroundOpacity(_frameBackgroundOpacity);
         }
     }
    
    private async Task LoadAndApplyAcrylicSettingAsync()
    {
        try
        {
            var localSettingsService = App.GetService<ILocalSettingsService>();

            var backdropJson = await localSettingsService.ReadSettingAsync("WindowBackdrop");
            WindowBackdropType backdropType;

            if (backdropJson != null)
            {
                backdropType = (WindowBackdropType)Convert.ToInt32(backdropJson);
            }
            else
            {
                var acrylicEnabled = await localSettingsService.ReadSettingAsync("IsAcrylicEnabled");
                bool isEnabled = acrylicEnabled == null ? true : Convert.ToBoolean(acrylicEnabled);
                backdropType = isEnabled ? WindowBackdropType.Acrylic : WindowBackdropType.None;
            }

            ApplyBackdrop(backdropType);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载背景设置失败: {ex.Message}");
            ApplyBackdrop(WindowBackdropType.Acrylic);
        }
    }
    private async Task LoadGlobalBackgroundAsync()
    {
        try
        {
            var globalBgSetting = await _localSettingsService.ReadSettingAsync("UseGlobalBackground");
            bool useGlobalBg = globalBgSetting == null ? true : Convert.ToBoolean(globalBgSetting);
            if (!useGlobalBg)
            {
                await ClearGlobalBackgroundAsync();
                return;
            }

            var customPathObj = await _localSettingsService.ReadSettingAsync("CustomBackgroundPath");
            var customPath = customPathObj?.ToString();
            var hasCustom = !string.IsNullOrEmpty(customPath) && File.Exists(customPath);

            if (hasCustom)
            {
                var customResult = await _backgroundRenderer.GetCustomBackgroundAsync(customPath);
                await ApplyGlobalBackgroundAsync(customResult);
                return;
            }

            var enabledJson = await _localSettingsService.ReadSettingAsync(LocalSettingsService.IsBackgroundEnabledKey);
            bool isEnabled = enabledJson == null ? true : Convert.ToBoolean(enabledJson);
            if (!isEnabled)
            {
                await ClearGlobalBackgroundAsync();
                return;
            }

            var preferVideoSetting = await _localSettingsService.ReadSettingAsync("UserPreferVideoBackground");
            bool preferVideo = preferVideoSetting != null && Convert.ToBoolean(preferVideoSetting);

            var serverJson = await _localSettingsService.ReadSettingAsync(LocalSettingsService.BackgroundServerKey);
            int serverValue = serverJson != null ? Convert.ToInt32(serverJson) : 0;
            var server = (ServerType)serverValue;

            var result = await _backgroundRenderer.GetBackgroundAsync(server, preferVideo);
            await ApplyGlobalBackgroundAsync(result);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainWindow] 加载全局背景失败: {ex.Message}");
            await ClearGlobalBackgroundAsync();
        }
    }

    private Task ApplyGlobalBackgroundAsync(BackgroundRenderResult? result)
    {
        return RunOnUIThreadAsync(() =>
        {
            if (result == null)
            {
                GlobalBackgroundImage.Source = null;
                GlobalBackgroundImage.Visibility = Visibility.Collapsed;
                GlobalBackgroundVideo.Source = null;
                GlobalBackgroundVideo.Visibility = Visibility.Collapsed;
                _globalBackgroundPlayer?.Pause();
                _globalBackgroundPlayer = null;
                return;
            }

            if (result.IsVideo)
            {
                GlobalBackgroundImage.Source = null;
                GlobalBackgroundImage.Visibility = Visibility.Collapsed;

                _globalBackgroundPlayer?.Pause();
                _globalBackgroundPlayer = new MediaPlayer
                {
                    Source = result.VideoSource,
                    IsMuted = true,
                    IsLoopingEnabled = true,
                    AutoPlay = true
                };

                GlobalBackgroundVideo.SetMediaPlayer(_globalBackgroundPlayer);
                GlobalBackgroundVideo.Visibility = Visibility.Visible;
            }
            else
            {
                _globalBackgroundPlayer?.Pause();
                _globalBackgroundPlayer = null;
                GlobalBackgroundVideo.Source = null;
                GlobalBackgroundVideo.Visibility = Visibility.Collapsed;

                GlobalBackgroundImage.Source = result.ImageSource;
                GlobalBackgroundImage.Visibility = Visibility.Visible;
            }
        });
    }

    private Task ClearGlobalBackgroundAsync()
    {
        return RunOnUIThreadAsync(() =>
        {
            GlobalBackgroundImage.Source = null;
            GlobalBackgroundImage.Visibility = Visibility.Collapsed;
            GlobalBackgroundVideo.Source = null;
            GlobalBackgroundVideo.Visibility = Visibility.Collapsed;
            _globalBackgroundPlayer?.Pause();
            _globalBackgroundPlayer = null;
        });
    }
    private void ApplyBackdrop(WindowBackdropType type)
    {
        Debug.WriteLine($"[MainWindow] 执行 ApplyBackdrop: {type}");
        try
        {
            this.SystemBackdrop = null;
            Debug.WriteLine("[MainWindow] 旧背景已清除");

            switch (type)
            {
                case WindowBackdropType.Mica:
                    Debug.WriteLine("[MainWindow] 创建 MicaBackdrop...");
                    this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
                    break;
                case WindowBackdropType.Acrylic:
                    Debug.WriteLine("[MainWindow] 创建 DesktopAcrylicBackdrop...");
                    this.SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
                    break;
                default:
                    Debug.WriteLine("[MainWindow] 设置为纯色 (SystemBackdrop = null)");
                    break;
            }
            Debug.WriteLine("[MainWindow] 背景设置完成");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainWindow]设置背景报错: {ex}");
        }
    }

    public async Task InitializeWindowSizeAsync()
    {
        try
        {
            var localSettings = App.GetService<ILocalSettingsService>();
            var saveEnabledObj = await localSettings.ReadSettingAsync("IsSaveWindowSizeEnabled");
            
            if (saveEnabledObj != null && Convert.ToBoolean(saveEnabledObj))
            {
                var widthObj = await localSettings.ReadSettingAsync("SavedWindowWidth");
                var heightObj = await localSettings.ReadSettingAsync("SavedWindowHeight");

                if (widthObj != null && heightObj != null)
                {
                    if (double.TryParse(widthObj.ToString(), out double w) && double.TryParse(heightObj.ToString(), out double h))
                    {
                        Width = w;
                        Height = h;
                        Debug.WriteLine($"[MainWindow] 已恢复窗口大小: {w}x{h}");
                        return;
                    }
                }
            }

            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
                AppWindow.Id,
                Microsoft.UI.Windowing.DisplayAreaFallback.Primary
            );

            if (displayArea != null)
            {
                var screenWidth = displayArea.WorkArea.Width;
                var screenHeight = displayArea.WorkArea.Height;

                if (screenWidth >= 2560 && screenHeight >= 1440)
                {
                    Width = 1750;
                    Height = 1000;
                }
                else
                {
                    Width = 1360;
                    Height = 768;
                }
            }
            else
            {
                Width = 1360;
                Height = 768;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"设置初始窗口大小失败: {ex.Message}");
            Width = 1360;
            Height = 768;
        }
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        try
        {
            SetTitleBar(AppTitleBar);

            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico");
            if (File.Exists(iconPath))
            {
                TitleBarIcon.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(iconPath));
            }

            UpdateTitleBarWithAdminStatus();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"设置标题栏失败: {ex.Message}");
        }

        Activated -= OnWindowActivated;
    }

    private void UpdateTitleBarWithAdminStatus()
    {
        try
        {
            bool isAdmin = IsRunningAsAdministrator();
            var baseTitle = "芙芙启动器";
            TitleBarText.Text = isAdmin ? $"{baseTitle} [管理员]" : baseTitle;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"更新管理员标题失败: {ex.Message}");
        }
    }

    private bool IsRunningAsAdministrator()
    {
        try
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
        catch
        {
            return false;
        }
    }

    private async void NavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await LoadFrameBackgroundOpacityAsync();
             await LoadOverlayOpacityAsync();
             await LoadAndApplyAcrylicSettingAsync();
             await LoadGlobalBackgroundAsync();
             await LoadMinimizeToTraySettingAsync();
             await CheckUserAgreementAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"导航加载异常: {ex.Message}");
            ShowMainContent();
        }
    }

    private async Task LoadMinimizeToTraySettingAsync()
    {
        try
        {
            var value = await _localSettingsService.ReadSettingAsync("MinimizeToTray");
            _minimizeToTray = value != null && Convert.ToBoolean(value);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载托盘设置失败: {ex.Message}");
            _minimizeToTray = false;
        }
    }

    private async Task CheckUserAgreementAsync()
    {
        try
        {
            var localSettingsService = App.GetService<ILocalSettingsService>();
            var accepted = await localSettingsService.ReadSettingAsync("UserAgreementAccepted");

            bool isAccepted = false;
            if (accepted != null)
            {
                try { isAccepted = Convert.ToBoolean(accepted); }
                catch { isAccepted = false; }
            }

            if (!isAccepted) ShowAgreementPage();
            else ShowMainContent();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"协议检查失败: {ex.Message}");
            ShowMainContent();
        }
    }

    private void ShowAgreementPage()
    {
        try
        {
            AgreementFrame.Visibility = Visibility.Visible;
            NavigationView.Visibility = Visibility.Collapsed;
            AgreementFrame.Navigate(typeof(Views.AgreementPage));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"显示协议页异常: {ex.Message}");
        }
    }

    private void ShowMainContent()
    {
        try
        {
            AgreementFrame.Visibility = Visibility.Collapsed;
            NavigationView.Visibility = Visibility.Visible;
            NavigationView.SelectedItem = NavigationView.MenuItems[0];

            if (ContentFrame.CurrentSourcePageType != typeof(Views.MainPage))
                ContentFrame.Navigate(typeof(Views.MainPage));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"显示主内容异常: {ex.Message}");
        }
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem selectedItem)
        {
            var viewModelTag = selectedItem.Tag?.ToString();

            if (viewModelTag == "FufuLauncher.ViewModels.SettingsViewModel")
            {
                PlaySettingsIconRotationAnimation();
            }

            if (!string.IsNullOrEmpty(viewModelTag)) NavigateToPage(viewModelTag);
        }
    }

    private void PlaySettingsIconRotationAnimation()
    {
        try
        {
            if (SettingsIcon != null)
            {
                if (SettingsIcon.RenderTransform == null || SettingsIcon.RenderTransform is not RotateTransform)
                {
                    SettingsIcon.RenderTransform = new RotateTransform { CenterX = 14, CenterY = 14 };
                }
                var rotateAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new CircleEase { EasingMode = EasingMode.EaseInOut }
                };

                var storyboard = new Storyboard();
                Storyboard.SetTarget(rotateAnimation, SettingsIcon.RenderTransform);
                Storyboard.SetTargetProperty(rotateAnimation, "Angle");
                storyboard.Children.Add(rotateAnimation);
                storyboard.Begin();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"播放设置图标动画失败: {ex.Message}");
        }
    }

    private void NavigateToPage(string viewModelTag)
    {
        var pageType = viewModelTag switch
        {
            "FufuLauncher.ViewModels.MainViewModel" => typeof(Views.MainPage),
            "FufuLauncher.ViewModels.BlankViewModel" => typeof(Views.BlankPage),
            "FufuLauncher.ViewModels.SettingsViewModel" => typeof(Views.SettingsPage),
            "FufuLauncher.ViewModels.AccountViewModel" => typeof(Views.AccountPage),
            "FufuLauncher.ViewModels.OtherViewModel" => typeof(Views.OtherPage),
            "FufuLauncher.ViewModels.CalculatorViewModel" => typeof(Views.CalculatorPage),
            "FufuLauncher.ViewModels.ControlPanelModel" => typeof(Views.PanelPage),
            "FufuLauncher.ViewModels.PluginViewModel" => typeof(Views.PluginPage),
            _ => null
        };

        if (pageType != null && ContentFrame.CurrentSourcePageType != pageType) ContentFrame.Navigate(pageType);
    }

    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        dispatcherQueue.TryEnqueue(() => { TitleBarHelper.ApplySystemThemeToCaptionButtons(); });
    }

    private void ShowNotification(NotificationMessage message)
    {
        try
        {
            var notificationCard = CreateNotificationCard(message);
            NotificationPanel.Children.Add(notificationCard);

            AnimateNotification(notificationCard, 380, 0, 300);

            if (message.Duration > 0)
                SetupAutoDismiss(notificationCard, message.Duration);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"显示通知失败: {ex.Message}");
        }
    }

    private Grid CreateNotificationCard(NotificationMessage message)
    {
        var card = new Grid
        {
            Background = GetNotificationBrush(message.Type),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            Height = 80,
            Width = 360,
            Margin = new Thickness(0, 0, 0, 8),
            RenderTransform = new TranslateTransform { X = 380 }
        };

        var icon = new FontIcon
        {
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 16,
            Glyph = GetNotificationIcon(message.Type),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 0, 0),
            Foreground = new SolidColorBrush(Colors.White)
        };

        var contentPanel = new StackPanel { Spacing = 4, Margin = new Thickness(12, 0, 0, 0) };

        var titleText = new TextBlock
        {
            Text = message.Title,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.WrapWholeWords,
            Foreground = new SolidColorBrush(Colors.White)
        };
        contentPanel.Children.Add(titleText);

        var messageText = new TextBlock
        {
            Text = message.Message,
            FontSize = 12,
            Opacity = 0.9,
            TextWrapping = TextWrapping.WrapWholeWords,
            Foreground = new SolidColorBrush(Colors.White)
        };
        contentPanel.Children.Add(messageText);

        var closeButton = new Button
        {
            Content = new FontIcon { Glyph = "\uE711", FontSize = 12 },
            Background = new SolidColorBrush(Colors.Transparent),
            BorderBrush = new SolidColorBrush(Colors.Transparent),
            Width = 32,
            Height = 32,
            Padding = new Thickness(0),
            Margin = new Thickness(0, -4, -4, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Foreground = new SolidColorBrush(Colors.White),
            IsHitTestVisible = true,
        };

        closeButton.Click += (s, e) =>
        {
            Debug.WriteLine($"[通知系统] 用户点击关闭按钮");
            try
            {
                if (NotificationPanel.Children.Contains(card))
                {
                    NotificationPanel.Children.Remove(card);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[通知系统] 关闭异常: {ex.Message}");
            }
        };

        card.Children.Add(icon);
        card.Children.Add(contentPanel);
        card.Children.Add(closeButton);

        return card;
    }

    private void AnimateNotification(FrameworkElement element, double from, double to, int duration)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = new Duration(TimeSpan.FromMilliseconds(duration)),
            EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(animation, element.RenderTransform);
        Storyboard.SetTargetProperty(animation, "X");

        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private void SetupAutoDismiss(FrameworkElement card, int duration)
    {
        if (duration <= 0) return;

        var timer = dispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(duration);
        timer.Tick += (s, e) =>
        {
            try
            {
                if (NotificationPanel.Children.Contains(card))
                {
                    NotificationPanel.Children.Remove(card);
                }
            }
            catch { }
            timer.Stop();
        };
        timer.Start();
    }

    private Brush GetNotificationBrush(NotificationType type)
    {
        return type switch
        {
            NotificationType.Success => new SolidColorBrush(ColorHelper.FromArgb(255, 28, 175, 95)),
            NotificationType.Warning => new SolidColorBrush(ColorHelper.FromArgb(255, 255, 185, 0)),
            NotificationType.Error => new SolidColorBrush(ColorHelper.FromArgb(255, 232, 17, 35)),
            _ => new SolidColorBrush(ColorHelper.FromArgb(255, 0, 103, 192))
        };
    }

    private string GetNotificationIcon(NotificationType type)
    {
        return type switch
        {
            NotificationType.Success => "\uE930",
            NotificationType.Warning => "\uE7BA",
            NotificationType.Error => "\uE711",
            _ => "\uE946"
        };
    }

    private async Task LoadOverlayOpacityAsync()
    {
        try
        {
            var valueObj = await _localSettingsService.ReadSettingAsync("GlobalBackgroundOverlayOpacity");
            double opacity = 0.3;
            if (valueObj != null && double.TryParse(valueObj.ToString(), out var parsed))
            {
                opacity = parsed;
            }

            ApplyOverlayOpacity(opacity);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainWindow] 加载背景遮罩透明度失败: {ex.Message}");
            ApplyOverlayOpacity(0.3);
        }
    }

    private async Task LoadFrameBackgroundOpacityAsync()
    {
        try
        {
            var valueObj = await _localSettingsService.ReadSettingAsync("ContentFrameBackgroundOpacity");
            double opacity = 0.5;
            if (valueObj != null && double.TryParse(valueObj.ToString(), out var parsed))
            {
                opacity = parsed;
            }

            ApplyFrameBackgroundOpacity(opacity);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainWindow] 加载 Frame 背景透明度失败: {ex.Message}");
            ApplyFrameBackgroundOpacity(0.5);
        }
    }

    private void ApplyOverlayOpacity(double value)
    {
        var clamped = Math.Clamp(value, 0.0, 1.0);
        GlobalBackgroundOverlay.Opacity = clamped;
    }

    private void ApplyFrameBackgroundOpacity(double value)
    {
        // 1. 限制输入范围
        _frameBackgroundOpacity = Math.Clamp(value, 0.0, 1.0);

        // 2. 确保 ContentFrame 可用
        if (ContentFrame == null) return;

        // 3. 获取或创建 SolidColorBrush
        SolidColorBrush brush;
        if (ContentFrame.Background is SolidColorBrush existingBrush)
        {
            brush = existingBrush;
        }
        else
        {
            brush = new SolidColorBrush();
            ContentFrame.Background = brush;
        }

        // 4. 根据当前主题确定基色 (黑或白)
        var theme = ElementTheme.Default;
        if (Content is FrameworkElement root)
        {
            theme = root.ActualTheme;
            if (theme == ElementTheme.Default)
            {
                theme = Application.Current.RequestedTheme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
            }
        }

        var baseColor = theme == ElementTheme.Dark ? Colors.Black : Colors.White;

        // 5. 【关键修改】直接映射 0.0-1.0 到 0-255
        // 这样当 _frameBackgroundOpacity 为 0 时，A 也为 0 (完全透明)
        baseColor.A = (byte)(_frameBackgroundOpacity * 255);

        // 6. 应用颜色
        brush.Color = baseColor;
    }
}
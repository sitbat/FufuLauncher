using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;
using WinRT.Interop;

namespace update;

public sealed partial class MainWindow : Window
{
    private readonly HttpClient _httpClient = new HttpClient();
    private const string UpdateApiUrl = "http://philia093.xyz/update";
    private const string NoticeApiUrl = "http://philia093.xyz/api/notice";
    private readonly string _targetDir;
    private readonly string _launcherExePath;
    private readonly string _noticeLogPath;
    private UpdateInfo? _currentUpdateInfo;
    private bool _isUpdateComplete = false;
    private long _lastUiUpdateTick = 0;
    private NoticeWindow? _noticeWindow;
    private bool _isMainUiVisible = false;
    public MainWindow()
    {
        InitializeComponent();
        
        string currentBaseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        DirectoryInfo? parentDirInfo = Directory.GetParent(currentBaseDir);
        _targetDir = parentDirInfo != null ? parentDirInfo.FullName : currentBaseDir;
        _launcherExePath = Path.Combine(_targetDir, "FufuLauncher.exe");
        _noticeLogPath = Path.Combine(_targetDir, "notice_read.cache");
        ExtendsContentIntoTitleBar = true;
        SetWindowSize(760, 454);
        SetWindowIcon("Assets/WindowIcon.ico"); 

        this.Activated += MainWindow_Activated;
    }

    private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        this.Activated -= MainWindow_Activated;
        _isMainUiVisible = true;
        await CheckForUpdates();
    }

    public async Task CheckUpdateSilentAsync(string localVersionStr)
    {
        try
        {
            this.Activated -= MainWindow_Activated;
            _isMainUiVisible = false;

            NoticeInfo? notice = await FetchNoticeAsync();
            
            bool allowUpdate = true;
            bool isNoticeShowing = false;

            if (notice != null)
            {
                if (notice.enableClient && !string.IsNullOrEmpty(notice.message))
                {
                    if (ShouldShowNotice(notice.message))
                    {
                        isNoticeShowing = true;
                        MarkNoticeAsRead(notice.message);
                        DispatcherQueue.TryEnqueue(() => ShowNoticeWindow(notice.message));
                    }
                }
                allowUpdate = notice.enableUpdate;
            }

            if (!allowUpdate)
            {
                if (!isNoticeShowing) Application.Current.Exit();
                return;
            }

            string jsonString = await _httpClient.GetStringAsync(UpdateApiUrl);
            
            _currentUpdateInfo = JsonSerializer.Deserialize(jsonString, AppJsonContext.Default.UpdateInfo);

            if (_currentUpdateInfo != null)
            {
                Version localVersion;
                Version serverVersion;

                if (!Version.TryParse(localVersionStr, out localVersion)) localVersion = new Version(0, 0, 0);
                if (!Version.TryParse(_currentUpdateInfo.version, out serverVersion)) serverVersion = new Version(0, 0, 0);

                if (serverVersion > localVersion)
                {
                    PreprareUiForUpdate(_currentUpdateInfo);
                    this.Activate(); 
                    _isMainUiVisible = true;
                }
                else
                {
                    if (!isNoticeShowing) Application.Current.Exit();
                }
            }
            else
            {
                if (!isNoticeShowing) Application.Current.Exit();
            }
        }
        catch (Exception)
        {
             if (_noticeWindow == null) Application.Current.Exit();
        }
    }

    private async Task CheckForUpdates()
    {
        try
        {
            VersionTitleText.Text = "正在连接服务器...";
            
            NoticeInfo? notice = await FetchNoticeAsync();
            bool allowUpdate = true;

            if (notice != null)
            {
                if (notice.enableClient && !string.IsNullOrEmpty(notice.message))
                {
                    if (ShouldShowNotice(notice.message))
                    {
                        MarkNoticeAsRead(notice.message);
                        ShowNoticeWindow(notice.message);
                    }
                }
                allowUpdate = notice.enableUpdate;
            }

            if (!allowUpdate)
            {
                SetNoUpdateState("服务器已暂时关闭更新通道，或当前版本已是最新。");
                return;
            }

            VersionTitleText.Text = "正在获取版本信息...";
            string jsonString = await _httpClient.GetStringAsync(UpdateApiUrl);
            _currentUpdateInfo = JsonSerializer.Deserialize(jsonString, AppJsonContext.Default.UpdateInfo);

            if (_currentUpdateInfo != null)
            {
                PreprareUiForUpdate(_currentUpdateInfo);
            }
        }
        catch (Exception ex)
        {
            ShowError($"检查更新失败: {ex.Message}");
        }
    }

    private bool ShouldShowNotice(string newUrl)
    {
        try
        {
            if (!File.Exists(_noticeLogPath)) return true;
            string lastUrl = File.ReadAllText(_noticeLogPath).Trim();
            return !string.Equals(lastUrl, newUrl, StringComparison.OrdinalIgnoreCase);
        }
        catch { return true; }
    }

    private void MarkNoticeAsRead(string url)
    {
        try { File.WriteAllText(_noticeLogPath, url); } catch { }
    }

    private async Task<NoticeInfo?> FetchNoticeAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            string jsonString = await _httpClient.GetStringAsync(NoticeApiUrl, cts.Token);
            
            return JsonSerializer.Deserialize(jsonString, AppJsonContext.Default.NoticeInfo);
        }
        catch { return null; }
    }

    private void ShowNoticeWindow(string url)
    {
        if (_noticeWindow == null)
        {
            _noticeWindow = new NoticeWindow(url);
            _noticeWindow.Closed += (s, e) => 
            { 
                _noticeWindow = null;
                if (!_isMainUiVisible) Application.Current.Exit();
            };
            _noticeWindow.Activate();
        }
        else
        {
            _noticeWindow.Activate();
        }
    }

    private void SetNoUpdateState(string message)
    {
        LoadingRing.IsActive = false;
        LoadingRing.Visibility = Visibility.Collapsed;
        VersionTitleText.Text = "当前无需更新";
        ReleaseNoteText.Text = message;
        UpdateInfoPanel.Visibility = Visibility.Visible;
        ActionButton.IsEnabled = true;
        ActionButton.Content = "启动";
        _isUpdateComplete = true;
    }

    private void SetWindowIcon(string iconPathRelative)
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(this);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
        string fullPath = Path.Combine(AppContext.BaseDirectory, iconPathRelative);
        if (File.Exists(fullPath)) appWindow.SetIcon(fullPath);
    }

    private void PreprareUiForUpdate(UpdateInfo info)
    {
        LoadingRing.IsActive = false;
        LoadingRing.Visibility = Visibility.Collapsed;
        VersionTitleText.Text = $"发现新版本 v{info.version}";
        ReleaseNoteText.Text = info.description;
        UpdateInfoPanel.Visibility = Visibility.Visible;
        ActionButton.IsEnabled = true;
        ActionButton.Content = "立即更新";
    }

    private async void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isUpdateComplete) LaunchApplication();
        else if (_currentUpdateInfo != null) await PerformUpdate(_currentUpdateInfo);
    }

    private async Task PerformUpdate(UpdateInfo info)
    {
        ActionButton.IsEnabled = false;
        UpdateInfoPanel.Visibility = Visibility.Collapsed;
        ProgressPanel.Visibility = Visibility.Visible;
        VersionTitleText.Text = "正在更新 FufuLauncher...";
        CloseButton.IsEnabled = false; 

        string tempFile = Path.GetTempFileName();

        try
        {
            CurrentStepText.Text = "正在连接服务器...";
            await DownloadFileAsync(info.downloadUrl, tempFile);

            CurrentStepText.Text = "正在准备解压...";
            MainProgressBar.IsIndeterminate = false; 
            
            await ExtractAndReplaceAsync(tempFile, _targetDir);

            if (File.Exists(tempFile)) File.Delete(tempFile);

            _isUpdateComplete = true;
            ProgressPanel.Visibility = Visibility.Collapsed;
            
            VersionTitleText.Text = "更新成功！";
            ReleaseNoteText.Text = "最新版本已成功安装。点击“启动”开始使用。";
            UpdateInfoPanel.Visibility = Visibility.Visible;

            ActionButton.Content = "启动";
            ActionButton.IsEnabled = true;
            CloseButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
            ActionButton.IsEnabled = true;
            ActionButton.Content = "重试";
            CloseButton.IsEnabled = true;
        }
    }

    private async Task DownloadFileAsync(string url, string destinationPath)
    {
        using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
        {
            response.EnsureSuccessStatusCode();
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;

            using (var contentStream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                var buffer = new byte[8192];
                long totalRead = 0;
                int read;

                while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    totalRead += read;

                    if (totalBytes != -1)
                    {
                        long currentTick = Environment.TickCount64;
                        if (currentTick - _lastUiUpdateTick > 100) 
                        {
                            _lastUiUpdateTick = currentTick;
                            var progress = (double)totalRead / totalBytes * 100;
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                MainProgressBar.Value = progress;
                                ProgressDetailText.Text = $"{FormatBytes(totalRead)} / {FormatBytes(totalBytes)}";
                                CurrentStepText.Text = "正在下载更新包...";
                            });
                        }
                    }
                }
            }
        }
    }

    private async Task ExtractAndReplaceAsync(string archivePath, string destinationDir)
    {
        await Task.Run(() =>
        {
            if (!Directory.Exists(destinationDir)) Directory.CreateDirectory(destinationDir);

            List<string> allKeys;
            using (var archive = SevenZipArchive.Open(archivePath))
            {
                allKeys = archive.Entries.Where(e => !e.IsDirectory).Select(e => e.Key).ToList();
            }

            int totalFiles = allKeys.Count;
            if (totalFiles == 0) return;

            int maxThreads = Math.Min(4, Math.Max(2, Environment.ProcessorCount / 2));
            var chunks = allKeys.Select((v, i) => new { v, i }).GroupBy(x => x.i % maxThreads).Select(g => g.Select(x => x.v).ToHashSet()).ToList();
            int processedCount = 0;
            
            Parallel.ForEach(chunks, new ParallelOptions { MaxDegreeOfParallelism = maxThreads }, (chunk) =>
            {
                using (var archive = SevenZipArchive.Open(archivePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.IsDirectory && chunk.Contains(entry.Key))
                        {
                            string extractPath = Path.Combine(destinationDir, entry.Key);
                            string? parentDir = Path.GetDirectoryName(extractPath);
                            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);
                            try { entry.WriteToFile(extractPath, new ExtractionOptions { ExtractFullPath = true, Overwrite = true }); } catch (IOException) { }

                            int current = Interlocked.Increment(ref processedCount);
                            long now = Environment.TickCount64;
                            if (now - Interlocked.Read(ref _lastUiUpdateTick) > 100)
                            {
                                Interlocked.Exchange(ref _lastUiUpdateTick, now);
                                DispatcherQueue.TryEnqueue(() => {
                                    CurrentStepText.Text = $"正在安装... {current}/{totalFiles}";
                                    ProgressDetailText.Text = $"{((double)current / totalFiles * 100):0}%";
                                    MainProgressBar.Value = ((double)current / totalFiles) * 100;
                                });
                            }
                        }
                    }
                }
            });
            DispatcherQueue.TryEnqueue(() => {
                CurrentStepText.Text = "完成";
                ProgressDetailText.Text = $"{totalFiles} / {totalFiles}";
                MainProgressBar.Value = 100;
            });
        });
    }

    private string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = (decimal)bytes;
        while (Math.Round(number / 1024) >= 1) { number /= 1024; counter++; }
        return string.Format("{0:n1} {1}", number, suffixes[counter]);
    }

    private void LaunchApplication()
    {
        try
        {
            if (File.Exists(_launcherExePath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = _launcherExePath, UseShellExecute = true, WorkingDirectory = _targetDir });
                Application.Current.Exit();
            }
            else ShowError($"未找到文件: {_launcherExePath}");
        }
        catch (Exception ex) { ShowError("启动失败: " + ex.Message); }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();

    private void ShowError(string message)
    {
        DispatcherQueue.TryEnqueue(() => {
            LoadingRing.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Collapsed;
            UpdateInfoPanel.Visibility = Visibility.Collapsed;
            ErrorText.Visibility = Visibility.Visible;
            ErrorText.Text = message;
            VersionTitleText.Text = "出错了";
            CloseButton.IsEnabled = true;
            ActionButton.IsEnabled = true;
            ActionButton.Content = "重试";
            _isMainUiVisible = true; 
            this.Activate(); 
        });
    }

    private void SetWindowSize(int width, int height)
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(this);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new SizeInt32 { Width = width, Height = height });
        if (appWindow.Presenter is OverlappedPresenter presenter) { presenter.IsMaximizable = false; presenter.IsResizable = false; }
    }
}
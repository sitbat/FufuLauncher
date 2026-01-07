using System.Diagnostics;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FufuLauncher.Models;
using FufuLauncher.Views;

namespace FufuLauncher.ViewModels;

public partial class ControlPanelModel : ObservableObject
{
    private const string TargetProcessName = "yuanshen";
    private const string TargetProcessNameAlt = "GenshinImpact";
    
<<<<<<< HEAD
    private const string HotSwitchDllName = "input_hot_switch.dll";
    private const string HotSwitchDllDisabledName = "input_hot_switch.dll.disabled";
    private readonly string _baseDirectory;
    
    private UdpClient? _udpClient;
    private IPEndPoint? _remoteEndPoint;
=======
>>>>>>> e479bcb4a0327b3eb023564baa2b34cd444bd279
    private readonly string _configPath;
    private bool _isLoaded;
    private CancellationTokenSource _cancellationTokenSource;

    private DateTime? _gameStartTime;
    private readonly Dictionary<string, long> _playTimeData;

    [ObservableProperty]
    private WeeklyPlayTimeStats _weeklyStats = new();

    [ObservableProperty]
    private bool _isGameRunning;

<<<<<<< HEAD
    [ObservableProperty]
    private string _connectionStatus = "请启动游戏";
    
    [ObservableProperty]
    private bool _enableFpsFakeReporting;
    
    [ObservableProperty]
    private bool _enableFovCutsceneFix;
    
    [ObservableProperty]
    private bool _disableInputHotSwitch;
    
    [RelayCommand]
    private void OpenDiagnosticsWindow()
    {
        var window = new DiagnosticsWindow();
        window.Activate();
    }

    partial void OnDisableInputHotSwitchChanged(bool value)
    {
        ToggleInputHotSwitchDll(value);
    }
    
    partial void OnEnableFovCutsceneFixChanged(bool value)
    {
        SendCommand(value ? "enable_fov_cutscene_fix" : "disable_fov_cutscene_fix");
        SaveConfig();
    }

    partial void OnEnableFpsFakeReportingChanged(bool value)
    {
        SendCommand(value ? "enable_fps_fake_reporting" : "disable_fps_fake_reporting");
        SaveConfig();
    }

=======
>>>>>>> e479bcb4a0327b3eb023564baa2b34cd444bd279
    public ControlPanelModel()
    {
        _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "fufu", "FufuConfig.cfg");
        _cancellationTokenSource = new CancellationTokenSource();
        _playTimeData = new Dictionary<string, long>();
        _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

<<<<<<< HEAD
        try
        {
            _udpClient = new UdpClient();
            _udpClient.Client.ReceiveTimeout = 3000;
            _remoteEndPoint = new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UDP] Init Error: {ex.Message}");
        }
        
        string normalPath = Path.Combine(_baseDirectory, HotSwitchDllName);
        string disabledPath = Path.Combine(_baseDirectory, HotSwitchDllDisabledName);
        
        if (File.Exists(disabledPath) && !File.Exists(normalPath))
        {
            _disableInputHotSwitch = true;
        }
        else
        {
            _disableInputHotSwitch = false;
        }

        _ = StartConnectionLoopAsync(_cancellationTokenSource.Token);
=======
>>>>>>> e479bcb4a0327b3eb023564baa2b34cd444bd279
        LoadConfig();
        
        _ = StartGameMonitoringLoopAsync(_cancellationTokenSource.Token);
    }
    
<<<<<<< HEAD
    private void ToggleInputHotSwitchDll(bool disable)
    {
        string normalPath = Path.Combine(_baseDirectory, HotSwitchDllName);
        string disabledPath = Path.Combine(_baseDirectory, HotSwitchDllDisabledName);

        try
        {
            if (disable)
            {
                if (File.Exists(normalPath))
                {
                    if (File.Exists(disabledPath)) File.Delete(disabledPath);
                    File.Move(normalPath, disabledPath);
                }
            }
            else
            {
                if (File.Exists(disabledPath))
                {
                    if (File.Exists(normalPath)) File.Delete(normalPath);
                    File.Move(disabledPath, normalPath);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FileOp] Error toggling dll: {ex.Message}");
        }
    }
    
    [ObservableProperty]
    private bool _enableFpsOverride;

    partial void OnEnableFpsOverrideChanged(bool value)
    {
        SendCommand(value ? "enable_fps_override" : "disable_fps_override");
        SaveConfig();
    }

    [ObservableProperty]
    private int _targetFps = 60;

    partial void OnTargetFpsChanged(int value)
    {
        SendCommand($"set_fps {value}");
        SaveConfig();
    }

    [ObservableProperty]
    private bool _enableFovOverride;

    partial void OnEnableFovOverrideChanged(bool value)
    {
        SendCommand(value ? "enable_fov_override" : "disable_fov_override");
        SaveConfig();
    }

    [ObservableProperty]
    private float _targetFov = 45.0f;

    partial void OnTargetFovChanged(float value)
    {
        SendCommand($"set_fov {value}");
        SaveConfig();
    }
    
    [ObservableProperty]
    private bool _enableFogOverride;

    partial void OnEnableFogOverrideChanged(bool value)
    {
        SendCommand(value ? "enable_display_fog_override" : "disable_display_fog_override");
        SaveConfig();
    }

    [ObservableProperty]
    private bool _enablePerspectiveOverride;

    partial void OnEnablePerspectiveOverrideChanged(bool value)
    {
        SendCommand(value ? "enable_Perspective_override" : "disable_Perspective_override");
        SaveConfig();
    }
    
    [ObservableProperty]
    private bool _removeQuestBanner = true;

    partial void OnRemoveQuestBannerChanged(bool value)
    {
        SaveConfig();
    }

    [ObservableProperty]
    private bool _removeDamageText = true;

    partial void OnRemoveDamageTextChanged(bool value)
    {
        SaveConfig();
    }

    [ObservableProperty]
    private bool _enableTouchScreenMode;

    partial void OnEnableTouchScreenModeChanged(bool value)
    {
        SaveConfig();
    }

    [ObservableProperty]
    private bool _disableEventCameraMove = true;

    partial void OnDisableEventCameraMoveChanged(bool value)
    {
        SaveConfig();
    }

    [ObservableProperty]
    private bool _removeTeamProgressLimit = true;

    partial void OnRemoveTeamProgressLimitChanged(bool value)
    {
        SaveConfig();
    }

    [ObservableProperty]
    private bool _enableRedirectCombineEntry;

    partial void OnEnableRedirectCombineEntryChanged(bool value)
    {
        SaveConfig();
    }
    
    [ObservableProperty]
    private bool _resinListItemId000106Allowed;

    partial void OnResinListItemId000106AllowedChanged(bool value)
    {
        SaveConfig();
    }

    [ObservableProperty]
    private bool _resinListItemId000201Allowed;

    partial void OnResinListItemId000201AllowedChanged(bool value)
    {
        SaveConfig();
    }

    [ObservableProperty]
    private bool _resinListItemId107009Allowed;

    partial void OnResinListItemId107009AllowedChanged(bool value)
    {
        SaveConfig();
    }

    [ObservableProperty]
    private bool _resinListItemId107012Allowed;

    partial void OnResinListItemId107012AllowedChanged(bool value)
    {
        SaveConfig();
    }

    [ObservableProperty]
    private bool _resinListItemId220007Allowed;

    partial void OnResinListItemId220007AllowedChanged(bool value)
    {
        SaveConfig();
    }

    private async void SendCommand(string command)
    {
        if (!_isConnected) return;
        await SendAndReceiveAsync(command);
    }

    private async Task<bool> SendAndReceiveAsync(string command, CancellationToken token = default)
    {
        if (_udpClient == null || _remoteEndPoint == null) return false;

        try
        {
            await _socketLock.WaitAsync(token);
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(command);
                await _udpClient.SendAsync(data, data.Length, _remoteEndPoint);
                Debug.WriteLine($"[UDP] Sent: {command}");

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(3000);

                var result = await _udpClient.ReceiveAsync(cts.Token);
                string response = Encoding.UTF8.GetString(result.Buffer);
                Debug.WriteLine($"[UDP] Received: {response}");
                return response == "OK" || response == "alive";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UDP] Error: {ex.Message}");
                return false;
            }
            finally
            {
                _socketLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

=======
>>>>>>> e479bcb4a0327b3eb023564baa2b34cd444bd279
    private (string Name, int Id)? FindTargetProcess()
    {
        var processes = Process.GetProcessesByName(TargetProcessName);
        if (processes.Length > 0) return (processes[0].ProcessName, processes[0].Id);

        processes = Process.GetProcessesByName(TargetProcessNameAlt);
        if (processes.Length > 0) return (processes[0].ProcessName, processes[0].Id);

        return null;
    }

    private async Task StartGameMonitoringLoopAsync(CancellationToken token)
    {
        bool wasRunning = false;

        while (!token.IsCancellationRequested)
        {
            try
            {
                var processInfo = FindTargetProcess();
                bool isRunning = processInfo.HasValue;

                if (isRunning && !wasRunning)
                {
                    _gameStartTime = DateTime.Now;
                    App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        IsGameRunning = true;
                    });
                    Debug.WriteLine($"[GameMonitor] Game started at {_gameStartTime}");
                }
                else if (!isRunning && wasRunning)
                {
                    if (_gameStartTime.HasValue)
                    {
                        var playTime = DateTime.Now - _gameStartTime.Value;
                        UpdateTodayPlayTime(playTime);
                        Debug.WriteLine($"[GameMonitor] Game stopped. Play time: {playTime}");
                    }

                    App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        IsGameRunning = false;
                    });
                    _gameStartTime = null;
                }

                wasRunning = isRunning;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GameMonitor] Error: {ex.Message}");
            }

            await Task.Delay(5000, token);
        }
    }

    private void UpdateTodayPlayTime(TimeSpan additionalTime)
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");

        if (!_playTimeData.ContainsKey(today))
        {
            _playTimeData[today] = 0;
        }

        _playTimeData[today] += (long)additionalTime.TotalSeconds;

        var thirtyDaysAgo = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
        var keysToRemove = _playTimeData.Keys.Where(k => string.Compare(k, thirtyDaysAgo) < 0).ToList();
        foreach (var key in keysToRemove)
        {
            _playTimeData.Remove(key);
        }

        CalculateWeeklyStats();
        SaveConfig();
    }

    private void CalculateWeeklyStats()
    {
        var stats = new WeeklyPlayTimeStats();
        var today = DateTime.Now.Date;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

        for (int i = 0; i < 7; i++)
        {
            var date = startOfWeek.AddDays(i);
            var dateKey = date.ToString("yyyy-MM-dd");

            if (_playTimeData.TryGetValue(dateKey, out var seconds) && seconds > 0)
            {
                stats.DailyRecords.Add(new GamePlayTimeRecord
                {
                    Date = date,
                    PlayTimeSeconds = seconds
                });
            }
        }

        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            WeeklyStats = stats;
        });
    }

    private void LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                var config = JsonSerializer.Deserialize<ControlPanelConfig>(json);
                if (config != null)
                {
                    _isLoaded = false;
                    
                    if (config.GamePlayTimeData != null)
                    {
                        foreach (var kvp in config.GamePlayTimeData)
                        {
                            _playTimeData[kvp.Key] = kvp.Value;
                        }
                    }

                    _isLoaded = true;
                    CalculateWeeklyStats();
                }
            }
            else
            {
                _isLoaded = true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading config: {ex.Message}");
            _isLoaded = true;
        }
    }

    private async void SaveConfig()
    {
        if (!_isLoaded) return;
        try
        {
            var config = new ControlPanelConfig
            {
                GamePlayTimeData = _playTimeData,
                LastPlayDate = DateTime.Now.ToString("yyyy-MM-dd")
            };

            var dir = Path.GetDirectoryName(_configPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(config);
            await File.WriteAllTextAsync(_configPath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving config: {ex.Message}");
        }
    }
}

public class ControlPanelConfig
{
    public Dictionary<string, long> GamePlayTimeData { get; set; }
    public string LastPlayDate { get; set; }

}
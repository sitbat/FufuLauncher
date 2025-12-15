using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage.Pickers;
using Windows.System;
using FufuLauncher.Contracts.Services;
using FufuLauncher.Services;
using WinRT.Interop;

namespace FufuLauncher.ViewModels
{
    public partial class OtherViewModel : ObservableObject
    {
        private readonly ILocalSettingsService _localSettingsService;
        private readonly IAutoClickerService _autoClickerService;
        private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;

        [ObservableProperty] private bool _isAdditionalProgramEnabled;
        [ObservableProperty] private string _additionalProgramPath = string.Empty;
        [ObservableProperty] private string _statusMessage = string.Empty;

        [ObservableProperty] private bool _isAutoClickerEnabled;
        [ObservableProperty] private string _triggerKey = "F8";
        [ObservableProperty] private string _clickKey = "F";
        [ObservableProperty] private bool _isRecordingTriggerKey;
        [ObservableProperty] private bool _isRecordingClickKey;

        public IAsyncRelayCommand BrowseProgramCommand { get; }
        public IAsyncRelayCommand SaveSettingsCommand { get; }
        public IRelayCommand RecordTriggerKeyCommand { get; }
        public IRelayCommand RecordClickKeyCommand { get; }

        public OtherViewModel(ILocalSettingsService localSettingsService, IAutoClickerService autoClickerService)
        {
            _localSettingsService = localSettingsService;
            _autoClickerService = autoClickerService;
            _dispatcherQueue = App.MainWindow.DispatcherQueue;
            
            BrowseProgramCommand = new AsyncRelayCommand(BrowseProgramAsync);
            SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
            RecordTriggerKeyCommand = new RelayCommand(StartRecordingTriggerKey);
            RecordClickKeyCommand = new RelayCommand(StartRecordingClickKey);
            
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                Debug.WriteLine("[OtherViewModel] 开始加载配置...");
                
                var enabled = _localSettingsService.ReadSettingAsync("AdditionalProgramEnabled").Result;
                var path = _localSettingsService.ReadSettingAsync("AdditionalProgramPath").Result;
                IsAdditionalProgramEnabled = enabled != null && Convert.ToBoolean(enabled);
                AdditionalProgramPath = path?.ToString()?.Trim('"') ?? string.Empty;
                
                var autoClickerEnabled = _localSettingsService.ReadSettingAsync("AutoClickerEnabled").Result;
                var triggerKey = _localSettingsService.ReadSettingAsync("AutoClickerTriggerKey").Result;
                var clickKey = _localSettingsService.ReadSettingAsync("AutoClickerClickKey").Result;
                
                Debug.WriteLine($"[OtherViewModel] 原始配置 - Enabled: {autoClickerEnabled}, TriggerKey: {triggerKey}, ClickKey: {clickKey}");
                
                IsAutoClickerEnabled = autoClickerEnabled != null && Convert.ToBoolean(autoClickerEnabled);
                _autoClickerService.IsEnabled = IsAutoClickerEnabled;

                TriggerKey = triggerKey?.ToString()?.Trim('"') ?? "F8";
                ClickKey = clickKey?.ToString()?.Trim('"') ?? "F";
                
                if (Enum.TryParse<VirtualKey>(TriggerKey, out var tk)) 
                {
                    _autoClickerService.TriggerKey = tk;
                    Debug.WriteLine($"[OtherViewModel] 触发键解析成功: {tk}");
                }
                
                if (Enum.TryParse<VirtualKey>(ClickKey, out var ck)) 
                {
                    _autoClickerService.ClickKey = ck;
                    Debug.WriteLine($"[OtherViewModel] 连点键解析成功: {ck}");
                }
                
                Debug.WriteLine($"[OtherViewModel] 最终配置 - 启用: {IsAutoClickerEnabled}, 触发键: {TriggerKey}, 连点键: {ClickKey}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OtherViewModel] 加载配置失败: {ex.Message}");
            }
        }

        private void StartRecordingTriggerKey()
        {
            IsRecordingTriggerKey = true;
            IsRecordingClickKey = false;
            Debug.WriteLine("[OtherViewModel] 开始录制触发键");
        }

        private void StartRecordingClickKey()
        {
            IsRecordingClickKey = true;
            IsRecordingTriggerKey = false;
            Debug.WriteLine("[OtherViewModel] 开始录制连点键");
        }

        private async Task BrowseProgramAsync()
        {
            try
            {
                var picker = new FileOpenPicker
                {
                    ViewMode = PickerViewMode.List,
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    FileTypeFilter = { ".exe" }
                };
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
                var file = await picker.PickSingleFileAsync();
                if (file != null) AdditionalProgramPath = file.Path.Trim('"');
            }
            catch { }
        }

        partial void OnIsAutoClickerEnabledChanged(bool value)
        {
            _autoClickerService.IsEnabled = value;
            _ = SaveSettingsAsync();
            Debug.WriteLine($"[OtherViewModel] 连点器启用状态切换: {value}");
        }

        public void UpdateKey(string keyType, VirtualKey key)
        {
            var keyStr = key.ToString();
            Debug.WriteLine($"[OtherViewModel] 更新按键 - 类型: {keyType}, 按键: {keyStr}");
            
            if (keyType == "Trigger")
            {
                TriggerKey = keyStr;
                _autoClickerService.TriggerKey = key;
            }
            else if (keyType == "Click")
            {
                ClickKey = keyStr;
                _autoClickerService.ClickKey = key;
            }

            IsRecordingTriggerKey = false;
            IsRecordingClickKey = false;
            
            _ = SaveSettingsAsync();
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                string cleanPath = AdditionalProgramPath.Trim('"');
                await _localSettingsService.SaveSettingAsync("AdditionalProgramEnabled", IsAdditionalProgramEnabled);
                await _localSettingsService.SaveSettingAsync("AdditionalProgramPath", cleanPath);
                await _localSettingsService.SaveSettingAsync("AutoClickerEnabled", IsAutoClickerEnabled);

                await _localSettingsService.SaveSettingAsync("AutoClickerTriggerKey", TriggerKey);
                await _localSettingsService.SaveSettingAsync("AutoClickerClickKey", ClickKey);
                
                Debug.WriteLine($"[连点器] 配置保存成功 - 启用: {IsAutoClickerEnabled}, 触发键: {TriggerKey}, 连点键: {ClickKey}");
                
                _ = Task.Delay(2000).ContinueWith(_ => 
                    _dispatcherQueue?.TryEnqueue(() => StatusMessage = string.Empty));
                AdditionalProgramPath = cleanPath;
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存失败: {ex.Message}";
                Debug.WriteLine($"[连点器] 配置保存失败: {ex.Message}");
            }
        }
    }
}
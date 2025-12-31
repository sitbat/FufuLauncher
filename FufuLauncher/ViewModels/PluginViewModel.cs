using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using FufuLauncher.Models;
using Windows.Storage.Pickers;

namespace FufuLauncher.ViewModels;

public class PluginViewModel : INotifyPropertyChanged
{
    private readonly string _pluginsPath;
    private ObservableCollection<PluginItem> _plugins;
    private string _statusMessage;
    private bool _isLoading;
    private bool _isEmpty;

    public ObservableCollection<PluginItem> Plugins
    {
        get => _plugins;
        set { _plugins = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        set { _isEmpty = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand AddPluginCommand { get; }
    public ICommand TogglePluginCommand { get; }
    public ICommand DeletePluginCommand { get; }
    public ICommand SortCommand { get; }
    public ICommand OpenFolderCommand { get; }

    public PluginViewModel()
    {
        _pluginsPath = Path.Combine(AppContext.BaseDirectory, "Plugins");
        Plugins = new ObservableCollection<PluginItem>();

        RefreshCommand = new RelayCommand(LoadPlugins);
        AddPluginCommand = new RelayCommand(AddPluginAsync);
        TogglePluginCommand = new RelayCommand<PluginItem>(TogglePlugin);
        DeletePluginCommand = new RelayCommand<PluginItem>(DeletePlugin);
        SortCommand = new RelayCommand<string>(SortPlugins);
        OpenFolderCommand = new RelayCommand(OpenPluginFolder);

        LoadPlugins();
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_pluginsPath))
        {
            Directory.CreateDirectory(_pluginsPath);
        }
    }

    private void UpdateIsEmpty()
    {
        IsEmpty = Plugins == null || Plugins.Count == 0;
    }

    public void LoadPlugins()
    {
        try
        {
            IsLoading = true;
            EnsureDirectoryExists();
            Plugins.Clear();

            var directoryInfo = new DirectoryInfo(_pluginsPath);

            var allFiles = directoryInfo.GetFiles("*.*")
                .Where(f => f.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                            f.Name.EndsWith(".dll.disabled", StringComparison.OrdinalIgnoreCase));

            foreach (var file in allFiles)
            {
                var isEnabled = !file.Name.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);

                Plugins.Add(new PluginItem
                {
                    FileName = file.Name,
                    FullPath = file.FullName,
                    IsEnabled = isEnabled,
                    FileSize = file.Length,
                    DateModified = file.LastWriteTime
                });
            }
            StatusMessage = $"加载完成，共 {Plugins.Count} 个插件";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
            Debug.WriteLine($"[PluginVM] Load Error: {ex}");
        }
        finally
        {
            IsLoading = false;
            UpdateIsEmpty();
        }
    }

    private async void AddPluginAsync()
    {
        try
        {
            var picker = new FileOpenPicker();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add(".dll");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                EnsureDirectoryExists();
                var destPath = Path.Combine(_pluginsPath, file.Name);

                File.Copy(file.Path, destPath, true);
                StatusMessage = $"已添加插件: {file.Name}";
                LoadPlugins();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"添加失败: {ex.Message}";
        }
    }
    
    private void TogglePlugin(PluginItem? item)
    {
        if (item == null || !File.Exists(item.FullPath)) return;

        try
        {
            string newPath;
            bool targetState;
            
            if (item.FullPath.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
            {
                newPath = item.FullPath.Substring(0, item.FullPath.Length - ".disabled".Length);
                targetState = true;
            }
            else
            {
                if (!item.FullPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    StatusMessage = "无法识别的文件类型";
                    return;
                }
                newPath = item.FullPath + ".disabled";
                targetState = false;
            }

            if (File.Exists(newPath))
            {
                StatusMessage = "操作失败：目标文件名已存在";
                item.RefreshState(); 
                return;
            }
            
            File.Move(item.FullPath, newPath);
            item.FullPath = newPath;
            item.FileName = Path.GetFileName(newPath);
            item.IsEnabled = targetState;

            StatusMessage = $"已{(targetState ? "启用" : "禁用")}: {item.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"切换状态失败: {ex.Message}";
            item.RefreshState();
        }
    }

    private void DeletePlugin(PluginItem? item)
    {
        if (item == null) return;
        try
        {
            if (File.Exists(item.FullPath))
            {
                File.Delete(item.FullPath);
                Plugins.Remove(item);
                UpdateIsEmpty();
                StatusMessage = $"已删除: {item.FileName}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除失败: {ex.Message}";
        }
    }
    
    public void PerformRename(PluginItem item, string newNameWithoutExtension)
    {
        try
        {
            string currentExtension;
            if (item.FullPath.EndsWith(".dll.disabled", StringComparison.OrdinalIgnoreCase))
            {
                currentExtension = ".dll.disabled";
            }
            else if (item.FullPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                currentExtension = ".dll";
            }
            else
            {
                StatusMessage = "无法重命名：未知的文件后缀";
                return;
            }
            
            var newFileName = newNameWithoutExtension + currentExtension;
            var newPath = Path.Combine(_pluginsPath, newFileName);

            if (string.Equals(item.FullPath, newPath, StringComparison.OrdinalIgnoreCase)) return;

            if (File.Exists(newPath))
            {
                StatusMessage = "重命名失败：文件名已存在";
                return;
            }
            
            File.Move(item.FullPath, newPath);
            
            item.FileName = newFileName;
            item.FullPath = newPath;
            
            StatusMessage = $"重命名成功";
        }
        catch (Exception ex)
        {
            StatusMessage = $"重命名失败: {ex.Message}";
        }
    }

    private void SortPlugins(string? sortType)
    {
        if (string.IsNullOrEmpty(sortType)) return;

        var sorted = sortType switch
        {
            "Name" => Plugins.OrderBy(x => x.FileName),
            "Size" => Plugins.OrderByDescending(x => x.FileSize),
            "Date" => Plugins.OrderByDescending(x => x.DateModified),
            "Status" => Plugins.OrderByDescending(x => x.IsEnabled),
            _ => Plugins.OrderBy(x => x.FileName)
        };

        var list = sorted.ToList();
        Plugins.Clear();
        foreach (var item in list) Plugins.Add(item);

        StatusMessage = "列表已排序";
    }

    private void OpenPluginFolder()
    {
        try
        {
            EnsureDirectoryExists();
            Process.Start(new ProcessStartInfo
            {
                FileName = _pluginsPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch { }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
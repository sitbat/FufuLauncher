using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FufuLauncher.Models;

public class PluginItem : INotifyPropertyChanged
{
    private string _fileName;
    private string _fullPath;
    private bool _isEnabled;
    private long _fileSize;
    private DateTime _dateModified;

    public string FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); }
    }

    public string FullPath
    {
        get => _fullPath;
        set { _fullPath = value; OnPropertyChanged(); }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set 
        { 
            if (_isEnabled != value)
            {
                _isEnabled = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusColor));
            }
        }
    }

    public long FileSize
    {
        get => _fileSize;
        set { _fileSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(FileSizeDisplay)); }
    }

    public DateTime DateModified
    {
        get => _dateModified;
        set { _dateModified = value; OnPropertyChanged(); }
    }
    
    public string FileSizeDisplay => $"{(FileSize / 1024.0):F2} KB";
    public string StatusText => IsEnabled ? "已启用" : "已禁用";
    public string StatusColor => IsEnabled ? "#00CC6A" : "#999999"; 

    public void RefreshState()
    {
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusColor));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
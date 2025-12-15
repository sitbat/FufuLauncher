
using CommunityToolkit.Mvvm.ComponentModel;

namespace FufuLauncher.Models;

public partial class AccountInfo : ObservableObject
{
    [ObservableProperty] private string _nickname = "";
    [ObservableProperty] private string _gameUid = "";
    [ObservableProperty] private string _server = "";
    [ObservableProperty] private string _avatarUrl = "ms-appx:///Assets/DefaultAvatar.png";
    [ObservableProperty] private string _level = "";
}

public class UserDisplayConfig
{
    public string Nickname { get; set; } = "";
    public string GameUid { get; set; } = "";
    public string Server { get; set; } = "";
    public string AvatarUrl { get; set; } = "ms-appx:///Assets/DefaultAvatar.png";
    public string Level { get; set; } = "";
}
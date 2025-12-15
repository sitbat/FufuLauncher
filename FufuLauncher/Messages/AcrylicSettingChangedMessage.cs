using CommunityToolkit.Mvvm.Messaging;

namespace FufuLauncher.Messages
{



    public class AcrylicSettingChangedMessage
    {
        public bool IsEnabled { get; }

        public AcrylicSettingChangedMessage(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }
    }
}
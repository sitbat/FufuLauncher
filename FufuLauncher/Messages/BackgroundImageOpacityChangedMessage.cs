using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FufuLauncher.Messages
{
    public class BackgroundImageOpacityChangedMessage : ValueChangedMessage<double>
    {
        public BackgroundImageOpacityChangedMessage(double value) : base(value)
        {
        }
    }
}
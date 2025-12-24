using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FufuLauncher.Messages
{
    public class BackgroundOverlayOpacityChangedMessage : ValueChangedMessage<double>
    {
        public BackgroundOverlayOpacityChangedMessage(double value) : base(value)
        {
        }
    }
}

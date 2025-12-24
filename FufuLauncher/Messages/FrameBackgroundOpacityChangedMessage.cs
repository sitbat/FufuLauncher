using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FufuLauncher.Messages
{
    public class FrameBackgroundOpacityChangedMessage : ValueChangedMessage<double>
    {
        public FrameBackgroundOpacityChangedMessage(double value) : base(value)
        {
        }
    }
}

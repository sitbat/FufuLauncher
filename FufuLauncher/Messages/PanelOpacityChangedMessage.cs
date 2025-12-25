using CommunityToolkit.Mvvm.Messaging.Messages;

namespace FufuLauncher.Messages
{
    public class PanelOpacityChangedMessage : ValueChangedMessage<double>
    {
        public PanelOpacityChangedMessage(double value) : base(value)
        {
        }
    }
}
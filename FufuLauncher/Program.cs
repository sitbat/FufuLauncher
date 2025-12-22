using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace FufuLauncher
{
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }
    }
}
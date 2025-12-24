using System;
using System.Linq;
using FufuLauncher.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace FufuLauncher
{
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && string.Equals(args[0], "--elevated-inject", StringComparison.OrdinalIgnoreCase))
            {
                RunElevatedInjection(args);
                return;
            }

            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }

        private static void RunElevatedInjection(string[] args)
        {
            // args: --elevated-inject <gameExePath> <dllPath> <commandLineArgs...>
            int exitCode = 1;
            try
            {
                if (args.Length < 3)
                {
                    return;
                }

                string gameExePath = args[1];
                string dllPath = args[2];
                string commandLineArgs = args.Length > 3 ? string.Join(' ', args.Skip(3)) : string.Empty;

                var launcher = new LauncherService();
                var result = launcher.LaunchGameAndInject(gameExePath, dllPath, commandLineArgs, out var errorMessage, out var pid);

                exitCode = result == 0 ? 0 : 1;
            }
            finally
            {
                Environment.Exit(exitCode);
            }
        }
    }
}
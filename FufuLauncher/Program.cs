using System;
using System.Linq;
using FufuLauncher.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

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

            var key = "FufuLauncher_Main_Instance_Key";
            var mainInstance = AppInstance.FindOrRegisterForKey(key);

            if (!mainInstance.IsCurrent)
            {
                var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                var task = mainInstance.RedirectActivationToAsync(activationArgs).AsTask();
                task.Wait();
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
<<<<<<< HEAD
﻿using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
=======
﻿using System.Runtime.InteropServices;
>>>>>>> e479bcb4a0327b3eb023564baa2b34cd444bd279
using FufuLauncher.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace FufuLauncher
{
    public static class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool DuplicateTokenEx(IntPtr existingToken, uint desiredAccess, IntPtr tokenAttributes, SECURITY_IMPERSONATION_LEVEL impersonationLevel, TOKEN_TYPE tokenType, out IntPtr newToken);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateProcessWithTokenW(IntPtr token, uint logonFlags, string? applicationName, string commandLine, uint creationFlags, IntPtr environment, string? currentDirectory, ref STARTUPINFO startupInfo, out PROCESS_INFORMATION processInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint TOKEN_ASSIGN_PRIMARY = 0x0001;
        private const uint TOKEN_DUPLICATE = 0x0002;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint TOKEN_ADJUST_DEFAULT = 0x0080;
        private const uint TOKEN_ADJUST_SESSIONID = 0x0100;
        private const uint TOKEN_ALL_ACCESS = TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_QUERY | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID;
        private const string ForceNonAdminSettingKey = "ForceNonAdmin";

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && string.Equals(args[0], "--elevated-inject", StringComparison.OrdinalIgnoreCase))
            {
                RunElevatedInjection(args);
                return;
            }

            bool forceNonAdmin = ShouldForceNonAdmin();

            if (forceNonAdmin && IsProcessElevated())
            {
                if (TryRelaunchAsStandardUser(args)) return;
                MessageBox(IntPtr.Zero, "请以非管理员身份运行 FufuLauncher。", "FufuLauncher", 0x30);
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
            int exitCode = 1;
            try
            {
                if (args.Length < 4)
                {
                    return;
                }

                string gameExePath = args[1];
                string dllPath = args[2];
<<<<<<< HEAD

                if (!int.TryParse(args[3], out int configMask))
                {
                    MessageBox(IntPtr.Zero, "配置参数格式错误", "FufuLauncher 错误", 0x10);
                    return;
                }
=======
>>>>>>> e479bcb4a0327b3eb023564baa2b34cd444bd279

                string commandLineArgs = args.Length > 4 ? string.Join(' ', args.Skip(4)) : string.Empty;

                var launcher = new LauncherService();

                var result = launcher.LaunchGameAndInject(gameExePath, dllPath, commandLineArgs, out var errorMessage, out var pid);

                if (result != 0)
                {
                    MessageBox(IntPtr.Zero, $"注入启动失败: {errorMessage} (代码: {result})", "FufuLauncher 错误", 0x10); // MB_ICONERROR
                }

                exitCode = result == 0 ? 0 : 1;
            }
            catch (Exception ex)
            {
                MessageBox(IntPtr.Zero, $"注入进程发生异常: {ex.Message}", "FufuLauncher 错误", 0x10);
            }
            finally
            {
                Environment.Exit(exitCode);
            }
        }

        private static bool IsProcessElevated()
        {
            try
            {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryRelaunchAsStandardUser(string[] args)
        {
            IntPtr shellToken = IntPtr.Zero;
            IntPtr primaryToken = IntPtr.Zero;
            try
            {
                var explorer = Process.GetProcessesByName("explorer").FirstOrDefault();
                if (explorer == null) return false;

                if (!OpenProcessToken(explorer.Handle, TOKEN_ALL_ACCESS, out shellToken))
                {
                    return false;
                }

                if (!DuplicateTokenEx(shellToken, TOKEN_ALL_ACCESS, IntPtr.Zero, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, TOKEN_TYPE.TokenPrimary, out primaryToken))
                {
                    return false;
                }

                var startupInfo = new STARTUPINFO();
                startupInfo.cb = Marshal.SizeOf<STARTUPINFO>();

                string exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                if (string.IsNullOrWhiteSpace(exePath)) return false;

                string cmdLine = BuildCommandLine(exePath, args);

                if (!CreateProcessWithTokenW(primaryToken, 0, null, cmdLine, 0, IntPtr.Zero, Path.GetDirectoryName(exePath), ref startupInfo, out var processInfo))
                {
                    return false;
                }

                CloseHandle(processInfo.hProcess);
                CloseHandle(processInfo.hThread);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (primaryToken != IntPtr.Zero) CloseHandle(primaryToken);
                if (shellToken != IntPtr.Zero) CloseHandle(shellToken);
            }
        }

        private static string BuildCommandLine(string exePath, string[] args)
        {
            if (args.Length == 0) return $"\"{exePath}\"";
            var builder = new StringBuilder();
            builder.Append('"').Append(exePath).Append('"');
            foreach (var arg in args)
            {
                if (string.IsNullOrEmpty(arg)) continue;
                if (arg.Contains(' ') || arg.Contains("\"")) builder.Append(' ').Append('"').Append(arg.Replace("\"", "\\\"")).Append('"');
                else builder.Append(' ').Append(arg);
            }
            return builder.ToString();
        }

        private static bool ShouldForceNonAdmin()
        {
            try
            {
                var settings = new LocalSettingsService();
                var value = settings.ReadSettingAsync(ForceNonAdminSettingKey).GetAwaiter().GetResult();
                return value == null ? true : Convert.ToBoolean(value);
            }
            catch
            {
                return true;
            }
        }

        private enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation = 2
        }

        private enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public string? lpReserved;
            public string? lpDesktop;
            public string? lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }
    }
}
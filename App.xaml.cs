using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.Windows.AppLifecycle;
using NikkeDiscordRPC.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WinRT.Interop;

namespace NikkeDiscordRPC
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        private const string SingleInstanceMutexName = "NikkeDiscordRPC.SingleInstance";
        private const string BringToFrontEventName = "NikkeDiscordRPC.BringToFront";

        private const long MaxLogsFolderBytes = 50L * 1024L * 1024L;
        private static bool _logsFolderChecked;
        private static Mutex? _singleInstanceMutex;
        private static EventWaitHandle? _bringToFrontEvent;
        private static RegisteredWaitHandle? _bringToFrontRegistration;

        private readonly bool _isPrimaryInstance;

        public static Window? AppMainWindow { get; set; }
        public static bool HandleClosedEvents { get; set; } = true;

        public IServiceProvider Services { get; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool createdNew);
            _isPrimaryInstance = createdNew;

            if (!_isPrimaryInstance)
            {
                SignalPrimaryInstanceToBringToFront();
                Environment.Exit(0);
                return;
            }

            Services = ConfigureServices();
            this.InitializeComponent();

            this.UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                ReleaseSingleInstanceMutex();
                ReleaseBringToFrontSignal();
            };

            SetupBringToFrontSignal();

            CheckLogFolderSize();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            AppMainWindow = new MainWindow();
            AppMainWindow.Closed += (sender, args) =>
            {
                if (HandleClosedEvents)
                {
                    string exitBehavior = Services.GetService<IConfigService>()?.CurrentConfig.ExitBehavior ?? "Exit App";
                    if (string.Equals(exitBehavior, "Minimize to Tray", StringComparison.OrdinalIgnoreCase))
                    {
                        args.Handled = true;
                        AppMainWindow.Hide(false);
                    }
                }
            };
            
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "NikkeIcon.ico");
            AppMainWindow.AppWindow.SetIcon(iconPath);
            AppMainWindow.Activate();

            if (DetectPackagedState())
            {
                AppActivationArguments appActivationArguments = AppInstance.GetCurrent().GetActivatedEventArgs();
                if (appActivationArguments.Kind is ExtendedActivationKind.StartupTask)
                {
                    AppMainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        AppMainWindow.Hide(false);
                    });
                }
            }
            else
            {
                string launchArgs = e.Arguments;
                if (!string.IsNullOrEmpty(launchArgs))
                {
                    if (launchArgs.Contains("--minimized"))
                    {
                        AppMainWindow.Hide(false);
                    }
                }
                string[] envArgs = Environment.GetCommandLineArgs();
                if (envArgs.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase)))
                {
                    AppMainWindow.Hide(false);
                }
            }
        }

        private static void ReleaseSingleInstanceMutex()
        {
            try
            {
                _singleInstanceMutex?.ReleaseMutex();
            }
            catch
            {
            }
            finally
            {
                _singleInstanceMutex?.Dispose();
                _singleInstanceMutex = null;
            }
        }

        private static void SetupBringToFrontSignal()
        {
            _bringToFrontEvent = new EventWaitHandle(false, EventResetMode.AutoReset, BringToFrontEventName);
            _bringToFrontRegistration = ThreadPool.RegisterWaitForSingleObject(
                _bringToFrontEvent,
                static (_, _) => BringMainWindowToFront(),
                null,
                Timeout.Infinite,
                executeOnlyOnce: false);
        }

        private static void ReleaseBringToFrontSignal()
        {
            try
            {
                _bringToFrontRegistration?.Unregister(null);
            }
            catch
            {
            }

            _bringToFrontRegistration = null;
            _bringToFrontEvent?.Dispose();
            _bringToFrontEvent = null;
        }

        private static void SignalPrimaryInstanceToBringToFront()
        {
            try
            {
                using EventWaitHandle signal = EventWaitHandle.OpenExisting(BringToFrontEventName);
                signal.Set();
            }
            catch
            {
            }
        }

        private static void BringMainWindowToFront()
        {
            if (AppMainWindow == null)
            {
                return;
            }

            AppMainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                IntPtr windowHandle = WindowNative.GetWindowHandle(AppMainWindow);
                WindowId windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
                AppWindow? appWindow = AppWindow.GetFromWindowId(windowId);
                AppMainWindow?.Show();
                AppMainWindow?.Activate();
                if (windowHandle != IntPtr.Zero)
                { 
                    SetForegroundWindow(windowHandle);
                }
            });
        }

        private static ServiceProvider ConfigureServices()
        {
            ServiceCollection? services = new();

            // Services
            services.AddSingleton<IRichPresenceService, RichPresenceService>();
            services.AddSingleton<IConfigService, ConfigService>();

            // ViewModels
            services.AddSingleton<HomeViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<SettingsViewModel>();

            return services.BuildServiceProvider();
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            WriteUnhandledExceptionLog("UI.UnhandledException", e.Exception);
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            Exception? exception = e.ExceptionObject as Exception;
            WriteUnhandledExceptionLog("AppDomain.UnhandledException", exception, e.IsTerminating);
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            WriteUnhandledExceptionLog("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        }

        private static void WriteUnhandledExceptionLog(string source, Exception? exception, bool isTerminating = false)
        {
            Debug.WriteLine("exception: " + exception?.ToString());
            try
            {
                string logsFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NikkeDiscordRPC",
                    "Logs");


                Directory.CreateDirectory(logsFolder);

                string filePath = Path.Combine(logsFolder, $"unhandled-{DateTime.Now:yyyyMMdd}.log");
                StringBuilder sb = new();
                sb.AppendLine("==============================");
                sb.AppendLine($"Timestamp: {DateTime.Now:O}");
                sb.AppendLine($"Source: {source}");
                sb.AppendLine($"IsTerminating: {isTerminating}");

                if (exception != null)
                {
                    sb.AppendLine($"Type: {exception.GetType().FullName}");
                    sb.AppendLine($"Message: {exception.Message}");
                    sb.AppendLine("StackTrace:");
                    sb.AppendLine(exception.StackTrace ?? "<no stack trace>");
                }
                else
                {
                    sb.AppendLine("Exception: <null>");
                }

                sb.AppendLine();
                File.AppendAllText(filePath, sb.ToString());
            }
            catch
            {
            }
        }

        private static void CheckLogFolderSize()
        {
            if (_logsFolderChecked)
            {
                return;
            }

            _logsFolderChecked = true;

            try
            {
                string logsFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NikkeDiscordRPC",
                    "Logs");

                if (!Directory.Exists(logsFolder))
                {
                    return;
                }

                DirectoryInfo directory = new(logsFolder);
                long totalBytes = directory.GetFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);

                if (totalBytes <= MaxLogsFolderBytes)
                {
                    return;
                }

                foreach (FileInfo file in directory.GetFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        private static bool DetectPackagedState()
        {
            try
            {
                _ = Windows.ApplicationModel.Package.Current;
                return true;
            }
            catch
            {
                return false;
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

    }
}

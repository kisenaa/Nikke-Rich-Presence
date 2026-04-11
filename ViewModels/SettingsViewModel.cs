using Microsoft.UI.Dispatching;
using Microsoft.Win32;
using Microsoft.Windows.AppLifecycle;
using NikkeDiscordRPC.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.ApplicationModel;

namespace NikkeDiscordRPC.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private const string RunRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AutoLaunchValueName = "NikkeDiscordRPC";

        private readonly IConfigService _configService;
        private readonly DispatcherQueueTimer _saveTimer;
        private bool _isInitializing;

        public bool IsPackagedApp { get; private set; }

        public ObservableCollection<string> ProcessPriorities { get; } =
        [
            "Idle",
            "Below Normal",
            "Normal",
            "Above Normal",
            "High",
            "Real Time",
        ];

        [ObservableProperty]
        public partial string SelectedProcessPriority { get; set; } = "Normal";

        [ObservableProperty]
        public partial int PollingIntervalSeconds { get; set; } = 4;

        [ObservableProperty]
        public partial bool AutoLaunchOnBoot { get; set; } = true;

        [ObservableProperty]
        public partial bool ForegroundHotkeyEnabled { get; set; } = true;

        [ObservableProperty]
        public partial string ForegroundHotkey { get; set; } = "Ctrl + Shift + Alt + N";

        [ObservableProperty]
        public partial string? NikkeLauncherPath { get; set; }

        private string _minimizeBehavior = "To Tray";
        public string MinimizeBehavior
        {
            get => _minimizeBehavior;
            set
            {
                if (SetProperty(ref _minimizeBehavior, value) && !_isInitializing)
                {
                    _saveTimer.Stop();
                    _saveTimer.Start();
                }
            }
        }

        private string _exitBehavior = "Minimize to Tray";
        public string ExitBehavior
        {
            get => _exitBehavior;
            set
            {
                if (SetProperty(ref _exitBehavior, value) && !_isInitializing)
                {
                    _saveTimer.Stop();
                    _saveTimer.Start();
                }
            }
        }

        public SettingsViewModel(IConfigService configService)
        {
            _configService = configService;
            _isInitializing = true;
            IsPackagedApp = DetectPackagedState();

            _saveTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _saveTimer.Interval = TimeSpan.FromMilliseconds(100);
            _saveTimer.IsRepeating = false;
            _saveTimer.Tick += async (_, _) => await SaveSettings();

            string configuredPriority = _configService.CurrentConfig.ProcessPriority;
            SelectedProcessPriority = ProcessPriorities.Contains(configuredPriority)
                ? configuredPriority
                : "Normal";

            int configuredInterval = _configService.CurrentConfig.PollingIntervalSeconds;
            PollingIntervalSeconds = Math.Clamp(configuredInterval, 1, 20);
            AutoLaunchOnBoot = _configService.CurrentConfig.AutoLaunchOnBoot;
            ForegroundHotkeyEnabled = _configService.CurrentConfig.ForegroundHotkeyEnabled;
            ForegroundHotkey = string.IsNullOrWhiteSpace(_configService.CurrentConfig.ForegroundHotkey)
                ? "Ctrl + Shift + Alt + N"
                : _configService.CurrentConfig.ForegroundHotkey;
            NikkeLauncherPath = _configService.CurrentConfig.NikkeLauncherPath;
            MinimizeBehavior = string.IsNullOrEmpty(_configService.CurrentConfig.MinimizeBehavior)
                ? "To Tray"
                : _configService.CurrentConfig.MinimizeBehavior;
            ExitBehavior = string.IsNullOrEmpty(_configService.CurrentConfig.ExitBehavior)
                ? "Minimize to Tray"
                : _configService.CurrentConfig.ExitBehavior;

            _isInitializing = false;
        }

        partial void OnSelectedProcessPriorityChanged(string value)
        {
            if (_isInitializing) return;

            try
            {
                ProcessPriorityClass priority = value switch
                {
                    "Idle" => ProcessPriorityClass.Idle,
                    "Below Normal" => ProcessPriorityClass.BelowNormal,
                    "Normal" => ProcessPriorityClass.Normal,
                    "Above Normal" => ProcessPriorityClass.AboveNormal,
                    "High" => ProcessPriorityClass.High,
                    "Real Time" => ProcessPriorityClass.RealTime,
                    _ => ProcessPriorityClass.Normal
                };

                Process.GetCurrentProcess().PriorityClass = priority;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set priority: {ex.Message}");
            }

            _saveTimer.Stop();
            _saveTimer.Start();
        }

        partial void OnPollingIntervalSecondsChanged(int value)
        {
            int clampedValue = Math.Clamp(value, 1, 20);
            if (value != clampedValue)
            {
                PollingIntervalSeconds = clampedValue;
                return;
            }

            if (_isInitializing) return;

            _saveTimer.Stop();
            _saveTimer.Start();
        }

        partial void OnAutoLaunchOnBootChanged(bool value)
        {
            if (_isInitializing) return;

            _saveTimer.Stop();
            _saveTimer.Start();
        }

        partial void OnForegroundHotkeyEnabledChanged(bool value)
        {
            if (_isInitializing) return;

            _saveTimer.Stop();
            _saveTimer.Start();
        }
      
        partial void OnForegroundHotkeyChanged(string value)
        {
            if (_isInitializing) return;

            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private async Task SaveSettings()
        {
            bool shouldRefreshHotkey =
                _configService.CurrentConfig.ForegroundHotkey != ForegroundHotkey
                || _configService.CurrentConfig.ForegroundHotkeyEnabled != ForegroundHotkeyEnabled;

            if (_configService.CurrentConfig.AutoLaunchOnBoot != AutoLaunchOnBoot)
            {
                await ApplyAutoLaunchSetting();
            }

            _configService.Update(config =>
            {
                config.ProcessPriority = SelectedProcessPriority;
                config.PollingIntervalSeconds = PollingIntervalSeconds;
                config.AutoLaunchOnBoot = AutoLaunchOnBoot;
                config.ForegroundHotkeyEnabled = ForegroundHotkeyEnabled;
                config.ForegroundHotkey = ForegroundHotkey;
                config.NikkeLauncherPath = NikkeLauncherPath;
                config.MinimizeBehavior = MinimizeBehavior;
                config.ExitBehavior = ExitBehavior;
            });

            if (shouldRefreshHotkey && App.AppMainWindow is MainWindow mainWindow)
            {
                mainWindow.RefreshConfiguredHotkey();
            }
        }

        public async Task ApplyAutoLaunchSetting()
        {
          if (!IsPackagedApp)
            {
                try
                {
                    using RegistryKey? key = Registry.CurrentUser.CreateSubKey(RunRegistryPath, writable: true);
                    if (key == null)
                    {
                        return;
                    }

                    if (AutoLaunchOnBoot)
                    {
                        string? appPath = Environment.ProcessPath;

                        if (string.IsNullOrEmpty(appPath))
                        {
                            appPath = Process.GetCurrentProcess().MainModule?.FileName;
                        }
                        else
                        {
                            key.SetValue(AutoLaunchValueName, $"\"{appPath}\" --minimized", RegistryValueKind.String);
                        }
                    }
                    else
                    {
                        key.DeleteValue(AutoLaunchValueName, throwOnMissingValue: false);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Auto-launch apply failed: {ex.Message}");
                }
            }
           else
            {
                try
                {
                    StartupTask startupTask = await StartupTask.GetAsync("NikkeRichPresenceStartUp");
                    if (startupTask == null)
                    {
                        return;
                    }

                    if (AutoLaunchOnBoot)
                    {
                        if (startupTask.State == StartupTaskState.Disabled)
                        {
                            await startupTask.RequestEnableAsync();
                        }
                    }
                    else
                    {
                        if (startupTask.State == StartupTaskState.Enabled)
                        {
                            startupTask.Disable();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Auto-launch apply failed: {ex.Message}");
                }
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

        public void ResetAllSettingsAndRestart()
        {
            _configService.ResetToDefaults();

            string? appPath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(appPath))
            {
                Process.Start(new ProcessStartInfo(appPath)
                {
                    UseShellExecute = true,
                });
            }

            Application.Current.Exit();
        }
    }
}

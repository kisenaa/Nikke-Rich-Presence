using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.System;
using WinRT.Interop;
using NikkeDiscordRPC.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NikkeDiscordRPC
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private const int HotkeyId = 0xA100;
        private const int WmHotkey = 0x0312;
        private const int GwlWndProc = -4;

        private MainPage? mainPage;
        private MainWindowViewModel ViewModel { get; }
        private readonly IConfigService _configService;
        private readonly AppWindow _appWindow;
        private readonly IntPtr _windowHandle;
        private IntPtr _previousWndProc;
        private WndProcDelegate? _currentWndProc;


        public MainWindow()
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            ViewModel = ((App)App.Current).Services.GetService<MainWindowViewModel>()!;
            _configService = ((App)App.Current).Services.GetService<IConfigService>()!;

            _windowHandle = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(_windowHandle);
            _appWindow = AppWindow.GetFromWindowId(windowId);
            _appWindow.Changed += OnAppWindowChanged;

            HookWindowProc();
            RegisterConfiguredHotkey();
            Closed += MainWindow_Closed;

            rootFrame.Loaded += (sender, e) =>
            {
                mainPage = rootFrame.Content as MainPage;
            };
        }

        private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            mainPage?.TogglePane();
        }

        private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (!args.DidPresenterChange)
            {
                return;
            }

            if (sender.Presenter is not OverlappedPresenter presenter)
            {
                return;
            }

            if (presenter.State != OverlappedPresenterState.Minimized)
            {
                return;
            }

            string minimizeBehavior = _configService.CurrentConfig.MinimizeBehavior;
            if (string.Equals(minimizeBehavior, "To Tray", StringComparison.OrdinalIgnoreCase))
            {
                this.Hide(false);
            }
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            UnregisterGlobalHotkey();
            RestoreWindowProc();
        }

        private void HookWindowProc()
        {
            _currentWndProc = WindowProc;
            IntPtr procPtr = Marshal.GetFunctionPointerForDelegate(_currentWndProc);
            _previousWndProc = SetWindowLongPtr(_windowHandle, GwlWndProc, procPtr);
        }

        private void RestoreWindowProc()
        {
            if (_previousWndProc != IntPtr.Zero)
            {
                SetWindowLongPtr(_windowHandle, GwlWndProc, _previousWndProc);
                _previousWndProc = IntPtr.Zero;
            }
        }

        private void RegisterConfiguredHotkey()
        {
            UnregisterGlobalHotkey();

            var config = _configService.CurrentConfig;
            if (!config.ForegroundHotkeyEnabled)
            {
                return;
            }

            if (!TryParseHotkey(config.ForegroundHotkey, out uint modifiers, out uint keyCode))
            {
                Debug.WriteLine($"Invalid hotkey format: {config.ForegroundHotkey}");
                return;
            }

            if (!RegisterHotKey(_windowHandle, HotkeyId, modifiers, keyCode))
            {
                Debug.WriteLine($"RegisterHotKey failed. Error={Marshal.GetLastWin32Error()}");
            }
        }

        public void RefreshConfiguredHotkey()
        {
            RegisterConfiguredHotkey();
        }

        private void UnregisterGlobalHotkey()
        {
            if (_windowHandle != IntPtr.Zero)
            {
                UnregisterHotKey(_windowHandle, HotkeyId);
            }
        }

        private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
            {
                this.Show();
                Activate();
                SetForegroundWindow(_windowHandle);
                return IntPtr.Zero;
            }

            return CallWindowProc(_previousWndProc, hWnd, msg, wParam, lParam);
        }

        private static bool TryParseHotkey(string? text, out uint modifiers, out uint keyCode)
        {
            modifiers = 0;
            keyCode = 0;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string[] parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("Control", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers |= 0x0002;
                    continue;
                }

                if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers |= 0x0004;
                    continue;
                }

                if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers |= 0x0001;
                    continue;
                }

                if (TryParseVirtualKey(part, out uint parsedKey))
                {
                    keyCode = parsedKey;
                }
            }

            return modifiers != 0 && keyCode != 0;
        }

        private static bool TryParseVirtualKey(string token, out uint keyCode)
        {
            keyCode = 0;

            if (token.Length == 1)
            {
                char c = char.ToUpperInvariant(token[0]);
                if (char.IsLetterOrDigit(c))
                {
                    keyCode = c;
                    return true;
                }
            }

            if (token.StartsWith("F", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(token[1..], out int functionKey)
                && functionKey >= 1
                && functionKey <= 24)
            {
                keyCode = (uint)(0x70 + functionKey - 1);
                return true;
            }

            if (token.Equals("Esc", StringComparison.OrdinalIgnoreCase))
            {
                keyCode = (uint)VirtualKey.Escape;
                return true;
            }

            return Enum.TryParse(token, true, out VirtualKey vKey) && (keyCode = (uint)vKey) > 0;
        }

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newProc)
        {
            if (IntPtr.Size == 8)
            {
                return SetWindowLongPtr64(hWnd, nIndex, newProc);
            }

            return new IntPtr(SetWindowLong32(hWnd, nIndex, newProc.ToInt32()));
        }
    }
}

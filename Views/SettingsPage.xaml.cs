using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NikkeDiscordRPC.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private SettingsViewModel ViewModel { get; }

        public string MinimizeBehavior
        {
            get => ViewModel.MinimizeBehavior;
            set => ViewModel.MinimizeBehavior = value;
        }

        public string ExitBehavior
        {
            get => ViewModel.ExitBehavior;
            set => ViewModel.ExitBehavior = value;
        }

        public SettingsPage()
        {
            ViewModel = ((App)App.Current).Services.GetService<SettingsViewModel>()!;
            InitializeComponent();
        }

        private void HotkeyTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            bool isCtrlDown = IsKeyDown(VirtualKey.Control);
            bool isShiftDown = IsKeyDown(VirtualKey.Shift);
            bool isAltDown = IsKeyDown(VirtualKey.Menu);

            VirtualKey key = e.Key;
            if (key is VirtualKey.Control or VirtualKey.Shift or VirtualKey.Menu)
            {
                e.Handled = true;
                return;
            }

            List<string> parts = [];
            if (isCtrlDown) parts.Add("Ctrl");
            if (isShiftDown) parts.Add("Shift");
            if (isAltDown) parts.Add("Alt");
            parts.Add(FormatKey(key));

            ViewModel.ForegroundHotkey = string.Join(" + ", parts);
            e.Handled = true;
        }

        private static bool IsKeyDown(VirtualKey key)
        {
            var state = InputKeyboardSource.GetKeyStateForCurrentThread(key);
            return (state & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
        }

        private static string FormatKey(VirtualKey key)
        {
            return key switch
            {
                VirtualKey.Number0 => "0",
                VirtualKey.Number1 => "1",
                VirtualKey.Number2 => "2",
                VirtualKey.Number3 => "3",
                VirtualKey.Number4 => "4",
                VirtualKey.Number5 => "5",
                VirtualKey.Number6 => "6",
                VirtualKey.Number7 => "7",
                VirtualKey.Number8 => "8",
                VirtualKey.Number9 => "9",
                VirtualKey.Space => "Space",
                VirtualKey.Escape => "Esc",
                _ => key.ToString(),
            };
        }

        private async void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new()
            {
                XamlRoot = XamlRoot,
                Title = "Reset all settings?",
                Content = "This will delete config.json, restore default settings, and restart the app.",
                PrimaryButtonText = "Reset and restart",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
            };

            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.ResetAllSettingsAndRestart();
            }
        }
    }
}

using H.NotifyIcon;
using NikkeDiscordRPC.Services;

namespace NikkeDiscordRPC.ViewModels
{
    public partial class MainWindowViewModel: ObservableObject
    {
        private readonly IConfigService _configService;

        public MainWindowViewModel(IConfigService configService)
        {
            _configService = configService;
            _configService.OnConfigReset += ExitApplication;
        }

        [ObservableProperty]
        public partial bool IsWindowVisible { get; set; }

        [RelayCommand]
        public void ShowHideWindow()
        {
            Window? window = App.AppMainWindow;
            if (window == null)
            {
                return;
            }

            if (window.Visible)
            {
                window.Hide(false);
            }
            else
            {
                window.Show();
            }
            IsWindowVisible = window.Visible;
        }

        [RelayCommand]
        public void ExitApplication()
        {
            App.HandleClosedEvents = false;

            if (App.AppMainWindow is MainWindow mainWindow)
            {
                mainWindow.TrayIcon.Dispose();
            }

            App.AppMainWindow?.Close();
            Application.Current.Exit();
        }
    }
}

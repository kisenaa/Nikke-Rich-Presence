using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.Net.Http;
using Windows.ApplicationModel;
using Windows.System;

namespace NikkeDiscordRPC.Views
{
    /// <summary>
    /// A simple page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string VersionSourceUrl = "https://raw.githubusercontent.com/kisenaa/Nikke-Rich-Presence/refs/heads/main/version.txt";
        private const string DownloadNowUrl = "https://github.com/kisenaa/Nikke-Rich-Presence/releases/latest";
        private const string LatestReleaseUrl = "https://github.com/kisenaa/Nikke-Rich-Presence/releases/latest";

        private static readonly HttpClient _httpClient = new();
        private static bool _hasCheckedUpdate;

        private MainViewModel ViewModel { get; }
        private SettingsViewModel SettingsViewModel { get; }

        public MainPage()
        {
            this.InitializeComponent();
            ViewModel = ((App)App.Current).Services.GetService<MainViewModel>()!;
            SettingsViewModel = ((App)App.Current).Services.GetService<SettingsViewModel>()!;
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await SettingsViewModel.ApplyAutoLaunchSetting();
        }

        public void TogglePane()
        {
            NavView.IsPaneOpen = !NavView.IsPaneOpen;
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigated += On_Navigated; // Set handler for header name
            ContentFrame.NavigationFailed += OnNavigationFailed;

            _ = CheckForUpdatesOnceAsync();

            // NavView doesn't load any page by default, so load home page.
            NavView.SelectedItem = NavView.MenuItems[0];
            Navigate(typeof(HomePage), new EntranceNavigationTransitionInfo());
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked == true)
            {
                Navigate(typeof(SettingsPage), args.RecommendedNavigationTransitionInfo);
            }
            else if (args.InvokedItemContainer != null)
            {
                String pageTag = args.InvokedItemContainer.Tag.ToString() ?? String.Empty;
                Type? navPageType = Type.GetType(pageTag);
                if (navPageType != null)
                {
                    Navigate(navPageType, args.RecommendedNavigationTransitionInfo);
                }
            }
        }
        private void Navigate(Type navPageType, NavigationTransitionInfo transitionInfo)
        {
            // Get the page type before navigation so we can prevent duplicate entries in the backstack.
            Type preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (navPageType is not null && !Type.Equals(preNavPageType, navPageType))
            {
                ContentFrame.Navigate(navPageType, null, transitionInfo);
            }
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            NavView.IsBackEnabled = ContentFrame.CanGoBack;
            Type pageType = ContentFrame.SourcePageType;

            if (pageType == null)
            {
                throw new Exception("On_Navigated should have navigated to a page type, but page type is null");
            }

            if (pageType == typeof(SettingsPage))
            {
                // SettingsItem is not part of NavView.MenuItems, and doesn't have a Tag.
                NavView.SelectedItem = (NavigationViewItem)NavView.SettingsItem;
                ViewModel.PageTitle = "Settings";
                return;
            }

            NavigationViewItem? selected = NavView.MenuItems.OfType<NavigationViewItem>()
                                                            .FirstOrDefault(i => (string)i.Tag == pageType.FullName);
            if (selected != null)
            {
                NavView.SelectedItem = selected;
                ViewModel.PageTitle = selected.Content?.ToString() ?? String.Empty;
            }
            else
            {
                throw new Exception("Selected nav item doesn't have a page");
            }
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private async Task CheckForUpdatesOnceAsync()
        {
            if (_hasCheckedUpdate)
            {
                return;
            }

            _hasCheckedUpdate = true;

            try
            {
                string remoteText = (await _httpClient.GetStringAsync(VersionSourceUrl)).Trim();
                if (remoteText.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                {
                    remoteText = remoteText[1..];
                }

                if (!Version.TryParse(remoteText, out Version? remoteVersion))
                {
                    return;
                }

                Version localVersion = GetLocalAppVersion();
                if (remoteVersion > localVersion)
                {
                    PrefixText.Text = "New version ";
                    RemoteVersionText.Text = remoteVersion.ToString();
                    LocalVersionText.Text = localVersion.ToString();
                    UpdateInfoBar.IsOpen = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
            }
        }

        private static Version GetLocalAppVersion()
        {
            var v = Package.Current.Id.Version;
            return new Version(v.Major, v.Minor, v.Build);
        }

        private async void DownloadNow_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(DownloadNowUrl));
        }
    }
 }

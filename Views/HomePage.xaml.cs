using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using NikkeDiscordRPC.Models;

namespace NikkeDiscordRPC.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        private HomeViewModel ViewModel { get; }
        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        private DispatcherQueueTimer? _statusTimer;

        public HomePage()
        {
            ViewModel = ((App)App.Current).Services.GetService<HomeViewModel>()!;
            InitializeComponent();
            ViewModel.ShowStatusEvent += OnShowStatus;
            Unloaded += HomePage_Unloaded;
        }

        private void HomePage_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ShowStatusEvent -= OnShowStatus;
            _statusTimer?.Stop();
        }

        private void ImageSuggestBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not AutoSuggestBox autoSuggestBox)
            {
                return;
            }

            if (ViewModel.SelectedImage == null)
            {
                ViewModel.SelectedImage = ViewModel.ImageSelection.FirstOrDefault(v => v.Key == "nikke-default");
            }

            if (ViewModel.SelectedImage != null)
            {
                autoSuggestBox.Text = ViewModel.SelectedImage.Name;
            }

            autoSuggestBox.ItemsSource = ViewModel.ImageSelection;
        }

        private void ImageSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            {
                return;
            }

            string query = sender.Text?.Trim() ?? string.Empty;
            var suggestions = ViewModel.ImageSelection
                .Select(item => new { Image = item, Score = GetImageSearchScore(item.Name, query) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Image.Name)
                .Select(x => x.Image)
                .Take(20)
                .ToList();

            sender.ItemsSource = suggestions;
        }

        private void ImageSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is RpcImage selected)
            {
                ViewModel.SelectedImage = selected;
                sender.Text = selected.Name;
            }
        }

        private void ImageSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is RpcImage chosen)
            {
                ViewModel.SelectedImage = chosen;
                sender.Text = chosen.Name;
                return;
            }

            string query = sender.Text?.Trim() ?? string.Empty;
            var bestMatch = ViewModel.ImageSelection
                .Select(item => new { Image = item, Score = GetImageSearchScore(item.Name, query) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Image.Name)
                .Select(x => x.Image)
                .FirstOrDefault();

            if (bestMatch != null)
            {
                ViewModel.SelectedImage = bestMatch;
                sender.Text = bestMatch.Name;
            }
        }

        private static int GetImageSearchScore(string source, string query)
        {
            if (string.IsNullOrWhiteSpace(source)) return 0;
            if (string.IsNullOrWhiteSpace(query)) return 100;

            string s = source.ToLowerInvariant();
            string q = query.ToLowerInvariant();

            if (s == q) return 2000;

            int containsIndex = s.IndexOf(q, StringComparison.Ordinal);
            if (containsIndex >= 0)
            {
                return 1200 - containsIndex;
            }

            int qi = 0;
            for (int si = 0; si < s.Length && qi < q.Length; si++)
            {
                if (s[si] == q[qi])
                {
                    qi++;
                }
            }

            return qi == q.Length ? 400 : 0;
        }

        private void OnShowStatus(string title, string message, InfoBarSeverity severity)
        {
            ViewModel.IsStatusBarOpen = true;
            StatusInfoBar.Title = title;
            StatusInfoBar.Severity = severity;
            StatusInfoBar.IsOpen = true;
            StatusMessage.Text = message;

            _statusTimer?.Stop();

            if (_statusTimer == null)
            {
                _statusTimer = _dispatcherQueue.CreateTimer();
                _statusTimer.Interval = TimeSpan.FromSeconds(2);
                _statusTimer.IsRepeating = false;

                _statusTimer.Tick += (s, e) =>
                {
                    StatusInfoBar.IsOpen = false;
                    ViewModel.IsStatusBarOpen = false;
                };
            }
            _statusTimer?.Start();
        }

        private void StatusInfoBar_CloseButtonClick(InfoBar sender, object args)
        {
            ViewModel.IsStatusBarOpen = false;
        }

        private void CloseProTipButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CloseProTip();
        }

        private void OpenProTipButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenProTip();
        }
    }
}

namespace NikkeDiscordRPC.Views
{
    public sealed partial class AboutPage : Page
    {
        public string GithubProfileUrl { get; } = "https://github.com/kisenaa";
        public string RepositoryUrl { get; } = "https://github.com/kisenaa/Nikke-Rich-Presence";

        public AboutPage()
        {
            InitializeComponent();
        }
    }
}

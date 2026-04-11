namespace NikkeDiscordRPC.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        public partial String? PageTitle { get; set; }
    }
}

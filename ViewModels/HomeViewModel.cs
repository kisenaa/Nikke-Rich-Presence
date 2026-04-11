using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using NikkeDiscordRPC.Constants;
using NikkeDiscordRPC.Models;
using NikkeDiscordRPC.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace NikkeDiscordRPC.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private const string DefaultDiscordAvatarUrl = "https://cdn.discordapp.com/embed/avatars/0.png";

        private readonly IRichPresenceService _richPresenceService;
        private readonly IConfigService _configService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _configSaveTimer;

        public HomeViewModel(IRichPresenceService richPresenceService, IConfigService configService)
        {
            _richPresenceService = richPresenceService;
            _configService = configService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            _richPresenceService.DiscordIdentityChanged += OnDiscordIdentityChanged;
            _richPresenceService.ProcessStatusChanged += OnProcessStatusChanged;

            var status = _richPresenceService.GetCurrentProcessStatus();
            IsDiscordRunning = status.IsDiscordRunning;
            IsNikkeRunning = status.IsNikkeRunning;

            if (configService.CurrentConfig.RichPresence?.ImageKey != null)
            {
                SelectedImage = RpcData.Images.FirstOrDefault(img => img.Key == configService.CurrentConfig.RichPresence?.ImageKey);
            }
            Details = configService.CurrentConfig.RichPresence?.Details;
            State = configService.CurrentConfig.RichPresence?.State;
            UseCustomImage = configService.CurrentConfig.RichPresence?.UseCustomImage ?? false;
            CustomDiscordApplicationId = configService.CurrentConfig.RichPresence?.CustomDiscordApplicationId;
            CustomImageKey = configService.CurrentConfig.RichPresence?.CustomImageKey;
            ShowProTip = configService.CurrentConfig.ShowProTip;

            _configSaveTimer = _dispatcherQueue.CreateTimer();
            _configSaveTimer.Interval = TimeSpan.FromMilliseconds(200);
            _configSaveTimer.IsRepeating = false;
            _configSaveTimer.Tick += OnConfigSaveTimerTick;
            PropertyChanged += OnViewModelPropertyChanged;

            RestoreRpcState();
        }

        // Event
        public event Action<string, string, InfoBarSeverity>? ShowStatusEvent;

        // Form Data
        public ObservableCollection<RpcImage> ImageSelection => RpcData.Images;

        [ObservableProperty]
        public partial RpcImage? SelectedImage { get; set; }

        [ObservableProperty]
        public partial string? Details { get; set; }

        [ObservableProperty]
        public partial string? State { get; set; }

        [ObservableProperty]
        public partial string DiscordUsername { get; set; } = "Not connected";

        [ObservableProperty]
        public partial string DiscordAvatarUrl { get; set; } = DefaultDiscordAvatarUrl;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GameProvidedImageVisibility))]
        [NotifyPropertyChangedFor(nameof(CustomImageFieldsVisibility))]
        public partial bool UseCustomImage { get; set; }

        [ObservableProperty]
        public partial string? CustomDiscordApplicationId { get; set; }

        [ObservableProperty]
        public partial string? CustomImageKey { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DiscordRunningVisibility))]
        [NotifyPropertyChangedFor(nameof(DiscordNotRunningVisibility))]
        public partial bool IsDiscordRunning { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NikkeRunningVisibility))]
        [NotifyPropertyChangedFor(nameof(NikkeNotRunningVisibility))]
        public partial bool IsNikkeRunning { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProTipVisibility))]
        [NotifyPropertyChangedFor(nameof(ProTipExpandVisibility))]
        public partial bool ShowProTip { get; set; } = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProTipVisibility))]
        [NotifyPropertyChangedFor(nameof(ProTipExpandVisibility))]
        public partial bool IsStatusBarOpen { get; set; }

        public Visibility DiscordRunningVisibility => IsDiscordRunning ? Visibility.Visible : Visibility.Collapsed;
        public Visibility DiscordNotRunningVisibility => IsDiscordRunning ? Visibility.Collapsed : Visibility.Visible;
        public Visibility NikkeRunningVisibility => IsNikkeRunning ? Visibility.Visible : Visibility.Collapsed;
        public Visibility NikkeNotRunningVisibility => IsNikkeRunning ? Visibility.Collapsed : Visibility.Visible;
        public Visibility GameProvidedImageVisibility => UseCustomImage ? Visibility.Collapsed : Visibility.Visible;
        public Visibility CustomImageFieldsVisibility => UseCustomImage ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ProTipVisibility => ShowProTip && !IsStatusBarOpen ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ProTipExpandVisibility => !ShowProTip && !IsStatusBarOpen ? Visibility.Visible : Visibility.Collapsed;


        // Start & Stop Button
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartRpcCommand), nameof(StopRpcCommand))]
        public partial bool IsRpcRunning { get; set; } = false;

        public bool CanStartRpc() => !IsRpcRunning;
        public bool CanStopRpc() => IsRpcRunning;

        [RelayCommand(CanExecute = nameof(CanStartRpc))]
        public void StartRpc()
        {
            try
            {
                if (UseCustomImage && !HasValidCustomImageValues())
                {
                    ShowStatusEvent?.Invoke("Missing Values", "Custom image requires Discord App ID and Image Key.", InfoBarSeverity.Warning);
                    return;
                }

                _richPresenceService.StartRichPresence(CreateRichPresencePayload());
                IsRpcRunning = true;
                _configService.Update(config => config.IsRpcEnabled = true);
                ShowStatusEvent?.Invoke("Success", "Discord RPC is now active!", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                IsRpcRunning = false;
                _configService.Update(config => config.IsRpcEnabled = false);
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                ShowStatusEvent?.Invoke("Failed", "Failed to enable discord rich presence!", InfoBarSeverity.Error);

            }
        }

        [RelayCommand]
        public void CloseProTip()
        {
            ShowProTip = false;
            _configSaveTimer.Stop();
            _configSaveTimer.Start();
        }

        [RelayCommand]
        public void OpenProTip()
        {
            ShowProTip = true;
            _configSaveTimer.Stop();
            _configSaveTimer.Start();
        }

        [RelayCommand(CanExecute = nameof(CanStopRpc))]
        public void StopRpc()
        {
            try
            {
                _richPresenceService.StopRichPresence();
                IsRpcRunning = false;
                _configService.Update(config => config.IsRpcEnabled = false);
                ShowStatusEvent?.Invoke("Success", "Discord RPC has been stopped!", InfoBarSeverity.Success);
            }
            catch (Exception)
            {
                ShowStatusEvent?.Invoke("Failed", "Failed to disable discord rich presence!", InfoBarSeverity.Error);

            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedImage) ||  e.PropertyName == nameof(Details) 
                    || e.PropertyName == nameof(State)
                    || e.PropertyName == nameof(UseCustomImage)
                    || e.PropertyName == nameof(CustomDiscordApplicationId)
                    || e.PropertyName == nameof(CustomImageKey)
                    || e.PropertyName == nameof(ShowProTip))
            {
                _configSaveTimer.Stop();
                _configSaveTimer.Start();
            }
        }

        private void OnConfigSaveTimerTick(object? sender, object e)
        {
            if (IsRpcRunning && (!UseCustomImage || HasValidCustomImageValues()))
            {
                _richPresenceService.UpdateRichPresence(CreateRichPresencePayload());
            }
            _configService.Update(config =>
            {
                config.RichPresence ??= new RichPresence();

                config.RichPresence?.ImageKey = SelectedImage?.Key;
                config.RichPresence?.Details = Details;
                config.RichPresence?.State = State;
                config.RichPresence?.UseCustomImage = UseCustomImage;
                config.RichPresence?.CustomDiscordApplicationId = CustomDiscordApplicationId;
                config.RichPresence?.CustomImageKey = CustomImageKey;
                config.ShowProTip = ShowProTip;
            });
        }

        private RichPresence CreateRichPresencePayload()
        {
            string imageKey = UseCustomImage
                ? (CustomImageKey?.Trim() ?? string.Empty)
                : (SelectedImage?.Key ?? ImageSelection[0].Key);

            return new RichPresence
            {
                Details = Details ?? string.Empty,
                State = State ?? string.Empty,
                ImageKey = imageKey,
                UseCustomImage = UseCustomImage,
                CustomDiscordApplicationId = CustomDiscordApplicationId?.Trim(),
                CustomImageKey = CustomImageKey?.Trim(),
            };
        }

        private bool HasValidCustomImageValues()
        {
            return !string.IsNullOrWhiteSpace(CustomDiscordApplicationId)
                && !string.IsNullOrWhiteSpace(CustomImageKey);
        }

        private void RestoreRpcState()
        {
            if (!_configService.CurrentConfig.IsRpcEnabled)
            {
                return;
            }

            try
            {
                if (UseCustomImage && !HasValidCustomImageValues())
                {
                    _configService.Update(config => config.IsRpcEnabled = false);
                    return;
                }

                _richPresenceService.StartRichPresence(CreateRichPresencePayload());
                IsRpcRunning = true;
            }
            catch
            {
                IsRpcRunning = false;
                _configService.Update(config => config.IsRpcEnabled = false);
            }
        }

        private void OnDiscordIdentityChanged(string? username, string? avatarUrl)
        {
            void Apply()
            {
                DiscordUsername = string.IsNullOrWhiteSpace(username) ? "Not connected" : username;
                DiscordAvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? DefaultDiscordAvatarUrl : avatarUrl;
            }

            if (_dispatcherQueue.HasThreadAccess)
            {
                Apply();
            }
            else
            {
                _dispatcherQueue.TryEnqueue(Apply);
            }
        }

        private void OnProcessStatusChanged(bool isDiscordRunning, bool isNikkeRunning)
        {
            void Apply()
            {
                IsDiscordRunning = isDiscordRunning;
                IsNikkeRunning = isNikkeRunning;
            }

            if (_dispatcherQueue.HasThreadAccess)
            {
                Apply();
            }
            else
            {
                _dispatcherQueue.TryEnqueue(Apply);
            }
        }
    }
}

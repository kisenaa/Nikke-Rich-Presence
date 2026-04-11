using DiscordRPC;
using NikkeDiscordRPC.Models; // Assuming your custom model is here
using System.Diagnostics;

namespace NikkeDiscordRPC.Services
{
    public class RichPresenceService : IRichPresenceService
    {
        private const string DefaultDiscordAvatarUrl = "https://cdn.discordapp.com/embed/avatars/0.png";

        private DiscordRpcClient? _client;
        private DiscordRPC.RichPresence? _currentPresence;

        private bool _isEnabled = true;
        private string _applicationId = IRichPresenceService.DISCORD_APPLICATION_ID;
        private readonly IConfigService _configService;
        private CancellationTokenSource? _cts;
        private Task? _workerTask;

        private readonly Lock _clientLock = new Lock();
        private bool? _lastDiscordRunning;
        private bool? _lastNikkeRunning;

        public event Action<string?, string?>? DiscordIdentityChanged;
        public event Action<bool, bool>? ProcessStatusChanged;

        public (bool IsDiscordRunning, bool IsNikkeRunning) GetCurrentProcessStatus()
        {
            if (_lastDiscordRunning is null || _lastNikkeRunning is null)
            {
                GetRunningStates(out bool currentDiscordRunning, out bool currentNikkeRunning);
                _lastDiscordRunning = currentDiscordRunning;
                _lastNikkeRunning = currentNikkeRunning;
            }

            bool isDiscordRunning = _lastDiscordRunning ?? false;
            bool isNikkeRunning = _lastNikkeRunning ?? false;
            return (isDiscordRunning, isNikkeRunning);
        }

        public RichPresenceService(IConfigService configService)
        {
            _configService = configService;
            _cts = new CancellationTokenSource();
            _workerTask = Task.Run(() => ConnectionWorkerLoop(_cts.Token));
        }

        private void InitClient()
        {
            DiscordRpcClient client;
            string applicationId;
            lock (_clientLock)
            {
                if (_client != null) return;
                applicationId = _applicationId;
                client = new DiscordRpcClient(applicationId);
                _client = client;
            }

            client.OnReady += (sender, msg) =>
            {
                Debug.WriteLine($"Connected to discord with user: {msg.User.Username}");

                string username = BuildDiscordUsername(msg.User);
                string avatarUrl = BuildDiscordAvatarUrl(msg.User);
                DiscordIdentityChanged?.Invoke(username, avatarUrl);

                DiscordRPC.RichPresence? presenceToApply = null;
                lock (_clientLock)
                {
                    if (_isEnabled && ReferenceEquals(_client, client) && _currentPresence != null)
                    {
                        presenceToApply = _currentPresence;
                    }
                }

                if (presenceToApply != null)
                {
                    client.SetPresence(presenceToApply);
                }
            };

            client.OnClose += (sender, msg) =>
            {
                Debug.WriteLine("Discord closed. Awaiting reconnect...");
                DisposeClient(client);
            };

            client.OnConnectionFailed += (sender, msg) =>
            {
                Debug.WriteLine($"Failed to connect to discord: {msg.Type}");
                DisposeClient(client);
            };

            client.OnError += (sender, msg) =>
            {
                Debug.WriteLine($"Error occurred: {msg.Message}");
            };

            client.Initialize();
        }

        private void DisposeClient(DiscordRpcClient? expectedClient = null, bool waitClearPresence = false)
        {
            DiscordRpcClient? clientToDispose;
            lock (_clientLock)
            {
                if (_client == null)
                {
                    return;
                }

                if (expectedClient != null && !ReferenceEquals(_client, expectedClient))
                {
                    return;
                }

                clientToDispose = _client;
                _client = null;
            }

            try
            {
                if (clientToDispose.IsInitialized)
                {
                    clientToDispose.ClearPresence();

                    if (waitClearPresence)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            if (clientToDispose.CurrentPresence == null || String.IsNullOrEmpty(clientToDispose.CurrentPresence.State))
                            {
                                break;
                            }
                            clientToDispose.ClearPresence();
                            Thread.Sleep(1000);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to clear presence during dispose: {ex.Message}");
            }
            finally
            {
                clientToDispose.Dispose();
            }
        }

        public bool StopRichPresence()
        {
            _isEnabled = false;
            DisposeClient();
            return true;
        }

        public bool StartRichPresence(Models.RichPresence? richPresence)
        {
            _isEnabled = true;
            return UpdateRichPresence(richPresence);
        }

        public bool UpdateRichPresence(Models.RichPresence? richPresence)
        {
            string requestedApplicationId = string.IsNullOrWhiteSpace(richPresence?.DiscordApplicationId)
                ? IRichPresenceService.DISCORD_APPLICATION_ID
                : richPresence.DiscordApplicationId.Trim();

            bool shouldReinitializeClient = false;
            lock (_clientLock)
            {
                if (!string.Equals(_applicationId, requestedApplicationId, StringComparison.Ordinal))
                {
                    _applicationId = requestedApplicationId;
                    shouldReinitializeClient = _client != null;
                }
            }

            if (shouldReinitializeClient)
            {
                DisposeClient();
            }

            if (richPresence != null)
            {
                _currentPresence = new DiscordRPC.RichPresence()
                {
                    Details = richPresence.Details,
                    State = richPresence.State,
                    Assets = new Assets()
                    {
                        LargeImageKey = richPresence.ImageKey
                    }
                };
            }
            else
            {
                _currentPresence = null;
            }

            if (!_isEnabled)
            {
                return true;
            }

            lock (_clientLock)
            {
                if (_client != null && _client.IsInitialized)
                {
                    if (_currentPresence == null)
                        _client.ClearPresence();
                    else
                        _client.SetPresence(_currentPresence);
                }
            }

            return true;
        }

        private async Task ConnectionWorkerLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    GetRunningStates(out bool isDiscordRunning, out bool isNikkeRunning);
                    NotifyProcessStatusChanged(isDiscordRunning, isNikkeRunning);

                    if (_isEnabled)
                    {
                        bool hasClient;
                        lock (_clientLock)
                        {
                            hasClient = _client != null;
                        }

                        if (isDiscordRunning && isNikkeRunning && !hasClient)
                        {
                            InitClient();
                        }
                        else if ((!isDiscordRunning || !isNikkeRunning) && hasClient)
                        {
                            DisposeClient(null, isDiscordRunning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Worker loop error: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(GetPollingIntervalSeconds()), token);
            }
        }

        private int GetPollingIntervalSeconds()
        {
            return _configService.CurrentConfig.PollingIntervalSeconds;
        }

        private void NotifyProcessStatusChanged(bool isDiscordRunning, bool isNikkeRunning)
        {
            if (_lastDiscordRunning == isDiscordRunning && _lastNikkeRunning == isNikkeRunning)
            {
                return;
            }

            _lastDiscordRunning = isDiscordRunning;
            _lastNikkeRunning = isNikkeRunning;
            ProcessStatusChanged?.Invoke(isDiscordRunning, isNikkeRunning);
        }

        private static void GetRunningStates(out bool isDiscordRunning, out bool isNikkeRunning)
        {
            isDiscordRunning = false;
            isNikkeRunning = false;

            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    string processName = process.ProcessName;

                    if (!isDiscordRunning &&
                        (processName.Equals("Discord", StringComparison.OrdinalIgnoreCase)
                        || processName.Equals("DiscordPTB", StringComparison.OrdinalIgnoreCase)
                        || processName.Equals("DiscordCanary", StringComparison.OrdinalIgnoreCase)))
                    {
                        isDiscordRunning = true;
                    }

                    if (!isNikkeRunning && processName.Equals("Nikke", StringComparison.OrdinalIgnoreCase))
                    {
                        isNikkeRunning = true;
                    }

                    if (isDiscordRunning && isNikkeRunning)
                    {
                        return;
                    }
                }
                catch
                {
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            DisposeClient();
        }

        private static string BuildDiscordUsername(object? user)
        {
            if (user == null)
            {
                return "Not connected";
            }

            Type userType = user.GetType();
            string? username = userType.GetProperty("Username")?.GetValue(user)?.ToString();
            string? discriminator = userType.GetProperty("Discriminator")?.GetValue(user)?.ToString();

            if (string.IsNullOrWhiteSpace(username))
            {
                return "Not connected";
            }

            if (!string.IsNullOrWhiteSpace(discriminator) && discriminator != "0")
            {
                return $"{username}#{discriminator}";
            }

            return username;
        }

        private static string BuildDiscordAvatarUrl(object? user)
        {
            if (user == null)
            {
                return DefaultDiscordAvatarUrl;
            }

            Type userType = user.GetType();
            string? avatarHash = userType.GetProperty("Avatar")?.GetValue(user)?.ToString();
            string? id = userType.GetProperty("ID")?.GetValue(user)?.ToString();

            if (!string.IsNullOrWhiteSpace(avatarHash) && !string.IsNullOrWhiteSpace(id))
            {
                return $"https://cdn.discordapp.com/avatars/{id}/{avatarHash}.webp?size=128";
            }

            return DefaultDiscordAvatarUrl;
        }
    }
}
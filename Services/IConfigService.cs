using NikkeDiscordRPC.Models;

namespace NikkeDiscordRPC.Services
{
    public interface IConfigService
    {
        public AppConfig CurrentConfig { get; }

        public event Action? OnConfigReset;

        public void Update(Action<AppConfig> updateAction);
        public void SaveToDisk();
        public void ResetToDefaults();
    }
}
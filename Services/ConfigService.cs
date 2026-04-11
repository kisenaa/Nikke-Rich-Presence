using NikkeDiscordRPC.Models;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NikkeDiscordRPC.Services
{
    [JsonSerializable(typeof(AppConfig))]
    [JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
    public partial class AppConfigContext: JsonSerializerContext
    {
    }

    public class ConfigService: IConfigService
    {
        private readonly string _filePath;
        private readonly Lock _fileLock = new();
        private bool _isResetting;
        public AppConfig CurrentConfig { get; private set; }

        public event Action? OnConfigReset;

        public ConfigService(string? filePath = null)
        {
            string basePath;

            if (DetectPackagedState())
            {
                basePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NikkeDiscordRpc");
            }
            else
            {
                basePath = AppDomain.CurrentDomain.BaseDirectory;
            }

            _filePath = filePath ?? Path.Combine(basePath, "config.json");
            CurrentConfig = LoadFromDisk();
        }

        private static bool DetectPackagedState()
        {
            try
            {
                _ = Windows.ApplicationModel.Package.Current;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Update(Action<AppConfig> updateAction)
        {
            lock (_fileLock)
            {
                if (_isResetting)
                    return;

                updateAction(CurrentConfig);
                SaveToDiskUnsafe();
            }
        }

        private AppConfig LoadFromDisk()
        {
            lock (_fileLock)
            {
                if (!File.Exists(_filePath)) return new AppConfig();

                try
                {
                    string json = File.ReadAllText(_filePath);

                    AppConfig? config = JsonSerializer.Deserialize(json, AppConfigContext.Default.AppConfig);
                    return config ?? new AppConfig();
                }
                catch
                {
                    return new AppConfig();
                }
            }
        }

        public void SaveToDisk()
        {
            lock (_fileLock)
            {
                SaveToDiskUnsafe();
            }
        }

        public void ResetToDefaults()
        {
            lock (_fileLock)
            {
                _isResetting = true;
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }

                CurrentConfig = new AppConfig();
                OnConfigReset?.Invoke();
            }
        }

        private void SaveToDiskUnsafe()
        {
            string? dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(CurrentConfig, AppConfigContext.Default.AppConfig);
            File.WriteAllText(_filePath, json);
        }
    }
}
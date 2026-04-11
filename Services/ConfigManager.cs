using System.Text.Json;
using System.Text.Json.Nodes;

namespace NikkeDiscordRPC.Services
{
    public class ConfigManager(string? filePath = null) : IConfigManager
    {
        private readonly string _filePath = filePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        private readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = false };

        /// <summary>
        /// Loads the entire configuration file into a specific type.
        /// </summary>
        public T? LoadConfig<T>()
        {
            if (!File.Exists(_filePath)) return default;

            string json = File.ReadAllText(_filePath);

            // Handle empty files gracefully
            if (string.IsNullOrWhiteSpace(json)) return default;

            return JsonSerializer.Deserialize<T>(json);
        }

        /// <summary>
        /// Gets a specific value by key from the JSON file.
        /// </summary>
        public T? GetConfigValue<T>(string key)
        {
            if (!File.Exists(_filePath)) return default;

            string json = File.ReadAllText(_filePath);
            var node = JsonNode.Parse(json);

            // Check if root is an object and contains the key
            if (node is JsonObject obj && obj.TryGetPropertyValue(key, out JsonNode? valueNode))
            {
                // Deserialize just that specific node
                return valueNode.Deserialize<T>();
            }

            return default;
        }

        /// <summary>
        /// Updates or adds a specific key-value pair to the JSON file.
        /// </summary>
        public void SetConfigValue<T>(string key, T value)
        {
            JsonObject rootObject;

            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                try
                {
                    // Parse existing JSON. If null or not an object, start fresh.
                    var node = JsonNode.Parse(json);
                    rootObject = node as JsonObject ?? [];
                }
                catch (JsonException)
                {
                    rootObject = [];
                }
            }
            else
            {
                rootObject = [];
            }

            // Update or Add the value
            rootObject[key] = JsonSerializer.SerializeToNode(value);

            // Ensure directory exists first (in case path is in a subfolder)
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_filePath, rootObject.ToJsonString(_writeOptions));
        }
    }
}
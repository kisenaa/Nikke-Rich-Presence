using System.Text.Json.Serialization;

namespace NikkeDiscordRPC.Models
{
    public class RichPresence
    {
        public string? Details { get; set; }
        public string? State { get; set; }
        public string? ImageKey { get; set; }
        public bool UseCustomImage { get; set; }
        public string? CustomDiscordApplicationId { get; set; }
        public string? CustomImageKey { get; set; }

        [JsonIgnore]
        public string? DiscordApplicationId => UseCustomImage ? CustomDiscordApplicationId : null;
    }
}

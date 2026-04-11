using NikkeDiscordRPC.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NikkeDiscordRPC.Services
{
    public interface IRichPresenceService
    {
        public static readonly string DISCORD_APPLICATION_ID = "1196659687970586676";
        public event Action<string?, string?>? DiscordIdentityChanged;
        public event Action<bool, bool>? ProcessStatusChanged;
        public (bool IsDiscordRunning, bool IsNikkeRunning) GetCurrentProcessStatus();
        public bool StartRichPresence(RichPresence? richPresence);
        public bool UpdateRichPresence(RichPresence? richPresence);
        public bool StopRichPresence();
    }
}

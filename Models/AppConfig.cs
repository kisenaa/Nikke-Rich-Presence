using System;
using System.Collections.Generic;
using System.Text;

namespace NikkeDiscordRPC.Models
{
    public class AppConfig
    {
        public RichPresence RichPresence { get; set; } = new RichPresence();
        public bool IsRpcEnabled { get; set; }
        public string ProcessPriority { get; set; } = "Normal";
        public int PollingIntervalSeconds { get; set; } = 4;
        public bool ShowProTip { get; set; } = true;
        public string MinimizeBehavior { get; set; } = "To Tray";
        public string ExitBehavior { get; set; } = "Minimize to Tray";
        public bool AutoLaunchOnBoot { get; set; } = true;
        public bool ForegroundHotkeyEnabled { get; set; } = true;
        public string ForegroundHotkey { get; set; } = "Ctrl + Shift + Alt + N";
        public string? NikkeLauncherPath { get; set; }
    }
}

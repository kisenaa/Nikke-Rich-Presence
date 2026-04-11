using System;
using System.Collections.Generic;
using System.Text;

namespace NikkeDiscordRPC.Services
{
    public interface IConfigManager
    {
        public T? LoadConfig<T>();
        public T? GetConfigValue<T>(string key);
        public void SetConfigValue<T>(string key, T value);
    }
}

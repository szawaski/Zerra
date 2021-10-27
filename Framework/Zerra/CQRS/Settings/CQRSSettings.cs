// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Text;
using Zerra.Logging;
using Zerra.Serialization;

namespace Zerra.CQRS.Settings
{
    public static class CQRSSettings
    {
        public const string SettingsFileName = "cqrssettings.json";

        public static ServiceSettings Get(string path = null)
        {
            var filePath = Config.GetConfigFile(SettingsFileName, path);
            if (filePath == null)
            {
                Log.InfoAsync($"{filePath} not found").GetAwaiter().GetResult();
                throw new Exception($"{filePath} not found");
            }

            ServiceSettings settings;
            try
            {
                var data = File.ReadAllText(filePath, Encoding.UTF8);
                settings = JsonSerializer.Deserialize<ServiceSettings>(data);
            }
            catch (Exception ex)
            {
                Log.InfoAsync($"Invalid {filePath}").GetAwaiter().GetResult();
                throw new Exception($"Invalid {filePath}", ex);
            }
            _ = Log.InfoAsync($"Loaded {filePath}");

            var messageHost = Config.GetSetting(nameof(settings.MessageHost));
            if (!String.IsNullOrWhiteSpace(messageHost))
                settings.MessageHost = messageHost;
            _ = Log.InfoAsync($"Set {nameof(settings.MessageHost)} at {messageHost}");

            var relayUrl = Config.GetSetting(nameof(settings.RelayUrl));
            if (!String.IsNullOrWhiteSpace(relayUrl))
                settings.RelayUrl = relayUrl;
            _ = Log.InfoAsync($"Set {nameof(settings.RelayUrl)} at {relayUrl}");

            var relayKey = Config.GetSetting(nameof(settings.RelayKey));
            if (!String.IsNullOrWhiteSpace(relayUrl))
                settings.RelayKey = relayKey;
            //_ = Log.InfoAsync($"Set {nameof(settings.RelayKey)} at {relayKey}"); security don't display

            foreach (var service in settings.Services)
            {
                var url = Config.GetSetting(service.Name);
                if (!String.IsNullOrWhiteSpace(url))
                    service.ExternalUrl = url;
                _ = Log.InfoAsync($"Set {service.Name} at {service.ExternalUrl}");
            }

            return settings;
        }
    }
}

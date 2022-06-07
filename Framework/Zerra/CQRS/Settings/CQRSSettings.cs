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

        public static ServiceSettings Get()
        {
            var filePath = Config.GetEnvironmentFilePath(SettingsFileName);
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
            if (!String.IsNullOrWhiteSpace(settings.MessageHost))
                _ = Log.InfoAsync($"Set {nameof(settings.MessageHost)} at {settings.MessageHost}");

            var relayUrl = Config.GetSetting(nameof(settings.RelayUrl));
            if (!String.IsNullOrWhiteSpace(relayUrl))
                settings.RelayUrl = relayUrl;
            if (!String.IsNullOrWhiteSpace(settings.RelayUrl))
                _ = Log.InfoAsync($"Set {nameof(settings.RelayUrl)} at {settings.RelayUrl}");

            var relayKey = Config.GetSetting(nameof(settings.RelayKey));
            if (!String.IsNullOrWhiteSpace(relayUrl))
                settings.RelayKey = relayKey;

            foreach (var service in settings.Services)
            {
                var url = Config.GetSetting(service.Name);
                if (!String.IsNullOrWhiteSpace(url))
                    service.ExternalUrl = url;
                if (!String.IsNullOrWhiteSpace(service.ExternalUrl))
                    _ = Log.InfoAsync($"Set {service.Name} at {service.ExternalUrl}");
            }

            return settings;
        }
    }
}

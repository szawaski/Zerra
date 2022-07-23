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
        private const string settingsFileName = "cqrssettings.json";
        private const string genericSettingsFileName = "cqrssettings.{0}.json";

        public static ServiceSettings Get(string environmentName = null)
        {
            string filePath = null;

            if (String.IsNullOrWhiteSpace(environmentName))
                environmentName = Config.EnvironmentName;

            if (!String.IsNullOrWhiteSpace(environmentName))
                filePath = Config.GetEnvironmentFilePath(String.Format(genericSettingsFileName, environmentName));
            if (filePath == null)
                filePath = Config.GetEnvironmentFilePath(settingsFileName);

            if (filePath == null)
            {
                Log.InfoAsync($"{settingsFileName} not found").GetAwaiter().GetResult();
                throw new Exception($"{settingsFileName} not found");
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
                service.InternalUrl = Config.GetInternalUrl(service.InternalUrl);
                service.ExternalUrl = Config.GetExternalUrl(service.Name, service.ExternalUrl);

                //docker notation externalport:internalport
                _ = Log.InfoAsync($"Set {service.Name} at External {(String.IsNullOrWhiteSpace(service.ExternalUrl) ? "?" : service.ExternalUrl)} to Internal {service.InternalUrl} ");
            }

            return settings;
        }
    }
}

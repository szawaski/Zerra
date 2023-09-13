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

        public static ServiceSettings Get(string serviceName)
        {
            string filePath = null;

            var environmentName = Config.EnvironmentName;

            if (!String.IsNullOrWhiteSpace(environmentName))
                filePath = Config.GetEnvironmentFilePath(String.Format(genericSettingsFileName, environmentName));
            filePath ??= Config.GetEnvironmentFilePath(settingsFileName);

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

            _ = Log.InfoAsync($"{nameof(CQRSSettings)} Loaded {filePath}");

            foreach (var service in settings.Services)
            {
                if (service.Name == serviceName)
                    service.InternalUrl = Config.GetInternalUrl(service.InternalUrl);
                service.ExternalUrl = Config.GetExternalUrl(service.Name, service.ExternalUrl);

                if (!String.IsNullOrWhiteSpace(service.ExternalUrl))
                {
                    if (service.Name == serviceName)
                        _ = Log.InfoAsync($"Hosting {service.Name} at {service.ExternalUrl} (Internal {service.InternalUrl})");
                    else
                        _ = Log.InfoAsync($"Set {service.Name} at {service.ExternalUrl}");
                }
            }

            settings.ThisServiceName = serviceName;

            return settings;
        }
    }
}

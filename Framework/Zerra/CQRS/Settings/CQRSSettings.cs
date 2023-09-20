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

        public static ServiceSettings Get(string serviceName, bool bindingUrlFromConfig)
        {
            _ = Log.InfoAsync($"Configuring {serviceName}");

            string filePath = null;

            var environmentName = Config.EnvironmentName;

            if (!String.IsNullOrWhiteSpace(environmentName))
                filePath = Config.GetEnvironmentFilePath(String.Format(genericSettingsFileName, environmentName));
            filePath ??= Config.GetEnvironmentFilePath(settingsFileName);

            if (filePath == null)
            {
                var notFound = $"{serviceName} did not find {settingsFileName}";
                Log.InfoAsync(notFound).GetAwaiter().GetResult();
                throw new Exception(notFound);
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
         
            _ = Log.InfoAsync($"{serviceName} Loaded {filePath}");

            foreach (var service in settings.Services)
            {
                var loadedBindingUrl = false;
                if (bindingUrlFromConfig && service.Name == serviceName)
                {
                    var newBindinglUrl = Config.GetBindingUrl(service.BindingUrl);
                    if (newBindinglUrl != service.BindingUrl)
                    {
                        loadedBindingUrl = true;
                        service.BindingUrl = newBindinglUrl;
                    }
                }

                var loadedExternalUrl = false;
                var newExternalUrl = Config.GetExternalUrl(service.Name, service.ExternalUrl);
                if (newExternalUrl != service.ExternalUrl)
                {
                    service.ExternalUrl = newExternalUrl;
                    loadedExternalUrl = true;
                }

                if (!String.IsNullOrWhiteSpace(service.ExternalUrl))
                {
                    if (service.Name == serviceName)
                        _ = Log.InfoAsync($"Hosting {service.Name} at {service.ExternalUrl}{(loadedExternalUrl ? " (from Config)" : null)} Binding {service.BindingUrl}{(loadedBindingUrl ? " (from Config)" : null)}");
                    else
                        _ = Log.InfoAsync($"Set {service.Name} at {service.ExternalUrl}{(loadedExternalUrl ? " (from Config)" : null)}");
                }
            }

            settings.ThisServiceName = serviceName;

            return settings;
        }
    }
}

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

        private const string bindingUrl0 = nameof(ServiceSetting.BindingUrl);
        private const string bindingUrl1 = "urls";
        private const string bindingUrl2 = "ASPNETCORE_URLS";
        private const string bindingUrl3 = "ASPNETCORE_SERVER.URLS";
        private const string bindingUrl4 = "DOTNET_URLS"; //docker

        private const string bindingUrl5_azureSiteName = "WEBSITE_SITE_NAME";
        private const string bindingUrl5_azureSiteUrls = "http://{0}:80;https://{0}:443";

        private const string bindingUrlDefault = "http://localhost:5000;https://localhost:5001";

        public static ServiceSettings Get(bool bindingUrlFromStandardVariables)
        {
            var settingsName = Config.EntryAssemblyName;
            if (settingsName == null)
                throw new Exception($"Entry Assembly is null, {nameof(CQRSSettings)} cannot identify which service is running");
            return Get(settingsName, bindingUrlFromStandardVariables);
        }
        public static ServiceSettings Get(string serviceName, bool bindingUrlFromStandardVariables)
        {
            _ = Log.InfoAsync($"Configuring {serviceName}");

            var environmentName = Config.EnvironmentName;

            string filePath = null;
            string fileName = null;
            if (!String.IsNullOrWhiteSpace(environmentName))
            {
                fileName = String.Format(genericSettingsFileName, environmentName);
                filePath = Config.GetEnvironmentFilePath(fileName);
            }
            if (filePath == null)
            {
                fileName = settingsFileName;
                filePath = Config.GetEnvironmentFilePath(fileName);
            }
            if (filePath == null || fileName == null)
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

            _ = Log.InfoAsync($"{serviceName} Loaded {fileName}");

            foreach (var service in settings.Services)
            {
                if (service.Name == serviceName)
                {
                    if (GetBindingUrl(service.Name, service.BindingUrl, bindingUrlFromStandardVariables, out var newUrl, out var urlSource))
                    {
                        service.BindingUrl = newUrl;
                        if (!String.IsNullOrWhiteSpace(service.BindingUrl))
                            _ = Log.InfoAsync($"Hosting {service.Name} at {service.BindingUrl} (from {urlSource})");
                    }
                    else
                    {
                        if (!String.IsNullOrWhiteSpace(service.BindingUrl))
                            _ = Log.InfoAsync($"Hosting {service.Name} at {service.BindingUrl} (from {fileName})");
                    }
                }
                else
                {
                    if (GetExternalUrl(service.Name, service.ExternalUrl, out var newUrl, out var urlSource))
                    {
                        service.ExternalUrl = newUrl;
                        if (!String.IsNullOrWhiteSpace(service.ExternalUrl))
                            _ = Log.InfoAsync($"Set {service.Name} at {service.ExternalUrl} (from {urlSource})");
                    }
                    else
                    {
                        if (!String.IsNullOrWhiteSpace(service.ExternalUrl))
                            _ = Log.InfoAsync($"Set {service.Name} at {service.ExternalUrl} (from {fileName})");
                    }
                }
            }

            settings.ThisServiceName = serviceName;

            return settings;
        }

        public static bool GetBindingUrl(string settingName, string defaultUrl, bool useStandardVariables, out string url, out string urlSource)
        {
            urlSource = bindingUrl0;
            url = Config.GetSetting(bindingUrl0);
            if (!String.IsNullOrWhiteSpace(url))
                return true;

            urlSource = settingName;
            url = Config.GetSetting(settingName);
            if (!String.IsNullOrWhiteSpace(url))
                return true;

            if (useStandardVariables)
            {
                urlSource = bindingUrl1;
                url = Config.GetSetting(bindingUrl1);
                if (!String.IsNullOrWhiteSpace(url))
                    return true;

                urlSource = bindingUrl2;
                url = Config.GetSetting(bindingUrl2);
                if (!String.IsNullOrWhiteSpace(url))
                    return true;

                urlSource = bindingUrl3;
                url = Config.GetSetting(bindingUrl3);
                if (!String.IsNullOrWhiteSpace(url))
                    return true;

                urlSource = bindingUrl4;
                url = Config.GetSetting(bindingUrl4);
                if (!String.IsNullOrWhiteSpace(url))
                    return true;
            }

            urlSource = null;
            url = defaultUrl;
            if (!String.IsNullOrWhiteSpace(url))
                return false;

            if (useStandardVariables)
            {
                urlSource = bindingUrl5_azureSiteName;
                var siteName = Config.GetSetting(bindingUrl5_azureSiteName);
                if (!String.IsNullOrWhiteSpace(siteName))
                    url = String.Format(bindingUrl5_azureSiteUrls, siteName);
                if (!String.IsNullOrWhiteSpace(url))
                    return true;
            }

            urlSource = null;
            url = bindingUrlDefault;
            return false;
        }

        public static bool GetExternalUrl(string settingName, string defaultUrl, out string url, out string urlSource)
        {
            urlSource = settingName;
            url = Config.GetSetting(settingName);
            if (!String.IsNullOrWhiteSpace(url))
                return true;

            urlSource = null;
            url = defaultUrl;
            if (!String.IsNullOrWhiteSpace(url))
                return false;

            urlSource = null;
            url = null;
            return false;
        }
    }
}

// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Text;
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
                throw new Exception($"{SettingsFileName} not found");

            try
            {
                var data = File.ReadAllText(filePath, Encoding.UTF8);
                var settings = JsonSerializer.Deserialize<ServiceSettings>(data);
                Console.WriteLine($"Loaded {filePath}");
                return settings;
            }
            catch (Exception ex)
            {
                throw new Exception($"Invalid {SettingsFileName}", ex);
            }
        }
    }
}

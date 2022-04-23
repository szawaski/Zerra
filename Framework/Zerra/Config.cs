// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;
using Zerra.Encryption;

namespace Zerra
{
    public static class Config
    {
        private static IConfiguration configuration;
        public static void LoadConfiguration(string[] commandLineArgs = null, string encryptionKey = null, string path = null)
        {
            var builder = new ConfigurationBuilder();

            var data = ReadAppSettingsFile(path);
            if (data != null)
            {
                if (!String.IsNullOrWhiteSpace(encryptionKey) && !IsEncrypted(data))
                {
                    data = EncryptToString(encryptionKey, data);
                    WriteAppSettingsFile(data, path);
                }
                var jsonStream = DecryptToStream(encryptionKey, data);
                builder.AddJsonStream(jsonStream);
            }

            builder.AddEnvironmentVariables();

            if (commandLineArgs != null && commandLineArgs.Length > 0)
                builder.AddCommandLine(commandLineArgs);

            configuration = builder.Build();
        }
        public static void LoadConfiguration(IConfiguration configuration)
        {
            Config.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public static string GetSetting(string name, params string[] sections)
        {
            if (configuration == null) 
                LoadConfiguration();
            IConfiguration config = configuration;
            if (sections != null && sections.Length > 0)
            {
                foreach (var section in sections)
                {
                    config = config.GetSection(section);
                    if (config == null) throw new Exception($"Config section {section} not found");
                }
            }
            var value = config[name];
            return value;
        }

        public static IConfiguration GetConfiguration()
        {
            if (configuration == null)
                LoadConfiguration();
            return configuration;
        }
        public static T Bind<T>(params string[] sections)
        {
            if (configuration == null)
                LoadConfiguration();
            IConfiguration config = configuration;
            if (sections != null && sections.Length > 0)
            {
                foreach (var section in sections)
                {
                    config = config.GetSection(section);
                    if (config == null) throw new Exception($"Config section {section} not found");
                }
            }
            var value = config.Get<T>();
            return value;
        }

        private const string settingsFileName = "appsettings.json";
        private static string ReadAppSettingsFile(string path)
        {
            var filePath = GetConfigFile(settingsFileName, path);
            if (filePath == null)
                return null;
            var data = File.ReadAllText(filePath, Encoding.UTF8);
            Console.WriteLine($"Loaded {filePath}");
            return data;
        }
        private static void WriteAppSettingsFile(string data, string path)
        {
            var filePath = GetConfigFile(settingsFileName, path);
            File.WriteAllText(filePath, data);
        }

        public static string GetConfigFile(string fileName, string path = null)
        {
            string filePath;
            if (!String.IsNullOrWhiteSpace(path))
            {
                if (path.EndsWith("\\") || path.EndsWith("/"))
                    filePath = path + fileName;
                else
                    filePath = $"{path}/{fileName}";

                if (File.Exists(filePath))
                    return filePath;
            }

            var executingAssemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            filePath = $"{executingAssemblyPath}/{fileName}";
            if (File.Exists(filePath))
                return filePath;

            filePath = $"{Environment.CurrentDirectory}/{fileName}";
            if (File.Exists(filePath))
                return filePath;

            return null;
        }

        private const string encryptionPrefix = "<encrypted>";
        private static bool IsEncrypted(string value)
        {
            return value.StartsWith(encryptionPrefix);
        }
        private static string EncryptToString(string key, string valueDecrypted)
        {
            var symmetricKey = SymmetricEncryptor.GetKey(key, null, SymmetricKeySize.Bits_256, SymmetricBlockSize.Bits_128);
            var valueEncrypted = SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AESwithShift, symmetricKey, valueDecrypted);
            return encryptionPrefix + valueEncrypted;
        }
        private static Stream DecryptToStream(string key, string valueEncrypted)
        {
            if (IsEncrypted(valueEncrypted))
            {
                if (key == null) throw new InvalidOperationException("Encrypted AppSettings needs a decryption key");
                valueEncrypted = valueEncrypted.Remove(0, encryptionPrefix.Length);
                var ms = new MemoryStream(Convert.FromBase64String(valueEncrypted));
                var symmetricKey = SymmetricEncryptor.GetKey(key, null, SymmetricKeySize.Bits_256, SymmetricBlockSize.Bits_128);
                var decryptionStream = SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AESwithShift, symmetricKey, ms, false, false);
                return decryptionStream;
            }
            else
            {
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(valueEncrypted));
                return ms;
            }
        }
    }
}
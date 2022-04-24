// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;

namespace Zerra
{
    public static class Config
    {
        private const string settingsFileName = "appsettings.json";
        private const string genericSettingsFileName = "appsettings.{0}.json";

        private static IConfiguration configuration;
        public static void LoadConfiguration(string[] args = null, Action<ConfigurationBuilder> build = null)
        {
            var builder = new ConfigurationBuilder();

            AddSettingsFile(builder, settingsFileName);
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (!String.IsNullOrWhiteSpace(environmentName))
            {
                var environmentSettingsFileName = String.Format(genericSettingsFileName, environmentName);
                AddSettingsFile(builder, environmentSettingsFileName);
            }
            builder.AddEnvironmentVariables();

            if (args != null && args.Length > 0)
                builder.AddCommandLine(args);

            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
                builder.AddUserSecrets(entryAssembly);

            build?.Invoke(builder);

            configuration = builder.Build();
        }
        public static void SetConfiguration(IConfiguration configuration)
        {
            Config.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        private static void AddSettingsFile(ConfigurationBuilder builder, string fileName)
        {
            var filePath = FindFilePath(fileName);
            if (filePath == null)
                return;
            var file = File.OpenRead(filePath);
            builder.AddJsonStream(file);
            Console.WriteLine($"Loaded {filePath}");
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

        public static string FindFilePath(string fileName)
        {
            var executingAssemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var filePath = $"{executingAssemblyPath}/{fileName}";
            if (File.Exists(filePath))
                return filePath;

            filePath = $"{Environment.CurrentDirectory}/{fileName}";
            if (File.Exists(filePath))
                return filePath;

            return null;
        }

        //private static void AddSettingsFile(ConfigurationBuilder builder, string fileName, string encryptionKey = null)
        //{
        //    var text = ReadAppSettingsFile(fileName);
        //    if (text == null)
        //        return;

        //    if (!String.IsNullOrWhiteSpace(encryptionKey))
        //    {
        //        if (!IsEncrypted(text))
        //        {
        //            text = EncryptToString(encryptionKey, text);
        //            WriteAppSettingsFile(fileName, text);
        //            builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(text)));
        //        }
        //        else
        //        {
        //            builder.AddJsonStream(DecryptToStream(encryptionKey, text));
        //        }
        //    }
        //    else
        //    {
        //        builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(text)));
        //    }
        //}
        //private static string ReadAppSettingsFile(string fileName)
        //{
        //    var filePath = FindFilePath(fileName);
        //    if (filePath == null)
        //        return null;
        //    var data = File.ReadAllText(filePath, Encoding.UTF8);
        //    Console.WriteLine($"Loaded {filePath}");
        //    return data;
        //}
        //private static void WriteAppSettingsFile(string fileName, string data)
        //{
        //    var filePath = FindFilePath(fileName);
        //    File.WriteAllText(filePath, data);
        //}

        //private const string encryptionPrefix = "<encrypted>";
        //private static bool IsEncrypted(string value)
        //{
        //    return value.StartsWith(encryptionPrefix);
        //}
        //private static string EncryptToString(string key, string valueDecrypted)
        //{
        //    var symmetricKey = SymmetricEncryptor.GetKey(key, null, SymmetricKeySize.Bits_256, SymmetricBlockSize.Bits_128);
        //    var valueEncrypted = SymmetricEncryptor.Encrypt(SymmetricAlgorithmType.AESwithShift, symmetricKey, valueDecrypted);
        //    return encryptionPrefix + valueEncrypted;
        //}
        //private static Stream DecryptToStream(string key, string valueEncrypted)
        //{
        //    if (key == null) throw new InvalidOperationException("Encrypted AppSettings needs a decryption key");
        //    valueEncrypted = valueEncrypted.Remove(0, encryptionPrefix.Length);
        //    var ms = new MemoryStream(Convert.FromBase64String(valueEncrypted));
        //    var symmetricKey = SymmetricEncryptor.GetKey(key, null, SymmetricKeySize.Bits_256, SymmetricBlockSize.Bits_128);
        //    var decryptionStream = SymmetricEncryptor.Decrypt(SymmetricAlgorithmType.AESwithShift, symmetricKey, ms, false, false);
        //    return decryptionStream;
        //}
    }
}
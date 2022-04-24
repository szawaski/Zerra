// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
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
            var executingAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var filePath = $"{executingAssemblyPath}/{fileName}";
            if (File.Exists(filePath))
                return filePath;

            filePath = $"{Environment.CurrentDirectory}/{fileName}";
            if (File.Exists(filePath))
                return filePath;

            return null;
        }


        private static object discoveryLock = new object();
        private static bool discoveryStarted;
        internal static bool DiscoveryStarted
        {
            get { lock (discoveryLock) { return discoveryStarted; } }
            set { lock (discoveryLock) { discoveryStarted = value; } }
        }

        internal static string[] DiscoveryNamespaces;

        private static readonly string entryNameSpace;
        private static readonly string frameworkNameSpace;
        static Config()
        {
            entryNameSpace = Assembly.GetEntryAssembly()?.GetName().Name.Split('.')[0] + '.';
            frameworkNameSpace = Assembly.GetExecutingAssembly().GetName().Name.Split('.')[0] + '.';

            if (entryNameSpace != null)
                DiscoveryNamespaces = new string[] { entryNameSpace, frameworkNameSpace };
            else
                DiscoveryNamespaces = new string[] { frameworkNameSpace };
            discoveryStarted = false;
        }

        public static void AddDiscoveryNamespaces(params string[] namespaces)
        {
            lock (discoveryLock)
            {
                if (discoveryStarted)
                    throw new InvalidOperationException("Discovery has already started");

                var newNamespaces = namespaces.Select(x => x + '.').ToArray();

                var newNamespacesToLoad = new string[DiscoveryNamespaces.Length + newNamespaces.Length];
                DiscoveryNamespaces.CopyTo(newNamespacesToLoad, 0);
                newNamespaces.CopyTo(newNamespacesToLoad, DiscoveryNamespaces.Length);
                DiscoveryNamespaces = newNamespacesToLoad;
            }
        }
        public static void AddDiscoveryAssemblies(params Assembly[] assemblies)
        {
            lock (discoveryLock)
            {
                if (discoveryStarted)
                    throw new InvalidOperationException("Discovery has already started");

                var newNamespaces = assemblies.Select(x => x.GetName().Name).ToArray();

                var newNamespacesToLoad = new string[DiscoveryNamespaces.Length + newNamespaces.Length];
                DiscoveryNamespaces.CopyTo(newNamespacesToLoad, 0);
                newNamespaces.CopyTo(newNamespacesToLoad, DiscoveryNamespaces.Length);
                DiscoveryNamespaces = newNamespacesToLoad;
            }
        }

        public static void SetDiscoveryNamespaces(params string[] namespaces)
        {
            lock (discoveryLock)
            {
                if (discoveryStarted)
                    throw new InvalidOperationException("Discovery has already started");

                var newNamespaces = namespaces.Select(x => x + '.').ToArray();

                var newNamespacesToLoad = new string[newNamespaces.Length + 1];
                newNamespacesToLoad[0] = frameworkNameSpace;
                newNamespaces.CopyTo(newNamespacesToLoad, 1);
                DiscoveryNamespaces = newNamespacesToLoad;
            }
        }
        public static void SetDiscoveryAssemblies(params Assembly[] assemblies)
        {
            lock (discoveryLock)
            {
                if (discoveryStarted)
                    throw new InvalidOperationException("Discovery has already started");

                var newNamespaces = assemblies.Select(x => x.GetName().Name).ToArray();

                var newNamespacesToLoad = new string[newNamespaces.Length + 1];
                newNamespacesToLoad[0] = frameworkNameSpace;
                newNamespaces.CopyTo(newNamespacesToLoad, 1);
                DiscoveryNamespaces = newNamespacesToLoad;
            }
        }
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Zerra
{
    public static class Config
    {
        private const string settingsFileName = "appsettings.json";
        private const string genericSettingsFileName = "appsettings.{0}.json";
        private const string environmentalVariable = "ASPNETCORE_ENVIRONMENT";

        private static readonly object discoveryLock = new();
        private static bool discoveryStarted;
        internal static bool DiscoveryStarted
        {
            get
            {
                lock (discoveryLock)
                {
                    return discoveryStarted;
                }
            }
            set
            {
                lock (discoveryLock)
                {
                    discoveryStarted = value;
                }
            }
        }

        internal static string[] DiscoveryNamespaces;

        private static readonly Assembly entryAssembly;
        private static readonly Assembly executingAssembly;
        private static readonly string entryNameSpace;
        private static readonly string frameworkNameSpace;
        static Config()
        {
            entryAssembly = Assembly.GetEntryAssembly();
            executingAssembly = Assembly.GetExecutingAssembly();

            entryNameSpace = entryAssembly?.GetName().Name.Split('.')[0] + '.';
            frameworkNameSpace = executingAssembly.GetName().Name.Split('.')[0] + '.';

            DiscoveryNamespaces = entryNameSpace != null ? (new string[] { entryNameSpace, frameworkNameSpace }) : (new string[] { frameworkNameSpace });
            discoveryStarted = false;
        }

        private static IConfiguration configuration;
        public static void LoadConfiguration() { LoadConfiguration(null, null, null); }
        public static void LoadConfiguration(string[] args) { LoadConfiguration(args, null, null); }
        public static void LoadConfiguration(string[] args, Action<ConfigurationBuilder> build) { LoadConfiguration(args, null, build); }
        public static void LoadConfiguration(string[] args, string[] settingsFiles) { LoadConfiguration(args, settingsFiles, null); }
        public static void LoadConfiguration(string[] args, string[] settingsFiles, Action<ConfigurationBuilder> build)
        {
            var builder = new ConfigurationBuilder();

            var settingsFileNames = GetEnvironmentFiles(settingsFileName);
            foreach (var settingsFileName in settingsFileNames)
                AddSettingsFile(builder, settingsFileName);

            var environmentName = Environment.GetEnvironmentVariable(environmentalVariable);
            if (!String.IsNullOrWhiteSpace(environmentName))
            {
                var environmentSettingsFileNames = GetEnvironmentFiles(String.Format(genericSettingsFileName, environmentName));
                foreach (var environmentSettingsFileName in environmentSettingsFileNames)
                    AddSettingsFile(builder, environmentSettingsFileName);
            }

            if (settingsFiles != null && settingsFiles.Length > 0)
            {
                foreach (var settingsFile in settingsFiles)
                {
                    var file = settingsFile;
                    while (file.StartsWith("/") || file.StartsWith("\\"))
                        file = settingsFile.Substring(1);
                    AddSettingsFile(builder, file);
                }
            }

            _ = builder.AddEnvironmentVariables();

            if (args != null && args.Length > 0)
                _ = builder.AddCommandLine(args);

            if (entryAssembly != null)
                _ = builder.AddUserSecrets(entryAssembly, true);

            build?.Invoke(builder);

            configuration = builder.Build();
        }
        public static void SetConfiguration(IConfiguration configuration)
        {
            Config.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        private static void AddSettingsFile(ConfigurationBuilder builder, string fileName)
        {
            var file = File.OpenRead(fileName);
            _ = builder.AddJsonStream(file);
            Console.WriteLine($"{nameof(Config)} Loaded {fileName}");
        }

        public static string GetSetting(string name, params string[] sections)
        {
            if (configuration == null)
                LoadConfiguration();
            var config = configuration;
            if (sections != null && sections.Length > 0)
            {
                foreach (var section in sections)
                {
                    config = config.GetSection(section);
                    if (config == null)
                        throw new Exception($"Config section {section} not found");
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
            var config = configuration;
            if (sections != null && sections.Length > 0)
            {
                foreach (var section in sections)
                {
                    config = config.GetSection(section);
                    if (config == null)
                        throw new Exception($"Config section {section} not found");
                }
            }
            var value = config.Get<T>();
            return value;
        }

        public static string GetEnvironmentFilePath(string fileName)
        {
            var executingAssemblyPath = Path.GetDirectoryName(executingAssembly.Location);
            var filePath = $"{executingAssemblyPath}/{fileName}";
            if (File.Exists(filePath))
                return filePath;

            filePath = $"{Environment.CurrentDirectory}/{fileName}";
            return File.Exists(filePath) ? filePath : null;
        }
        private static IEnumerable<string> GetEnvironmentFiles(string fileSuffix)
        {
            var files = new List<string>();

            var searchPattern = $"*{fileSuffix}";

            var executingAssemblyPath = Path.GetDirectoryName(executingAssembly.Location);
            var executingAssemblyPathFiles = Directory.GetFiles(executingAssemblyPath, searchPattern);
            files.AddRange(executingAssemblyPathFiles);

            if (Environment.CurrentDirectory != executingAssemblyPath)
            {
                var currentDirectoryFiles = Directory.GetFiles(Environment.CurrentDirectory, searchPattern);
                files.AddRange(currentDirectoryFiles);
            }

            return files;
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

        public static Assembly EntryAssembly { get { return entryAssembly; } }

        private static readonly Lazy<bool> isDebugEntryAssembly = new(() => entryAssembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(x => x.IsJITTrackingEnabled));
        public static bool IsDebugBuild { get { return isDebugEntryAssembly.Value; } }
    }
}
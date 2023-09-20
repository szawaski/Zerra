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

        private const string environmentNameVariable1 = "ASPNETCORE_ENVIRONMENT";
        private const string environmentNameVariable2 = "Hosting:Environment";
        private const string environmentNameVariable3 = "ASPNET_ENV";

        private static readonly object discoveryLock = new();
        private static bool discoveryStarted;
        internal static void SetDiscoveryStarted()
        {
            lock (discoveryLock)
            {
                discoveryStarted = true;
            }
        }

        internal static string[] DiscoveryAssemblyNameStartsWiths;

        private static readonly Assembly entryAssembly;
        private static readonly Assembly executingAssembly;
        private static readonly string entryAssemblyName;
        private static readonly string entryNameSpace;
        private static readonly string frameworkNameSpace;
        static Config()
        {
            entryAssembly = Assembly.GetEntryAssembly();
            executingAssembly = Assembly.GetExecutingAssembly();

            entryAssemblyName = entryAssembly?.GetName().Name;

            entryNameSpace = entryAssemblyName?.Split('.')[0] + '.';
            frameworkNameSpace = executingAssembly.GetName().Name.Split('.')[0] + '.';

            DiscoveryAssemblyNameStartsWiths = entryNameSpace != null ? (new string[] { entryNameSpace, frameworkNameSpace }) : (new string[] { frameworkNameSpace });
            discoveryStarted = false;
        }

        private static IConfiguration configuration = null;
        private static string environmentName = null;
        private static string applicationIdentifier = null;
        public static void LoadConfiguration() { LoadConfiguration(null, null, null); }
        public static void LoadConfiguration(string environmentName) { LoadConfiguration(null, environmentName, null); }
        public static void LoadConfiguration(string[] args) { LoadConfiguration(args, null, null); }
        public static void LoadConfiguration(string[] args, string environmentName) { LoadConfiguration(args, environmentName, null); }
        public static void LoadConfiguration(string[] args, Action<ConfigurationBuilder> build) { LoadConfiguration(args, null, build); }
        public static void LoadConfiguration(Action<ConfigurationBuilder> build) { LoadConfiguration(null, null, build); }
        public static void LoadConfiguration(string[] args, string environmentName, Action<ConfigurationBuilder> build)
        {
            var builder = new ConfigurationBuilder();

            if (String.IsNullOrWhiteSpace(environmentName))
                environmentName = Environment.GetEnvironmentVariable(environmentNameVariable1);
            if (String.IsNullOrWhiteSpace(environmentName))
                environmentName = Environment.GetEnvironmentVariable(environmentNameVariable2);
            if (String.IsNullOrWhiteSpace(environmentName))
                environmentName = Environment.GetEnvironmentVariable(environmentNameVariable3);

            Console.WriteLine($"Assembly: {entryAssemblyName}");
            Console.WriteLine($"Environment: {environmentName}");
            Console.WriteLine($"Machine: {Environment.MachineName}");

            Config.environmentName = environmentName;
            Config.applicationIdentifier = $"{entryAssemblyName}:{environmentName}:{Environment.MachineName}";

            var settingsFileNames = GetEnvironmentFilesBySuffix(settingsFileName);
            foreach (var settingsFileName in settingsFileNames)
                AddSettingsFile(builder, settingsFileName);

            if (!String.IsNullOrWhiteSpace(environmentName))
            {
                var environmentSettingsFileNames = GetEnvironmentFilesBySuffix(String.Format(genericSettingsFileName, environmentName));
                foreach (var environmentSettingsFileName in environmentSettingsFileNames)
                    AddSettingsFile(builder, environmentSettingsFileName);
            }

            _ = builder.AddEnvironmentVariables();

            if (args != null && args.Length > 0)
                _ = builder.AddCommandLine(args);
            
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
            Console.WriteLine($"{nameof(Config)} Loaded {Path.GetFileName(fileName)}");
        }

        public static string GetSetting(string name, params string[] sections)
        {
            if (configuration == null)
                throw new Exception("Config not loaded");

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


        public static IConfiguration Configuration
        {
            get
            {
                if (configuration == null)
                    throw new Exception("Config not loaded");
                return configuration;
            }
        }
        public static string EnvironmentName
        {
            get
            {
                return environmentName;
            }
        }
        public static string ApplicationIdentifier
        {
            get
            {
                return applicationIdentifier;
            }
        }

        public static T Bind<T>(params string[] sections)
        {
            if (configuration == null)
                throw new Exception("Config not loaded");

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
            if (Directory.Exists(filePath))
                return filePath;
            return null;
        }
        public static IReadOnlyCollection<string> GetEnvironmentFilesBySuffix(string fileSuffix)
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

        public static string GetEnvironmentDirectory(string directoryName)
        {
            var executingAssemblyPath = Path.GetDirectoryName(executingAssembly.Location);
            var directoryPath = $"{executingAssemblyPath}/{directoryName}";
            if (Directory.Exists(directoryPath))
                return directoryPath;

            directoryPath = $"{Environment.CurrentDirectory}/{directoryPath}";
            if (Directory.Exists(directoryPath))
                return directoryPath;
            return null;
        }

        public static void AddDiscoveryAssemblyNameStartsWiths(params string[] assemblyNameStartsWiths)
        {
            if (assemblyNameStartsWiths == null)
                throw new ArgumentNullException(nameof(assemblyNameStartsWiths));

            lock (discoveryLock)
            {
                if (discoveryStarted)
                    throw new InvalidOperationException("Discovery has already started");

                var newNamespaces = assemblyNameStartsWiths.Select(x => x + '.').ToArray();

                var newNamespacesToLoad = new string[DiscoveryAssemblyNameStartsWiths.Length + newNamespaces.Length];
                DiscoveryAssemblyNameStartsWiths.CopyTo(newNamespacesToLoad, 0);
                newNamespaces.CopyTo(newNamespacesToLoad, DiscoveryAssemblyNameStartsWiths.Length);
                DiscoveryAssemblyNameStartsWiths = newNamespacesToLoad;
            }
        }
        public static void AddDiscoveryAssemblies(params Assembly[] assemblies)
        {
            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            lock (discoveryLock)
            {
                if (discoveryStarted)
                    throw new InvalidOperationException("Discovery has already started");

                var newNamespaces = assemblies.Select(x => x.GetName().Name).ToArray();

                var newNamespacesToLoad = new string[DiscoveryAssemblyNameStartsWiths.Length + newNamespaces.Length];
                DiscoveryAssemblyNameStartsWiths.CopyTo(newNamespacesToLoad, 0);
                newNamespaces.CopyTo(newNamespacesToLoad, DiscoveryAssemblyNameStartsWiths.Length);
                DiscoveryAssemblyNameStartsWiths = newNamespacesToLoad;
            }
        }

        public static void SetDiscoveryAssemblyNameStartsWiths(params string[] assemblyNameStartsWiths)
        {
            if (assemblyNameStartsWiths == null)
                throw new ArgumentNullException(nameof(assemblyNameStartsWiths));

            lock (discoveryLock)
            {
                if (discoveryStarted)
                    throw new InvalidOperationException("Discovery has already started");

                var newNamespaces = assemblyNameStartsWiths.Select(x => x + '.').ToArray();

                var newNamespacesToLoad = new string[newNamespaces.Length + 1];
                newNamespacesToLoad[0] = frameworkNameSpace;
                newNamespaces.CopyTo(newNamespacesToLoad, 1);
                DiscoveryAssemblyNameStartsWiths = newNamespacesToLoad;
            }
        }
        public static void SetDiscoveryAssemblies(params Assembly[] assemblies)
        {
            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            lock (discoveryLock)
            {
                if (discoveryStarted)
                    throw new InvalidOperationException("Discovery has already started");

                var newNamespaces = assemblies.Select(x => x.GetName().Name).ToArray();

                var newNamespacesToLoad = new string[newNamespaces.Length + 1];
                newNamespacesToLoad[0] = frameworkNameSpace;
                newNamespaces.CopyTo(newNamespacesToLoad, 1);
                DiscoveryAssemblyNameStartsWiths = newNamespacesToLoad;
            }
        }

        public static void SetDiscoveryAllAssemblies()
        {
            lock (discoveryLock)
            {
                if (discoveryStarted)
                    throw new InvalidOperationException("Discovery has already started");

                DiscoveryAssemblyNameStartsWiths = Array.Empty<string>();
            }
        }

        public static string EntryAssemblyName { get { return entryAssemblyName; } }
        public static Assembly EntryAssembly { get { return entryAssembly; } }

        private static readonly Lazy<bool> isDebugEntryAssembly = new(() => entryAssembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(x => x.IsJITTrackingEnabled));
        public static bool IsDebugBuild { get { return isDebugEntryAssembly.Value; } }
    }
}
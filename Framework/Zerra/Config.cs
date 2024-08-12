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

        private static readonly Assembly? entryAssembly;
        private static readonly string? entryAssemblyName;
        private static readonly string? entryNameSpace;
        private static readonly string? executingAssemblyPath;
        static Config()
        {
            entryAssembly = Assembly.GetEntryAssembly();
            var executingAssembly = Assembly.GetExecutingAssembly();

            entryAssemblyName = entryAssembly?.GetName().Name;
            var executingAssemblyName = executingAssembly.GetName().Name;

            entryNameSpace = entryAssemblyName != null ? entryAssemblyName.Split('.')[0] + '.' : null;
            executingAssemblyPath = Path.GetDirectoryName(executingAssembly.Location);

            if (!String.IsNullOrWhiteSpace(entryNameSpace))
                DiscoveryAssemblyNameStartsWiths = ["Zerra,", "Zerra.", entryNameSpace];
            else
                DiscoveryAssemblyNameStartsWiths = ["Zerra,", "Zerra."];

            discoveryStarted = false;
        }

        private static IConfiguration? configuration = null;
        private static string? environmentName = null;
        private static string? applicationIdentifier = null;
        public static void LoadConfiguration() { LoadConfiguration(null, null, null); }
        public static void LoadConfiguration(string? environmentName) { LoadConfiguration(null, environmentName, null); }
        public static void LoadConfiguration(string[]? args) { LoadConfiguration(args, null, null); }
        public static void LoadConfiguration(string[]? args, string? environmentName) { LoadConfiguration(args, environmentName, null); }
        public static void LoadConfiguration(string[]? args, Action<ConfigurationBuilder>? build) { LoadConfiguration(args, null, build); }
        public static void LoadConfiguration(Action<ConfigurationBuilder>? build) { LoadConfiguration(null, null, build); }
        public static void LoadConfiguration(string[]? args, string? environmentName, Action<ConfigurationBuilder>? build)
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
            _ = builder.AddJsonFile(fileName);
            Console.WriteLine($"{nameof(Config)} Loaded {Path.GetFileName(fileName)}");
        }

        public static string? GetSetting(string name, params string[] sections)
        {
            if (configuration == null)
                throw new Exception("Config not loaded");

            var value = configuration[name];
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
        public static string? EnvironmentName
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
                if (applicationIdentifier == null)
                    throw new Exception("Config not loaded");
                return applicationIdentifier;
            }
        }

        public static T? Bind<T>(string? section = null)
        {
            if (configuration == null)
                throw new Exception("Config not loaded");

            var config = configuration;
            if (!String.IsNullOrWhiteSpace(section))
                config = config.GetSection(section);
            var value = config.Get<T>();
            return value;
        }

        public static string? GetEnvironmentFilePath(string fileName)
        {
            var filePath = $"{executingAssemblyPath}{Path.DirectorySeparatorChar}{fileName}";
            if (File.Exists(filePath))
                return filePath;

            filePath = $"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}{fileName}";
            if (Directory.Exists(filePath))
                return filePath;

            return null;
        }
        public static IEnumerable<string> GetEnvironmentFilesBySuffix(string fileSuffix)
        {
            var searchPattern = $"*{fileSuffix}";

            if (String.IsNullOrWhiteSpace(executingAssemblyPath))
                return Enumerable.Empty<string>();

            if (Environment.CurrentDirectory != executingAssemblyPath)
            {
                var files = new List<string>();
                var executingAssemblyPathFiles = Directory.GetFiles(executingAssemblyPath, searchPattern);
                files.AddRange(executingAssemblyPathFiles);

                if (Environment.CurrentDirectory != executingAssemblyPath)
                {
                    var currentDirectoryFiles = Directory.GetFiles(Environment.CurrentDirectory, searchPattern);
                    files.AddRange(currentDirectoryFiles);
                }

                return files;
            }
            else
            {
                var executingAssemblyPathFiles = Directory.GetFiles(executingAssemblyPath, searchPattern);
                return executingAssemblyPathFiles;
            }
        }

        public static string? GetEnvironmentDirectory(string directoryName)
        {
            var directoryPath = $"{executingAssemblyPath}{Path.DirectorySeparatorChar}{directoryName}";
            if (Directory.Exists(directoryPath))
                return directoryPath;

            directoryPath = $"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}{directoryPath}";
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

                var newNamespacesToLoad = new string[newNamespaces.Length + 2];
                newNamespacesToLoad[0] = "Zerra,";
                newNamespacesToLoad[1] = "Zerra.";
                newNamespaces.CopyTo(newNamespacesToLoad, 2);
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

                string[] newNamespaces = assemblies.Select(x => x.GetName().Name).Where(x => x != null).ToArray()!;

                var newNamespacesToLoad = new string[newNamespaces.Length + 2];
                newNamespacesToLoad[0] = "Zerra,";
                newNamespacesToLoad[1] = "Zerra.";
                newNamespaces.CopyTo(newNamespacesToLoad, 2);
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

        public static string? EntryAssemblyName { get { return entryAssemblyName; } }
        public static Assembly? EntryAssembly { get { return entryAssembly; } }

        private static readonly Lazy<bool> isDebugEntryAssembly = new(() => entryAssembly != null && entryAssembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(x => x.IsJITTrackingEnabled));
        public static bool IsDebugBuild { get { return isDebugEntryAssembly.Value; } }
    }
}
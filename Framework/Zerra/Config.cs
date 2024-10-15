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
using Zerra.Reflection;

namespace Zerra
{
    /// <summary>
    /// Application Configuration setup using Microsoft's IConfiguration without dependency injection.
    /// At the start of an application call <see cref="LoadConfiguration(string[], string, Action{ConfigurationBuilder})" />.
    /// It will attempt to find the environment name and then use that to find and load configuration files.
    /// There are overloads to add custom components to the configuration builder.
    /// This can be completely custom using <see cref="SetConfiguration(string, IConfiguration)" />.
    /// This class is also responsible for setting up which assemblies to load and search for the discovery service using <see cref="AddDiscoveryAssemblies(Assembly[])" /> and related methods.
    /// </summary>
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

        private static bool discoveryEnabled = true;
        /// <summary>
        /// Indicates if Discovery will search all the loaded assemblies. Default is True.
        /// </summary>
        public static bool DiscoveryEnabled
        {
            get
            {
                lock (discoveryLock)
                {
                    return discoveryEnabled;
                }
            }
            set
            {
                lock (discoveryLock)
                {
                    if (discoveryStarted)
                        throw new InvalidOperationException("Discovery has already started");
                    discoveryEnabled = value;
                }
            }
        }

        private static bool assemblyLoaderEnabled = true;
        /// <summary>
        /// Indicates if Discovery will load all the assemblies. Default is True.
        /// </summary>
        public static bool AssemblyLoaderEnabled
        {
            get
            {
                lock (discoveryLock)
                {
                    return assemblyLoaderEnabled;
                }
            }
            set
            {
                lock (discoveryLock)
                {
                    if (discoveryStarted)
                        throw new InvalidOperationException("Discovery has already started");
                    assemblyLoaderEnabled = value;
                }
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

            entryNameSpace = entryAssemblyName is not null ? entryAssemblyName.Split('.')[0] + '.' : null;
            executingAssemblyPath = Path.GetDirectoryName(executingAssembly.Location);

            if (!String.IsNullOrWhiteSpace(entryNameSpace))
                DiscoveryAssemblyNameStartsWiths = [entryNameSpace];
            else
                DiscoveryAssemblyNameStartsWiths = [];

            discoveryStarted = false;
        }

        private static IConfiguration? configuration = null;
        private static string? environmentName = null;
        private static string? applicationIdentifier = null;

        /// <summary>
        /// Loads the application configuration starting with the environmental variable the then configuration files.
        /// This initilizes the discovery process if not disabled.
        /// </summary>
        /// <param name="args">The program level arguments from the application Main method.</param>
        /// <param name="environmentName">The environment name, otherwise it will be searched for in environmental variables.</param>
        /// <param name="build">A function to add customizations to the configuration before it is built.</param>
        public static void LoadConfiguration(string[]? args = null, string? environmentName = null, Action<ConfigurationBuilder>? build = null)
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

            if (args is not null && args.Length > 0)
                _ = builder.AddCommandLine(args);

            build?.Invoke(builder);

            configuration = builder.Build();

            if (discoveryEnabled && !discoveryStarted)
            {
                Discovery.Discover();
            }
        }

        /// <summary>
        /// Sets the configuration manually from a prebuilt one.
        /// </summary>
        /// <param name="environmentName">The environment name, otherwise it will be searched for in environmental variables.</param>
        /// <param name="configuration">The prebuild configuration</param>
        /// <exception cref="ArgumentNullException">Throws if the configuration is null</exception>
        public static void SetConfiguration(string environmentName, IConfiguration configuration)
        {
            Console.WriteLine($"Assembly: {entryAssemblyName}");
            Console.WriteLine($"Environment: {environmentName}");
            Console.WriteLine($"Machine: {Environment.MachineName}");

            Config.environmentName = environmentName;
            Config.applicationIdentifier = $"{entryAssemblyName}:{environmentName}:{Environment.MachineName}";

            Config.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        private static void AddSettingsFile(ConfigurationBuilder builder, string fileName)
        {
            _ = builder.AddJsonFile(fileName);
            Console.WriteLine($"{nameof(Config)} Loaded {Path.GetFileName(fileName)}");
        }

        /// <summary>
        /// Gets a setting using IConfiguration syntax which uses a colon to seperate child settings.
        /// </summary>
        /// <param name="name">The setting name.</param>
        /// <returns>The value of the setting.</returns>
        /// <exception cref="InvalidOperationException">Throw if the configuration was not loaded</exception>
        public static string? GetSetting(string name)
        {
            if (configuration is null)
                throw new InvalidOperationException("Config not loaded");

            var value = configuration[name];
            return value;
        }

        /// <summary>
        /// Accessor for the IConfiguration being used.
        /// </summary>
        /// <exception cref="InvalidOperationException">Throw if the configuration was not loaded</exception>
        public static IConfiguration Configuration
        {
            get
            {
                if (configuration is null)
                    throw new InvalidOperationException("Config not loaded");
                return configuration;
            }
        }
        /// <summary>
        /// The application environment.
        /// </summary>
        public static string? EnvironmentName
        {
            get
            {
                return environmentName;
            }
        }
        /// <summary>
        /// An identifier that should be unique that is useful for services.
        /// This is a combination of Assembly Name, Environment Name, and Machine Name
        /// </summary>
        /// <exception cref="InvalidOperationException">Throw if the configuration was not loaded</exception>
        public static string ApplicationIdentifier
        {
            get
            {
                if (applicationIdentifier is null)
                    throw new Exception("Config not loaded");
                return applicationIdentifier;
            }
        }

        /// <summary>
        /// Attempts to bind the configuration instance to a new instance of type T.
        /// If this configuration section has a value, that will be used.
        /// Otherwise binding by matching property names against configuration keys recursively.
        /// </summary>
        /// <typeparam name="T">The type of the new instance to bind.</typeparam>
        /// <param name="section">Optional. A subsection of the configuration to bind.</param>
        /// <returns>The new instance of T if successful, default(T) otherwise.</returns>
        /// <exception cref="InvalidOperationException">Throw if the configuration was not loaded</exception>
        public static T? Bind<T>(string? section = null)
        {
            if (configuration is null)
                throw new InvalidOperationException("Config not loaded");

            var config = configuration;
            if (!String.IsNullOrWhiteSpace(section))
                config = config.GetSection(section);
            var value = config.Get<T>();
            return value;
        }

        /// <summary>
        /// Gets the path to an environmental file.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <returns>The path to the file if it exists; otherwise, null.</returns>
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
        /// <summary>
        /// Gets the paths to environmental files using a suffix search.
        /// </summary>
        /// <param name="fileSuffix">The suffix of the file name.</param>
        /// <returns>The paths to the files</returns>
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

        /// <summary>
        /// Gets the path to an environmental directory
        /// </summary>
        /// <param name="directoryName">The name of the directory.</param>
        /// <returns>The directory path if it exsists; otherwise, null.</returns>
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

        /// <summary>
        /// The discovery service loads and searches assemblies for the application.
        /// This adds a search for assembly names that start with indicated strings.
        /// </summary>
        /// <param name="assemblyNameStartsWiths">The strings that assembly names must start with.</param>
        /// <exception cref="ArgumentNullException">Throws if argument is null.</exception>
        /// <exception cref="InvalidOperationException">Throws if discovery has already started.</exception>
        public static void AddDiscoveryAssemblyNameStartsWiths(params string[] assemblyNameStartsWiths)
        {
            if (assemblyNameStartsWiths is null)
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
        /// <summary>
        /// The discovery service loads and searches assemblies for the application.
        /// This adds assemblies to the search.
        /// </summary>
        /// <param name="assemblies">The assemblies to add to the search.</param>
        /// <exception cref="ArgumentNullException">Throws if argument is null.</exception>
        /// <exception cref="InvalidOperationException">Throws if discovery has already started.</exception>
        public static void AddDiscoveryAssemblies(params Assembly[] assemblies)
        {
            if (assemblies is null)
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

        /// <summary>
        /// The discovery service loads and searches assemblies for the application.
        /// This sets a search for assembly names that start with indicated strings. Any existing searches are replaced.
        /// </summary>
        /// <param name="assemblyNameStartsWiths">The strings that assembly names must start with.</param>
        /// <exception cref="ArgumentNullException">Throws if argument is null.</exception>
        /// <exception cref="InvalidOperationException">Throws if discovery has already started.</exception>
        public static void SetDiscoveryAssemblyNameStartsWiths(params string[] assemblyNameStartsWiths)
        {
            if (assemblyNameStartsWiths is null)
                throw new ArgumentNullException(nameof(assemblyNameStartsWiths));

            lock (discoveryLock)
            {
                if (discoveryStarted)
                    throw new InvalidOperationException("Discovery has already started");

                var newNamespaces = assemblyNameStartsWiths.Select(x => x + '.').ToArray();

                DiscoveryAssemblyNameStartsWiths = newNamespaces;
            }
        }
        /// <summary>
        /// The discovery service loads and searches assemblies for the application.
        /// This sets the assemblies to the search. Any existing searches are replaced.
        /// </summary>
        /// <param name="assemblies">The assemblies to add to the search.</param>
        /// <exception cref="ArgumentNullException">Throws if argument is null.</exception>
        /// <exception cref="InvalidOperationException">Throws if discovery has already started.</exception>
        public static void SetDiscoveryAssemblies(params Assembly[] assemblies)
        {
            if (assemblies is null)
                throw new ArgumentNullException(nameof(assemblies));

            lock (discoveryLock)
            {
                if (discoveryStarted)
                    throw new InvalidOperationException("Discovery has already started");

                string[] newNamespaces = assemblies.Select(x => x.GetName().Name).Where(x => x is not null).ToArray()!;

                DiscoveryAssemblyNameStartsWiths = newNamespaces;
            }
        }

        /// <summary>
        /// The name of the entry assembly.
        /// </summary>
        public static string? EntryAssemblyName { get { return entryAssemblyName; } }
        /// <summary>
        /// The entry assembly.
        /// </summary>
        public static Assembly? EntryAssembly { get { return entryAssembly; } }

        private static readonly Lazy<bool> isDebugEntryAssembly = new(() => entryAssembly is not null && entryAssembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(x => x.IsJITTrackingEnabled));
        /// <summary>
        /// Indicates if the build is a debug by checking the DebuggableAttribute and if JIT Tracking is enabled.
        /// </summary>
        public static bool IsDebugBuild { get { return isDebugEntryAssembly.Value; } }
    }
}
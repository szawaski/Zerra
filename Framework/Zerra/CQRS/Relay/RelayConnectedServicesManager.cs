// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET5_0_OR_GREATER

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Logging;
using Zerra.Serialization;

namespace Zerra.CQRS.Relay
{
    internal static class RelayConnectedServicesManager
    {
        private const int maxStatistics = 1000;
        private const int runExpireEveryMilliseconds = 15000;

        private static readonly SemaphoreSlim stateLock = new SemaphoreSlim(1, 1);
        private static readonly ConcurrentDictionary<string, RelayConnectedService> servicesByUrl = new ConcurrentDictionary<string, RelayConnectedService>();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, RelayConnectedService>> servicesByProviderType = new ConcurrentDictionary<string, ConcurrentDictionary<string, RelayConnectedService>>();
        private static void AddOrUpdate(ServiceInfo info, bool saveState)
        {
            lock (servicesByUrl)
            {
                var service = servicesByUrl.GetOrAdd(info.Url,
                (Func<string, RelayConnectedService>)((id) =>
                {
                    return new RelayConnectedService() { Url = info.Url };
                }));

                foreach (var serviceByProviderType in servicesByProviderType)
                {
                    if (!info.ProviderTypes.Contains(serviceByProviderType.Key))
                    {
                        serviceByProviderType.Value.TryRemove(info.Url, out _);
                    }
                }
                foreach (var providerType in info.ProviderTypes)
                {
                    var servicesForProvider = servicesByProviderType.GetOrAdd(providerType, (key) => { return new ConcurrentDictionary<string, RelayConnectedService>(); });
                    servicesForProvider.TryAdd(info.Url, service);
                    Log.InfoAsync($"Service Added {providerType} {service.Url}");
                }
            }
            if (saveState)
            {
                Task.Run(async () => { await SaveState(); });
            }
        }
        public static void AddOrUpdate(ServiceInfo info) { AddOrUpdate(info, true); }
        public static void Remove(string url)
        {
            lock (servicesByUrl)
            {
                var service = servicesByUrl[url];
                if (service != null)
                {
                    servicesByUrl.TryRemove(service.Url, out _);
                    foreach (var servicesForProvider in servicesByProviderType.Values)
                    {
                        servicesForProvider.TryRemove(service.Url, out _);
                    }

                    Log.InfoAsync($"Service Removed {service.Url}");
                }
            }
            Task.Run(async () => { await SaveState(); });
        }

        public static RelayConnectedService GetBestService(string providerType)
        {
            if (!servicesByProviderType.TryGetValue(providerType, out ConcurrentDictionary<string, RelayConnectedService> servicesForProvider))
                return null;

            return servicesForProvider.Values.Where(x => !x.Failed).OrderBy(x => x.Load).FirstOrDefault();
        }

        public static Task StartExpireStatistics(CancellationToken token)
        {
            return Task.Run(() =>
            {
                Thread.Sleep(runExpireEveryMilliseconds);
                while (!token.IsCancellationRequested)
                {
                    foreach (var service in servicesByUrl.Values)
                    {
                        service.ExpireStatistis(maxStatistics);
                    }
                    Thread.Sleep(runExpireEveryMilliseconds);
                }
            });
        }

        private static async Task SaveState()
        {
            await stateLock.WaitAsync();
            try
            {
                var path = $"{GetAssemblyLocation()}\\relaystate.dat";

                var infos = new List<ServiceInfo>();
                foreach (var service in servicesByUrl.Values)
                {
                    var providerTypes = servicesByProviderType.Where(x => x.Value.Any(y => y.Key == service.Url)).Select(x => x.Key).ToArray();
                    var info = new ServiceInfo()
                    {
                        Url = service.Url,
                        ProviderTypes = providerTypes
                    };
                    infos.Add(info);
                }
                var infoArray = infos.ToArray();
                try
                {
                    using (var file = File.Create(path))
                    {
                        var serializer = new ByteSerializer();
                        await serializer.SerializeAsync(file, infoArray);
                    }
                }
                catch { }
            }
            finally
            {
                stateLock.Release();
            }
        }

        public static async Task LoadState()
        {
            await stateLock.WaitAsync();
            try
            {
                var path = $"{GetAssemblyLocation()}\\relaystate.dat";
                if (File.Exists(path))
                {
                    ServiceInfo[] infoArray = null;
                    try
                    {
                        using (var file = File.OpenRead(path))
                        {
                            var serializer = new ByteSerializer();
                            infoArray = await serializer.DeserializeAsync<ServiceInfo[]>(file);
                        }
                    }
                    catch
                    {
                        try
                        {
                            File.Delete(path);
                        }
                        catch { }
                    }
                    if (infoArray != null)
                    {
                        foreach (var info in infoArray)
                        {
                            AddOrUpdate(info, false);
                        }
                    }
                }
            }
            finally
            {
                stateLock.Release();
            }
        }

        private static string GetAssemblyLocation()
        {
            var assemblyUri = new UriBuilder(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var assemblyUriPath = Uri.UnescapeDataString(assemblyUri.Path);
            var assemblyPath = System.IO.Path.GetDirectoryName(assemblyUriPath);
            return assemblyPath;
        }
    }
}

#endif
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
using Zerra.Serialization.Bytes;

namespace Zerra.CQRS.Relay
{
    internal static class RelayConnectedServicesManager
    {
        private const int maxStatistics = 1000;
        private const int runExpireEveryMilliseconds = 15000;

        private static readonly SemaphoreSlim stateLock = new(1, 1);
        private static readonly ConcurrentDictionary<string, RelayConnectedService> servicesByUrl = new();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, RelayConnectedService>> servicesByProviderType = new();
        private static void AddOrUpdate(ServiceInfo info, bool saveState)
        {
            if (String.IsNullOrWhiteSpace(info.Url))
                return;
            if (info.ProviderTypes == null || info.ProviderTypes.Length == 0)
                return;

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
                        _ = serviceByProviderType.Value.TryRemove(info.Url, out _);
                    }
                }
                foreach (var providerType in info.ProviderTypes)
                {
                    var servicesForProvider = servicesByProviderType.GetOrAdd(providerType, (key) => { return new ConcurrentDictionary<string, RelayConnectedService>(); });
                    _ = servicesForProvider.TryAdd(info.Url, service);
                    _ = Log.InfoAsync($"Service Added {providerType} {service.Url}");
                }
            }
            if (saveState)
            {
                _ = SaveState();
            }
        }
        public static void AddOrUpdate(ServiceInfo info) { AddOrUpdate(info, true); }
        public static void Remove(string? url)
        {
            if (String.IsNullOrWhiteSpace(url))
                return;

            lock (servicesByUrl)
            {
                var service = servicesByUrl[url];
                if (service != null)
                {
                    _ = servicesByUrl.TryRemove(url, out _);
                    foreach (var servicesForProvider in servicesByProviderType.Values)
                    {
                        _ = servicesForProvider.TryRemove(url, out _);
                    }

                    _ = Log.InfoAsync($"Service Removed {url}");
                }
            }
            _ = SaveState();
        }

        public static RelayConnectedService? GetBestService(string providerType)
        {
            if (!servicesByProviderType.TryGetValue(providerType, out var servicesForProvider))
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
            }, token);
        }

        private static async Task SaveState()
        {
            await stateLock.WaitAsync();
            try
            {
                var path = Config.GetEnvironmentFilePath("relaystate.dat");
                if (path == null)
                    throw new InvalidOperationException("Could not load environment to save relaystate.dat");

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
                        await ByteSerializer.SerializeAsync(file, infoArray);
                    }
                }
                catch { }
            }
            finally
            {
                _ = stateLock.Release();
            }
        }

        public static async Task LoadState()
        {
            await stateLock.WaitAsync();
            try
            {
                var path = Config.GetEnvironmentFilePath("relaystate.dat");
                if (File.Exists(path))
                {
                    ServiceInfo[]? infoArray = null;
                    try
                    {
                        using (var file = File.OpenRead(path))
                        {
                            infoArray = await ByteSerializer.DeserializeAsync<ServiceInfo[]>(file);
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
                _ = stateLock.Release();
            }
        }
    }
}

#endif
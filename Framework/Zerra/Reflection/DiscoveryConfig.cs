// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Buffers;
using System.Linq;
using System.Reflection;

namespace Zerra.Reflection
{
    public static class DiscoveryConfig
    {
        internal static string[] LoadNamespaces;
        internal static bool Started;

        static DiscoveryConfig()
        {
            var entryNameSpace = Assembly.GetEntryAssembly().GetName().Name.Split('.')[0];
            var frameworkNameSpace = Assembly.GetExecutingAssembly().GetName().Name.Split('.')[0];
            DiscoveryConfig.LoadNamespaces = new string[] { entryNameSpace, frameworkNameSpace };
            DiscoveryConfig.Started = false;
        }

        public static void SetLoad(string[] namespaces)
        {
            if (DiscoveryConfig.Started)
                throw new InvalidOperationException($"{nameof(Discovery)} has already started");
            DiscoveryConfig.LoadNamespaces = namespaces;
        }
        public static void SetLoad(Assembly[] assemblies)
        {
            if (DiscoveryConfig.Started)
                throw new InvalidOperationException($"{nameof(Discovery)} has already started");
            DiscoveryConfig.LoadNamespaces = assemblies.Select(x => x.GetName().Name).ToArray();
        }
    }
}
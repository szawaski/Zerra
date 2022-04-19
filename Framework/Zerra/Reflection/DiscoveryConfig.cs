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
        internal static string[] NamespacesToLoad;
        internal static bool Started;

        private static readonly string entryNameSpace = Assembly.GetEntryAssembly()?.GetName().Name.Split('.')[0] + '.';
        private static readonly string frameworkNameSpace = Assembly.GetExecutingAssembly().GetName().Name.Split('.')[0] + '.';

        static DiscoveryConfig()
        {
            if (entryNameSpace != null)
                NamespacesToLoad = new string[] { entryNameSpace, frameworkNameSpace };
            else
                NamespacesToLoad = new string[] { frameworkNameSpace };
            Started = false;
        }

        public static void AddNamespacesToLoad(params string[] assemblyNames)
        {
            if (Started)
                throw new InvalidOperationException("Discovery has already started");

            var newNamespaces = assemblyNames.Select(x => x + '.').ToArray();

            var newNamespacesToLoad = new string[NamespacesToLoad.Length + newNamespaces.Length];
            NamespacesToLoad.CopyTo(newNamespacesToLoad, 0);
            newNamespaces.CopyTo(newNamespaces, NamespacesToLoad.Length);
            NamespacesToLoad = newNamespacesToLoad;
        }
        public static void AddAssembliesToLoad(params Assembly[] assemblies)
        {
            if (Started)
                throw new InvalidOperationException("Discovery has already started");

            var newNamespaces = assemblies.Select(x => x.GetName().Name).ToArray();

            var newNamespacesToLoad = new string[NamespacesToLoad.Length + newNamespaces.Length];
            NamespacesToLoad.CopyTo(newNamespacesToLoad, 0);
            newNamespaces.CopyTo(newNamespaces, NamespacesToLoad.Length);
            NamespacesToLoad = newNamespacesToLoad;
        }

        public static void SetNamespacesToLoad(params string[] assemblyNames)
        {
            if (Started)
                throw new InvalidOperationException("Discovery has already started");

            var newNamespaces = assemblyNames.Select(x => x + '.').ToArray();

            var newNamespacesToLoad = new string[newNamespaces.Length + 1];
            newNamespacesToLoad[0] = frameworkNameSpace;
            newNamespaces.CopyTo(newNamespacesToLoad, 1);
            NamespacesToLoad = newNamespacesToLoad;
        }
        public static void SetAssembliesToLoad(params Assembly[] assemblies)
        {
            if (Started)
                throw new InvalidOperationException("Discovery has already started");

            var newNamespaces = assemblies.Select(x => x.GetName().Name).ToArray();

            var newNamespacesToLoad = new string[newNamespaces.Length + 1];
            newNamespacesToLoad[0] = frameworkNameSpace;
            newNamespaces.CopyTo(newNamespacesToLoad, 1);
            NamespacesToLoad = newNamespacesToLoad;
        }
    }
}
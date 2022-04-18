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
        internal static string[] AssemblyNames;
        internal static bool Started;

        private static readonly string entryNameSpace = Assembly.GetEntryAssembly()?.GetName().Name.Split('.')[0] + '.';
        private static readonly string frameworkNameSpace = Assembly.GetExecutingAssembly().GetName().Name.Split('.')[0] + '.';

        static DiscoveryConfig()
        {
            if (entryNameSpace != null)
                AssemblyNames = new string[] { entryNameSpace, frameworkNameSpace };
            else
                AssemblyNames = new string[] { frameworkNameSpace };
            Started = false;
        }

        public static void AddAssembliesToLoad(params string[] assemblyNames)
        {
            if (Started)
                throw new InvalidOperationException("Discovery has already started");

            var newNamespaces = assemblyNames.Select(x => x + '.').ToArray();

            var newAssemblyNames = new string[AssemblyNames.Length + newNamespaces.Length];
            AssemblyNames.CopyTo(newAssemblyNames, 0);
            newNamespaces.CopyTo(newNamespaces, AssemblyNames.Length);
            AssemblyNames = newAssemblyNames;
        }
        public static void AddAssembliesToLoad(params Assembly[] assemblies)
        {
            if (Started)
                throw new InvalidOperationException("Discovery has already started");

            var newNamespaces = assemblies.Select(x => x.GetName().Name).ToArray();

            var newAssemblyNames = new string[AssemblyNames.Length + newNamespaces.Length];
            AssemblyNames.CopyTo(newAssemblyNames, 0);
            newNamespaces.CopyTo(newNamespaces, AssemblyNames.Length);
            AssemblyNames = newAssemblyNames;
        }

        public static void SetAssembliesToLoad(params string[] assemblyNames)
        {
            if (Started)
                throw new InvalidOperationException("Discovery has already started");

            var newNamespaces = assemblyNames.Select(x => x + '.').ToArray();

            var newAssemblyNames = new string[newNamespaces.Length + 1];
            newAssemblyNames[0] = frameworkNameSpace;
            AssemblyNames.CopyTo(newAssemblyNames, 1);
            AssemblyNames = newAssemblyNames;
        }
        public static void SetAssembliesToLoad(params Assembly[] assemblies)
        {
            if (Started)
                throw new InvalidOperationException("Discovery has already started");

            var newNamespaces = assemblies.Select(x => x.GetName().Name).ToArray();

            var newAssemblyNames = new string[newNamespaces.Length + 1];
            newAssemblyNames[0] = frameworkNameSpace;
            AssemblyNames.CopyTo(newAssemblyNames, 1);
            AssemblyNames = newAssemblyNames;
        }
    }
}
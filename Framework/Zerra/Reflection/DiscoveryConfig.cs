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

        private static readonly string entryNameSpace = Assembly.GetEntryAssembly().GetName().Name.Split('.')[0] + '.';
        private static readonly string frameworkNameSpace = Assembly.GetExecutingAssembly().GetName().Name.Split('.')[0] + '.';

        static DiscoveryConfig()
        {
            AssemblyNames = new string[] { entryNameSpace, frameworkNameSpace };
            Started = false;
        }

        public static void SetAssembliesToLoad(string[] assemblyNames)
        {
            if (Started)
                throw new InvalidOperationException("Discovery has already started");
#if NETSTANDARD2_0
            AssemblyNames = assemblyNames.Select(x => x + '.').Concat(new string[] { frameworkNameSpace }).ToArray();
#else
            AssemblyNames = assemblyNames.Select(x => x + '.').Append(frameworkNameSpace).ToArray();
#endif
        }
        public static void SetAssembliesToLoad(Assembly[] assemblies)
        {
            if (Started)
                throw new InvalidOperationException("Discovery has already started");
            AssemblyNames = assemblies.Select(x => x.GetName().Name).Append(frameworkNameSpace).ToArray();
        }
    }
}
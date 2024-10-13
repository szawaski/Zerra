// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Reflection;
using System.Reflection.Emit;

namespace Zerra.Reflection
{
    public static class GeneratedAssembly
    {
        private static readonly object moduleBuilderLock = new();
        private static ModuleBuilder? moduleBuilderCache = null;
        public static ModuleBuilder GetModuleBuilder()
        {
            if (moduleBuilderCache is null)
            {
                lock (moduleBuilderLock)
                {
                    if (moduleBuilderCache is null)
                    {
                        var assemblyName = new AssemblyName("ZerraDynamicAssembly");
                        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                        moduleBuilderCache = assemblyBuilder.DefineDynamicModule("MainModule");
                    }
                }
            }
            return moduleBuilderCache;
        }
    }
}

// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace Zerra.Reflection.Dynamic
{
#if !NETSTANDARD2_0
    [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
#endif
    internal static class GeneratedAssembly
    {
#if NETSTANDARD2_0
        private static readonly object moduleBuilderLock = new();
#else
        private static readonly Lock moduleBuilderLock = new();
#endif
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

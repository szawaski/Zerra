// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Zerra.Reflection
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public abstract class BaseGenerateAttribute : Attribute
    {
        public abstract Type Generate(Type type);

        private static readonly object moduleBuilderLock = new object();
        private static ModuleBuilder moduleBuilderCache = null;
        protected static ModuleBuilder GetModuleBuilder()
        {
            if (moduleBuilderCache == null)
            {
                lock (moduleBuilderLock)
                {
                    if (moduleBuilderCache == null)
                    {
                        var assemblyName = new AssemblyName($"{nameof(EmptyImplementations)}_Assembly");
                        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                        moduleBuilderCache = assemblyBuilder.DefineDynamicModule($"{nameof(BaseGenerateAttribute)}_MainModule");
                    }
                }
            }
            return moduleBuilderCache;
        }
    }
}

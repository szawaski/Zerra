// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Zerra.Reflection
{
    public static class Resolver
    {
        public static bool TryGetSingle<TInterface>(
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)] out TInterface provider
#else
            out TInterface? provider
#endif
        )
        {
            provider = GetSingle<TInterface>(false);
            return provider != null;
        }

        public static TInterface GetSingle<TInterface>() { return GetSingle<TInterface>(true)!; }

        public static bool TryGetSingle(Type type, out object provider)
        {
            provider = GetSingleGeneric(type, false);
            return provider != null;
        }

        public static object GetSingle(Type type) { return GetSingleGeneric(type, true); }

        public static bool TryGetNew<TInterface>(
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)] out TInterface provider
#else
            out TInterface? provider
#endif
        )
        {
            provider = GetNew<TInterface>(false);
            return provider != null;
        }

        public static TInterface GetNew<TInterface>() { return GetNew<TInterface>(true)!; }

        public static bool TryGetNew(Type type, out object provider)
        {
            provider = GetNewGeneric(type, false);
            return provider != null;
        }

        public static object GetNew(Type type) { return GetNewGeneric(type, true); }

        private static readonly MethodInfo methodProviderManagerGetSingle = typeof(Resolver).GetMethod(nameof(Resolver.GetSingle), BindingFlags.Static | BindingFlags.NonPublic) ?? throw new Exception($"{nameof(Resolver)}.{nameof(Resolver.GetSingle)} method not found");
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object GetSingleGeneric(Type type, bool throwException)
        {
            var genericMethodProviderManagerTryGetSingle = TypeAnalyzer.GetGenericMethodDetail(methodProviderManagerGetSingle, type);
            var provider = genericMethodProviderManagerTryGetSingle.Caller(null, new object[] { throwException })!;
            return provider;
        }

        private static readonly MethodInfo methodProviderManagerGetNew = typeof(Resolver).GetMethod(nameof(Resolver.GetNew), BindingFlags.Static | BindingFlags.NonPublic) ?? throw new Exception($"{nameof(Resolver)}.{nameof(Resolver.GetNew)} method not found");
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object GetNewGeneric(Type type, bool throwException)
        {
            var genericMethodProviderManagerTryGetNew = TypeAnalyzer.GetGenericMethodDetail(methodProviderManagerGetNew, type);
            var provider = genericMethodProviderManagerTryGetNew.Caller(null, new object[] { throwException })!;
            return provider;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static InterfaceT? GetSingle<InterfaceT>(bool throwException)
        {
            var interfaceType = typeof(InterfaceT);
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Generic parameter must be an interface");

            var providerType = Discovery.GetClassByInterface(interfaceType, throwException);

            if (providerType == null)
                return default;

            var provider = (InterfaceT)Instantiator.GetSingle(providerType);
            if (provider == null)
                return provider;

            return provider;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static InterfaceT? GetNew<InterfaceT>(bool throwException)
        {
            var interfaceType = typeof(InterfaceT);
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Generic parameter must be an interface");

            var providerType = Discovery.GetClassByInterface(interfaceType, throwException);

            if (providerType == null)
                return default;

            var provider = (InterfaceT)Instantiator.Create(providerType);
            if (provider == null)
                return provider;

            return provider;
        }
    }
}

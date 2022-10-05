// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Zerra.Reflection
{
    public static class Resolver
    {
        public static bool TryGet<TInterface>(out TInterface provider)
        {
            provider = Get<TInterface>(false);
            return provider != null;
        }

        public static TInterface Get<TInterface>(){ return Get<TInterface>(true); }

        public static bool TryGet(Type type, out object provider)
        {
            provider = GetGeneric(type, false);
            return provider != null;
        }

        public static object Get(Type type) { return GetGeneric(type, true); }

        private static readonly MethodInfo methodProviderManagerGet = typeof(Resolver).GetMethod(nameof(Resolver.Get), BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object GetGeneric(Type type, bool throwException)
        {
            var genericMethodProviderManagerTryGet = TypeAnalyzer.GetGenericMethodDetail(methodProviderManagerGet, type);
            var provider = genericMethodProviderManagerTryGet.Caller(null, new object[] { throwException });
            return provider;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static InterfaceT Get<InterfaceT>(bool throwException)
        {
            var interfaceType = typeof(InterfaceT);
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Generic parameter must be an interface");

            var providerType = Discovery.GetImplementationType(interfaceType, throwException);

            if (providerType == null)
                return default;

            var provider = (InterfaceT)Instantiator.GetSingleInstance(providerType);
            if (provider == null)
                return provider;

            return provider;
        }
    }
}

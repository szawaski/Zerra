// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zerra.Reflection;

namespace Zerra.Providers
{
    public static class ProviderResolver
    {
        public static bool TryGet<TInterface>(out TInterface provider) where TInterface : IBaseProvider
        {
            provider = Get<TInterface>(null, false);
            return provider != null;
        }
        public static bool TryGet<TInterface>(Type providerInterfaceType, out TInterface provider) where TInterface : IBaseProvider
        {
            provider = Get<TInterface>(providerInterfaceType, false);
            return provider != null;
        }

        public static TInterface Get<TInterface>() where TInterface : IBaseProvider { return Get<TInterface>(null, true); }
        public static TInterface Get<TInterface>(Type providerInterfaceType) where TInterface : IBaseProvider { return Get<TInterface>(providerInterfaceType, true); }

        public static bool TryGet(Type type, out object provider)
        {
            provider = GetGeneric(type, null, false);
            return provider != null;
        }
        public static bool TryGet(Type type, Type providerInterfaceType, out object provider)
        {
            provider = GetGeneric(type, providerInterfaceType, false);
            return provider != null;
        }

        public static object Get(Type type) { return GetGeneric(type, null, true); }
        public static object Get(Type type, Type providerInterfaceType) { return GetGeneric(type, providerInterfaceType, true); }

        private static readonly MethodInfo methodProviderManagerGet = typeof(ProviderResolver).GetMethod(nameof(ProviderResolver.Get), BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object GetGeneric(Type type, Type providerInterfaceType, bool throwException)
        {
            var genericMethodProviderManagerTryGet = TypeAnalyzer.GetGenericMethodDetail(methodProviderManagerGet, type);
            var provider = genericMethodProviderManagerTryGet.Caller(null, new object[] { providerInterfaceType, throwException });
            return provider;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static InterfaceT Get<InterfaceT>(Type providerInterface, bool throwException)
            where InterfaceT : IBaseProvider
        {
            var interfaceType = typeof(InterfaceT);
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Generic parameter must be an interface derived from {typeof(IBaseProvider).Name}");

            if (providerInterface != null && (!providerInterface.IsInterface || !ProviderLayers.GetProviderInterfaceStack().Contains(providerInterface)))
                throw new ArgumentException($"Provider Interface parameter must be one of the following {String.Join(", ", ProviderLayers.GetProviderInterfaceStack().Select(x => x.Name).ToArray())}");

            int interfaceIndex;
            if (providerInterface != null)
                interfaceIndex = ProviderLayers.GetProviderInterfaceIndex(providerInterface);
            else
                interfaceIndex = 0;

            var providerType = Discovery.GetImplementationType(interfaceType, ProviderLayers.GetProviderInterfaceStack(), interfaceIndex, throwException);

            if (providerType == null)
                return default;

            var provider = (InterfaceT)Instantiator.GetSingle(providerType);
            if (provider == null)
                return provider;

            return provider;
        }
    }
}

// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra.Providers
{
    public static class ProviderResolver
    {
        private static readonly Type[] interfaceStack = new Type[]
        {
            typeof(IRuleProvider),
            typeof(ICacheProvider),
            typeof(IEncryptionProvider),
            typeof(ICompressionProvider),
            typeof(IDualBaseProvider),
            null
        };

        public static IReadOnlyList<Type> InterfaceStack => interfaceStack;

        public static int GetInterfaceIndex(Type interfaceType)
        {
            var i = 0;
            for (; i < interfaceStack.Length; i++)
            {
                if (interfaceStack[i] == interfaceType)
                    return i;
            }
            return i;
        }

        private static readonly ConcurrentFactoryDictionary<Type, Type> highestProviderInterfaceTypes = new();
        public static bool TryGetNextType<TInterface>(Type providerType, out Type nextProviderType)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var highestInterfaceType = highestProviderInterfaceTypes.GetOrAdd(providerType, (t) =>
            {
                Type highest = null;
                var providerTypeDetail = TypeAnalyzer.GetTypeDetail(t);

                foreach (var providerInterface in interfaceStack)
                {
                    foreach (var thisInterface in providerTypeDetail.Interfaces)
                    {
                        if (providerInterface == thisInterface)
                        {
                            highest = providerInterface;
                            break;
                        }
                    }
                    if (highest != null)
                        break;
                }
                return highest;
            });

            if (highestInterfaceType == null)
            {
                nextProviderType = null;
                return false;
            }
            
            var nextInterfaceIndex = 0;
            for (var i = 0; i < interfaceStack.Length - 1; i++)
            {
                if (interfaceStack[i] == highestInterfaceType)
                {
                    nextInterfaceIndex = i + 1;
                    break;
                }
            }

            nextProviderType = Discovery.GetImplementationType(interfaceType, interfaceStack, nextInterfaceIndex, false);
            return nextProviderType != null;
        }
        public static bool TryGetNext<TInterface>(Type providerType, out TInterface provider)
        {
            if (!TryGetNextType<TInterface>(providerType, out var nextProviderType))
            {
                provider = default;
                return false;
            }
            provider = (TInterface)Instantiator.GetSingle(nextProviderType);
            return true;
        }

        public static bool TryGet<TInterface>(out TInterface provider)
        {
            provider = GetGeneric<TInterface>(null, false);
            return provider != null;
        }
        public static bool TryGet<TInterface>(Type providerInterfaceType, out TInterface provider)
        {
            provider = GetGeneric<TInterface>(providerInterfaceType, false);
            return provider != null;
        }

        public static TInterface Get<TInterface>() => GetGeneric<TInterface>(null, true);
        public static TInterface Get<TInterface>(Type providerInterfaceType) => GetGeneric<TInterface>(providerInterfaceType, true);

        private static readonly MethodInfo methodProviderManagerGet = typeof(ProviderResolver).GetMethod(nameof(GetGeneric), BindingFlags.Static | BindingFlags.NonPublic);
        private static object Get(Type type, Type providerInterfaceType, bool throwException)
        {
            var genericMethodProviderManagerTryGet = TypeAnalyzer.GetGenericMethodDetail(methodProviderManagerGet, type);
            var provider = genericMethodProviderManagerTryGet.Caller(null, new object[] { providerInterfaceType, throwException });
            return provider;
        }

        private static TInterface GetGeneric<TInterface>(Type providerInterface, bool throwException)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            if (providerInterface != null && (!providerInterface.IsInterface || !interfaceStack.Contains(providerInterface)))
                throw new ArgumentException($"Provider Interface parameter must be one of the following {String.Join(", ", interfaceStack.Where(x => x != null).Select(x => x.Name).ToArray())}");

            int interfaceIndex;
            if (providerInterface != null)
                interfaceIndex = GetInterfaceIndex(providerInterface);
            else
                interfaceIndex = 0;

            var providerType = Discovery.GetImplementationType(interfaceType, interfaceStack, interfaceIndex, throwException);

            if (providerType == null)
                return default;

            var provider = (TInterface)Instantiator.GetSingle(providerType);
            if (provider == null)
                return provider;

            return provider;
        }
    }
}

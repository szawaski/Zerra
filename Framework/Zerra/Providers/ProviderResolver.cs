// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
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

        private static Type ignoreInterface = typeof(IIgnoreProviderResolver);

        public static IReadOnlyList<Type> InterfaceStack => interfaceStack;
        public static Type IgnoreInterface => ignoreInterface;

        private static int GetInterfaceIndex(Type interfaceType)
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
        public static Type GetNextType<TInterface>(Type providerType)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var highestInterfaceType = highestProviderInterfaceTypes.GetOrAdd(providerType, (providerType) =>
            {
                Type highest = null;
                var providerTypeDetail = TypeAnalyzer.GetTypeDetail(providerType);

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
                return null;

            var nextInterfaceIndex = 0;
            for (var i = 0; i < interfaceStack.Length - 1; i++)
            {
                if (interfaceStack[i] == highestInterfaceType)
                {
                    nextInterfaceIndex = i + 1;
                    break;
                }
            }

            var nextProviderType = Discovery.GetImplementationClass(interfaceType, interfaceStack, nextInterfaceIndex, ignoreInterface, false);
            return nextProviderType;
        }
        public static TInterface GetNext<TInterface>(Type providerType)
        {
            var nextProviderType = GetNextType<TInterface>(providerType);
            if (nextProviderType == null)
                return default;

            var provider = (TInterface)Instantiator.GetSingle(nextProviderType);
            return provider;
        }

        public static TInterface GetFirst<TInterface>(bool throwException = true) => (TInterface)GetFirst(typeof(TInterface), throwException);
        public static object GetFirst(Type interfaceType, bool throwException = true)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = Discovery.GetImplementationClass(interfaceType, interfaceStack, 0, ignoreInterface, throwException);

            if (providerType == null)
                return default;

            var provider = Instantiator.GetSingle(providerType);
            if (provider == null)
                return provider;

            return provider;
        }
        public static Type GetFirstType(Type interfaceType, bool throwException = true)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = Discovery.GetImplementationClass(interfaceType, interfaceStack, 0, ignoreInterface, throwException);
            return providerType;
        }

        public static TInterface GetBase<TInterface>(bool throwException = true) => (TInterface)GetBase(typeof(TInterface), throwException);
        public static object GetBase(Type interfaceType, bool throwException = true)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = Discovery.GetImplementationClass(interfaceType, interfaceStack, interfaceStack.Length - 1, ignoreInterface, throwException);

            if (providerType == null)
                return default;

            var provider = Instantiator.GetSingle(providerType);
            if (provider == null)
                return provider;

            return provider;
        }
        public static Type GetBaseType(Type interfaceType, bool throwException = true)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = Discovery.GetImplementationClass(interfaceType, interfaceStack, interfaceStack.Length - 1, ignoreInterface, throwException);

            return providerType;
        }

        public static TInterface GetSpecific<TInterface>(Type secondaryInterfaceType, bool throwException = true) => (TInterface)GetSpecific(typeof(TInterface), secondaryInterfaceType, throwException);
        public static object GetSpecific(Type interfaceType, Type secondaryInterfaceType, bool throwException = true)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var index = GetInterfaceIndex(secondaryInterfaceType);
            var providerType = Discovery.GetImplementationClass(interfaceType, interfaceStack, index, ignoreInterface, throwException);

            if (providerType == null)
                return default;

            var provider = Instantiator.GetSingle(providerType);
            if (provider == null)
                return provider;

            return provider;
        }
        public static Type GetSpecificType(Type interfaceType, Type secondaryInterfaceType, bool throwException = true)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var index = GetInterfaceIndex(secondaryInterfaceType);
            var providerType = Discovery.GetImplementationClass(interfaceType, interfaceStack, index, ignoreInterface, throwException);
            return providerType;
        }

        public static bool HasAny<TInterface>() => HasAny(typeof(TInterface));
        public static bool HasAny(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var has = Discovery.HasImplementationClass(interfaceType, interfaceStack, 0, ignoreInterface);

            return has;
        }

        public static bool HasSpecific<TInterface>(Type secondaryInterfaceType) => HasSpecific(typeof(TInterface), secondaryInterfaceType);
        public static bool HasSpecific(Type interfaceType, Type secondaryInterfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var index = GetInterfaceIndex(secondaryInterfaceType);
            var has = Discovery.HasImplementationClass(interfaceType, interfaceStack, index, ignoreInterface);

            return has;
        }

        public static bool HasBase<TInterface>() => HasBase(typeof(TInterface));
        public static bool HasBase(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var has = Discovery.HasImplementationClass(interfaceType, interfaceStack, interfaceStack.Length - 1, ignoreInterface);

            return has;
        }
    }
}

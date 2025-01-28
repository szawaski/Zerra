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
        private static readonly Type?[] interfaceStack =
        [
            typeof(IRuleProvider),
            typeof(ICacheProvider),
            typeof(IEncryptionProvider),
            typeof(ICompressionProvider),
            typeof(IDualBaseProvider),
            null
        ];

        private static readonly Type ignoreInterface = typeof(IIgnoreProviderResolver);

        public static IReadOnlyList<Type?> InterfaceStack => interfaceStack;
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

        private static readonly ConcurrentFactoryDictionary<Type, Type?> highestProviderInterfaceTypes = new();
        public static Type? GetNextType<TInterface>(Type providerType)
        {
            var interfaceType = typeof(TInterface);
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var highestInterfaceType = highestProviderInterfaceTypes.GetOrAdd(providerType, static (providerType) =>
            {
                Type? highest = null;
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
                    if (highest is not null)
                        break;
                }
                return highest;
            });

            if (highestInterfaceType is null)
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

            var nextProviderType = Discovery.GetClassByInterface(interfaceType, interfaceStack, nextInterfaceIndex, ignoreInterface, false);
            return nextProviderType;
        }
        public static TInterface? GetNext<TInterface>(Type providerType)
        {
            var nextProviderType = GetNextType<TInterface>(providerType);
            if (nextProviderType is null)
                return default;

            var provider = (TInterface)Instantiator.GetSingle(nextProviderType);
            return provider;
        }

        public static TInterface GetFirst<TInterface>() => (TInterface)GetFirst(typeof(TInterface));
        public static object GetFirst(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = Discovery.GetClassByInterface(interfaceType, interfaceStack, 0, ignoreInterface, true)!;
            var provider = Instantiator.GetSingle(providerType);

            return provider;
        }
        public static Type GetTypeFirst(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = Discovery.GetClassByInterface(interfaceType, interfaceStack, 0, ignoreInterface, true)!;
            return providerType;
        }

        public static TInterface GetBase<TInterface>() => (TInterface)GetBase(typeof(TInterface));
        public static object GetBase(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = Discovery.GetClassByInterface(interfaceType, interfaceStack, interfaceStack.Length - 1, ignoreInterface, true)!;
            var provider = Instantiator.GetSingle(providerType);

            return provider;
        }
        public static Type GetTypeBase(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = Discovery.GetClassByInterface(interfaceType, interfaceStack, interfaceStack.Length - 1, ignoreInterface, true)!;

            return providerType;
        }

        public static TInterface GetSpecific<TInterface>(Type secondaryInterfaceType) => (TInterface)GetSpecific(typeof(TInterface), secondaryInterfaceType);
        public static object GetSpecific(Type interfaceType, Type secondaryInterfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var index = GetInterfaceIndex(secondaryInterfaceType);
            var providerType = Discovery.GetClassByInterface(interfaceType, interfaceStack, index, ignoreInterface, true)!;

            var provider = Instantiator.GetSingle(providerType);

            return provider;
        }
        public static Type GetSpecificType(Type interfaceType, Type secondaryInterfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var index = GetInterfaceIndex(secondaryInterfaceType);
            var providerType = Discovery.GetClassByInterface(interfaceType, interfaceStack, index, ignoreInterface, true)!;
            return providerType;
        }

        public static TInterface? GetFirstOrDefault<TInterface>() => (TInterface?)GetFirstOrDefault(typeof(TInterface));
        public static object? GetFirstOrDefault(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = Discovery.GetClassByInterface(interfaceType, interfaceStack, 0, ignoreInterface, false);
            if (providerType is null)
                return null;

            var provider = Instantiator.GetSingle(providerType);

            return provider;
        }
        public static Type? GetTypeFirstOrDefault(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = Discovery.GetClassByInterface(interfaceType, interfaceStack, 0, ignoreInterface, false);
            if (providerType is null)
                return null;

            return providerType;
        }

        public static TInterface? GetBaseOrDefault<TInterface>() => (TInterface?)GetBaseOrDefault(typeof(TInterface));
        public static object? GetBaseOrDefault(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = Discovery.GetClassByInterface(interfaceType, interfaceStack, interfaceStack.Length - 1, ignoreInterface, true);
            if (providerType is null)
                return null;

            var provider = Instantiator.GetSingle(providerType);
            return provider;
        }
        public static Type? GetTypeBaseOrDefault(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = Discovery.GetClassByInterface(interfaceType, interfaceStack, interfaceStack.Length - 1, ignoreInterface, false);
            if (providerType is null)
                return null;

            return providerType;
        }

        public static TInterface? GetSpecificOrDefault<TInterface>(Type secondaryInterfaceType) => (TInterface?)GetSpecificOrDefault(typeof(TInterface), secondaryInterfaceType);
        public static object? GetSpecificOrDefault(Type interfaceType, Type secondaryInterfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var index = GetInterfaceIndex(secondaryInterfaceType);
            var providerType = Discovery.GetClassByInterface(interfaceType, interfaceStack, index, ignoreInterface, true);
            if (providerType is null)
                return null;

            var provider = Instantiator.GetSingle(providerType);

            return provider;
        }
        public static Type? GetTypeSpecificOrDefault(Type interfaceType, Type secondaryInterfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var index = GetInterfaceIndex(secondaryInterfaceType);
            var providerType = Discovery.GetClassByInterface(interfaceType, interfaceStack, index, ignoreInterface, false);
            if (providerType is null)
                return null;

            return providerType;
        }

        public static bool HasAny<TInterface>() => HasAny(typeof(TInterface));
        public static bool HasAny(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var has = Discovery.HasClassByInterface(interfaceType, interfaceStack, 0, ignoreInterface);

            return has;
        }

        public static bool HasSpecific<TInterface>(Type secondaryInterfaceType) => HasSpecific(typeof(TInterface), secondaryInterfaceType);
        public static bool HasSpecific(Type interfaceType, Type secondaryInterfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var index = GetInterfaceIndex(secondaryInterfaceType);
            var has = Discovery.HasClassByInterface(interfaceType, interfaceStack, index, ignoreInterface);

            return has;
        }

        public static bool HasBase<TInterface>() => HasBase(typeof(TInterface));
        public static bool HasBase(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var has = Discovery.HasClassByInterface(interfaceType, interfaceStack, interfaceStack.Length - 1, ignoreInterface);

            return has;
        }
    }
}

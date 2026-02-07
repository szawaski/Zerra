// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Collections;
using Zerra.Reflection;
using Zerra.Reflection.Dynamic;

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

            var nextProviderType = GetClassByInterface(interfaceType, interfaceStack, nextInterfaceIndex, ignoreInterface, false);
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

            var providerType = GetClassByInterface(interfaceType, interfaceStack, 0, ignoreInterface, true)!;
            var provider = Instantiator.GetSingle(providerType);

            return provider;
        }
        public static Type GetTypeFirst(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = GetClassByInterface(interfaceType, interfaceStack, 0, ignoreInterface, true)!;
            return providerType;
        }

        public static TInterface GetBase<TInterface>() => (TInterface)GetBase(typeof(TInterface));
        public static object GetBase(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = GetClassByInterface(interfaceType, interfaceStack, interfaceStack.Length - 1, ignoreInterface, true)!;
            var provider = Instantiator.GetSingle(providerType);

            return provider;
        }
        public static Type GetTypeBase(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = GetClassByInterface(interfaceType, interfaceStack, interfaceStack.Length - 1, ignoreInterface, true)!;

            return providerType;
        }

        public static TInterface GetSpecific<TInterface>(Type secondaryInterfaceType) => (TInterface)GetSpecific(typeof(TInterface), secondaryInterfaceType);
        public static object GetSpecific(Type interfaceType, Type secondaryInterfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var index = GetInterfaceIndex(secondaryInterfaceType);
            var providerType = GetClassByInterface(interfaceType, interfaceStack, index, ignoreInterface, true)!;

            var provider = Instantiator.GetSingle(providerType);

            return provider;
        }
        public static Type GetSpecificType(Type interfaceType, Type secondaryInterfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var index = GetInterfaceIndex(secondaryInterfaceType);
            var providerType = GetClassByInterface(interfaceType, interfaceStack, index, ignoreInterface, true)!;
            return providerType;
        }

        public static TInterface? GetFirstOrDefault<TInterface>() => (TInterface?)GetFirstOrDefault(typeof(TInterface));
        public static object? GetFirstOrDefault(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = GetClassByInterface(interfaceType, interfaceStack, 0, ignoreInterface, false);
            if (providerType is null)
                return null;

            var provider = Instantiator.GetSingle(providerType);

            return provider;
        }
        public static Type? GetTypeFirstOrDefault(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = GetClassByInterface(interfaceType, interfaceStack, 0, ignoreInterface, false);
            if (providerType is null)
                return null;

            return providerType;
        }

        public static TInterface? GetBaseOrDefault<TInterface>() => (TInterface?)GetBaseOrDefault(typeof(TInterface));
        public static object? GetBaseOrDefault(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = GetClassByInterface(interfaceType, interfaceStack, interfaceStack.Length - 1, ignoreInterface, true);
            if (providerType is null)
                return null;

            var provider = Instantiator.GetSingle(providerType);
            return provider;
        }
        public static Type? GetTypeBaseOrDefault(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var providerType = GetClassByInterface(interfaceType, interfaceStack, interfaceStack.Length - 1, ignoreInterface, false);
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
            var providerType = GetClassByInterface(interfaceType, interfaceStack, index, ignoreInterface, true);
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
            var providerType = GetClassByInterface(interfaceType, interfaceStack, index, ignoreInterface, false);
            if (providerType is null)
                return null;

            return providerType;
        }

        public static bool HasAny<TInterface>() => HasAny(typeof(TInterface));
        public static bool HasAny(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var has = HasClassByInterface(interfaceType, interfaceStack, 0, ignoreInterface);

            return has;
        }

        public static bool HasSpecific<TInterface>(Type secondaryInterfaceType) => HasSpecific(typeof(TInterface), secondaryInterfaceType);
        public static bool HasSpecific(Type interfaceType, Type secondaryInterfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var index = GetInterfaceIndex(secondaryInterfaceType);
            var has = HasClassByInterface(interfaceType, interfaceStack, index, ignoreInterface);

            return has;
        }

        public static bool HasBase<TInterface>() => HasBase(typeof(TInterface));
        public static bool HasBase(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Generic parameter must be an interface");

            var has = HasClassByInterface(interfaceType, interfaceStack, interfaceStack.Length - 1, ignoreInterface);

            return has;
        }


        private static unsafe bool HasClassByInterface(Type interfaceType, IReadOnlyList<Type?> secondaryInterfaces, int secondaryInterfaceStartIndex, Type ignoreInterface)
        {
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.Name} is not an interface");
            if (secondaryInterfaces is null)
                throw new ArgumentNullException(nameof(secondaryInterfaces));
            if (secondaryInterfaceStartIndex < 0 || secondaryInterfaceStartIndex > secondaryInterfaces.Count - 1)
                throw new ArgumentOutOfRangeException(nameof(secondaryInterfaceStartIndex));

            var classList = Discovery.GetClassesByInterface(interfaceType);

            var levels = stackalloc short[classList.Count];

            for (var j = 0; j < classList.Count; j++)
                levels[j] = -2;

            var firstLevelFound = -1;

            for (var j = 0; j < classList.Count; j++)
            {
                var classType = classList[j];

                var interfaceList = Discovery.GetInterfacesByType(classType);
                if (interfaceList.Count == 0)
                {
                    levels[j] = -1;
                    continue;
                }

                for (var i = 0; i < secondaryInterfaces.Count; i++)
                {
                    if (secondaryInterfaces[i] is not null)
                    {
                        var found = false;
                        for (var k = 0; k < interfaceList.Count; k++)
                        {
                            if (interfaceList[k] == secondaryInterfaces[i])
                            {
                                found = true;
                            }
                            if (ignoreInterface is not null && ignoreInterface == interfaceList[k])
                            {
                                found = false;
                                break;
                            }
                        }
                        if (found)
                        {
                            levels[j] = (short)i;
                            if (i >= secondaryInterfaceStartIndex && (i <= firstLevelFound || firstLevelFound == -1))
                                firstLevelFound = i;
                            break;
                        }
                    }
                    else //base provider
                    {
                        var found = false;
                        for (var k = 0; k < interfaceList.Count; k++)
                        {
                            if (ignoreInterface is not null && ignoreInterface == interfaceList[k])
                            {
                                found = true;
                                break;
                            }
                            if (secondaryInterfaces.Contains(interfaceList[k]))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            levels[j] = (short)i;
                            if (i >= secondaryInterfaceStartIndex && (i <= firstLevelFound || firstLevelFound == -1))
                                firstLevelFound = i;
                            break;
                        }
                    }
                }
            }

            var index = -1;
            for (var j = 0; j < classList.Count; j++)
            {
                if (levels[j] == firstLevelFound)
                {
                    if (index == -1)
                    {
                        index = j;
                        break;
                    }
                }
            }

            return index != -1;
        }
        private static unsafe Type? GetClassByInterface(Type interfaceType, IReadOnlyList<Type?> secondaryInterfaces, int secondaryInterfaceStartIndex, Type ignoreInterface, bool throwException = true)
        {
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.Name} is not an interface");
            if (secondaryInterfaces is null)
                throw new ArgumentNullException(nameof(secondaryInterfaces));
            if (secondaryInterfaceStartIndex < 0 || secondaryInterfaceStartIndex > secondaryInterfaces.Count - 1)
                throw new ArgumentOutOfRangeException(nameof(secondaryInterfaceStartIndex));

            var classList = Discovery.GetClassesByInterface(interfaceType);

            var levels = stackalloc short[classList.Count];

            for (var j = 0; j < classList.Count; j++)
                levels[j] = -2;

            var firstLevelFound = -1;

            for (var j = 0; j < classList.Count; j++)
            {
                var classType = classList[j];

                var interfaceList = Discovery.GetInterfacesByType(classType);
                if (interfaceList.Count == 0)
                {
                    levels[j] = -1;
                    continue;
                }

                for (var i = 0; i < secondaryInterfaces.Count; i++)
                {
                    if (secondaryInterfaces[i] is not null)
                    {
                        var found = false;
                        for (var k = 0; k < interfaceList.Count; k++)
                        {
                            if (interfaceList[k] == secondaryInterfaces[i])
                            {
                                found = true;
                            }
                            if (ignoreInterface is not null && ignoreInterface == interfaceList[k])
                            {
                                found = false;
                                break;
                            }
                        }
                        if (found)
                        {
                            levels[j] = (short)i;
                            if (i >= secondaryInterfaceStartIndex && (i <= firstLevelFound || firstLevelFound == -1))
                                firstLevelFound = i;
                            break;
                        }
                    }
                    else //base provider
                    {
                        var found = false;
                        for (var k = 0; k < interfaceList.Count; k++)
                        {
                            if (ignoreInterface is not null && ignoreInterface == interfaceList[k])
                            {
                                found = true;
                                break;
                            }
                            if (secondaryInterfaces.Contains(interfaceList[k]))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            levels[j] = (short)i;
                            if (i >= secondaryInterfaceStartIndex && (i <= firstLevelFound || firstLevelFound == -1))
                                firstLevelFound = i;
                            break;
                        }
                    }
                }
            }

            var index = -1;
            for (var j = 0; j < classList.Count; j++)
            {
                if (levels[j] == firstLevelFound)
                {
                    if (index == -1)
                        index = j;
                    else
                        index = -2;
                }
            }

            if (index == -2)
            {
                if (throwException)
                    throw new Exception($"Multiple classes found for {interfaceType.Name} with secondary interfaces type {secondaryInterfaces[firstLevelFound]?.Name}");
                else
                    return null;
            }
            if (index == -1)
            {
                if (throwException)
                    throw new Exception($"No classes found for {interfaceType.Name} with secondary interfaces types {String.Join(", ", secondaryInterfaces.Select(x => x is null ? "null" : x.Name))}");
                else
                    return null;
            }

            return classList[index];
        }
    }
}

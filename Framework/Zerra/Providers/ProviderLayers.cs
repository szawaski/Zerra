// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra.Providers
{
    public static class ProviderLayers
    {
        private static readonly Type[] providerInterfaceLayersStack = new Type[]
        {
            typeof(IRuleProvider),
            typeof(ICacheProvider),
            typeof(IEncryptionProvider),
            typeof(ICompressionProvider),
            typeof(IDualBaseProvider),
            typeof(IBaseProvider),
        };

        public static Type[] GetProviderInterfaceStack()
        {
            return providerInterfaceLayersStack;
        }
        public static Type GetProviderInterfaceLayerAfter(Type providerInterfaceLayerType)
        {
            if (providerInterfaceLayerType == null)
                throw new ArgumentNullException(nameof(providerInterfaceLayerType));
            for (var i = 0; i < providerInterfaceLayersStack.Length - 1; i++)
            {
                if (providerInterfaceLayersStack[i] == providerInterfaceLayerType)
                    return providerInterfaceLayersStack[i + 1];
            }
            throw new ArgumentException($"Provider Interface parameter must be one of the following {String.Join(", ", providerInterfaceLayersStack.Select(x => x.Name).ToArray())}");
        }
        public static int GetProviderInterfaceIndex(Type providerInterfaceType)
        {
            if (providerInterfaceType == null)
                throw new ArgumentNullException(nameof(providerInterfaceType));
            var i = 0;
            for (; i < providerInterfaceLayersStack.Length; i++)
            {
                if (providerInterfaceLayersStack[i] == providerInterfaceType)
                    return i;
            }
            return i;
        }

        private static readonly ConcurrentFactoryDictionary<Type, Type> highestProviderInterfaces = new ConcurrentFactoryDictionary<Type, Type>();
        public static Type GetHighestProviderInterface(Type providerType)
        {
            if (providerType == null)
                throw new ArgumentNullException(nameof(providerType));
            var highestProviderInterface = highestProviderInterfaces.GetOrAdd(providerType, (t) =>
            {
                Type highest = null;
                var providerTypeInfo = TypeAnalyzer.GetTypeDetail(t);

                foreach (var providerInterface in providerInterfaceLayersStack)
                {
                    foreach (var thisInterface in providerTypeInfo.Interfaces)
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

            return highestProviderInterface;
        }
    }
}

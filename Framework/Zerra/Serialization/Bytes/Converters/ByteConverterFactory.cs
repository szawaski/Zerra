// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal static class ByteConverterFactory<TParent>
    {
        private static readonly ConcurrentFactoryDictionary<Type, ConcurrentFactoryDictionary<string, ByteConverter<TParent>>> cache = new();
        private static ByteConverterTypeRequired<TParent>? cacheByteConverterTypeInfo;

        private static readonly Type parentType = typeof(TParent);

        public static ByteConverter<TParent> GetRoot(TypeDetail typeDetail)
             => Get(typeDetail, null, null, null);

        public static ByteConverter<TParent> Get(TypeDetail typeDetail, string? memberKey, Delegate? getter, Delegate? setter)
        {
            var cache2 = cache.GetOrAdd(typeDetail.Type, x => new());
            var converter = cache2.GetOrAdd(memberKey ?? String.Empty, x =>
            {
                var newConverter = Create(typeDetail);
                newConverter.Setup(typeDetail, memberKey, getter, setter);
                return newConverter;
            });
            return converter;
        }

        public static ByteConverter<TParent> GetTypeRequired()
        {
            if (cacheByteConverterTypeInfo == null)
            {
                lock (cache)
                {
                    if (cacheByteConverterTypeInfo == null)
                    {
                        var newConverter = new ByteConverterTypeRequired<TParent>();
                        newConverter.Setup(typeof(object).GetTypeDetail(), null, null, null);
                        cacheByteConverterTypeInfo = newConverter;
                    }
                }
            }
            return cacheByteConverterTypeInfo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Type? Discover(Type interfaceType)
        {
            var discoveredTypes = Discovery.GetClassesByInterface(interfaceType);
            Type? discoveredType;
            if (discoveredTypes.Count == 1)
            {
                discoveredType = discoveredTypes[0];
            }
            else if (discoveredTypes.Count > 1)
            {
                var customDiscoveredTypes = discoveredTypes.Where(x => x.Namespace == null || !x.Namespace.StartsWith("Zerra.")).ToList();
                if (customDiscoveredTypes.Count == 1)
                {
                    discoveredType = customDiscoveredTypes[0];
                }
                else if (customDiscoveredTypes.Count > 1)
                {
                    throw new InvalidOperationException($"Multiple custom implementations of {nameof(ByteConverter)} found for {interfaceType.GetNiceName()}");
                }
                else
                {
                    discoveredType = null;
                }
            }
            else
            {
                discoveredType = null;
            }
            return discoveredType;
        }

        private static readonly Type byteConverterHandlesType = typeof(IByteConverterHandles<>);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ByteConverter<TParent> Create(TypeDetail typeDetail)
        {
            //exact match
            {
                var interfaceType = byteConverterHandlesType.GetGenericType(typeDetail.Type);
                var discoveredType = Discover(interfaceType);

                if (discoveredType != null)
                {
                    var discoveredTypeDetail = discoveredType.GetTypeDetail();
                    if (discoveredTypeDetail.InnerTypes.Count == 1)
                    {
                        var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType);
                        var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                        return (ByteConverter<TParent>)converter;
                    }
                }
            }

            //generic match
            if (typeDetail.Type.IsGenericType)
            {
                var genericType = typeDetail.Type.GetGenericTypeDefinition();
                var interfaceType = byteConverterHandlesType.GetGenericType(genericType);
                var name = interfaceType.GetNiceName();
                var discoveredType = Discover(interfaceType);
                if (discoveredType != null)
                {
                    var discoveredTypeDetail = discoveredType.GetTypeDetail();
                    if (discoveredTypeDetail.InnerTypes.Count == 3)
                    {
                        var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerType);
                        var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                        return (ByteConverter<TParent>)converter;
                    }
                }
            }

            //enum
            if (typeDetail.Type.IsEnum || typeDetail.IsNullable && typeDetail.InnerTypes[0].IsEnum)
            {
                var converter = typeof(ByteConverterEnum<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //array
            if (typeDetail.Type.IsArray)
            {
                var converter = typeof(ByteConverterArray<,>).GetGenericTypeDetail(parentType, typeDetail.InnerTypes[0]).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //generic list
            if (typeDetail.IsIList && typeDetail.Type.IsGenericType)
            {
                var converter = typeof(ByteConverterListT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0]).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //generic set
            if (typeDetail.IsISet && typeDetail.Type.IsGenericType)
            {
                var converter = typeof(ByteConverterHashSetT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0]).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //generic dictionary
            if (typeDetail.SpecialType == SpecialType.Dictionary)
            {
                var converter = typeof(ByteConverterDictionaryT<,,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0], typeDetail.InnerTypes[1]).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //enumerable
            if (typeDetail.IsIEnumerableGeneric)
            {
                var converter = typeof(ByteConverterArrayT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.IEnumerableGenericInnerType).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //object
            var converterObject = typeof(ByteConverterObject<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
            return (ByteConverter<TParent>)converterObject;
        }
    }
}
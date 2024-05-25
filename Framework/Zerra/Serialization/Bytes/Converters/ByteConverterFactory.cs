// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
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
                        newConverter.Setup(null, null, null, null);
                        cacheByteConverterTypeInfo = newConverter;
                    }
                }
            }
            return cacheByteConverterTypeInfo;
        }

        private static readonly Type byteConverterDiscoverableType = typeof(IByteConverterDiscoverable<>);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ByteConverter<TParent> Create(TypeDetail typeDetail)
        {
            //exact match
            var discoveredType = Discovery.GetImplementationType(byteConverterDiscoverableType.GetGenericType(typeDetail.Type), false);
            if (discoveredType != null)
            {
                var converter = discoveredType.GetGenericTypeDetail(parentType).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
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
                var converter = typeof(ByteConverterDictionaryT<,,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypeDetails[0].InnerTypes[0], typeDetail.InnerTypeDetails[0].InnerTypes[1]).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //enumerable
            if (typeDetail.IsIEnumerableGeneric)
            {
                var converter = typeof(ByteConverterArrayT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0]).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //object
            var converterObject = typeof(ByteConverterObject<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
            return (ByteConverter<TParent>)converterObject;
        }
    }
}
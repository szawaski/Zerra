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
        private static readonly ConcurrentFactoryDictionary<ByteConverterOptions, ConcurrentFactoryDictionary<Type, ConcurrentFactoryDictionary<string, ByteConverter<TParent>>>> cache = new();
        private static readonly ConcurrentFactoryDictionary<ByteConverterOptions, ByteConverterTypeInfo<TParent>> cacheByteConverterTypeInfo = new();
        private static readonly Type defaultType = typeof(object);
        private static readonly Type parentType = typeof(TParent);

        public static ByteConverter<TParent> Get(ByteConverterOptions options, TypeDetail typeDetail, string? memberKey = null, Delegate? getter = null, Delegate? setter = null)
        {
            var cache1 = cache.GetOrAdd(options, x => new());
            var cache2 = cache1.GetOrAdd(typeDetail.Type, x => new());
            var newConverter = cache2.GetOrAdd(memberKey ?? String.Empty, x =>
            {
                var newConverter = Create(typeDetail);
                newConverter.Setup(options, typeDetail, getter, setter);
                return newConverter;
            });

            return newConverter;
        }

        public static ByteConverter<TParent> GetRoot(ByteConverterOptions options, TypeDetail typeDetail)
        {
            if (options.HasFlag(ByteConverterOptions.IncludePropertyTypes) || (typeDetail.Type.IsInterface && !typeDetail.IsIEnumerableGeneric))
            {
                var newConverter = cacheByteConverterTypeInfo.GetOrAdd(options, key =>
                {
                    var newConverter = new ByteConverterTypeInfo<TParent>();
                    newConverter.Setup(options, null, null, null);
                    return newConverter;
                });
                return newConverter;
            }
            return Get(options, typeDetail);
        }

        public static ByteConverter<TParent> GetMayNeedTypeInfo(ByteConverterOptions options, TypeDetail typeDetail, ByteConverter<TParent> converter)
        {
            if (options.HasFlag(ByteConverterOptions.IncludePropertyTypes) || (typeDetail.Type.IsInterface && !typeDetail.IsIEnumerableGeneric))
            {
                var newConverter = cacheByteConverterTypeInfo.GetOrAdd(options, key =>
                {
                    var newConverter = new ByteConverterTypeInfo<TParent>();
                    newConverter.Setup(options, null, null, null);
                    return newConverter;
                });
                return newConverter;
            }
            return converter;
        }

        public static ByteConverter<TParent> GetNeedTypeInfo(ByteConverterOptions options)
        {
            var newConverter = cacheByteConverterTypeInfo.GetOrAdd(options, key =>
            {
                var newConverter = new ByteConverterTypeInfo<TParent>();
                newConverter.Setup(options, null, null, null);
                return newConverter;
            });
            return newConverter;
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
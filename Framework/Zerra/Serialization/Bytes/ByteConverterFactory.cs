// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal static class ByteConverterFactory<TParent>
    {
        private static readonly ConcurrentFactoryDictionary<ByteConverterOptions, ConcurrentFactoryDictionary<Type, ConcurrentFactoryDictionary<Type, ByteConverter<TParent>>>> cache = new();
        private static readonly ConcurrentFactoryDictionary<ByteConverterOptions, ByteConverterTypeInfo<TParent>> cacheByteConverterTypeInfo = new();
        private static readonly Type defaultType = typeof(object);

        public static ByteConverter<TParent> Get(ByteConverterOptions options, TypeDetail typeDetail, TypeDetail? parentTypeDetail = null, Delegate? getter = null, Delegate? setter = null)
        {
            var cache1 = cache.GetOrAdd(options, x => new());
            var cache2 = cache1.GetOrAdd(typeDetail.Type, x => new());
            var newConverter = cache2.GetOrAdd(parentTypeDetail?.Type ?? defaultType, x =>
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

        private static ByteConverter<TParent> Create(TypeDetail typeDetail)
        {
            var discoveredTypes = Discovery.GetImplementationClasses(typeof(IByteConverterDiscoverable<>));
            foreach (var discoveredType in discoveredTypes)
            {
                var discoveredTypeDetail = discoveredType.GetTypeDetail();
                if (discoveredTypeDetail.InnerTypes.Count == 2 && discoveredTypeDetail.InnerTypes[1] == typeDetail.Type)
                {
                    var converter = discoveredTypeDetail.Creator();
                    return (ByteConverter<TParent>)converter;
                }
            }

            var converterObject = typeof(ByteConverterObject<,>).GetGenericTypeDetail(typeof(TParent), typeDetail.Type).Creator();
            return (ByteConverter<TParent>)converterObject;
        }
    }
}
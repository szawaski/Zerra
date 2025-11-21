// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Collections;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.Converters.Collections;
using Zerra.Serialization.Bytes.Converters.Collections.Collections;
using Zerra.Serialization.Bytes.Converters.Collections.Dictionaries;
using Zerra.Serialization.Bytes.Converters.Collections.Enumerables;
using Zerra.Serialization.Bytes.Converters.Collections.Lists;
using Zerra.Serialization.Bytes.Converters.Collections.Sets;
using Zerra.Serialization.Bytes.Converters.General;

namespace Zerra.Serialization.Bytes.Converters
{
    public static class ByteConverterFactory<TParent>
    {
        private static readonly TypeDetail objectTypeDetail = typeof(object).GetTypeDetail();
        private static readonly ConcurrentFactoryDictionary<Type, ConcurrentFactoryDictionary<string, ByteConverter<TParent>>> cache = new();
        private static ByteConverterTypeRequired<TParent>? cacheByteConverterTypeInfo;

        private static readonly Type parentType = typeof(TParent);

        internal static ByteConverter<TParent> GetRoot(TypeDetail typeDetail)
             => Get(typeDetail, "Root", null, null);

        public static ByteConverter<TParent> Get(TypeDetail typeDetail, string memberKey, Delegate? getter, Delegate? setter)
        {
            var cache2 = cache.GetOrAdd(typeDetail.Type, static () => new());
            var converter = cache2.GetOrAdd(memberKey, typeDetail, memberKey, getter, setter, static (typeDetail, memberKey, getter, setter) =>
            {
                var newConverterType = GetConverterType(typeDetail);
                var newConverter = (ByteConverter<TParent>)newConverterType.CreatorBoxed();
                //Debug.WriteLine($"{typeDetail.Type.GetNiceName()} - {newConverter.GetType().GetNiceName()}");
                newConverter.Setup(memberKey, getter, setter);
                return newConverter;
            });
            return converter;
        }

        internal static ByteConverter<TParent> GetDrainBytes()
        {
            if (cacheByteConverterTypeInfo is null)
            {
                lock (cache)
                {
                    if (cacheByteConverterTypeInfo is null)
                    {
                        var newConverter = new ByteConverterTypeRequired<TParent>();
                        newConverter.Setup("Drain", null, null);
                        cacheByteConverterTypeInfo = newConverter;
                    }
                }
            }
            return cacheByteConverterTypeInfo;
        }

        private static TypeDetail GetConverterType(TypeDetail typeDetail)
        {
            var discoveredType = ByteConverterDiscovery.Discover(typeDetail.Type);

            if (discoveredType is not null)
            {
                var discoveredTypeDetail = discoveredType.GetTypeDetail();
                switch (discoveredTypeDetail.InnerTypes.Count)
                {
                    case 1:
                            return discoveredTypeDetail.GetGenericTypeDetail(parentType);
                    case 2:
                        if (typeDetail.InnerTypes.Count == 1)
                            return discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.InnerTypes[0]);
                        else
                            return discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type);
 
                    case 3:
                        if (typeDetail.InnerTypes.Count == 1)
                            return discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0]);
                        else if (typeDetail.InnerTypes.Count == 2)
                            return discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.InnerTypes[0], typeDetail.InnerTypes[1]);
                        else
                            return discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, objectTypeDetail.Type);
                    case 4:
                        if (typeDetail.InnerTypes.Count == 1)
                            return discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0], objectTypeDetail.Type);
                        else if (typeDetail.InnerTypes.Count == 2)
                            return discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0], typeDetail.InnerTypes[1]);
                        else
                            return discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, objectTypeDetail.Type, objectTypeDetail.Type);
                }
            }

            if (typeDetail.CoreType.HasValue)
                throw new NotSupportedException($"No ByteConverter found to support {typeDetail.Type.GetNiceName()}");

            //Enum
            if (typeDetail.Type.IsEnum || typeDetail.IsNullable && typeDetail.InnerType.IsEnum)
                return typeof(ByteConverterEnum<,>).GetGenericTypeDetail(parentType, typeDetail.Type);

            //Array
            if (typeDetail.Type.IsArray)
                return typeof(ByteConverterArrayT<,>).GetGenericTypeDetail(parentType, typeDetail.InnerType);

            //IList<T> of type - specific types that inherit this
            if (typeDetail.HasIListGeneric)
                return typeof(ByteConverterIListTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.IEnumerableGenericInnerType);

            //IList of type - specific types that inherit this
            if (typeDetail.HasIList)
                return typeof(ByteConverterIListOfT<,>).GetGenericTypeDetail(parentType, typeDetail.Type);

            //ISet<T> of type - specific types that inherit this
            if (typeDetail.HasISetGeneric)
                return typeof(ByteConverterISetTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.IEnumerableGenericInnerType);

            //IDictionary<,> of type - specific types that inherit this
            if (typeDetail.HasIDictionaryGeneric)
                return typeof(ByteConverterIDictionaryTOfT<,,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.DictionaryInnerTypeDetail.InnerTypes[0], typeDetail.DictionaryInnerTypeDetail.InnerTypes[1]);

            //IDictionary of type - specific types that inherit this
            if (typeDetail.HasIDictionary)
                return typeof(ByteConverterIDictionaryOfT<,>).GetGenericTypeDetail(parentType, typeDetail.Type);

            //ICollection<T> of type - specific types that inherit this
            if (typeDetail.HasICollectionGeneric)
                return typeof(ByteConverterICollectionTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.IEnumerableGenericInnerType);

            //IEnumerable<T> of type  - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            if (typeDetail.HasIEnumerableGeneric)
                return typeof(ByteConverterIEnumerableTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.IEnumerableGenericInnerType);

            //IEnumerable - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            if (typeDetail.HasIEnumerable)
                return typeof(ByteConverterIEnumerableOfT<,>).GetGenericTypeDetail(parentType, typeDetail.Type);

            //Object
            return typeof(ByteConverterObject<,>).GetGenericTypeDetail(parentType, typeDetail.Type);
        }
    }
}
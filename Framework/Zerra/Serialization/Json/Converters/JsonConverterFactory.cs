// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Collections;
using Zerra.Reflection;
using Zerra.Serialization.Json.Converters.Collections;
using Zerra.Serialization.Json.Converters.Collections.Collections;
using Zerra.Serialization.Json.Converters.Collections.Dictionaries;
using Zerra.Serialization.Json.Converters.Collections.Enumerables;
using Zerra.Serialization.Json.Converters.Collections.Lists;
using Zerra.Serialization.Json.Converters.General;

namespace Zerra.Serialization.Json.Converters
{
    public static class JsonConverterFactory<TParent>
    {
        private static readonly Type objectType = typeof(object);
        private static readonly ConcurrentFactoryDictionary<Type, ConcurrentFactoryDictionary<string, JsonConverter<TParent>>> cache = new();

        private static readonly Type parentType = typeof(TParent);

        internal static JsonConverter<TParent> GetRoot(TypeDetail typeDetail)
             => Get(typeDetail, "Root", null, null);

        public static JsonConverter<TParent> Get(TypeDetail typeDetail, string memberKey, Delegate? getter, Delegate? setter)
        {
            var key = new TypeKey(memberKey, typeDetail.Type);
            var cache2 = cache.GetOrAdd(typeDetail.Type, static () => new());
            var converter = cache2.GetOrAdd(memberKey, typeDetail, memberKey, getter, setter, static (typeDetail, memberKey, getter, setter) =>
            {
                var newConverterType = GetConverterType(typeDetail);
                var newConverter = (JsonConverter<TParent>)newConverterType.CreatorBoxed();
                //Debug.WriteLine($"{typeDetail.Type.GetNiceName()} - {newConverter.GetType().GetNiceName()}");
                newConverter.Setup(memberKey, getter, setter);
                return newConverter;
            });
            return converter;
        }

        private static TypeDetail GetConverterType(TypeDetail typeDetail)
        {
            var discoveredType = JsonConverterDiscovery.Discover(typeDetail.Type);

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
                            return discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, objectType);
                    case 4:
                        if (typeDetail.InnerTypes.Count == 1)
                            return discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0], objectType);
                        else if (typeDetail.InnerTypes.Count == 2)
                            return discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0], typeDetail.InnerTypes[1]);
                        else
                            return discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, objectType, objectType);
                }
            }

            if (typeDetail.CoreType.HasValue)
                throw new NotSupportedException($"No JsonConverter found to support {typeDetail.Type.GetNiceName()}");

            //Enum
            if (typeDetail.Type.IsEnum || typeDetail.IsNullable && typeDetail.InnerType.IsEnum)
                return typeof(JsonConverterEnum<,>).GetGenericTypeDetail(parentType, typeDetail.Type);

            //Array
            if (typeDetail.Type.IsArray)
                return typeof(JsonConverterArrayT<,>).GetGenericTypeDetail(parentType, typeDetail.InnerType);

            //IList<T> of type - specific types that inherit this
            if (typeDetail.HasIListGeneric)
                return typeof(JsonConverterIListTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.IEnumerableGenericInnerType);

            //IList of type - specific types that inherit this
            if (typeDetail.HasIList)
                return typeof(JsonConverterIListOfT<,>).GetGenericTypeDetail(parentType, typeDetail.Type);

            //ISet<T> of type - specific types that inherit this
            if (typeDetail.HasISetGeneric)
                return typeof(JsonConverterISetTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.IEnumerableGenericInnerType);

            //IDictionary<,> of type - specific types that inherit this
            if (typeDetail.HasIDictionaryGeneric)
                return typeof(JsonConverterIDictionaryTOfT<,,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.DictionaryInnerTypeDetail.InnerTypes[0], typeDetail.DictionaryInnerTypeDetail.InnerTypes[1]);

            //IDictionary of type - specific types that inherit this
            if (typeDetail.HasIDictionary)
                return typeof(JsonConverterIDictionaryOfT<,>).GetGenericTypeDetail(parentType, typeDetail.Type);

            //ICollection<T> of type - specific types that inherit this
            if (typeDetail.HasICollectionGeneric)
                return typeof(JsonConverterICollectionTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.IEnumerableGenericInnerType);

            //IEnumerable<T> of type  - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            if (typeDetail.HasIEnumerableGeneric)
                return typeof(JsonConverterIEnumerableTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.IEnumerableGenericInnerType);

            //IEnumerable - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            if (typeDetail.HasIEnumerable)
                return typeof(JsonConverterIEnumerableOfT<,>).GetGenericTypeDetail(parentType, typeDetail.Type);

            //Object
            return typeof(JsonConverterObject<,>).GetGenericTypeDetail(parentType, typeDetail.Type);
        }
    }
}
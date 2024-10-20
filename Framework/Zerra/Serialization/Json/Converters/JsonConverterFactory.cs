﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
            var cache2 = cache.GetOrAdd(typeDetail.Type, static () => new());
            var converter = cache2.GetOrAdd(memberKey, typeDetail, memberKey, getter, setter, static (typeDetail, memberKey, getter, setter) =>
            {
                var newConverter = Create(typeDetail);
                //Debug.WriteLine($"{typeDetail.Type.GetNiceName()} - {newConverter.GetType().GetNiceName()}");
                newConverter.Setup(typeDetail, memberKey, getter, setter);
                return newConverter;
            });
            return converter;
        }

        private static JsonConverter<TParent> Create(TypeDetail typeDetail)
        {
            var discoveredType = JsonConverterDiscovery.Discover(typeDetail.Type);

            if (discoveredType is not null)
            {
                var discoveredTypeDetail = discoveredType.GetTypeDetail();
                switch (discoveredTypeDetail.InnerTypes.Count)
                {
                    case 1:
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (JsonConverter<TParent>)converter;
                        }
                    case 2:
                        if (typeDetail.InnerTypes.Count == 1)
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.InnerTypes[0]);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (JsonConverter<TParent>)converter;
                        }
                        else
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (JsonConverter<TParent>)converter;
                        }
                    case 3:
                        if (typeDetail.InnerTypes.Count == 1)
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0]);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (JsonConverter<TParent>)converter;
                        }
                        else if (typeDetail.InnerTypes.Count == 2)
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.InnerTypes[0], typeDetail.InnerTypes[1]);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (JsonConverter<TParent>)converter;
                        }
                        else
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, objectType);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (JsonConverter<TParent>)converter;
                        }
                    case 4:
                        if (typeDetail.InnerTypes.Count == 1)
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0], objectType);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (JsonConverter<TParent>)converter;
                        }
                        else if (typeDetail.InnerTypes.Count == 2)
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0], typeDetail.InnerTypes[1]);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (JsonConverter<TParent>)converter;
                        }
                        else
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, objectType, objectType);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (JsonConverter<TParent>)converter;
                        }
                }
            }

            if (typeDetail.CoreType.HasValue)
                throw new NotSupportedException($"No JsonConverter found to support {typeDetail.Type.GetNiceName()}");

            //Enum
            if (typeDetail.Type.IsEnum || typeDetail.IsNullable && typeDetail.InnerType.IsEnum)
            {
                var converter = typeof(JsonConverterEnum<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
                return (JsonConverter<TParent>)converter;
            }

            //Array
            if (typeDetail.Type.IsArray)
            {
                var converter = typeof(JsonConverterArrayT<,>).GetGenericTypeDetail(parentType, typeDetail.InnerType).CreatorBoxed();
                return (JsonConverter<TParent>)converter;
            }

            //IList<T> of type - specific types that inherit this
            if (typeDetail.HasIListGeneric)
            {
                var converter = typeof(JsonConverterIListTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerType).CreatorBoxed();
                return (JsonConverter<TParent>)converter;
            }

            //IList of type - specific types that inherit this
            if (typeDetail.HasIList)
            {
                var converter = typeof(JsonConverterIListOfT<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
                return (JsonConverter<TParent>)converter;
            }

            //ISet<T> of type - specific types that inherit this
            if (typeDetail.HasISetGeneric)
            {
                var converter = typeof(JsonConverterISetTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerType).CreatorBoxed();
                return (JsonConverter<TParent>)converter;
            }

            //IDictionary<,> of type - specific types that inherit this
            if (typeDetail.HasIDictionaryGeneric)
            {
                var converter = typeof(JsonConverterIDictionaryTOfT<,,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0], typeDetail.InnerTypes[1]).CreatorBoxed();
                return (JsonConverter<TParent>)converter;
            }

            //IDictionary of type - specific types that inherit this
            if (typeDetail.HasIDictionary)
            {
                var converter = typeof(JsonConverterIDictionaryOfT<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
                return (JsonConverter<TParent>)converter;
            }

            //ICollection<T> of type - specific types that inherit this
            if (typeDetail.HasICollectionGeneric)
            {
                var converter = typeof(JsonConverterICollectionTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerType).CreatorBoxed();
                return (JsonConverter<TParent>)converter;
            }

            //IEnumerable<T> of type  - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            if (typeDetail.HasIEnumerableGeneric)
            {
                var converter = typeof(JsonConverterIEnumerableTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.IEnumerableGenericInnerType).CreatorBoxed();
                return (JsonConverter<TParent>)converter;
            }

            //IEnumerable - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            if (typeDetail.HasIEnumerable)
            {
                var converter = typeof(JsonConverterIEnumerableOfT<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
                return (JsonConverter<TParent>)converter;
            }

            //Object
            var converterObject = typeof(JsonConverterObject<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
            return (JsonConverter<TParent>)converterObject;
        }
    }
}
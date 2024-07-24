// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Zerra.Collections;
using Zerra.Reflection;
using Zerra.Serialization.Json.Converters.Collections;
using Zerra.Serialization.Json.Converters.Collections.Dictionaries;
using Zerra.Serialization.Json.Converters.General;

namespace Zerra.Serialization.Json.Converters
{
    public static class JsonConverterFactory<TParent>
    {
        private static readonly TypeDetail objectTypeDetail = typeof(object).GetTypeDetail();
        private static readonly ConcurrentFactoryDictionary<Type, ConcurrentFactoryDictionary<string, JsonConverter<TParent>>> cache = new();

        private static readonly Type parentType = typeof(TParent);

        internal static JsonConverter<TParent> GetRoot(TypeDetail typeDetail)
             => Get(typeDetail, "Root", null, null);

        public static JsonConverter<TParent> Get(TypeDetail typeDetail, string memberKey, Delegate? getter, Delegate? setter)
        {
            var cache2 = cache.GetOrAdd(typeDetail.Type, x => new());
            var converter = cache2.GetOrAdd(memberKey, x =>
            {
                var newConverter = Create(typeDetail);
                Debug.WriteLine($"{typeDetail.Type.GetNiceName()} - {newConverter.GetType().GetNiceName()}");
                newConverter.Setup(typeDetail, memberKey, getter, setter);
                return newConverter;
            });
            return converter;
        }

        private static readonly Type byteConverterHandlesType = typeof(IJsonConverterHandles<>);
        private static JsonConverter<TParent> Create(TypeDetail typeDetail)
        {
            //Exact match
            var interfaceType = byteConverterHandlesType.GetGenericType(typeDetail.Type);
            var discoveredType = Discover(interfaceType);

            //Generic match
            if (discoveredType == null && typeDetail.Type.IsGenericType && !typeDetail.IsNullable)
            {
                var genericType = typeDetail.Type.GetGenericTypeDefinition();
                interfaceType = byteConverterHandlesType.GetGenericType(genericType);
                discoveredType = Discover(interfaceType);
            }

            if (discoveredType != null)
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
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, objectTypeDetail.Type);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (JsonConverter<TParent>)converter;
                        }
                    case 4:
                        if (typeDetail.InnerTypes.Count == 1)
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0], objectTypeDetail.Type);
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
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, objectTypeDetail.Type, objectTypeDetail.Type);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (JsonConverter<TParent>)converter;
                        }
                }
            }

            //Array
            if (typeDetail.Type.IsArray)
            {
                var converter = typeof(JsonConverterArrayT<,>).GetGenericTypeDetail(parentType, typeDetail.InnerTypes[0]).CreatorBoxed();
                return (JsonConverter<TParent>)converter;
            }

            //Enum
            if (typeDetail.Type.IsEnum || typeDetail.IsNullable && typeDetail.InnerTypes[0].IsEnum)
            {
                var converter = typeof(JsonConverterEnum<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
                return (JsonConverter<TParent>)converter;
            }

            ////IList<T> of type - specific types that inherit this
            //if (typeDetail.HasIListGeneric)
            //{
            //    var converter = typeof(JsonConverterIListTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0]).CreatorBoxed();
            //    return (JsonConverter<TParent>)converter;
            //}

            ////IList of type - specific types that inherit this
            //if (typeDetail.HasIList)
            //{
            //    var converter = typeof(JsonConverterIListOfT<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
            //    return (JsonConverter<TParent>)converter;
            //}

            ////ISet<T> of type - specific types that inherit this
            //if (typeDetail.HasISetGeneric)
            //{
            //    var converter = typeof(JsonConverterISetTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0]).CreatorBoxed();
            //    return (JsonConverter<TParent>)converter;
            //}

            //IDictionary<,> of type - specific types that inherit this
            if (typeDetail.HasIDictionaryGeneric)
            {
                var converter = typeof(JsonConverterIDictionaryTOfT<,,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0], typeDetail.InnerTypes[1]).CreatorBoxed();
                return (JsonConverter<TParent>)converter;
            }

            ////IDictionary of type - specific types that inherit this
            //if (typeDetail.HasIDictionary)
            //{
            //    var converter = typeof(JsonConverterIDictionaryOfT<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
            //    return (JsonConverter<TParent>)converter;
            //}

            ////IEnumerable<T> of type  - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            //if (typeDetail.HasIEnumerableGeneric)
            //{
            //    var converter = typeof(JsonConverterIEnumerableTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.IEnumerableGenericInnerType).CreatorBoxed();
            //    return (JsonConverter<TParent>)converter;
            //}

            ////IEnumerable - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            //if (typeDetail.HasIEnumerable)
            //{
            //    var converter = typeof(JsonConverterIEnumerableOfT<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
            //    return (JsonConverter<TParent>)converter;
            //}

            //Object
            var converterObject = typeof(JsonConverterObject<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
            return (JsonConverter<TParent>)converterObject;
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
                    throw new InvalidOperationException($"Multiple custom implementations of {nameof(JsonConverter)} found for {interfaceType.GetNiceName()}");
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
    }
}
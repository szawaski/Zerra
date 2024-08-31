// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
            var cache2 = cache.GetOrAdd(typeDetail.Type, x => new());
            var converter = cache2.GetOrAdd(memberKey, x =>
            {
                var newConverter = Create(typeDetail);
                //Debug.WriteLine($"{typeDetail.Type.GetNiceName()} - {newConverter.GetType().GetNiceName()}");
                newConverter.Setup(typeDetail, memberKey, getter, setter);
                return newConverter;
            });
            return converter;
        }

        internal static ByteConverter<TParent> GetDrainBytes()
        {
            if (cacheByteConverterTypeInfo == null)
            {
                lock (cache)
                {
                    if (cacheByteConverterTypeInfo == null)
                    {
                        var newConverter = new ByteConverterTypeRequired<TParent>();
                        newConverter.Setup(objectTypeDetail, "Drain", null, null);
                        cacheByteConverterTypeInfo = newConverter;
                    }
                }
            }
            return cacheByteConverterTypeInfo;
        }

        private static readonly Type byteConverterHandlesType = typeof(IByteConverterHandles<>);
        private static ByteConverter<TParent> Create(TypeDetail typeDetail)
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
                            return (ByteConverter<TParent>)converter;
                        }
                    case 2:
                        if (typeDetail.InnerTypes.Count == 1)
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.InnerTypes[0]);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (ByteConverter<TParent>)converter;
                        }
                        else
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (ByteConverter<TParent>)converter;
                        }
                    case 3:
                        if (typeDetail.InnerTypes.Count == 1)
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0]);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (ByteConverter<TParent>)converter;
                        }
                        else if (typeDetail.InnerTypes.Count == 2)
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.InnerTypes[0], typeDetail.InnerTypes[1]);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (ByteConverter<TParent>)converter;
                        }
                        else
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, objectTypeDetail.Type);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (ByteConverter<TParent>)converter;
                        }
                    case 4:
                        if (typeDetail.InnerTypes.Count == 1)
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0], objectTypeDetail.Type);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (ByteConverter<TParent>)converter;
                        }
                        else if (typeDetail.InnerTypes.Count == 2)
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0], typeDetail.InnerTypes[1]);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (ByteConverter<TParent>)converter;
                        }
                        else
                        {
                            var discoveredTypeDetailGeneric = discoveredTypeDetail.GetGenericTypeDetail(parentType, typeDetail.Type, objectTypeDetail.Type, objectTypeDetail.Type);
                            var converter = discoveredTypeDetailGeneric.CreatorBoxed();
                            return (ByteConverter<TParent>)converter;
                        }
                }
            }

            //Array
            if (typeDetail.Type.IsArray)
            {
                var converter = typeof(ByteConverterArrayT<,>).GetGenericTypeDetail(parentType, typeDetail.InnerTypes[0]).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //Enum
            if (typeDetail.Type.IsEnum || typeDetail.IsNullable && typeDetail.InnerTypes[0].IsEnum)
            {
                var converter = typeof(ByteConverterEnum<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //IList<T> of type - specific types that inherit this
            if (typeDetail.HasIListGeneric)
            {
                var converter = typeof(ByteConverterIListTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0]).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //IList of type - specific types that inherit this
            if (typeDetail.HasIList)
            {
                var converter = typeof(ByteConverterIListOfT<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //ISet<T> of type - specific types that inherit this
            if (typeDetail.HasISetGeneric)
            {
                var converter = typeof(ByteConverterISetTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0]).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //IDictionary<,> of type - specific types that inherit this
            if (typeDetail.HasIDictionaryGeneric)
            {
                var converter = typeof(ByteConverterIDictionaryTOfT<,,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0], typeDetail.InnerTypes[1]).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //IDictionary of type - specific types that inherit this
            if (typeDetail.HasIDictionary)
            {
                var converter = typeof(ByteConverterIDictionaryOfT<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //ICollection<T> of type - specific types that inherit this
            if (typeDetail.HasICollectionGeneric)
            {
                var converter = typeof(ByteConverterICollectionTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.InnerTypes[0]).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //IEnumerable<T> of type  - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            if (typeDetail.HasIEnumerableGeneric)
            {
                var converter = typeof(ByteConverterIEnumerableTOfT<,,>).GetGenericTypeDetail(parentType, typeDetail.Type, typeDetail.IEnumerableGenericInnerType).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //IEnumerable - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            if (typeDetail.HasIEnumerable)
            {
                var converter = typeof(ByteConverterIEnumerableOfT<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
                return (ByteConverter<TParent>)converter;
            }

            //Object
            var converterObject = typeof(ByteConverterObject<,>).GetGenericTypeDetail(parentType, typeDetail.Type).CreatorBoxed();
            return (ByteConverter<TParent>)converterObject;
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
    }
}
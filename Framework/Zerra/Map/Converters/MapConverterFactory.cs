// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Collections;
using Zerra.Map.Converters.Collections;
using Zerra.Map.Converters.Collections.Collections;
using Zerra.Map.Converters.Collections.Dictionaries;
using Zerra.Map.Converters.Collections.Enumerables;
using Zerra.Map.Converters.Collections.List;
using Zerra.Map.Converters.Collections.Sets;
using Zerra.Map.Converters.CoreTypes;
using Zerra.Map.Converters.General;
using Zerra.SourceGeneration;
using Zerra.SourceGeneration.Types;

namespace Zerra.Map.Converters
{
    public static class MapConverterFactory
    {
        private static readonly ConcurrentFactoryDictionary<TypePairKey, Func<MapConverter>> creators = new();
        private static readonly ConcurrentFactoryDictionary<TypePairKey, ConcurrentFactoryDictionary<string, MapConverter>> cache = new();

        internal static MapConverter GetRoot(TypeDetail sourceTypeDetail, TypeDetail targetTypeDetail)
            => Get(sourceTypeDetail, targetTypeDetail, "Root", null, null, null);

        public static MapConverter Get(TypeDetail sourceTypeDetail, TypeDetail targetTypeDetail, string memberKey, Delegate? sourceGetterDelegate, Delegate? targetGetterDelegate, Delegate? targetSetterDelegate)
        {
            var key = new TypePairKey(sourceTypeDetail.Type, targetTypeDetail.Type);
            var cache2 = cache.GetOrAdd(key, static () => new());
            var converter = cache2.GetOrAdd(memberKey, sourceTypeDetail, targetTypeDetail, sourceGetterDelegate, targetGetterDelegate, targetSetterDelegate, static (sourceType, targetType, sourceGetterDelegate, targetGetterDelegate, targetSetterDelegate) =>
            {
                var key = new TypePairKey(sourceType.Type, targetType.Type);
                var creator = creators.GetOrAdd(key, sourceType, targetType, GenerateMapConverterCreator);
                var newConverter = creator();
                newConverter.Setup(sourceGetterDelegate, targetGetterDelegate, targetSetterDelegate);
                return newConverter;
            });
            return converter;
        }

        internal static void AddConverter(Type sourceType, Type targetType, Func<MapConverter> converter)
        {
            var key = new TypePairKey(sourceType, targetType);
            creators[key] = converter;
        }

        internal static Func<MapConverter> GenerateMapConverterCreator(TypeDetail sourceTypeDetail, TypeDetail targetTypeDetail)
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                throw new InvalidOperationException($"MapConverter type not found for {sourceTypeDetail.Type.Name} to {targetTypeDetail.Type.Name} and dynamic code generation is not supported in this build configuration");

            var sourceType = sourceTypeDetail.Type;
            var targetType = targetTypeDetail.Type;
            var sourceEnumerableType = sourceTypeDetail.IEnumerableGenericInnerType ?? typeof(object);
            var targetEnumerableType = targetTypeDetail.IEnumerableGenericInnerType ?? typeof(object);
            var sourceDicionaryKeyType = sourceTypeDetail.DictionaryInnerTypeDetail?.InnerTypes.ElementAtOrDefault(0) ?? typeof(object);
            var sourceDicionaryValueType = sourceTypeDetail.DictionaryInnerTypeDetail?.InnerTypes.ElementAtOrDefault(1) ?? typeof(object);
            var targetDicionaryKeyType = targetTypeDetail.DictionaryInnerTypeDetail?.InnerTypes.ElementAtOrDefault(0) ?? typeof(object);
            var targetDicionaryValueType = targetTypeDetail.DictionaryInnerTypeDetail?.InnerTypes.ElementAtOrDefault(1) ?? typeof(object);

            var findCreatorMethodDefinition = typeof(MapConverterFactory).GetMethod(nameof(FindCreator), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
            var findCreatorMethod = findCreatorMethodDefinition.MakeGenericMethod(sourceType, targetType, sourceEnumerableType, targetEnumerableType, sourceDicionaryKeyType, sourceDicionaryValueType, targetDicionaryKeyType, targetDicionaryValueType);
            var creator = (Func<MapConverter>)findCreatorMethod.Invoke(null, [sourceTypeDetail, targetTypeDetail])!;
            return creator;
        }

        public static void RegisterCreator<TSource, TTarget>()
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);
            var key = new TypePairKey(sourceType, targetType);
            if (creators.ContainsKey(key))
                return;

            var sourceTypeDetail = sourceType.GetTypeDetail();
            var targetTypeDetail = targetType.GetTypeDetail();

            if (sourceTypeDetail.HasIEnumerable && targetTypeDetail.HasIEnumerable)
                throw new InvalidOperationException($"{sourceType.Name} and {targetType.Name} are both enumerable types. Use RegisterCreator with enumerable types");

            var creator = FindCreator<TSource, TTarget, object, object, object, object, object, object>(sourceTypeDetail, targetTypeDetail);
            _ = creators.TryAdd(key, creator);
        }

        public static void RegisterCreator<TSource, TTarget, TSourceEnumerable, TTargetEnumerable, TSourceKey, TSourceValue, TTargetKey, TTargetValue>()
            where TSourceKey : notnull
            where TTargetKey : notnull
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);
            var key = new TypePairKey(sourceType, targetType);
            if (creators.ContainsKey(key))
                return;

            var sourceTypeDetail = sourceType.GetTypeDetail();
            var targetTypeDetail = targetType.GetTypeDetail();

            var creator = FindCreator<TSource, TTarget, TSourceEnumerable, TTargetEnumerable, TSourceKey, TSourceValue, TTargetKey, TTargetValue>(sourceTypeDetail, targetTypeDetail);
            _ = creators.TryAdd(key, creator);
        }

        private static Func<MapConverter> FindCreator<TSource, TTarget, TSourceEnumerable, TTargetEnumerable, TSourceKey, TSourceValue, TTargetKey, TTargetValue>(TypeDetail sourceTypeDetail, TypeDetail targetTypeDetail)
            where TSourceKey : notnull
            where TTargetKey : notnull
        {
            if (sourceTypeDetail.CoreType.HasValue && targetTypeDetail.CoreType.HasValue)
            {
                if (sourceTypeDetail.CoreType == targetTypeDetail.CoreType)
                    return () => new MapConverterCoreType<TSource>();
                else
                    return () => new MapConverterCoreType<TSource, TTarget>();
            }

            //if (targetTypeDetail.IsIList)
            //    return () => new MapConverterIList();
            if (targetTypeDetail.IsIListGeneric)
                return () => new MapConverterIListT<TSource, TSourceEnumerable, TTargetEnumerable>();
            if (targetTypeDetail.IsIReadOnlyListGeneric)
                return () => new MapConverterIReadOnlyListT<TSource, TSourceEnumerable, TTargetEnumerable>();
            //if (targetTypeDetail.IsListGeneric)
            //    return () => new MapConverterListT<TSource, TSourceEnumerable, TTargetEnumerable>();

            if (targetTypeDetail.IsISetGeneric)
                return () => new MapConverterISetT<TSource, TSourceEnumerable, TTargetEnumerable>();
            if (targetTypeDetail.IsIReadOnlySetGeneric)
                return () => new MapConverterIReadOnlySetT<TSource, TSourceEnumerable, TTargetEnumerable>();
            //if (targetTypeDetail.IsHashSetGeneric)
            //    return () => new MapConverterHashSetT<TSource, TSourceEnumerable, TTargetEnumerable>();

            //if (targetTypeDetail.IsICollection)
            //    return () => new MapConverterICollection();
            if (targetTypeDetail.IsICollectionGeneric)
                return () => new MapConverterICollectionT<TSource, TSourceEnumerable, TTargetEnumerable>();
            if (targetTypeDetail.IsIReadOnlyCollectionGeneric)
                return () => new MapConverterIReadOnlyCollectionT<TSource, TSourceEnumerable, TTargetEnumerable>();

            //if (targetTypeDetail.IsIDictionary)
            //    return () => new MapConverterIDictionary();
            if (targetTypeDetail.IsIDictionaryGeneric)
                return () => new MapConverterIDictionaryT<TSource, TSourceKey, TSourceValue, TTargetKey, TTargetValue>();
            //if (targetTypeDetail.IsIReadOnlyDictionaryGeneric)
            //    return () => new MapConverterIReadOnlyDictionaryT<TDictionaryKey, TDictionaryValue>();
            //if (targetTypeDetail.IsDictionaryGeneric)
            //    return () => new MapConverterDictionaryT<TDictionaryKey, TDictionaryValue>();

            //if (targetTypeDetail.IsIEnumerable)
            //    return () => new MapConverterIEnumerable();
            if (targetTypeDetail.IsIEnumerableGeneric)
                return () => new MapConverterIEnumerableT<TSource, TSourceEnumerable, TTargetEnumerable>();

            //if (targetTypeDetail.Type.FullName == "System.Type")
            //    return () => new MapConverterType();
            //if (targetTypeDetail.Type.FullName == "System.Threading.CancellationToken")
            //    return () => new MapConverterCancellationToken();
            //if (targetTypeDetail.IsNullable && targetTypeDetail.InnerType!.FullName == "System.Threading.CancellationToken")
            //    return () => new MapConverterCancellationTokenNullable();

            if (targetTypeDetail.CoreType.HasValue)
                throw new NotSupportedException($"No MapConverter found to support {targetTypeDetail.Type.Name}");

            //Enum
            if (targetTypeDetail.Type.IsEnum || targetTypeDetail.IsNullable && targetTypeDetail.InnerType!.IsEnum)
                return () => new MapConverterEnum<TSource, TTarget>();

            //Array
            if (targetTypeDetail.Type.IsArray)
                return () => new MapConverterArray<TSource, TSourceEnumerable, TTargetEnumerable>();

            //IList<T> of type - specific types that inherit this
            if (targetTypeDetail.HasIListGeneric)
                return () => new MapConverterIListTOfT<TSource, TTarget, TSourceEnumerable, TTargetEnumerable>();

            ////IList of type - specific types that inherit this
            //if (targetTypeDetail.HasIList)
            //    return () => new MapConverterIListOfT<TSource, TTarget>();

            //ISet<T> of type - specific types that inherit this
            if (targetTypeDetail.HasISetGeneric)
                return () => new MapConverterISetTOfT<TSource, TTarget, TSourceEnumerable, TTargetEnumerable>();

            //IDictionary<,> of type - specific types that inherit this
            if (targetTypeDetail.HasIDictionaryGeneric)
                return () => new MapConverterIDictionaryTOfT<TSource, TTarget, TSourceKey, TSourceValue, TTargetKey, TTargetValue>();

            ////IDictionary of type - specific types that inherit this
            //if (targetTypeDetail.HasIDictionary)
            //    return () => new MapConverterIDictionaryOfT<TSource, TTarget>();

            //ICollection<T> of type - specific types that inherit this
            if (targetTypeDetail.HasICollectionGeneric)
                return () => new MapConverterICollectionTOfT<TSource, TTarget, TSourceEnumerable, TTargetEnumerable>();

            //IEnumerable<T> of type  - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            if (targetTypeDetail.HasIEnumerableGeneric)
                return () => new MapConverterIEnumerableTOfT<TSource, TTarget, TSourceEnumerable, TTargetEnumerable>();

            ////IEnumerable - specific types that inherit this (This cannot read because we have no interface to populate the collection)
            //if (targetTypeDetail.HasIEnumerable)
            //    return () => new MapConverterIEnumerableOfT<TSource, TTarget>();

            //Object
            return () => new MapConverterObject<TSource, TTarget>();
        }
    }
}
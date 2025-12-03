// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Collections;
using Zerra.SourceGeneration;
using Zerra.SourceGeneration.Types;

namespace Zerra.Map
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

        public static Func<MapConverter> GenerateMapConverterCreator(TypeDetail sourceTypeDetail, TypeDetail targetTypeDetail)
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

        public static void RegisterCreator<TSource, TTarget>(Type sourceType, Type targetType)
        {
            var creator = FindCreator<TSource, TTarget, object, object, object, object, object, object>(sourceType.GetTypeDetail(), targetType.GetTypeDetail());
            var key = new TypePairKey(sourceType, targetType);
            _ = creators.TryAdd(key, creator);
        }

        public static void RegisterCreator<TSource, TTarget, TSourceEnumerable, TTargetEnumerable, TSourceKey, TSourceValue, TTargetKey, TTargetValue>(Type sourceType, Type targetType)
            where TSourceKey : notnull
            where TTargetKey : notnull
        {
            var creator = FindCreator<TSource, TTarget, TSourceEnumerable, TTargetEnumerable, TSourceKey, TSourceValue, TTargetKey, TTargetValue>(sourceType.GetTypeDetail(), targetType.GetTypeDetail());
            var key = new TypePairKey(sourceType, targetType);
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

            if (sourceTypeDetail.EnumUnderlyingType.HasValue && targetTypeDetail.EnumUnderlyingType.HasValue)
            {
                if (sourceTypeDetail.EnumUnderlyingType == targetTypeDetail.EnumUnderlyingType)
                    return () => new MapConverterEnum<TSource>();
                else
                    return () => new MapConverterEnum<TSource, TTarget>();
            }

            if (sourceTypeDetail.HasIEnumerableGeneric && targetTypeDetail.HasIEnumerableGeneric)
            {
                if (targetTypeDetail.Type.IsArray)
                {
                    if (sourceTypeDetail.Type.IsArray)
                        return () => new MapConverterArrayToArray<TSourceEnumerable, TTargetEnumerable>();
                    else
                        return () => new MapConverterArray<TSource, TSourceEnumerable, TTargetEnumerable>();
                }

                if (targetTypeDetail.HasIListGeneric)
                {
                    if (targetTypeDetail.Type.IsInterface)
                        return () => new MapConverterIListT<TSource, TSourceEnumerable, TTargetEnumerable>();
                    else
                        return () => new MapConverterIListTOfT<TSource, TTarget, TSourceEnumerable, TTargetEnumerable>();
                }

                if (targetTypeDetail.HasISetGeneric)
                {
                    if (targetTypeDetail.Type.IsInterface)
                        return () => new MapConverterISetT<TSource, TSourceEnumerable, TTargetEnumerable>();
                    else
                        return () => new MapConverterISetTOfT<TSource, TTarget, TSourceEnumerable, TTargetEnumerable>();
                }

                if (targetTypeDetail.HasIList)
                {

                }

                if (targetTypeDetail.HasIDictionaryGeneric)
                {
                    if (targetTypeDetail.Type.IsInterface)
                        return () => new MapConverterIDictionaryT<TSource, TSourceKey, TSourceValue, TTargetKey, TTargetValue>();
                    else
                        return () => new MapConverterIDictionaryTOfT<TSource, TTarget, TSourceKey, TSourceValue, TTargetKey, TTargetValue>();
                }

                if (targetTypeDetail.HasIDictionary)
                {

                }

                if (targetTypeDetail.HasICollectionGeneric)
                {
                    if (targetTypeDetail.Type.IsInterface)
                        return () => new MapConverterICollectionT<TSource, TSourceEnumerable, TTargetEnumerable>();
                    else
                        return () => new MapConverterICollectionTOfT<TSource, TTarget, TSourceEnumerable, TTargetEnumerable>();
                }

                if (targetTypeDetail.Type.IsInterface)
                    return () => new MapConverterIEnumerableT<TSource, TSourceEnumerable, TTargetEnumerable>();
                else
                    return () => new MapConverterIEnumerableTOfT<TSource, TTarget, TSourceEnumerable, TTargetEnumerable>();
            }

            return () => new MapConverterObject<TSource, TTarget>();
        }

        internal static void AddConverter(Type sourceType, Type targetType, Func<MapConverter> converter)
        {
            var key = new TypePairKey(sourceType, targetType);
            creators[key] = converter;
        }
    }
}
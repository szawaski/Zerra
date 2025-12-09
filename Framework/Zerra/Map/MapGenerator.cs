// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Map.Converters;
using Zerra.Reflection;

namespace Zerra.Map
{
    public static class MapGenerator
    {
        public static Func<TSource, TTarget?, Graph?, TTarget?> Generate<TSource, TTarget>()
            where TSource : notnull
            where TTarget : notnull
            => Map<TSource, TTarget>;

        private static TTarget? Map<TSource, TTarget>(TSource source, TTarget? target, Graph? graph)
            where TSource : notnull
            where TTarget : notnull
        {
            var sourceTypeDetail = TypeAnalyzer<TSource>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTarget>.GetTypeDetail();
            var converter = (MapConverter<TSource, TTarget>)MapConverterFactory.GetRoot(sourceTypeDetail, targetTypeDetail);
            var resultTarget = converter.Map(source, target, graph);
            return resultTarget;
        }

        public static Func<object, TTarget?, Graph?, TTarget?> Generate<TTarget>()
            where TTarget : notnull
            => Map<TTarget>;

        private static TTarget? Map<TTarget>(object source, TTarget? target, Graph? graph)
            where TTarget : notnull
        {
            var sourceTypeDetail = source.GetType().GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTarget>.GetTypeDetail();
            var converter = MapConverterFactory.GetRoot(sourceTypeDetail, targetTypeDetail);
            var resultTarget = (TTarget?)converter.Map(source, target, graph);
            return resultTarget;
        }
    }
}

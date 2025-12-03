// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Zerra.SourceGeneration;
using Zerra.SourceGeneration.Types;

namespace Zerra.Map
{
    public static class MapGenerator
    {
        public static Func<TSource, TTarget?, Graph?, TTarget?> Generate<TSource, TTarget>()
            where TSource : notnull
            where TTarget : notnull
            => Map;

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
    }
}

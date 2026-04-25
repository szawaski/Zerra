// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Map.Converters;
using Zerra.Reflection;

namespace Zerra.Map
{
    internal static class MapGenerator
    {
        public static TTarget? Generate1<TSource, TTarget>(TSource source, TTarget? target, Graph? graph)
        {
            var sourceTypeDetail = TypeAnalyzer<TSource>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTarget>.GetTypeDetail();
            var converter = (MapConverter<TSource, TTarget>)MapConverterFactory.GetRoot(sourceTypeDetail, targetTypeDetail);
            var resultTarget = converter.Map(source, target, graph);
            return resultTarget;
        }

        public static TTarget? Generate2<TTarget>(object source, TTarget? target, Graph? graph)
        {
            var sourceTypeDetail = source.GetType().GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTarget>.GetTypeDetail();
            var converter = MapConverterFactory.GetRoot(sourceTypeDetail, targetTypeDetail);
            var resultTarget = (TTarget?)converter.Map(source, target, graph);
            return resultTarget;
        }

        public static object? Generate3(object source, object? target, Graph? graph)
        {
            var typeDetail = source.GetType().GetTypeDetail();
            var converter = MapConverterFactory.GetRoot(typeDetail, typeDetail);
            var resultTarget = converter.Map(source, target, graph);
            return resultTarget;
        }
    }
}

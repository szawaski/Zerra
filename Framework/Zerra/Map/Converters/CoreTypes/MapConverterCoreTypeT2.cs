// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Map.Converters.CoreTypes
{
    internal sealed class MapConverterCoreType<TSource, TTarget> : MapConverter<TSource, TTarget>
    {
        public override TTarget? Map(TSource? source, TTarget? target, Graph? graph)
            => (TTarget?)TypeAnalyzer.Convert(source, targetTypeDetail.CoreType!.Value);
    }
}
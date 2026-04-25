// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Map.Converters.General
{
    internal sealed class MapConverterEnum<TSource, TTarget> : MapConverter<TSource, TTarget>
    {
        public override TTarget? Map(TSource? source, TTarget? target, Graph? graph)
            => (TTarget?)(object?)source;
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Map
{
    internal sealed class MapConverterIEnumerableTOfT<TSource, TTarget, TSourceInner, TTargetInner> : MapConverter<TSource, TTarget>
    {
        public override TTarget? Map(TSource? source, TTarget? target, Graph? graph)
                   => throw new NotSupportedException($"Cannot Map {targetTypeDetail.Type.GetNiceName()} because no interface to populate the collection");
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Map.Converters.CoreTypes
{
    internal sealed class MapConverterCoreType<T> : MapConverter<T, T>
    {
        public override T? Map(T? source, T? target, Graph? graph)
            => source;
    }
}
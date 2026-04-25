// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Map.Converters.General
{
    internal sealed class MapConverterEnum<T> : MapConverter<T, T>
    {
        public override T? Map(T? source, T? target, Graph? graph)
            => source;
    }
}
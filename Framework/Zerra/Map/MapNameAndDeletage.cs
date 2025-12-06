// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Map
{
    internal sealed class MapNameAndDeletage<TSource, TTarget>
    {
        public string? Name { get; init; }
        public Action<TSource, TTarget> Delegate { get; init; }
        public MapNameAndDeletage(string? name, Action<TSource, TTarget> del)
        {
            this.Name = name;
            this.Delegate = del;
        }
    }
}

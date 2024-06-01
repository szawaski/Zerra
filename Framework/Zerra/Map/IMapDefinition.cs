// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Map
{
    public interface IMapDefinition<TSource, TTarget>
    {
        void Define(IMapSetup<TSource, TTarget> map);
    }
}

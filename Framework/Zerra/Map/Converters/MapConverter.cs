// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;

namespace Zerra.Map
{
    public abstract class MapConverter
    {
        public abstract void Setup(Delegate? sourceGetterDelegate, Delegate? targetGetterDelegate, Delegate? targetSetterDelegate);

        public abstract object? Map(object? source, object? target, Graph? graph);

        public abstract void MapFromParent(object sourceParent, object targetParent, Graph? graph);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract void CollectedValuesSetter(object parent, in object? value);
    }
}
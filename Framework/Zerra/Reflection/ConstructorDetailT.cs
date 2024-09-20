// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Zerra.Reflection
{
    public abstract class ConstructorDetail<T> : ConstructorDetail
    {
        public abstract Func<T> Creator { get; }
        public abstract bool HasCreator { get; }

        public abstract Func<object?[]?, T> CreatorWithArgs { get; }
        public abstract bool HasCreatorWithArgs { get; }
    }
}

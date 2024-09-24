// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Zerra.Reflection
{
    public abstract class ConstructorDetail
    {
        public abstract ConstructorInfo ConstructorInfo { get; }
        public abstract string Name { get; }

        public abstract IReadOnlyList<ParameterDetail> Parameters { get; }

        public abstract IReadOnlyList<Attribute> Attributes { get; }

        public abstract Func<object> CreatorBoxed { get; }
        public abstract bool HasCreatorBoxed { get; }

        public abstract Func<object?[]?, object> CreatorWithArgsBoxed { get; }
        public abstract bool HasCreatorWithArgsBoxed { get; }

        public abstract Delegate? CreatorTyped { get; }
        public abstract Delegate? CreatorWithArgsTyped { get; }
    }
}

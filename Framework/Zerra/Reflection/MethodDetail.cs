// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Zerra.Reflection
{
    public abstract class MethodDetail
    {
        public abstract MethodInfo MethodInfo { get; }
        public abstract string Name { get; }

        public abstract IReadOnlyList<ParameterDetail> ParameterDetails { get; }

        public abstract IReadOnlyList<Attribute> Attributes { get; }

        public abstract Func<object?, object?[]?, object?> CallerBoxed { get; }
        public abstract bool HasCallerBoxed { get; }
        public abstract Func<object?, object?[]?, Task<object?>> CallerBoxedAsync { get; }
        public abstract bool HasCallerBoxedAsync { get; }

        public abstract Delegate? CallerTyped { get; }

        public abstract Type ReturnType { get; }
        public abstract TypeDetail ReturnTypeDetail { get; }
    }
}

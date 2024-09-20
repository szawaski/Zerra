// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;

namespace Zerra.Reflection
{
    public abstract class MethodDetail<T> : MethodDetail
    {
        public abstract Func<T?, object?[]?, object?> Caller { get; }
        public abstract bool HasCaller { get; }
        public abstract Func<T?, object?[]?, Task<object?>> CallerAsync { get; }
        public abstract bool HasCallerAsync { get; }
    }
}

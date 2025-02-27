﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Reflection
{
    public abstract class MemberDetail<T, V> : MemberDetail
    {
        public abstract MemberDetail<T, V>? BackingFieldDetail { get; }

        public abstract Func<T, V?> Getter { get; }
        public abstract bool HasGetter { get; }

        public abstract Action<T, V?> Setter { get; }
        public abstract bool HasSetter { get; }

        public override sealed Delegate GetterTyped => Getter;
        public override sealed Delegate SetterTyped => Setter;

        public abstract TypeDetail<V> TypeDetail { get; }
    }
}

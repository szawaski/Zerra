// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Zerra.Reflection
{
    public abstract class MemberDetail
    {
        public abstract bool IsGenerated { get; }

        public abstract MemberDetail? BackingFieldDetailBoxed { get; }

        public abstract MemberInfo MemberInfo { get; }
        public abstract string Name { get; }
        public abstract Type Type { get; }
        public abstract bool IsBacked { get; }
        public abstract bool IsStatic { get; }
        public abstract bool IsExplicitFromInterface { get; }

        public abstract IReadOnlyList<Attribute> Attributes { get; }

        public abstract Func<object, object?> GetterBoxed { get; }
        public abstract bool HasGetterBoxed { get; }

        public abstract Action<object, object?> SetterBoxed { get; }
        public abstract bool HasSetterBoxed { get; }

        public abstract Delegate GetterTyped { get; }
        public abstract Delegate SetterTyped { get; }

        public abstract TypeDetail TypeDetailBoxed { get; }

        internal abstract void SetMemberInfo(MemberInfo memberInfo, MemberInfo? backingField);

        public override string ToString()
        {
            return $"{(IsGenerated ? "Generated" : "Runtime")} {Type.Name} {Name}";
        }
    }
}

// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    public sealed class MemberDetail<T> : MemberDetail
    {
        public readonly new bool HasGetter;
        public readonly new Func<object, T?>? Getter;

        public readonly new bool HasSetter;
        public readonly new Action<object, T?>? Setter;

        public MemberDetail(string name, Func<object, T?>? getter, Func<object, object?>? getterBoxed, Action<object, T?>? setter, Action<object, object?>? setterBoxed, IReadOnlyList<Attribute> attributes, bool isBacked, bool isStatic, bool isExplicitFromInterface)
            : base(typeof(T), name, getter, getterBoxed, setter, setterBoxed, attributes, isBacked, isStatic, isExplicitFromInterface)
        {
            this.HasGetter = getter != null;
            this.Getter = getter;
            this.HasSetter = setter != null;
            this.Setter = setter;
        }

        public TypeDetail<T> TypeDetail
        {
            get
            {
                field ??= (TypeDetail<T>)base.TypeDetailBoxed;
                return field;
            }
        }
    }
}

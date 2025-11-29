// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    public class MemberDetail
    {
        public readonly Type Type;
        public readonly string Name;

        public readonly bool HasGetterBoxed;
        public readonly Func<object, object?>? GetterBoxed;

        public readonly bool HasSetterBoxed;
        public readonly Action<object, object?>? SetterBoxed;

        public readonly bool HasGetter;
        public readonly Delegate? Getter;

        public readonly bool HasSetter;
        public readonly Delegate? Setter;

        public readonly IReadOnlyList<Attribute> Attributes;

        public readonly bool IsBacked;
        public readonly bool IsStatic;
        public readonly bool IsExplicitFromInterface;

        public MemberDetail(Type type, string name, Delegate? getter, Func<object, object?>? getterBoxed, Delegate? setter, Action<object, object?>? setterBoxed, IReadOnlyList<Attribute> attributes, bool isBacked, bool isStatic, bool isExplicitFromInterface)
        {
            this.Type = type;
            this.Name = name;
            this.HasGetterBoxed = getterBoxed != null;
            this.GetterBoxed = getterBoxed;
            this.HasSetterBoxed = setterBoxed != null;
            this.SetterBoxed = setterBoxed;
            this.HasGetter = getterBoxed != null;
            this.Getter = getterBoxed;
            this.HasSetter = setterBoxed != null;
            this.Setter = setterBoxed;
            this.Attributes = attributes;
            this.IsBacked = isBacked;
            this.IsStatic = isStatic;
            this.IsExplicitFromInterface = isExplicitFromInterface;
        }

        public TypeDetail TypeDetailBoxed
        {
            get
            {
                field ??= TypeAnalyzer.GetTypeDetail(this.Type);
                return field;
            }
        }
    }
}

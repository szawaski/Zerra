// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Reflection
{
    /// <summary>
    /// Metadata for a type member (property or field) including its type, accessor delegates, and characteristics.
    /// Provides both boxed and strongly-typed accessors for getting and setting member values.
    /// Used by the source generator and runtime reflection to enable efficient member access and serialization.
    /// </summary>
    public partial class MemberDetail
    {
        protected readonly Lock locker = new();

        /// <summary>The parent type that owns this member.</summary>
        public readonly Type ParentType;
        /// <summary>The type of this member.</summary>
        public readonly Type Type;
        /// <summary>The name of this member.</summary>
        public readonly string Name;
        /// <summary>Indicates whether this member is a field.</summary>
        public readonly bool IsField;

        /// <summary>Indicates whether a boxed getter delegate exists.</summary>
        public readonly bool HasGetterBoxed;
        /// <summary>Boxed delegate for getting the member value; returns object.</summary>
        public readonly Func<object, object?>? GetterBoxed;

        /// <summary>Indicates whether a boxed setter delegate exists.</summary>
        public readonly bool HasSetterBoxed;
        /// <summary>Boxed delegate for setting the member value; accepts object.</summary>
        public readonly Action<object, object?>? SetterBoxed;

        /// <summary>Indicates whether a strongly-typed getter delegate exists.</summary>
        public readonly bool HasGetter;
        /// <summary>Strongly-typed delegate for getting the member value without boxing.</summary>
        public readonly Delegate? Getter;

        /// <summary>Indicates whether a strongly-typed setter delegate exists.</summary>
        public readonly bool HasSetter;
        /// <summary>Strongly-typed delegate for setting the member value without boxing.</summary>
        public readonly Delegate? Setter;

        /// <summary>Collection of all custom attributes applied to this member.</summary>
        public readonly IReadOnlyList<Attribute> Attributes;

        /// <summary>Indicates whether this member is property or field backed (has an actual storage location).</summary>
        public readonly bool IsBacked;
        /// <summary>Indicates whether this member is static.</summary>
        public readonly bool IsStatic;
        /// <summary>Indicates whether this member is an explicit interface implementation.</summary>
        public readonly bool IsExplicitFromInterface;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemberDetail"/> class with member metadata.
        /// </summary>
        /// <param name="parentType">The parent type that owns this member.</param>
        /// <param name="type">The type of the member.</param>
        /// <param name="name">The name of the member.</param>
        /// <param name="isField">Whether the member is a field.</param>
        /// <param name="getter">Strongly-typed getter delegate.</param>
        /// <param name="getterBoxed">Boxed getter delegate.</param>
        /// <param name="setter">Strongly-typed setter delegate.</param>
        /// <param name="setterBoxed">Boxed setter delegate.</param>
        /// <param name="attributes">Custom attributes applied to the method.</param>
        /// <param name="isBacked">Whether the member has actual storage (is property or field backed).</param>
        /// <param name="isStatic">Whether the member is static.</param>
        /// <param name="isExplicitFromInterface">Whether the member is an explicit interface implementation.</param>
        public MemberDetail(Type parentType, Type type, string name, bool isField, Delegate? getter, Func<object, object?>? getterBoxed, Delegate? setter, Action<object, object?>? setterBoxed, IReadOnlyList<Attribute> attributes, bool isBacked, bool isStatic, bool isExplicitFromInterface)
        {
            this.ParentType = parentType;
            this.Type = type;
            this.Name = name;
            this.IsField = isField;
            this.HasGetterBoxed = getterBoxed != null;
            this.GetterBoxed = getterBoxed;
            this.HasSetterBoxed = setterBoxed != null;
            this.SetterBoxed = setterBoxed;
            this.HasGetter = getter != null;
            this.Getter = getter;
            this.HasSetter = setter != null;
            this.Setter = setter;
            this.Attributes = attributes;
            this.IsBacked = isBacked;
            this.IsStatic = isStatic;
            this.IsExplicitFromInterface = isExplicitFromInterface;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Type.Name} {Name}";
        }
    }
}

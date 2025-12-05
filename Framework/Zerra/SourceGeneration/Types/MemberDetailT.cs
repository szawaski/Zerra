// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    /// <summary>
    /// Generic specialized version of <see cref="MemberDetail"/> for a specific member type <typeparamref name="T"/>.
    /// Provides strongly-typed access to getter and setter delegates without boxing.
    /// Inherits all base member metadata and boxed accessors from <see cref="MemberDetail"/>.
    /// </summary>
    /// <typeparam name="T">The type of the member value.</typeparam>
    public sealed class MemberDetail<T> : MemberDetail
    {
        /// <summary>Indicates whether a strongly-typed getter delegate exists for type <typeparamref name="T"/>.</summary>
        public readonly new bool HasGetter;
        /// <summary>Strongly-typed delegate for getting the member value of type <typeparamref name="T"/> without boxing.</summary>
        public readonly new Func<object, T?>? Getter;

        /// <summary>Indicates whether a strongly-typed setter delegate exists for type <typeparamref name="T"/>.</summary>
        public readonly new bool HasSetter;
        /// <summary>Strongly-typed delegate for setting the member value of type <typeparamref name="T"/> without boxing.</summary>
        public readonly new Action<object, T?>? Setter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemberDetail{T}"/> class with generic type specialization.
        /// Provides strongly-typed access to getter and setter delegates for member type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="parentType">The parent type that owns this member.</param>
        /// <param name="name">The name of the member.</param>
        /// <param name="isField">Whether the member is a field.</param>
        /// <param name="getter">Strongly-typed getter delegate for type <typeparamref name="T"/>.</param>
        /// <param name="getterBoxed">Boxed getter delegate.</param>
        /// <param name="setter">Strongly-typed setter delegate for type <typeparamref name="T"/>.</param>
        /// <param name="setterBoxed">Boxed setter delegate.</param>
        /// <param name="attributes">Custom attributes applied to the member.</param>
        /// <param name="isBacked">Whether the member has actual storage (is property or field backed).</param>
        /// <param name="isStatic">Whether the member is static.</param>
        /// <param name="isExplicitFromInterface">Whether the member is an explicit interface implementation.</param>
        public MemberDetail(Type parentType, string name, bool isField, Func<object, T?>? getter, Func<object, object?>? getterBoxed, Action<object, T?>? setter, Action<object, object?>? setterBoxed, IReadOnlyList<Attribute> attributes, bool isBacked, bool isStatic, bool isExplicitFromInterface)
            : base(parentType, typeof(T), name, isField, getter, getterBoxed, setter, setterBoxed, attributes, isBacked, isStatic, isExplicitFromInterface)
        {
            this.HasGetter = getter != null;
            this.Getter = getter;
            this.HasSetter = setter != null;
            this.Setter = setter;
        }

        /// <summary>
        /// Gets the cached strongly-typed type detail for this member's type <typeparamref name="T"/>.
        /// Lazily initializes and casts the base type detail to the generic form without boxing.
        /// </summary>
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

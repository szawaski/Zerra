// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    /// <summary>
    /// Generic specialized version of <see cref="TypeDetail"/> for a specific type <typeparamref name="T"/>.
    /// Provides strongly-typed access to constructors and creator delegates without boxing.
    /// Inherits all base type metadata and collection interface information from <see cref="TypeDetail"/>.
    /// </summary>
    /// <typeparam name="T">The type being analyzed.</typeparam>
    public sealed class TypeDetail<T> : TypeDetail
    {
        /// <summary>Strongly-typed collection of constructors available for type <typeparamref name="T"/>.</summary>
        public readonly new IReadOnlyList<ConstructorDetail<T>> Constructors;

        /// <summary>Indicates whether a strongly-typed creator delegate exists for instantiation of type <typeparamref name="T"/>.</summary>
        public readonly new bool HasCreator;
        /// <summary>Strongly-typed delegate for creating instances of type <typeparamref name="T"/> without boxing.</summary>
        public readonly new Func<T>? Creator;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeDetail{T}"/> class with generic type specialization.
        /// Provides strongly-typed access to constructors and creation delegates for type <typeparamref name="T"/>.
        /// </summary>
        public TypeDetail(
            IReadOnlyList<MemberDetail> members,
            IReadOnlyList<ConstructorDetail<T>> constructors, 
            Func<T>? creator, 
            Func<object>? creatorBoxed,

            bool IsNullable,
            CoreType? CoreType,
            SpecialType? SpecialType,
            CoreEnumType? EnumUnderlyingType,

            bool HasIEnumerable,
            bool HasIEnumerableGeneric,
            bool HasICollection,
            bool HasICollectionGeneric,
            bool HasIReadOnlyCollectionGeneric,
            bool HasIList,
            bool HasIListGeneric,
            bool HasIReadOnlyListGeneric,
            bool HasISetGeneric,
            bool HasIReadOnlySetGeneric,
            bool HasIDictionary,
            bool HasIDictionaryGeneric,
            bool HasIReadOnlyDictionaryGeneric,

            bool IsIEnumerable,
            bool IsIEnumerableGeneric,
            bool IsICollection,
            bool IsICollectionGeneric,
            bool IsIReadOnlyCollectionGeneric,
            bool IsIList,
            bool IsIListGeneric,
            bool IsIReadOnlyListGeneric,
            bool IsISetGeneric,
            bool IsIReadOnlySetGeneric,
            bool IsIDictionary,
            bool IsIDictionaryGeneric,
            bool IsIReadOnlyDictionaryGeneric,

            Type? InnerType,
            Type? IEnumerableGenericInnerType,
            Type? DictionaryInnerType,

            IReadOnlyList<Type> InnerTypes,
            IReadOnlyList<Type> BaseTypes,
            IReadOnlyList<Type> Interfaces,
            IReadOnlyList<Attribute> Attributes
            )
            : base(
                typeof(T),
                members,
                constructors,
                creator,
                creatorBoxed,

                IsNullable,
                CoreType,
                SpecialType,
                EnumUnderlyingType,

                HasIEnumerable,
                HasIEnumerableGeneric,
                HasICollection,
                HasICollectionGeneric,
                HasIReadOnlyCollectionGeneric,
                HasIList,
                HasIListGeneric,
                HasIReadOnlyListGeneric,
                HasISetGeneric,
                HasIReadOnlySetGeneric,
                HasIDictionary,
                HasIDictionaryGeneric,
                HasIReadOnlyDictionaryGeneric,

                IsIEnumerable,
                IsIEnumerableGeneric,
                IsICollection,
                IsICollectionGeneric,
                IsIReadOnlyCollectionGeneric,
                IsIList,
                IsIListGeneric,
                IsIReadOnlyListGeneric,
                IsISetGeneric,
                IsIReadOnlySetGeneric,
                IsIDictionary,
                IsIDictionaryGeneric,
                IsIReadOnlyDictionaryGeneric,

                InnerType,
                IEnumerableGenericInnerType,
                DictionaryInnerType,

                InnerTypes,
                BaseTypes,
                Interfaces,
                Attributes
              )
        {
            this.Constructors = constructors;
            this.HasCreator = creator != null;
            this.Creator = creator;
        }
    }
}

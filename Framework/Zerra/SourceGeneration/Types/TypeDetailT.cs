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
            IReadOnlyList<MethodDetail>? methods,
            Func<T>? creator, 
            Func<object>? creatorBoxed,

            bool isNullable,
            CoreType? coreType,
            SpecialType? specialType,
            CoreEnumType? enumUnderlyingType,

            bool hasIEnumerable,
            bool hasIEnumerableGeneric,
            bool hasICollection,
            bool hasICollectionGeneric,
            bool hasIReadOnlyCollectionGeneric,
            bool hasIList,
            bool hasIListGeneric,
            bool hasIReadOnlyListGeneric,
            bool hasListGeneric,
            bool hasISetGeneric,
            bool hasIReadOnlySetGeneric,
            bool hasHashSetGeneric,
            bool hasIDictionary,
            bool hasIDictionaryGeneric,
            bool hasIReadOnlyDictionaryGeneric,
            bool hasDictionaryGeneric,

            bool isIEnumerable,
            bool isIEnumerableGeneric,
            bool isICollection,
            bool isICollectionGeneric,
            bool isIReadOnlyCollectionGeneric,
            bool isIList,
            bool isIListGeneric,
            bool isIReadOnlyListGeneric,
            bool isListGeneric,
            bool isISetGeneric,
            bool isIReadOnlySetGeneric,
            bool isHashSetGeneric,
            bool isIDictionary,
            bool isIDictionaryGeneric,
            bool isIReadOnlyDictionaryGeneric,
            bool isDictionaryGeneric,

            Type? innerType,
            Type? iEnumerableGenericInnerType,
            Type? dictionaryInnerType,

            IReadOnlyList<Type> innerTypes,
            IReadOnlyList<Type> baseTypes,
            IReadOnlyList<Type> interfaces,
            IReadOnlyList<Attribute> attributes
            )
            : base(
                typeof(T),
                members,
                constructors,
                methods,
                creator,
                creatorBoxed,

                isNullable,
                coreType,
                specialType,
                enumUnderlyingType,

                hasIEnumerable,
                hasIEnumerableGeneric,
                hasICollection,
                hasICollectionGeneric,
                hasIReadOnlyCollectionGeneric,
                hasIList,
                hasIListGeneric,
                hasIReadOnlyListGeneric,
                hasListGeneric,
                hasISetGeneric,
                hasIReadOnlySetGeneric,
                hasHashSetGeneric,
                hasIDictionary,
                hasIDictionaryGeneric,
                hasIReadOnlyDictionaryGeneric,
                hasDictionaryGeneric,

                isIEnumerable,
                isIEnumerableGeneric,
                isICollection,
                isICollectionGeneric,
                isIReadOnlyCollectionGeneric,
                isIList,
                isIListGeneric,
                isIReadOnlyListGeneric,
                isListGeneric,
                isISetGeneric,
                isIReadOnlySetGeneric,
                isHashSetGeneric,
                isIDictionary,
                isIDictionaryGeneric,
                isIReadOnlyDictionaryGeneric,
                isDictionaryGeneric,

                innerType,
                iEnumerableGenericInnerType,
                dictionaryInnerType,

                innerTypes,
                baseTypes,
                interfaces,
                attributes
              )
        {
            this.Constructors = constructors;
            this.HasCreator = creator != null;
            this.Creator = creator;
        }
    }
}

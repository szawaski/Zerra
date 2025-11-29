// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    public sealed class TypeDetail<T> : TypeDetail
    {
        public readonly new IReadOnlyList<ConstructorDetail<T>> Constructors;

        public readonly new bool HasCreator;
        public readonly new Func<T>? Creator;

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

// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    public class TypeDetail
    {
        public readonly Type Type;
        public readonly IReadOnlyList<MemberDetail> Members;
        public readonly IReadOnlyList<ConstructorDetail> Constructors;

        public readonly bool HasCreatorBoxed;
        public readonly Func<object>? CreatorBoxed;

        public readonly bool HasCreator;
        public readonly Delegate? Creator;

        public readonly bool IsNullable;
        public readonly CoreType? CoreType;
        public readonly SpecialType? SpecialType;
        public readonly CoreEnumType? EnumUnderlyingType;

        public readonly bool HasIEnumerable;
        public readonly bool HasIEnumerableGeneric;
        public readonly bool HasICollection;
        public readonly bool HasICollectionGeneric;
        public readonly bool HasIReadOnlyCollectionGeneric;
        public readonly bool HasIList;
        public readonly bool HasIListGeneric;
        public readonly bool HasIReadOnlyListGeneric;
        public readonly bool HasISetGeneric;
        public readonly bool HasIReadOnlySetGeneric;
        public readonly bool HasIDictionary;
        public readonly bool HasIDictionaryGeneric;
        public readonly bool HasIReadOnlyDictionaryGeneric;

        public readonly bool IsIEnumerable;
        public readonly bool IsIEnumerableGeneric;
        public readonly bool IsICollection;
        public readonly bool IsICollectionGeneric;
        public readonly bool IsIReadOnlyCollectionGeneric;
        public readonly bool IsIList;
        public readonly bool IsIListGeneric;
        public readonly bool IsIReadOnlyListGeneric;
        public readonly bool IsISetGeneric;
        public readonly bool IsIReadOnlySetGeneric;
        public readonly bool IsIDictionary;
        public readonly bool IsIDictionaryGeneric;
        public readonly bool IsIReadOnlyDictionaryGeneric;

        public readonly IReadOnlyList<Type> InnerTypes;

        public readonly IReadOnlyList<Type> BaseTypes;

        public readonly IReadOnlyList<Type> Interfaces;

        public readonly IReadOnlyList<Attribute> Attributes;

        public readonly Type? InnerType;

        public readonly Type? IEnumerableGenericInnerType;

        public readonly Type? DictionaryInnerType;

        public TypeDetail? InnerTypeDetail => InnerType == null ? null : TypeAnalyzer.GetTypeDetail(InnerType);

        public TypeDetail? IEnumerableGenericInnerTypeDetail => IEnumerableGenericInnerType == null ? null : TypeAnalyzer.GetTypeDetail(IEnumerableGenericInnerType);

        public TypeDetail? DictionaryInnerTypeDetail => DictionaryInnerType == null ? null : TypeAnalyzer.GetTypeDetail(DictionaryInnerType);

        public TypeDetail(
            Type type,
            IReadOnlyList<MemberDetail> members,
            IReadOnlyList<ConstructorDetail> constructors,
            Delegate? creator,
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
        {
            this.Type = type;
            this.Members = members;
            this.Constructors = constructors;
            this.HasCreator = creator != null;
            this.Creator = creator;
            this.HasCreatorBoxed = creatorBoxed != null;
            this.CreatorBoxed = creatorBoxed;

            this.IsNullable = IsNullable;
            this.CoreType = CoreType;
            this.SpecialType = SpecialType;
            this.EnumUnderlyingType = EnumUnderlyingType;

            this.HasIEnumerable = HasIEnumerable;
            this.HasIEnumerableGeneric = HasIEnumerableGeneric;
            this.HasICollection = HasICollection;
            this.HasICollectionGeneric = HasICollectionGeneric;
            this.HasIReadOnlyCollectionGeneric = HasIReadOnlyCollectionGeneric;
            this.HasIList = HasIList;
            this.HasIListGeneric = HasIListGeneric;
            this.HasIReadOnlyListGeneric = HasIReadOnlyListGeneric;
            this.HasISetGeneric = HasISetGeneric;
            this.HasIReadOnlySetGeneric = HasIReadOnlySetGeneric;
            this.HasIDictionary = HasIDictionary;
            this.HasIDictionaryGeneric = HasIDictionaryGeneric;
            this.HasIReadOnlyDictionaryGeneric = HasIReadOnlyDictionaryGeneric;

            this.IsIEnumerable = IsIEnumerable;
            this.IsIEnumerableGeneric = IsIEnumerableGeneric;
            this.IsICollection = IsICollection;
            this.IsICollectionGeneric = IsICollectionGeneric;
            this.IsIReadOnlyCollectionGeneric = IsIReadOnlyCollectionGeneric;
            this.IsIList = IsIList;
            this.IsIListGeneric = IsIListGeneric;
            this.IsIReadOnlyListGeneric = IsIReadOnlyListGeneric;
            this.IsISetGeneric = IsISetGeneric;
            this.IsIReadOnlySetGeneric = IsIReadOnlySetGeneric;
            this.IsIDictionary = IsIDictionary;
            this.IsIDictionaryGeneric = IsIDictionaryGeneric;
            this.IsIReadOnlyDictionaryGeneric = IsIReadOnlyDictionaryGeneric;

            this.InnerType = InnerType;
            this.IEnumerableGenericInnerType = IEnumerableGenericInnerType;
            this.DictionaryInnerType = DictionaryInnerType;

            this.InnerTypes = InnerTypes;
            this.BaseTypes = BaseTypes;
            this.Interfaces = Interfaces;
            this.Attributes = Attributes;
        }

        private IReadOnlyList<MemberDetail>? serializableMemberDetails = null;
        public IReadOnlyList<MemberDetail> SerializableMemberDetails
        {
            get
            {
                serializableMemberDetails ??= Members.Where(x => !x.IsStatic && x.IsBacked && !x.IsExplicitFromInterface && IsSerializableType(x.TypeDetailBoxed)).ToArray();
                return serializableMemberDetails;
            }
        }

        private static bool IsSerializableType(TypeDetail typeDetail)
        {
            if (typeDetail.CoreType.HasValue)
                return true;
            if (typeDetail.Type.IsEnum)
                return true;
            if (typeDetail.Type.IsArray)
                return true;
            if (typeDetail.IsNullable)
                return IsSerializableType(typeDetail.InnerTypeDetail!);
            if (typeDetail.Type.IsClass)
                return true;
            if (typeDetail.Type.IsInterface)
                return true;
            return false;
        }
    }
}

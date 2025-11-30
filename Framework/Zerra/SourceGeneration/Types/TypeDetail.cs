// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    /// <summary>
    /// Comprehensive metadata for a type including its members, constructors, type classification, and collection interface implementations.
    /// Generated at runtime or by source generators to provide detailed type information for serialization, reflection, and CQRS routing.
    /// Contains flags for collection types, nullable types, core types, and special framework types.
    /// </summary>
    public class TypeDetail
    {
        /// <summary>The type being analyzed.</summary>
        public readonly Type Type;
        /// <summary>Collection of all members (properties and fields) declared or inherited by this type.</summary>
        public readonly IReadOnlyList<MemberDetail> Members;
        /// <summary>Collection of all constructors available for this type.</summary>
        public readonly IReadOnlyList<ConstructorDetail> Constructors;

        /// <summary>Indicates whether a boxed creator delegate exists for instantiation.</summary>
        public readonly bool HasCreatorBoxed;
        /// <summary>Boxed delegate for creating instances of this type; returns object.</summary>
        public readonly Func<object>? CreatorBoxed;

        /// <summary>Indicates whether a typed creator delegate exists for instantiation.</summary>
        public readonly bool HasCreator;
        /// <summary>Typed delegate for creating instances of this type.</summary>
        public readonly Delegate? Creator;

        /// <summary>Indicates whether this type is a nullable value type.</summary>
        public readonly bool IsNullable;
        /// <summary>Core type classification if this type is a built-in supported type; otherwise null.</summary>
        public readonly CoreType? CoreType;
        /// <summary>Special type classification if this type is a framework special type; otherwise null.</summary>
        public readonly SpecialType? SpecialType;
        /// <summary>Enum underlying type classification if this type is an enum; otherwise null.</summary>
        public readonly CoreEnumType? EnumUnderlyingType;

        /// <summary>Indicates whether this type implements <see cref="System.Collections.IEnumerable"/>.</summary>
        public readonly bool HasIEnumerable;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.Generic.IEnumerable{T}"/>.</summary>
        public readonly bool HasIEnumerableGeneric;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.ICollection"/>.</summary>
        public readonly bool HasICollection;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.Generic.ICollection{T}"/>.</summary>
        public readonly bool HasICollectionGeneric;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.Generic.IReadOnlyCollection{T}"/>.</summary>
        public readonly bool HasIReadOnlyCollectionGeneric;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.IList"/>.</summary>
        public readonly bool HasIList;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.Generic.IList{T}"/>.</summary>
        public readonly bool HasIListGeneric;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.Generic.IReadOnlyList{T}"/>.</summary>
        public readonly bool HasIReadOnlyListGeneric;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.Generic.ISet{T}"/>.</summary>
        public readonly bool HasISetGeneric;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.Generic.IReadOnlySet{T}"/>.</summary>
        public readonly bool HasIReadOnlySetGeneric;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.IDictionary"/>.</summary>
        public readonly bool HasIDictionary;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>.</summary>
        public readonly bool HasIDictionaryGeneric;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/>.</summary>
        public readonly bool HasIReadOnlyDictionaryGeneric;

        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.IEnumerable"/>.</summary>
        public readonly bool IsIEnumerable;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.Generic.IEnumerable{T}"/>.</summary>
        public readonly bool IsIEnumerableGeneric;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.ICollection"/>.</summary>
        public readonly bool IsICollection;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.Generic.ICollection{T}"/>.</summary>
        public readonly bool IsICollectionGeneric;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.Generic.IReadOnlyCollection{T}"/>.</summary>
        public readonly bool IsIReadOnlyCollectionGeneric;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.IList"/>.</summary>
        public readonly bool IsIList;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.Generic.IList{T}"/>.</summary>
        public readonly bool IsIListGeneric;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.Generic.IReadOnlyList{T}"/>.</summary>
        public readonly bool IsIReadOnlyListGeneric;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.Generic.ISet{T}"/>.</summary>
        public readonly bool IsISetGeneric;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.Generic.IReadOnlySet{T}"/>.</summary>
        public readonly bool IsIReadOnlySetGeneric;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.IDictionary"/>.</summary>
        public readonly bool IsIDictionary;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>.</summary>
        public readonly bool IsIDictionaryGeneric;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/>.</summary>
        public readonly bool IsIReadOnlyDictionaryGeneric;

        /// <summary>Collection of all inner generic type arguments for generic types.</summary>
        public readonly IReadOnlyList<Type> InnerTypes;

        /// <summary>Collection of all direct base types (parent classes).</summary>
        public readonly IReadOnlyList<Type> BaseTypes;

        /// <summary>Collection of all interfaces implemented by this type.</summary>
        public readonly IReadOnlyList<Type> Interfaces;

        /// <summary>Collection of all custom attributes applied to this type.</summary>
        public readonly IReadOnlyList<Attribute> Attributes;

        /// <summary>The inner type of nullable types; null if this type is not nullable.</summary>
        public readonly Type? InnerType;

        /// <summary>The element type of <see cref="System.Collections.Generic.IEnumerable{T}"/> if implemented; otherwise null.</summary>
        public readonly Type? IEnumerableGenericInnerType;

        /// <summary>The value type of dictionaries if this type implements dictionary interfaces; otherwise null.</summary>
        public readonly Type? DictionaryInnerType;

        /// <summary>Gets the type detail for <see cref="InnerType"/> if this is a nullable type; otherwise null.</summary>
        public TypeDetail? InnerTypeDetail => InnerType == null ? null : TypeAnalyzer.GetTypeDetail(InnerType);

        /// <summary>Gets the type detail for <see cref="IEnumerableGenericInnerType"/> if this type is enumerable; otherwise null.</summary>
        public TypeDetail? IEnumerableGenericInnerTypeDetail => IEnumerableGenericInnerType == null ? null : TypeAnalyzer.GetTypeDetail(IEnumerableGenericInnerType);

        /// <summary>Gets the type detail for <see cref="DictionaryInnerType"/> if this type is a dictionary; otherwise null.</summary>
        public TypeDetail? DictionaryInnerTypeDetail => DictionaryInnerType == null ? null : TypeAnalyzer.GetTypeDetail(DictionaryInnerType);

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeDetail"/> class with comprehensive type metadata.
        /// </summary>
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
        /// <summary>
        /// Gets a cached collection of members that can be serialized.
        /// Includes non-static, backed members that are not explicit interface implementations and have serializable types.
        /// </summary>
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

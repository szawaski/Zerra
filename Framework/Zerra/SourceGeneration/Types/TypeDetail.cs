// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Zerra.SourceGeneration.Reflection;

namespace Zerra.SourceGeneration.Types
{
    /// <summary>
    /// Comprehensive metadata for a type including its members, constructors, type classification, and collection interface implementations.
    /// Generated at runtime or by source generators to provide detailed type information for serialization, reflection, and CQRS routing.
    /// Contains flags for collection types, nullable types, core types, and special framework types.
    /// </summary>
    public partial class TypeDetail
    {
        protected readonly Lock locker = new();

        /// <summary>The type being analyzed.</summary>
        public readonly Type Type;

        /// <summary>Collection of all members (properties and fields) declared or inherited by this type.</summary>
        public readonly IReadOnlyList<MemberDetail> Members;

        /// <summary>Collection of all constructors available for this type.</summary>
        public readonly IReadOnlyList<ConstructorDetail> Constructors;

        private IReadOnlyList<MethodDetail>? methods;
        /// <summary>Collection of all methods declared or inherited by this type.</summary>
        public IReadOnlyList<MethodDetail> Methods
        {
            get
            {
                if (methods == null)
                {
                    if (!RuntimeFeature.IsDynamicCodeSupported)
                        throw new NotSupportedException($"Cannot generate methods for {Type.Name}.  Dynamic code generation is not supported in this build configuration.");
                    lock (locker)
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
                        methods ??= TypeDetailGenerator.GetMethods(this.Type, Interfaces);
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
                }
                return methods;
            }
        }

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
        /// <summary>Indicates whether this type implements <see cref="System.Collections.Generic.List{T}"/>.</summary>
        public readonly bool HasListGeneric;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.Generic.ISet{T}"/>.</summary>
        public readonly bool HasISetGeneric;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.Generic.IReadOnlySet{T}"/>.</summary>
        public readonly bool HasIReadOnlySetGeneric;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.Generic.HashSet{T}"/>.</summary>
        public readonly bool HasHashSetGeneric;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.IDictionary"/>.</summary>
        public readonly bool HasIDictionary;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>.</summary>
        public readonly bool HasIDictionaryGeneric;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/>.</summary>
        public readonly bool HasIReadOnlyDictionaryGeneric;
        /// <summary>Indicates whether this type implements <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/>.</summary>
        public readonly bool HasDictionaryGeneric;

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
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.Generic.List{T}"/>.</summary>
        public readonly bool IsListGeneric;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.Generic.ISet{T}"/>.</summary>
        public readonly bool IsISetGeneric;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.Generic.IReadOnlySet{T}"/>.</summary>
        public readonly bool IsIReadOnlySetGeneric;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.Generic.HashSet{T}"/>.</summary>
        public readonly bool IsHashSetGeneric;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.IDictionary"/>.</summary>
        public readonly bool IsIDictionary;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>.</summary>
        public readonly bool IsIDictionaryGeneric;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/>.</summary>
        public readonly bool IsIReadOnlyDictionaryGeneric;
        /// <summary>Indicates whether this type is exactly <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/>.</summary>
        public readonly bool IsDictionaryGeneric;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeDetail"/> class with comprehensive type metadata.
        /// </summary>
        public TypeDetail(
            Type type,
            IReadOnlyList<MemberDetail> members,
            IReadOnlyList<ConstructorDetail> constructors,
            IReadOnlyList<MethodDetail>? methods,
            Delegate? creator,
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
        {
            this.Type = type;
            this.Members = members;
            this.Constructors = constructors;
            this.methods = methods;
            this.HasCreator = creator != null;
            this.Creator = creator;
            this.HasCreatorBoxed = creatorBoxed != null;
            this.CreatorBoxed = creatorBoxed;

            this.IsNullable = isNullable;
            this.CoreType = coreType;
            this.SpecialType = specialType;
            this.EnumUnderlyingType = enumUnderlyingType;

            this.HasIEnumerable = hasIEnumerable;
            this.HasIEnumerableGeneric = hasIEnumerableGeneric;
            this.HasICollection = hasICollection;
            this.HasICollectionGeneric = hasICollectionGeneric;
            this.HasIReadOnlyCollectionGeneric = hasIReadOnlyCollectionGeneric;
            this.HasIList = hasIList;
            this.HasIListGeneric = hasIListGeneric;
            this.HasIReadOnlyListGeneric = hasIReadOnlyListGeneric;
            this.HasListGeneric = hasListGeneric;
            this.HasISetGeneric = hasISetGeneric;
            this.HasIReadOnlySetGeneric = hasIReadOnlySetGeneric;
            this.HasHashSetGeneric = hasHashSetGeneric;
            this.HasIDictionary = hasIDictionary;
            this.HasIDictionaryGeneric = hasIDictionaryGeneric;
            this.HasIReadOnlyDictionaryGeneric = hasIReadOnlyDictionaryGeneric;
            this.HasDictionaryGeneric = hasDictionaryGeneric;

            this.IsIEnumerable = isIEnumerable;
            this.IsIEnumerableGeneric = isIEnumerableGeneric;
            this.IsICollection = isICollection;
            this.IsICollectionGeneric = isICollectionGeneric;
            this.IsIReadOnlyCollectionGeneric = isIReadOnlyCollectionGeneric;
            this.IsIList = isIList;
            this.IsIListGeneric = isIListGeneric;
            this.IsIReadOnlyListGeneric = isIReadOnlyListGeneric;
            this.IsListGeneric = isListGeneric;
            this.IsISetGeneric = isISetGeneric;
            this.IsIReadOnlySetGeneric = isIReadOnlySetGeneric;
            this.IsHashSetGeneric = isHashSetGeneric;
            this.IsIDictionary = isIDictionary;
            this.IsIDictionaryGeneric = isIDictionaryGeneric;
            this.IsIReadOnlyDictionaryGeneric = isIReadOnlyDictionaryGeneric;
            this.IsDictionaryGeneric = isDictionaryGeneric;

            this.InnerType = innerType;
            this.IEnumerableGenericInnerType = iEnumerableGenericInnerType;
            this.DictionaryInnerType = dictionaryInnerType;

            this.InnerTypes = innerTypes;
            this.BaseTypes = baseTypes;
            this.Interfaces = interfaces;
            this.Attributes = attributes;
        }
    }
}

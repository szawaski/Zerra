// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Zerra.Reflection.Runtime
{
    internal sealed class TypeDetailRuntime<T> : TypeDetail<T>
    {
        private static readonly string nullaleTypeName = typeof(Nullable<>).Name;
        private static readonly string enumberableTypeName = nameof(IEnumerable);
        private static readonly string enumberableGenericTypeName = typeof(IEnumerable<>).Name;

        private static readonly string collectionTypeName = nameof(ICollection);
        private static readonly string collectionGenericTypeName = typeof(ICollection<>).Name;
        private static readonly string readOnlyCollectionGenericTypeName = typeof(IReadOnlyCollection<>).Name;
        private static readonly string listTypeName = nameof(IList);
        private static readonly string listGenericTypeName = typeof(IList<>).Name;
        private static readonly string readOnlyListTypeName = typeof(IReadOnlyList<>).Name;
        private static readonly string setGenericTypeName = typeof(ISet<>).Name;
#if NET5_0_OR_GREATER
        private static readonly string readOnlySetGenericTypeName = typeof(IReadOnlySet<>).Name;
#endif
        private static readonly string dictionaryTypeName = typeof(IDictionary).Name;
        private static readonly string dictionaryGenericTypeName = typeof(IDictionary<,>).Name;
        private static readonly string readOnlyDictionaryGenericTypeName = typeof(IReadOnlyDictionary<,>).Name;

        private static readonly Type keyValuePairType = typeof(KeyValuePair<,>);
        private static readonly Type dictionaryEntryType = typeof(DictionaryEntry);

        public override bool IsNullable { get; }

        private bool coreTypeLoaded = false;
        private CoreType? coreType;
        public override CoreType? CoreType
        {
            get
            {
                if (!coreTypeLoaded)
                {
                    lock (locker)
                    {
                        if (!coreTypeLoaded)
                        {
                            if (TypeLookup.CoreTypeLookup(Type, out var coreTypeLookup))
                                coreType = coreTypeLookup;
                            coreTypeLoaded = true;
                        }
                    }
                }
                return coreType;
            }
        }

        private bool specialTypeLoaded = false;
        private SpecialType? specialType;
        public override SpecialType? SpecialType
        {
            get
            {
                if (!specialTypeLoaded)
                {
                    lock (locker)
                    {
                        if (!specialTypeLoaded)
                        {
                            if (TypeLookup.SpecialTypeLookup(Type, out var specialTypeLookup))
                                specialType = specialTypeLookup;
                            specialTypeLoaded = true;
                        }
                    }
                }
                return specialType;
            }
        }

        private Type[]? innerTypes = null;
        public override IReadOnlyList<Type> InnerTypes
        {
            get
            {
                if (innerTypes == null)
                {
                    lock (locker)
                    {
                        if (innerTypes == null)
                        {
                            if (Type.IsGenericType)
                            {
                                innerTypes = Type.GetGenericArguments();
                            }
                            else if (Type.IsArray)
                            {
                                innerTypes = new Type[] { Type.GetElementType()! };
                            }
                            else
                            {
                                innerTypes = Array.Empty<Type>();
                            }
                        }
                    }
                }
                return innerTypes;
            }
        }

        private bool innerTypeLoaded = false;
        private Type? innerType = null;
        public override Type InnerType
        {
            get
            {
                if (!innerTypeLoaded)
                {
                    lock (locker)
                    {
                        if (!innerTypeLoaded)
                        {
                            if (InnerTypes.Count == 1)
                            {
                                innerType = InnerTypes[0];
                            }
                            innerTypeLoaded = true;
                        }
                    }
                }
                return innerType ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name} does not have a {nameof(InnerType)}");
            }
        }

        private bool? isTask;
        public override bool IsTask
        {
            get
            {
                if (!isTask.HasValue)
                {
                    lock (locker)
                    {
                        if (!isTask.HasValue)
                        {
                            if (TypeLookup.SpecialTypeLookup(Type, out var specialTypeLookup))
                                isTask = specialTypeLookup == Reflection.SpecialType.Task;
                            else
                                isTask = false;
                        }
                    }
                }
                return isTask.Value;
            }
        }

        private bool enumUnderlyingTypeLoaded = false;
        private CoreEnumType? enumUnderlyingType;
        public override CoreEnumType? EnumUnderlyingType
        {
            get
            {
                if (!enumUnderlyingTypeLoaded)
                {
                    lock (locker)
                    {
                        if (!enumUnderlyingTypeLoaded)
                        {
                            if (Type.IsEnum)
                            {
                                var enumEnderlyingType = Enum.GetUnderlyingType(this.Type);
                                if (!TypeLookup.CoreEnumTypeLookup(enumEnderlyingType, out var enumCoreTypeLookup))
                                    throw new NotImplementedException("Should not happen");
                                enumUnderlyingType = enumCoreTypeLookup;
                            }
                            else if (this.IsNullable && this.InnerTypes[0].IsEnum)
                            {
                                var enumEnderlyingType = Enum.GetUnderlyingType(this.InnerTypes[0]);
                                if (!TypeLookup.CoreEnumTypeLookup(enumEnderlyingType, out var enumCoreTypeLookup))
                                    throw new NotImplementedException("Should not happen");
                                enumCoreTypeLookup = enumCoreTypeLookup switch
                                {
                                    CoreEnumType.Byte => CoreEnumType.ByteNullable,
                                    CoreEnumType.SByte => CoreEnumType.SByteNullable,
                                    CoreEnumType.Int16 => CoreEnumType.Int16Nullable,
                                    CoreEnumType.UInt16 => CoreEnumType.UInt16Nullable,
                                    CoreEnumType.Int32 => CoreEnumType.Int32Nullable,
                                    CoreEnumType.UInt32 => CoreEnumType.UInt32Nullable,
                                    CoreEnumType.Int64 => CoreEnumType.Int64Nullable,
                                    CoreEnumType.UInt64 => CoreEnumType.UInt64Nullable,
                                    _ => throw new NotImplementedException(),
                                };
                                enumUnderlyingType = enumCoreTypeLookup;
                            }
                            enumUnderlyingTypeLoaded = true;
                        }
                    }
                }
                return enumUnderlyingType;
            }
        }

        private Type[]? baseTypes = null;
        public override IReadOnlyList<Type> BaseTypes
        {
            get
            {
                if (baseTypes == null)
                {
                    lock (locker)
                    {
                        if (baseTypes == null)
                        {
                            var baseType = Type;
                            var items = new List<Type>();
                            while (baseType != null)
                            {
                                items.Add(baseType);
                                baseType = baseType.BaseType;
                            }
                            baseTypes = items.ToArray();
                        }
                    }
                }
                return baseTypes;
            }
        }

        private Type[]? interfaces = null;
        public override IReadOnlyList<Type> Interfaces
        {
            get
            {
                if (interfaces == null)
                {
                    lock (locker)
                    {
                        interfaces ??= Type.GetInterfaces();
                    }
                }
                return interfaces;
            }
        }

        private bool hasIEnumerable;
        private bool hasIEnumerableGeneric;
        private bool hasICollection;
        private bool hasICollectionGeneric;
        private bool hasIReadOnlyCollectionGeneric;
        private bool hasIList;
        private bool hasIListGeneric;
        private bool hasIReadOnlyListGeneric;
        private bool hasISetGeneric;
        private bool hasIReadOnlySetGeneric;
        private bool hasIDictionary;
        private bool hasIDictionaryGeneric;
        private bool hasIReadOnlyDictionaryGeneric;
        public override bool HasIEnumerable
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIEnumerable;
            }
        }
        public override bool HasIEnumerableGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIEnumerableGeneric;
            }
        }
        public override bool HasICollection
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasICollection;
            }
        }
        public override bool HasICollectionGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasICollectionGeneric;
            }
        }
        public override bool HasIReadOnlyCollectionGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIReadOnlyCollectionGeneric;
            }
        }
        public override bool HasIList
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIList;
            }
        }
        public override bool HasIListGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIListGeneric;
            }
        }
        public override bool HasIReadOnlyListGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIReadOnlyListGeneric;
            }
        }
        public override bool HasISetGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasISetGeneric;
            }
        }
        public override bool HasIReadOnlySetGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIReadOnlySetGeneric;
            }
        }
        public override bool HasIDictionary
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIDictionary;
            }
        }
        public override bool HasIDictionaryGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIDictionaryGeneric;
            }
        }
        public override bool HasIReadOnlyDictionaryGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIReadOnlyDictionaryGeneric;
            }
        }

        private bool isIEnumerable;
        private bool isIEnumerableGeneric;
        private bool isICollection;
        private bool isICollectionGeneric;
        private bool isIReadOnlyCollectionGeneric;
        private bool isIList;
        private bool isIListGeneric;
        private bool isIReadOnlyListGeneric;
        private bool isISetGeneric;
        private bool isIReadOnlySetGeneric;
        private bool isIDictionary;
        private bool isIDictionaryGeneric;
        private bool isIReadOnlyDictionaryGeneric;
        public override bool IsIEnumerable
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIEnumerable;
            }
        }
        public override bool IsIEnumerableGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIEnumerableGeneric;
            }
        }
        public override bool IsICollection
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isICollection;
            }
        }
        public override bool IsICollectionGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isICollectionGeneric;
            }
        }
        public override bool IsIReadOnlyCollectionGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIReadOnlyCollectionGeneric;
            }
        }
        public override bool IsIList
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIList;
            }
        }
        public override bool IsIListGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIListGeneric;
            }
        }
        public override bool IsIReadOnlyListGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIReadOnlyListGeneric;
            }
        }
        public override bool IsISetGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isISetGeneric;
            }
        }
        public override bool IsIReadOnlySetGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIReadOnlySetGeneric;
            }
        }
        public override bool IsIDictionary
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIDictionary;
            }
        }
        public override bool IsIDictionaryGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIDictionaryGeneric;
            }
        }
        public override bool IsIReadOnlyDictionaryGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIReadOnlyDictionaryGeneric;
            }
        }

        private bool hasIsInterfaceLoaded = false;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadHasIsInterface()
        {
            lock (locker)
            {
                if (!hasIsInterfaceLoaded)
                {
                    if (!TypeLookup.CoreTypes.Contains(Type))
                    {
                        var interfaces = new HashSet<string>(Interfaces.Select(x => x.Name));

                        hasIEnumerable = Type.IsArray || Type.Name == enumberableTypeName || interfaces.Contains(enumberableTypeName);
                        hasIEnumerableGeneric = Type.IsArray || Type.Name == enumberableGenericTypeName || interfaces.Contains(enumberableGenericTypeName);
                        hasICollection = Type.Name == collectionTypeName || interfaces.Contains(collectionTypeName);
                        hasICollectionGeneric = Type.Name == collectionGenericTypeName || interfaces.Contains(collectionGenericTypeName);
                        hasIReadOnlyCollectionGeneric = Type.Name == readOnlyCollectionGenericTypeName || interfaces.Contains(readOnlyCollectionGenericTypeName);
                        hasIList = Type.Name == listTypeName || interfaces.Contains(listTypeName);
                        hasIListGeneric = Type.Name == listGenericTypeName || interfaces.Contains(listGenericTypeName);
                        hasIReadOnlyListGeneric = Type.Name == readOnlyListTypeName || interfaces.Contains(readOnlyListTypeName);
                        hasISetGeneric = Type.Name == setGenericTypeName || interfaces.Contains(setGenericTypeName);
#if NET5_0_OR_GREATER
                        hasIReadOnlySetGeneric = Type.Name == readOnlySetGenericTypeName || interfaces.Contains(readOnlySetGenericTypeName);
#endif
                        hasIDictionary = Type.Name == dictionaryTypeName || interfaces.Contains(dictionaryTypeName);
                        hasIDictionaryGeneric = Type.Name == dictionaryGenericTypeName || interfaces.Contains(dictionaryGenericTypeName);
                        hasIReadOnlyDictionaryGeneric = Type.Name == readOnlyDictionaryGenericTypeName || interfaces.Contains(readOnlyDictionaryGenericTypeName);

                        isIEnumerable = Type.Name == enumberableTypeName;
                        isIEnumerableGeneric = Type.Name == enumberableGenericTypeName;
                        isICollection = Type.Name == collectionTypeName;
                        isICollectionGeneric = Type.Name == collectionGenericTypeName;
                        isIReadOnlyCollectionGeneric = Type.Name == readOnlyCollectionGenericTypeName;
                        isIList = Type.Name == listTypeName;
                        isIListGeneric = Type.Name == listGenericTypeName;
                        isIReadOnlyListGeneric = Type.Name == readOnlyListTypeName;
                        isISetGeneric = Type.Name == setGenericTypeName;
#if NET5_0_OR_GREATER
                        isIReadOnlySetGeneric = Type.Name == readOnlySetGenericTypeName;
#endif
                        isIDictionary = Type.Name == dictionaryTypeName;
                        isIDictionaryGeneric = Type.Name == dictionaryGenericTypeName;
                        isIReadOnlyDictionaryGeneric = Type.Name == readOnlyDictionaryGenericTypeName;
                    }
                    hasIsInterfaceLoaded = true;
                }
            }
        }

        private MemberDetail[]? memberDetails = null;
        public override IReadOnlyList<MemberDetail> MemberDetails
        {
            get
            {
                if (memberDetails == null)
                {
                    lock (locker)
                    {
                        if (memberDetails == null)
                        {
                            var items = new List<MemberDetail>();
                            if (!Type.IsGenericTypeDefinition)
                            {
                                IEnumerable<PropertyInfo> properties = Type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                IEnumerable<FieldInfo> fields = Type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                if (Type.IsInterface)
                                {
                                    foreach (var i in Interfaces)
                                    {
                                        var iProperties = i.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                        var iFields = i.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#if NETSTANDARD2_0
                                        var existingPropertyNames = properties.Select(y => y.Name).ToArray();
#else
                                        var existingPropertyNames = properties.Select(y => y.Name).ToHashSet();
#endif
                                        properties = properties.Concat(iProperties.Where(x => !existingPropertyNames.Contains(x.Name)));
#if NETSTANDARD2_0
                                        var existingFieldNames = fields.Select(y => y.Name).ToArray();
#else
                                        var existingFieldNames = fields.Select(y => y.Name).ToHashSet();
#endif
                                        fields = fields.Concat(iFields.Where(x => !existingFieldNames.Contains(x.Name)));
                                    }
                                }
                                foreach (var property in properties)
                                {
                                    if (property.GetIndexParameters().Length > 0)
                                        continue;
                                    MemberDetail? backingMember = null;

                                    //<{property.Name}>k__BackingField
                                    //<{property.Name}>i__Field
                                    var backingName = $"<{property.Name}>";
                                    var backingField = fields.FirstOrDefault(x => x.Name.StartsWith(backingName));
                                    if (backingField != null)
                                        backingMember = MemberDetailRuntime<object, object>.New(Type, property.PropertyType, backingField, null, locker);

                                    items.Add(MemberDetailRuntime<object, object>.New(Type, property.PropertyType, property, backingMember, locker));
                                }
                                foreach (var field in fields.Where(x => !items.Any(y => y.BackingFieldDetailBoxed?.MemberInfo == x)))
                                {
                                    items.Add(MemberDetailRuntime<object, object>.New(Type, field.FieldType, field, null, locker));
                                }
                            }

                            memberDetails = items.ToArray();
                        }
                    }
                }
                return memberDetails;
            }
        }

        private MethodDetail[]? methodDetailsBoxed = null;
        public override IReadOnlyList<MethodDetail> MethodDetailsBoxed
        {
            get
            {
                if (methodDetailsBoxed == null)
                {
                    lock (locker)
                    {
                        if (methodDetailsBoxed == null)
                        {
                            var items = new List<MethodDetail>();
                            if (!Type.IsGenericTypeDefinition)
                            {
                                var methods = Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                                foreach (var method in methods)
                                    items.Add(MethodDetailRuntime<object>.New(Type, method, locker));
                                if (Type.IsInterface)
                                {
                                    foreach (var i in Interfaces)
                                    {
                                        var iMethods = i.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                                        foreach (var method in iMethods)
                                        {
                                            var methodDetail = MethodDetailRuntime<object>.New(Type, method, locker);
                                            if (!items.Any(x => SignatureCompare(x, methodDetail)))
                                                items.Add(methodDetail);
                                        }
                                    }
                                }
                            }
                            methodDetailsBoxed = items.ToArray();
                        }
                    }
                }
                return methodDetailsBoxed;
            }
        }

        private ConstructorDetail[]? constructorDetailsBoxed = null;
        public override IReadOnlyList<ConstructorDetail> ConstructorDetailsBoxed
        {
            get
            {
                if (constructorDetailsBoxed == null)
                {
                    lock (locker)
                    {
                        if (constructorDetailsBoxed == null)
                        {
                            if (!Type.IsGenericTypeDefinition)
                            {
                                var constructors = Type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                var items = new ConstructorDetail[constructors.Length];
                                for (var i = 0; i < items.Length; i++)
                                    items[i] = ConstructorDetailRuntime<object>.New(Type, constructors[i], locker);
                                constructorDetailsBoxed = items;
                            }
                            else
                            {
                                constructorDetailsBoxed = Array.Empty<ConstructorDetail>();
                            }
                        }
                    }
                }
                return constructorDetailsBoxed;
            }
        }

        private Attribute[]? attributes = null;
        public override IReadOnlyList<Attribute> Attributes
        {
            get
            {
                if (attributes == null)
                {
                    lock (locker)
                    {
                        attributes ??= Type.GetCustomAttributes().ToArray();
                    }
                }
                return attributes;
            }
        }

        private TypeDetail[]? innerTypesDetails = null;
        public override IReadOnlyList<TypeDetail> InnerTypeDetails
        {
            get
            {
                if (innerTypesDetails == null)
                {
                    lock (locker)
                    {
                        if (innerTypesDetails == null)
                        {
                            var innerTypesRef = InnerTypes;
                            if (innerTypesRef != null)
                            {
                                var items = new TypeDetail[innerTypesRef.Count];
                                for (var i = 0; i < innerTypesRef.Count; i++)
                                {
                                    items[i] = TypeAnalyzer.GetTypeDetail(innerTypesRef[i]);
                                }
                                innerTypesDetails = items;
                            }
                            else
                            {
                                innerTypesDetails = Array.Empty<TypeDetail>();
                            }
                        }
                    }
                }
                return innerTypesDetails;
            }
        }

        private bool innerTypeDetailLoaded = false;
        private TypeDetail? innerTypesDetail = null;
        public override TypeDetail InnerTypeDetail
        {
            get
            {
                if (!innerTypeDetailLoaded)
                {
                    lock (locker)
                    {
                        if (!innerTypeDetailLoaded)
                        {
                            if (InnerTypes.Count == 1)
                            {
                                innerTypesDetail = InnerType.GetTypeDetail();
                            }
                        }
                        innerTypeDetailLoaded = true;
                    }
                }
                return innerTypesDetail ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name} does not have a {nameof(InnerTypeDetail)}");
            }
        }

        private Type? iEnumerableGenericInnerType = null;
        public override Type IEnumerableGenericInnerType
        {
            get
            {
                if (!HasIEnumerable)
                    throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name.GetType()} is not an IEnumerableGeneric");
                if (iEnumerableGenericInnerType == null)
                    LoadIEnumerableGeneric();
                return iEnumerableGenericInnerType ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name.GetType()} is not an IEnumerableGeneric"); ;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadIEnumerableGeneric()
        {
            lock (locker)
            {
                if (iEnumerableGenericInnerType == null)
                {
                    if (Type.Name == enumberableGenericTypeName)
                    {
                        iEnumerableGenericInnerType = Type.GetGenericArguments()[0];
                    }
                    else
                    {
                        var enumerableGeneric = Interfaces.Where(x => x.Name == enumberableGenericTypeName).ToArray();
                        if (enumerableGeneric.Length == 1)
                        {
                            iEnumerableGenericInnerType = enumerableGeneric[0].GetGenericArguments()[0];
                        }
                    }
                }
            }
        }

        private TypeDetail? iEnumerableGenericInnerTypeDetail = null;
        public override TypeDetail IEnumerableGenericInnerTypeDetail
        {
            get
            {
                if (IEnumerableGenericInnerType == null)
                    throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name.GetType()} is not an IEnumerableGeneric");
                if (iEnumerableGenericInnerTypeDetail == null)
                {
                    lock (locker)
                    {
                        iEnumerableGenericInnerTypeDetail ??= TypeAnalyzer.GetTypeDetail(IEnumerableGenericInnerType);
                    }
                }
                return iEnumerableGenericInnerTypeDetail;
            }
        }

        private bool dictionartyInnerTypeLoaded = false;
        private Type? dictionaryInnerType = null;
        public override Type DictionaryInnerType
        {
            get
            {
                if (!dictionartyInnerTypeLoaded)
                {
                    lock (locker)
                    {
                        if (!dictionartyInnerTypeLoaded)
                        {
                            if (HasIDictionaryGeneric || HasIReadOnlyDictionaryGeneric)
                            {
                                dictionaryInnerType = TypeAnalyzer.GetGenericType(keyValuePairType, (Type[])InnerTypes);
                            }
                            else if (HasIDictionary)
                            {
                                dictionaryInnerType = dictionaryEntryType;
                            }
                            dictionartyInnerTypeLoaded = true;
                        }
                    }
                }
                return dictionaryInnerType ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name} does not have a {nameof(DictionaryInnerType)}");
            }
        }

        private bool dictionaryInnerTypeDetailLoaded = false;
        private TypeDetail? dictionaryInnerTypesDetail = null;
        public override TypeDetail DictionaryInnerTypeDetail
        {
            get
            {
                if (!dictionaryInnerTypeDetailLoaded)
                {
                    lock (locker)
                    {
                        if (!dictionaryInnerTypeDetailLoaded)
                        {
                            dictionaryInnerTypesDetail = DictionaryInnerType.GetTypeDetail();
                        }
                        innerTypeDetailLoaded = true;
                    }
                }
                return dictionaryInnerTypesDetail ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name} does not have a {nameof(DictionaryInnerTypeDetail)}");
            }
        }

        Func<object, object?>? taskResultGetter = null;
        public override Func<object, object?> TaskResultGetter
        {
            get
            {
                if (!this.IsTask || !this.Type.IsGenericType)
                    throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name} does not have a {nameof(TaskResultGetter)}");

                LoadTaskResultGetter();
                return taskResultGetter ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name} does not have a {nameof(TaskResultGetter)}");
            }
        }
        public override bool HasTaskResultGetter
        {
            get
            {
                if (!this.IsTask || !this.Type.IsGenericType)
                    return false;

                if (taskResultGetter == null)
                    LoadTaskResultGetter();
                return taskResultGetter != null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadTaskResultGetter()
        {
            lock (locker)
            {
                taskResultGetter ??= GetMember("Result").GetterBoxed;
            }
        }

        private bool creatorBoxedLoaded = false;
        private Func<object>? creatorBoxed = null;
        public override Func<object> CreatorBoxed
        {
            get
            {
                if (!creatorBoxedLoaded)
                    LoadCreatorBoxed();
                return creatorBoxed ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name} does not have a {nameof(CreatorBoxed)}");
            }
        }
        public override bool HasCreatorBoxed
        {
            get
            {
                if (!creatorBoxedLoaded)
                    LoadCreatorBoxed();
                return creatorBoxed != null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadCreatorBoxed()
        {
            lock (locker)
            {
                if (!creatorBoxedLoaded)
                {
                    if (!Type.IsAbstract && !Type.IsGenericTypeDefinition)
                    {
                        var emptyConstructor = this.ConstructorDetailsBoxed.FirstOrDefault(x => x.Parameters.Count == 0);
                        if (emptyConstructor != null && emptyConstructor.HasCreatorBoxed)
                        {
                            creatorBoxed = emptyConstructor.CreatorBoxed;
                        }
                        else if (Type.IsValueType && Type.Name != "Void")
                        {
                            var constantExpression = Expression.Convert(Expression.Default(Type), typeof(object));
                            var lambda = Expression.Lambda<Func<object>>(constantExpression).Compile();
                            creatorBoxed = lambda;
                        }
                        else if (Type == typeof(string))
                        {
                            creatorBoxed = () => { return String.Empty; };
                        }
                    }
                    creatorBoxedLoaded = true;
                }
            }
        }

        private MethodDetail<T>[]? methodDetails = null;
        public override IReadOnlyList<MethodDetail<T>> MethodDetails
        {
            get
            {
                if (methodDetails == null)
                {
                    lock (locker)
                    {
                        if (methodDetails == null)
                        {
                            var items = new List<MethodDetail<T>>();
                            if (!Type.IsGenericTypeDefinition)
                            {
                                var methods = Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                                foreach (var method in methods)
                                    items.Add(new MethodDetailRuntime<T>(method, locker));
                                if (Type.IsInterface)
                                {
                                    foreach (var i in Interfaces)
                                    {
                                        var iMethods = i.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                                        foreach (var method in iMethods)
                                        {
                                            var methodDetail = new MethodDetailRuntime<T>(method, locker);
                                            if (!items.Any(x => SignatureCompare(x, methodDetail)))
                                                items.Add(methodDetail);
                                        }
                                    }
                                }
                            }
                            methodDetails = items.ToArray();
                        }
                    }
                }
                return methodDetails;
            }
        }

        private ConstructorDetail<T>[]? constructorDetails = null;
        public override IReadOnlyList<ConstructorDetail<T>> ConstructorDetails
        {
            get
            {
                if (constructorDetails == null)
                {
                    lock (locker)
                    {
                        if (constructorDetails == null)
                        {
                            if (!Type.IsGenericTypeDefinition)
                            {
                                var constructors = Type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                var items = new ConstructorDetailRuntime<T>[constructors.Length];
                                for (var i = 0; i < items.Length; i++)
                                    items[i] = new ConstructorDetailRuntime<T>(constructors[i], locker);
                                constructorDetails = items;
                            }
                            else
                            {
                                constructorDetails = Array.Empty<ConstructorDetail<T>>();
                            }
                        }
                    }
                }
                return constructorDetails;
            }
        }

        private bool creatorLoaded = false;
        private Func<T>? creator = null;
        public override Func<T> Creator
        {
            get
            {
                if (!creatorLoaded)
                    LoadCreator();
                return creator ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name} does not have a {nameof(Creator)}");
            }
        }
        public override bool HasCreator
        {
            get
            {
                if (!creatorLoaded)
                    LoadCreator();
                return creator != null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadCreator()
        {
            lock (locker)
            {
                if (!creatorLoaded)
                {
                    if (!Type.IsAbstract && !Type.IsGenericTypeDefinition)
                    {
                        var emptyConstructor = this.ConstructorDetails.FirstOrDefault(x => x.Parameters.Count == 0);
                        if (emptyConstructor != null && emptyConstructor.HasCreator)
                        {
                            creator = emptyConstructor.Creator;
                        }
                        else if (Type.IsValueType && Type.Name != "Void")
                        {
                            creator = () => { return default!; };
                        }
                        else if (Type.Name == "String")
                        {
                            creator = () => { return (T)(object)String.Empty; };
                        }
                    }
                    creatorLoaded = true;
                }
            }
        }

        internal TypeDetailRuntime(Type type) : base(type)
        {
            this.IsNullable = type.Name == nullaleTypeName;
        }

        private static readonly Type typeDetailT = typeof(TypeDetailRuntime<>);
        internal static TypeDetail New(Type type)
        {
            if (!type.ContainsGenericParameters)
            {
                var typeDetailGeneric = typeDetailT.MakeGenericType(type);
                var obj = typeDetailGeneric.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0].Invoke(new object[] { type });
                return (TypeDetail)obj;
            }
            else
            {
                return new TypeDetailRuntime(type);
            }
        }
    }
}
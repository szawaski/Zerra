// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zerra.Collections;

namespace Zerra.Reflection
{
    public class TypeDetail
    {
        protected readonly object locker = new();

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

        public Type Type { get; }
        public bool IsNullable { get; }
        public CoreType? CoreType { get; }
        public SpecialType? SpecialType { get; }

        private Type[]? innerTypes = null;
        public IReadOnlyList<Type> InnerTypes
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
        public Type InnerType
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
        public bool IsTask
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
        public CoreEnumType? EnumUnderlyingType
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
        public IReadOnlyList<Type> BaseTypes
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
        public IReadOnlyList<Type> Interfaces
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
        public bool HasIEnumerable
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIEnumerable;
            }
        }
        public bool HasIEnumerableGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIEnumerableGeneric;
            }
        }
        public bool HasICollection
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasICollection;
            }
        }
        public bool HasICollectionGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasICollectionGeneric;
            }
        }
        public bool HasIReadOnlyCollectionGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIReadOnlyCollectionGeneric;
            }
        }
        public bool HasIList
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIList;
            }
        }
        public bool HasIListGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIListGeneric;
            }
        }
        public bool HasIReadOnlyListGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIReadOnlyListGeneric;
            }
        }
        public bool HasISetGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasISetGeneric;
            }
        }
        public bool HasIReadOnlySetGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIReadOnlySetGeneric;
            }
        }
        public bool HasIDictionary
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIDictionary;
            }
        }
        public bool HasIDictionaryGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return hasIDictionaryGeneric;
            }
        }
        public bool HasIReadOnlyDictionaryGeneric
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
        public bool IsIEnumerable
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIEnumerable;
            }
        }
        public bool IsIEnumerableGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIEnumerableGeneric;
            }
        }
        public bool IsICollection
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isICollection;
            }
        }
        public bool IsICollectionGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isICollectionGeneric;
            }
        }
        public bool IsIReadOnlyCollectionGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIReadOnlyCollectionGeneric;
            }
        }
        public bool IsIList
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIList;
            }
        }
        public bool IsIListGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIListGeneric;
            }
        }
        public bool IsIReadOnlyListGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIReadOnlyListGeneric;
            }
        }
        public bool IsISetGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isISetGeneric;
            }
        }
        public bool IsIReadOnlySetGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIReadOnlySetGeneric;
            }
        }
        public bool IsIDictionary
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIDictionary;
            }
        }
        public bool IsIDictionaryGeneric
        {
            get
            {
                if (!hasIsInterfaceLoaded)
                    LoadHasIsInterface();
                return isIDictionaryGeneric;
            }
        }
        public bool IsIReadOnlyDictionaryGeneric
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
#if NETSTANDARD2_0
                        var interfaces = Interfaces.Select(x => x.Name).ToArray();
#else
                        var interfaces = Interfaces.Select(x => x.Name).ToHashSet();
#endif

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
#else
                        hasIReadOnlySetGeneric = false;
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
#else
                        isIReadOnlySetGeneric = false;
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
        public IReadOnlyList<MemberDetail> MemberDetails
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
                                        backingMember = MemberDetail.New(Type, property.PropertyType, backingField, null, locker);

                                    items.Add(MemberDetail.New(Type, property.PropertyType, property, backingMember, locker));
                                }
                                foreach (var field in fields.Where(x => !items.Any(y => y.BackingFieldDetail?.MemberInfo == x)))
                                {
                                    items.Add(MemberDetail.New(Type, field.FieldType, field, null, locker));
                                }
                            }

                            memberDetails = items.ToArray();
                        }
                    }
                }
                return memberDetails;
            }
        }

        private MethodDetail[]? methodDetails = null;
        public IReadOnlyList<MethodDetail> MethodDetails
        {
            get
            {
                if (methodDetails == null)
                {
                    lock (locker)
                    {
                        if (methodDetails == null)
                        {
                            var items = new List<MethodDetail>();
                            if (!Type.IsGenericTypeDefinition)
                            {
                                var methods = Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                                foreach (var method in methods)
                                    items.Add(MethodDetail.New(Type, method, locker));
                                if (Type.IsInterface)
                                {
                                    foreach (var i in Interfaces)
                                    {
                                        var iMethods = i.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                                        foreach (var method in iMethods)
                                        {
                                            var methodDetail = MethodDetail.New(Type, method, locker);
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

        private ConstructorDetail[]? constructorDetails = null;
        public IReadOnlyList<ConstructorDetail> ConstructorDetails
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
                                var items = new ConstructorDetail[constructors.Length];
                                for (var i = 0; i < items.Length; i++)
                                    items[i] = ConstructorDetail.New(Type, constructors[i], locker);
                                constructorDetails = items;
                            }
                            else
                            {
                                constructorDetails = Array.Empty<ConstructorDetail>();
                            }
                        }
                    }
                }
                return constructorDetails;
            }
        }

        private Attribute[]? attributes = null;
        public IReadOnlyList<Attribute> Attributes
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

        private Dictionary<string, MemberDetail>? membersByName = null;
        public MemberDetail GetMember(string name)
        {
            if (membersByName == null)
            {
                lock (locker)
                {
                    membersByName ??= this.MemberDetails.ToDictionary(x => x.Name);
                }
            }
            if (!this.membersByName.TryGetValue(name, out var member))
                throw new Exception($"{Type.Name} does not contain member {name}");
            return member;
        }
        public bool TryGetMember(string name,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out MemberDetail member)
        {
            if (membersByName == null)
            {
                lock (locker)
                {
                    membersByName ??= this.MemberDetails.ToDictionary(x => x.Name);
                }
            }
            return this.membersByName.TryGetValue(name, out member);
        }

        private readonly ConcurrentFactoryDictionary<TypeKey, MethodDetail?> methodLookups = new();
        private MethodDetail? GetMethodInternal(string name, Type[]? parameterTypes = null)
        {
            var key = new TypeKey(name, parameterTypes);
            MethodDetail? found = null;
            var method = methodLookups.GetOrAdd(key, (_) =>
            {
                foreach (var methodDetail in MethodDetails)
                {
                    if (SignatureCompare(name, parameterTypes, methodDetail))
                    {
                        if (found != null)
                            throw new InvalidOperationException($"More than one method found for {name}");
                        found = methodDetail;
                    }
                }
                return found;
            });
            return method;
        }
        public MethodDetail GetMethod(string name, Type[]? parameterTypes = null)
        {
            var method = GetMethodInternal(name, parameterTypes);
            if (method == null)
                throw new MissingMethodException($"{Type.Name}.{name} method not found for the given parameters {(parameterTypes == null || parameterTypes.Length == 0 ? "(none)" : String.Join(",", parameterTypes.Select(x => x.GetNiceName())))}");
            return method;
        }
        public bool TryGetMethod(string name,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out MethodDetail method)
        {
            method = GetMethodInternal(name, null);
            return method != null;
        }
        public bool TryGetMethod(string name, Type[] parameterTypes,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out MethodDetail method)
        {
            method = GetMethodInternal(name, parameterTypes);
            return method != null;
        }

        private readonly ConcurrentFactoryDictionary<TypeKey, ConstructorDetail?> constructorLookups = new();
        private ConstructorDetail? GetConstructorInternal(Type[]? parameterTypes)
        {
            var key = new TypeKey(parameterTypes);
            ConstructorDetail? found = null;
            var constructor = constructorLookups.GetOrAdd(key, (_) =>
            {
                foreach (var constructorDetail in ConstructorDetails)
                {
                    if (SignatureCompare(parameterTypes, constructorDetail))
                    {
                        if (found != null)
                            throw new InvalidOperationException($"More than one constructor found");
                        found = constructorDetail;
                    }
                }
                return found;
            });
            return constructor;
        }
        public ConstructorDetail GetConstructor(params Type[] parameterTypes)
        {
            var constructor = GetConstructorInternal(parameterTypes);
            if (constructor == null)
                throw new MissingMethodException($"{Type.Name} constructor not found for the given parameters {String.Join(",", parameterTypes.Select(x => x.GetNiceName()))}");
            return constructor;
        }
        public bool TryGetConstructor(
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)] 
#endif
        out ConstructorDetail constructor)
        {
            constructor = GetConstructorInternal(null);
            return constructor != null;
        }
        public bool TryGetConstructor(Type[] parameterTypes,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out ConstructorDetail constructor)
        {
            constructor = GetConstructorInternal(parameterTypes);
            return constructor != null;
        }

        private IReadOnlyList<MemberDetail>? serializableMemberDetails = null;
        public IReadOnlyList<MemberDetail> SerializableMemberDetails
        {
            get
            {
                if (serializableMemberDetails == null)
                {
                    lock (locker)
                    {
                        serializableMemberDetails ??= MemberDetails.Where(x => x.IsBacked && IsSerializableType(x.TypeDetail)).ToArray();
                    }
                }
                return serializableMemberDetails;
            }
        }

        private Dictionary<string, MemberDetail>? serializableMembersByNameLower = null;
        public MemberDetail GetSerializableMemberCaseInsensitive(string name)
        {
            if (serializableMembersByNameLower == null)
            {
                lock (locker)
                {
                    serializableMembersByNameLower ??= this.SerializableMemberDetails.GroupBy(x => x.Name.ToLower()).Where(x => x.Count() == 1).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
                }
            }
            if (!this.serializableMembersByNameLower.TryGetValue(name, out var member))
                throw new Exception($"{Type.Name} does not contain member {name}");
            return member;
        }
        public bool TryGetSerializableMemberCaseInsensitive(string name,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
         out MemberDetail member)
        {
            if (serializableMembersByNameLower == null)
            {
                lock (locker)
                {
                    serializableMembersByNameLower ??= this.SerializableMemberDetails.GroupBy(x => x.Name.ToLower()).Where(x => x.Count() == 1).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
                }
            }
            return this.serializableMembersByNameLower.TryGetValue(name, out member);
        }

        private Dictionary<string, MemberDetail>? membersFieldBackedByName = null;
        public MemberDetail GetMemberFieldBacked(string name)
        {
            if (membersFieldBackedByName == null)
            {
                lock (locker)
                {
                    membersFieldBackedByName ??= MemberDetails.ToDictionary(x => x.Name);
                }
            }
            if (!this.membersFieldBackedByName.TryGetValue(name, out var member))
                throw new Exception($"{Type.Name} does not contain member {name}");
            return member;
        }
        public bool TryGetGetMemberFieldBacked(string name,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out MemberDetail member)
        {
            if (membersFieldBackedByName == null)
            {
                lock (locker)
                {
                    membersFieldBackedByName ??= MemberDetails.ToDictionary(x => x.Name);
                }
            }
            return this.membersFieldBackedByName.TryGetValue(name, out member);
        }

        private TypeDetail[]? innerTypesDetails = null;
        public IReadOnlyList<TypeDetail> InnerTypeDetails
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
        public TypeDetail InnerTypeDetail
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
        public Type IEnumerableGenericInnerType
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
        public TypeDetail IEnumerableGenericInnerTypeDetail
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
        public Type DictionaryInnerType
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
        public TypeDetail DictionaryInnerTypeDetail
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
        public Func<object, object?> TaskResultGetter
        {
            get
            {
                if (!this.IsTask || !this.Type.IsGenericType)
                    throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name} does not have a {nameof(TaskResultGetter)}");

                LoadTaskResultGetter();
                return taskResultGetter ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name} does not have a {nameof(TaskResultGetter)}");
            }
        }
        public bool HasTaskResultGetter
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
        public Func<object> CreatorBoxed
        {
            get
            {
                if (!creatorBoxedLoaded)
                    LoadCreatorBoxed();
                return creatorBoxed ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name} does not have a {nameof(CreatorBoxed)}");
            }
        }
        public bool HasCreatorBoxed
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
                        var emptyConstructor = this.ConstructorDetails.FirstOrDefault(x => x.ParametersInfo.Count == 0);
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

        public virtual Delegate? CreatorTyped => throw new NotSupportedException();

        public override string ToString()
        {
            return Type.GetNiceFullName();
        }

        protected TypeDetail(Type type)
        {
            this.Type = type;

            this.IsNullable = type.Name == nullaleTypeName;

            if (TypeLookup.CoreTypeLookup(type, out var coreTypeLookup))
                this.CoreType = coreTypeLookup;

            if (TypeLookup.SpecialTypeLookup(Type, out var specialTypeLookup))
                this.SpecialType = specialTypeLookup;
        }

        private static readonly Type typeDetailT = typeof(TypeDetail<>);
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
                return new TypeDetail(type);
            }
        }

        protected static bool SignatureCompare(string name1, Type[]? parameters1, string name2, Type[]? parameters2)
        {
            if (name1 != name2)
                return false;

            if (parameters1 != null && parameters2 != null)
            {
                if (parameters1.Length != parameters2.Length)
                    return false;
                for (var i = 0; i < parameters1.Length; i++)
                {
                    if (parameters1[i] != parameters2[i])
                        return false;
                }
            }

            return true;
        }
        protected static bool SignatureCompare(string name1, Type[]? parameters1, MethodDetail methodDetail2)
        {
            if (name1 != methodDetail2.Name)
                return false;

            if (parameters1 != null)
            {
                if (parameters1.Length != methodDetail2.ParametersInfo.Count)
                    return false;
                for (var i = 0; i < parameters1.Length; i++)
                {
                    if (parameters1[i] != methodDetail2.ParametersInfo[i].ParameterType)
                        return false;
                }
            }

            return true;
        }
        protected static bool SignatureCompare(MethodDetail methodDetail1, MethodDetail methodDetail2)
        {
            if (methodDetail1.Name != methodDetail2.Name)
                return false;
            if (methodDetail1.ParametersInfo.Count == 0 && methodDetail2.ParametersInfo.Count == 0)
                return true;
            if (methodDetail1.ParametersInfo.Count == 0 || methodDetail2.ParametersInfo.Count == 0)
                return false;
            if (methodDetail1.ParametersInfo.Count != methodDetail2.ParametersInfo.Count)
                return false;
            for (var i = 0; i < methodDetail1.ParametersInfo.Count; i++)
            {
                if (methodDetail1.ParametersInfo[i].ParameterType != methodDetail2.ParametersInfo[i].ParameterType)
                    return false;
            }
            return true;
        }
        protected static bool SignatureCompare(Type[]? parameters1, ConstructorDetail constructorDetail2)
        {
            if (parameters1 != null)
            {
                if (parameters1.Length != constructorDetail2.ParametersInfo.Count)
                    return false;
                for (var i = 0; i < parameters1.Length; i++)
                {
                    if (parameters1[i] != constructorDetail2.ParametersInfo[i].ParameterType)
                        return false;
                }
            }

            return true;
        }

        protected static bool IsSerializableType(TypeDetail typeDetail)
        {
            if (typeDetail.CoreType.HasValue)
                return true;
            if (typeDetail.Type.IsEnum)
                return true;
            if (typeDetail.Type.IsArray)
                return true;
            if (typeDetail.IsNullable)
                return IsSerializableType(typeDetail.InnerTypeDetail);
            if (typeDetail.Type.IsClass)
                return true;
            if (typeDetail.Type.IsInterface)
                return true;
            return false;
        }
    }
}
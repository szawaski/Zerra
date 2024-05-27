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
        private static readonly string listTypeName = nameof(IList);
        private static readonly string setGenericTypeName = typeof(ISet<>).Name;
        private static readonly string listGenericTypeName = typeof(IList<>).Name;

        //private static readonly string readOnlyCollectionGenericTypeName = typeof(IReadOnlyCollection<>).Name;
        //private static readonly string readOnlyListGenericTypeName = typeof(IReadOnlyList<>).Name;
        //private static readonly string readOnlySetGenericTypeName = typeof(IReadOnlySet<>).Name;

        private static readonly Type keyValuePairType = typeof(KeyValuePair<,>);

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
        private CoreType? enumUnderlyingType;
        public CoreType? EnumUnderlyingType
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
                                if (!TypeLookup.CoreTypeLookup(enumEnderlyingType, out var enumCoreTypeLookup))
                                    throw new NotImplementedException("Should not happen");
                                enumUnderlyingType = enumCoreTypeLookup;
                            }
                            else if (this.IsNullable && this.InnerTypes[0].IsEnum)
                            {
                                var enumEnderlyingType = Enum.GetUnderlyingType(this.InnerTypes[0]);
                                if (!TypeLookup.CoreTypeLookup(enumEnderlyingType, out var enumCoreTypeLookup))
                                    throw new NotImplementedException("Should not happen");
                                enumCoreTypeLookup = enumCoreTypeLookup switch
                                {
                                    Reflection.CoreType.Byte => Reflection.CoreType.ByteNullable,
                                    Reflection.CoreType.SByte => Reflection.CoreType.SByteNullable,
                                    Reflection.CoreType.Int16 => Reflection.CoreType.Int16Nullable,
                                    Reflection.CoreType.UInt16 => Reflection.CoreType.UInt16Nullable,
                                    Reflection.CoreType.Int32 => Reflection.CoreType.Int32Nullable,
                                    Reflection.CoreType.UInt32 => Reflection.CoreType.UInt32Nullable,
                                    Reflection.CoreType.Int64 => Reflection.CoreType.Int64Nullable,
                                    Reflection.CoreType.UInt64 => Reflection.CoreType.UInt64Nullable,
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

        private bool? isGraphLocalProperty;
        public bool IsGraphLocalProperty
        {
            get
            {
                if (!isGraphLocalProperty.HasValue)
                {
                    lock (locker)
                    {
                        if (!isGraphLocalProperty.HasValue)
                        {
                            isGraphLocalProperty = this.CoreType.HasValue || this.EnumUnderlyingType.HasValue || this.SpecialType.HasValue || this.Type.IsArray || (this.IsNullable && this.InnerTypes[0].IsArray);
                        }
                    }
                }
                return isGraphLocalProperty.Value;
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

        private bool isIEnumerable;
        private bool isICollection;
        private bool isICollectionGeneric;
        private bool isIList;
        private bool isISet;
        public bool IsIEnumerable
        {
            get
            {
                if (!isInterfaceLoaded)
                    LoadIsInterface();
                return isIEnumerable;
            }
        }
        public bool IsICollection
        {
            get
            {
                if (!isInterfaceLoaded)
                    LoadIsInterface();
                return isICollection;
            }
        }
        public bool IsICollectionGeneric
        {
            get
            {
                if (!isInterfaceLoaded)
                    LoadIsInterface();
                return isICollectionGeneric;
            }
        }
        public bool IsIList
        {
            get
            {
                if (!isInterfaceLoaded)
                    LoadIsInterface();
                return isIList;
            }
        }
        public bool IsISet
        {
            get
            {
                if (!isInterfaceLoaded)
                    LoadIsInterface();
                return isISet;
            }
        }
        private bool isInterfaceLoaded = false;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadIsInterface()
        {
            lock (locker)
            {
                if (!isInterfaceLoaded)
                {
                    var isCoreType = TypeLookup.CoreTypes.Contains(Type);
                    isIEnumerable = !isCoreType && (Type.IsArray || Type.Name == enumberableTypeName || Interfaces.Select(x => x.Name).Contains(enumberableTypeName));
                    isICollection = !isCoreType && (Type.Name == collectionTypeName || Interfaces.Select(x => x.Name).Contains(collectionTypeName));
                    isICollectionGeneric = !isCoreType && (Type.Name == collectionGenericTypeName || Interfaces.Select(x => x.Name).Contains(collectionGenericTypeName));
                    isIList = !isCoreType && (Type.Name == listTypeName || Type.Name == listGenericTypeName || Interfaces.Select(x => x.Name).Contains(listTypeName) || Interfaces.Select(x => x.Name).Contains(listGenericTypeName));
                    isISet = !isCoreType && (Type.Name == setGenericTypeName || Interfaces.Select(x => x.Name).Contains(setGenericTypeName));
                    isInterfaceLoaded = true;
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
                                        var existingPropertyNames = properties.Select(y => y.Name).ToArray();
                                        properties = properties.Concat(iProperties.Where(x => !existingPropertyNames.Contains(x.Name)));
                                        var existingFieldNames = fields.Select(y => y.Name).ToArray();
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
                                            items.Add(MethodDetail.New(Type, method, locker));
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
            var method = methodLookups.GetOrAdd(key, (_) =>
            {
                foreach (var methodDetail in MethodDetails)
                {
                    if (methodDetail.Name == name && (parameterTypes == null || methodDetail.ParametersInfo.Count == parameterTypes.Length))
                    {
                        var match = true;
                        if (parameterTypes != null)
                        {
                            for (var i = 0; i < parameterTypes.Length; i++)
                            {
                                if (parameterTypes[i].Name != methodDetail.ParametersInfo[i].ParameterType.Name || parameterTypes[i].Namespace != methodDetail.ParametersInfo[i].ParameterType.Namespace)
                                {
                                    match = false;
                                    break;
                                }
                            }
                        }
                        if (match)
                            return methodDetail;
                    }
                }
                return null;
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
            var constructor = constructorLookups.GetOrAdd(key, (_) =>
            {
                foreach (var constructorDetail in ConstructorDetails)
                {
                    if (parameterTypes == null || constructorDetail.ParametersInfo.Count == parameterTypes.Length)
                    {
                        var match = true;
                        if (parameterTypes != null)
                        {
                            for (var i = 0; i < parameterTypes.Length; i++)
                            {
                                if (parameterTypes[i].Name != constructorDetail.ParametersInfo[i].ParameterType.Name || parameterTypes[i].Namespace != constructorDetail.ParametersInfo[i].ParameterType.Namespace)
                                {
                                    match = false;
                                    break;
                                }
                            }
                        }
                        if (match)
                            return constructorDetail;
                    }
                }
                return null;
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
                    serializableMembersByNameLower ??= this.SerializableMemberDetails.GroupBy(x => x.Name.ToLower()).Where(x => x.Count() == 1).ToDictionary(x => x.Key, x => x.First(), new StringOrdinalIgnoreCaseComparer());
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
                    serializableMembersByNameLower ??= this.SerializableMemberDetails.GroupBy(x => x.Name.ToLower()).Where(x => x.Count() == 1).ToDictionary(x => x.Key, x => x.First(), new StringOrdinalIgnoreCaseComparer());
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

        private bool isIEnumerableGeneric;
        private Type? iEnumerableGenericInnerType = null;
        public bool IsIEnumerableGeneric
        {
            get
            {
                if (!IsIEnumerable)
                    return false;
                if (iEnumerableGenericInnerType == null)
                    LoadIEnumerableGeneric();
                return isIEnumerableGeneric;
            }
        }
        public Type IEnumerableGenericInnerType
        {
            get
            {
                if (!IsIEnumerable)
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
                        isIEnumerableGeneric = true;
                        iEnumerableGenericInnerType = Type.GetGenericArguments()[0];
                    }
                    else
                    {
                        var enumerableGeneric = Interfaces.Where(x => x.Name == enumberableGenericTypeName).ToArray();
                        if (enumerableGeneric.Length == 1)
                        {
                            isIEnumerableGeneric = true;
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
                            if (TypeLookup.SpecialTypeLookup(Type, out var specialTypeLookup))
                            {
                                if (specialTypeLookup == Reflection.SpecialType.Dictionary)
                                {
                                    dictionaryInnerType = TypeAnalyzer.GetGenericType(keyValuePairType, (Type[])InnerTypes);
                                }
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
                            if (TypeLookup.SpecialTypeLookup(Type, out _))
                            {
                                dictionaryInnerTypesDetail = DictionaryInnerType.GetTypeDetail();
                            }
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
                        if (emptyConstructor != null && emptyConstructor.CreatorBoxed != null)
                        {
                            creatorBoxed = () => { return emptyConstructor.CreatorBoxed(null); };
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

        protected static bool IsSerializableType(TypeDetail typeDetail)
        {
            if (typeDetail.CoreType.HasValue)
                return true;
            if (typeDetail.SpecialType.HasValue)
                return true;
            if (typeDetail.Type.IsEnum)
                return true;
            if (typeDetail.IsNullable)
                return IsSerializableType(typeDetail.InnerTypeDetails[0]);
            if (typeDetail.Type.IsArray && typeDetail.InnerTypeDetails.Count == 1)
                return IsSerializableType(typeDetail.InnerTypeDetails[0]);
            if (typeDetail.IsIEnumerable && typeDetail.InnerTypeDetails.Count == 1)
                return IsSerializableType(typeDetail.InnerTypeDetails[0]);
            if (typeDetail.Type.IsClass)
                return true;
            if (typeDetail.Type.IsInterface)
                return true;
            return false;
        }
    }
}
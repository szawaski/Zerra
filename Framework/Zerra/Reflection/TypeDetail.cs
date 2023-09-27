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
using Zerra.Collections;

namespace Zerra.Reflection
{
    public sealed class TypeDetail
    {
        private readonly object locker = new();

        private static readonly string nullaleTypeName = typeof(Nullable<>).Name;
        private static readonly string enumberableTypeName = nameof(IEnumerable);
        private static readonly string enumberableGenericTypeName = typeof(IEnumerable<>).Name;
        private static readonly string collectionTypeName = nameof(ICollection);
        private static readonly string collectionGenericTypeName = typeof(ICollection<>).Name;
        private static readonly string listTypeName = nameof(IList);
        private static readonly string setGenericTypeName = typeof(ISet<>).Name;
        private static readonly string listGenericTypeName = typeof(IList<>).Name;
        private static readonly Type keyValuePairType = typeof(KeyValuePair<,>);

        public Type Type { get; private set; }
        public bool IsNullable { get; private set; }
        public CoreType? CoreType { get; private set; }
        public SpecialType? SpecialType { get; private set; }

        private Type[] innerTypes;
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
                                innerTypes = Type.GetGenericArguments();
                            else if (Type.IsArray)
                                innerTypes = new Type[] { Type.GetElementType() };
                            else
                                innerTypes = Type.EmptyTypes;

                            if (TypeLookup.SpecialTypeLookup(Type, out var specialTypeLookup))
                            {
                                if (specialTypeLookup == Reflection.SpecialType.Dictionary)
                                {
                                    var innerType = TypeAnalyzer.GetGenericType(keyValuePairType, (Type[])this.InnerTypes);
                                    innerTypes = new Type[] { innerType };
                                }
                            }
                        }
                    }
                }
                return innerTypes;
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
                            enumUnderlyingTypeLoaded = true;
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

        private Type[] baseTypes = null;
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

        private Type[] interfaces = null;
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
                LoadIsInterface();
                return isIEnumerable;
            }
        }
        public bool IsICollection
        {
            get
            {
                LoadIsInterface();
                return isICollection;
            }
        }
        public bool IsICollectionGeneric
        {
            get
            {
                LoadIsInterface();
                return isICollectionGeneric;
            }
        }
        public bool IsIList
        {
            get
            {
                LoadIsInterface();
                return isIList;
            }
        }
        public bool IsISet
        {
            get
            {
                LoadIsInterface();
                return isISet;
            }
        }
        private bool isInterfaceLoaded = false;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadIsInterface()
        {
            if (!isInterfaceLoaded)
            {
                lock (locker)
                {
                    if (!isInterfaceLoaded)
                    {
                        isInterfaceLoaded = true;
                        var isCoreType = TypeLookup.CoreTypes.Contains(Type);
                        isIEnumerable = !isCoreType && (Type.IsArray || Type.Name == enumberableTypeName || Interfaces.Select(x => x.Name).Contains(enumberableTypeName));
                        isICollection = !isCoreType && (Type.Name == collectionTypeName || Interfaces.Select(x => x.Name).Contains(collectionTypeName));
                        isICollectionGeneric = !isCoreType && (Type.Name == collectionGenericTypeName || Interfaces.Select(x => x.Name).Contains(collectionGenericTypeName));
                        isIList = !isCoreType && (Type.Name == listTypeName || Type.Name == listGenericTypeName || Interfaces.Select(x => x.Name).Contains(listTypeName) || Interfaces.Select(x => x.Name).Contains(listGenericTypeName));
                        isISet = !isCoreType && (Type.Name == setGenericTypeName || Interfaces.Select(x => x.Name).Contains(setGenericTypeName));
                    }
                }
            }
        }

        private MemberDetail[] memberDetails = null;
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
                                var properties = Type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                var fields = Type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                if (Type.IsInterface)
                                {
                                    foreach (var i in Interfaces)
                                    {
                                        var iProperties = i.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                        var iFields = i.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                        properties = properties.Concat(iProperties.Where(x => !properties.Select(y => y.Name).Contains(x.Name))).ToArray();
                                        fields = fields.Concat(iFields.Where(x => !fields.Select(y => y.Name).Contains(x.Name))).ToArray();
                                    }
                                }
                                foreach (var property in properties)
                                {
                                    if (property.GetIndexParameters().Length > 0)
                                        continue;
                                    MemberDetail backingMember = null;

                                    //<{property.Name}>k__BackingField
                                    //<{property.Name}>i__Field
                                    var backingName = $"<{property.Name}>";
                                    var backingField = fields.FirstOrDefault(x => x.Name.StartsWith(backingName));
                                    if (backingField != null)
                                        backingMember = new MemberDetail(backingField, null, locker);

                                    items.Add(new MemberDetail(property, backingMember, locker));
                                }
                                foreach (var field in fields.Where(x => !items.Any(y => y.BackingFieldDetail?.MemberInfo == x)))
                                {
                                    items.Add(new MemberDetail(field, null, locker));
                                }
                            }

                            memberDetails = items.ToArray();
                        }
                    }
                }
                return memberDetails;
            }
        }

        private MethodDetail[] methodDetails = null;
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
                                    items.Add(new MethodDetail(method, locker));
                                if (Type.IsInterface)
                                {
                                    foreach (var i in Interfaces)
                                    {
                                        var iMethods = i.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                                        foreach (var method in iMethods)
                                            items.Add(new MethodDetail(method, locker));
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

        private ConstructorDetail[] constructorDetails = null;
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
                                    items[i] = new ConstructorDetail(constructors[i], locker);
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

        private Attribute[] attributes = null;
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

        private IDictionary<string, MemberDetail> membersByName = null;
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
        public bool TryGetMember(string name, out MemberDetail member)
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

        private IDictionary<string, MemberDetail> membersByNameLower = null;
        public MemberDetail GetMemberCaseInsensitive(string name)
        {
            if (membersByNameLower == null)
            {
                lock (locker)
                {
                    membersByNameLower ??= this.MemberDetails.GroupBy(x => x.Name.ToLower()).Where(x => x.Count() == 1).ToDictionary(x => x.Key, x => x.First());
                }
            }
            if (!this.membersByNameLower.TryGetValue(name.ToLower(), out var member))
                throw new Exception($"{Type.Name} does not contain member {name}");
            return member;
        }
        public bool TryGetMemberCaseInsensitive(string name, out MemberDetail member)
        {
            if (membersByNameLower == null)
            {
                lock (locker)
                {
                    membersByNameLower ??= this.MemberDetails.GroupBy(x => x.Name.ToLower()).Where(x => x.Count() == 1).ToDictionary(x => x.Key, x => x.First());
                }
            }
            return this.membersByNameLower.TryGetValue(name.ToLower(), out member);
        }

        private readonly ConcurrentFactoryDictionary<TypeKey, MethodDetail> methodLookups = new();
        private MethodDetail GetMethodInternal(string name, Type[] parameterTypes = null)
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
        public MethodDetail GetMethod(string name, Type[] parameterTypes = null)
        {
            var method = GetMethodInternal(name, parameterTypes);
            if (method == null)
                throw new MissingMethodException($"{Type.Name}.{name} method not found for the given parameters {String.Join(",", parameterTypes.Select(x => x.GetNiceName()))}");
            return method;
        }
        public bool TryGetMethod(string name, out MethodDetail method)
        {
            method = GetMethodInternal(name, null);
            return method != null;
        }
        public bool TryGetMethod(string name, Type[] parameterTypes, out MethodDetail method)
        {
            method = GetMethodInternal(name, parameterTypes);
            return method != null;
        }

        private readonly ConcurrentFactoryDictionary<TypeKey, ConstructorDetail> constructorLookups = new();
        private ConstructorDetail GetConstructorInternal(Type[] parameterTypes)
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
        public bool TryGetConstructor(out ConstructorDetail constructor)
        {
            constructor = GetConstructorInternal(null);
            return constructor != null;
        }
        public bool TryGetConstructor(Type[] parameterTypes, out ConstructorDetail constructor)
        {
            constructor = GetConstructorInternal(parameterTypes);
            return constructor != null;
        }

        private IReadOnlyList<MemberDetail> serializableMemberDetails = null;
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

        private IDictionary<string, MemberDetail> membersFieldBackedByName;
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
        public bool TryGetSerializableMemberDetails(string name, out MemberDetail property)
        {
            if (membersFieldBackedByName == null)
            {
                lock (locker)
                {
                    membersFieldBackedByName ??= MemberDetails.ToDictionary(x => x.Name);
                }
            }
            return this.membersFieldBackedByName.TryGetValue(name, out property);
        }

        private TypeDetail[] innerTypesDetails = null;
        public IReadOnlyList<TypeDetail> InnerTypeDetails
        {
            get
            {
                if (InnerTypes == null)
                    return null;
                if (innerTypesDetails == null)
                {
                    lock (locker)
                    {
                        if (innerTypesDetails == null)
                        {
                            var items = new TypeDetail[InnerTypes.Count];
                            for (var i = 0; i < InnerTypes.Count; i++)
                            {
                                items[i] = TypeAnalyzer.GetTypeDetail(InnerTypes[i]);
                            }
                            innerTypesDetails = items;
                        }
                    }
                }
                return innerTypesDetails;
            }
        }

        private bool isIEnumerableGeneric;
        private Type iEnumerableGenericInnerType = null;
        public bool IsIEnumerableGeneric
        {
            get
            {
                if (!IsIEnumerable)
                    return false;
                LoadIEnumerableGeneric();
                return isIEnumerableGeneric;
            }
        }
        public Type IEnumerableGenericInnerType
        {
            get
            {
                if (!IsIEnumerable)
                    return null;
                LoadIEnumerableGeneric();
                return iEnumerableGenericInnerType;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadIEnumerableGeneric()
        {
            if (iEnumerableGenericInnerType == null)
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
        }

        private TypeDetail iEnumerableGenericInnerTypeDetails = null;
        public TypeDetail IEnumerableGenericInnerTypeDetails
        {
            get
            {
                if (IEnumerableGenericInnerType == null)
                    return null;
                if (iEnumerableGenericInnerTypeDetails == null)
                {
                    lock (locker)
                    {
                        iEnumerableGenericInnerTypeDetails ??= TypeAnalyzer.GetTypeDetail(IEnumerableGenericInnerType);
                    }
                }
                return iEnumerableGenericInnerTypeDetails;
            }
        }

        Func<object, object> taskResultGetter = null;
        public Func<object, object> TaskResultGetter
        {
            get
            {
                if (this.IsTask && this.Type.IsGenericType)
                {
                    if (taskResultGetter == null)
                    {
                        lock (locker)
                        {
                            taskResultGetter ??= GetMember("Result").Getter;
                        }
                    }
                    return taskResultGetter;
                }
                return null;
            }
        }

        private bool creatorLoaded = false;
        private Func<object> creator = null;
        public Func<object> Creator
        {
            get
            {
                if (!creatorLoaded)
                {
                    lock (locker)
                    {
                        if (!creatorLoaded)
                        {
                            creatorLoaded = true;
                            if (!Type.IsAbstract && !Type.IsGenericTypeDefinition)
                            {
                                var emptyConstructor = this.ConstructorDetails.FirstOrDefault(x => x.ParametersInfo.Count == 0);
                                if (emptyConstructor != null)
                                {
                                    creator = () => { return emptyConstructor.Creator(null); };
                                }
                                else if (Type.IsValueType && Type.Name != "Void")
                                {
                                    var constantExpression = Expression.Convert(Expression.Default(Type), typeof(object));
                                    var lambda = Expression.Lambda<Func<object>>(constantExpression).Compile();
                                    creator = lambda;
                                }
                                else if (Type == typeof(string))
                                {
                                    creator = () => { return String.Empty; };
                                }
                            }
                        }
                    }
                }
                return creator;
            }
        }

        public override string ToString()
        {
            return Type.GetNiceFullName();
        }

        internal TypeDetail(Type type)
        {
            this.Type = type;

            this.IsNullable = type.Name == nullaleTypeName;

            if (TypeLookup.CoreTypeLookup(type, out var coreTypeLookup))
                this.CoreType = coreTypeLookup;

            if (TypeLookup.SpecialTypeLookup(Type, out var specialTypeLookup))
                this.SpecialType = specialTypeLookup;
        }

        private static bool IsSerializableType(TypeDetail typeDetail)
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
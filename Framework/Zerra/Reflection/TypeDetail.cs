// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Zerra.Collections;

namespace Zerra.Reflection
{
    public sealed class TypeDetail
    {
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
        public CoreType? CoreType { get; private set; }
        public SpecialType? SpecialType { get; private set; }
        public IReadOnlyList<Type> InnerTypes { get; private set; }
        public bool IsIEnumerable { get; private set; }
        public bool IsIEnumerableGeneric { get; private set; }
        public Type IEnumerableGenericInnerType { get; private set; }
        public bool IsICollection { get; private set; }
        public bool IsICollectionGeneric { get; private set; }
        public bool IsIList { get; private set; }
        public bool IsISet { get; private set; }
        public bool IsNullable { get; private set; }
        public bool IsTask { get; private set; }
        public bool IsGraphLocalProperty { get; private set; }
        public CoreType? EnumUnderlyingType { get; private set; }
        public IReadOnlyList<Type> Interfaces { get; private set; }
        public IReadOnlyList<Type> BaseTypes { get; private set; }
        public IReadOnlyList<MemberDetail> MemberDetails { get; private set; }
        public IReadOnlyList<MethodDetail> MethodDetails { get; private set; }
        public IReadOnlyList<ConstructorDetails> ConstructorDetails { get; private set; }
        public IReadOnlyList<Attribute> Attributes { get; private set; }

        private IDictionary<string, MemberDetail> membersByName;
        public MemberDetail GetMember(string name)
        {
            if (!this.membersByName.TryGetValue(name, out var member))
                throw new Exception($"{Type.Name} does not contain member {name}");
            return member;
        }
        public bool TryGetMember(string name, out MemberDetail member)
        {
            return this.membersByName.TryGetValue(name, out member);
        }

        private IDictionary<string, MemberDetail> membersByNameLower = null;
        public MemberDetail GetMemberCaseInsensitive(string name)
        {
            if (membersByNameLower == null)
            {
                lock (this)
                {
                    if (membersByNameLower != null)
                        this.membersByNameLower = this.MemberDetails.GroupBy(x => x.Name.ToLower()).Where(x => x.Count() == 1).ToDictionary(x => x.Key, x => x.First());
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
                lock (this)
                {
                    if (membersByNameLower == null)
                        this.membersByNameLower = this.MemberDetails.GroupBy(x => x.Name.ToLower()).Where(x => x.Count() == 1).ToDictionary(x => x.Key, x => x.First());
                }
            }
            return this.membersByNameLower.TryGetValue(name.ToLower(), out member);
        }

        private readonly ConcurrentFactoryDictionary<TypeKey, MethodDetail> methodLookups = new();
        private MethodDetail GetMethodInternal(string name, Type[] parameterTypes = null)
        {
            var key = new TypeKey(name, parameterTypes);
            var method = methodLookups.GetOrAdd(key, (keyArg) =>
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

        private readonly ConcurrentFactoryDictionary<TypeKey, ConstructorDetails> constructorLookups = new();
        private ConstructorDetails GetConstructorInternal(Type[] parameterTypes)
        {
            var key = new TypeKey(parameterTypes);
            var constructor = constructorLookups.GetOrAdd(key, (keyArg) =>
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
        public ConstructorDetails GetConstructor(params Type[] parameterTypes)
        {
            var constructor = GetConstructorInternal(parameterTypes);
            if (constructor == null)
                throw new MissingMethodException($"{Type.Name} constructor not found for the given parameters {String.Join(",", parameterTypes.Select(x => x.GetNiceName()))}");
            return constructor;
        }
        public bool TryGetConstructor(out ConstructorDetails constructor)
        {
            constructor = GetConstructorInternal(null);
            return constructor != null;
        }
        public bool TryGetConstructor(Type[] parameterTypes, out ConstructorDetails constructor)
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
                    lock (this)
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
                lock (this)
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
                lock (this)
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
                    lock (this)
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

        private TypeDetail iEnumerableGenericInnerTypeDetails = null;
        public TypeDetail IEnumerableGenericInnerTypeDetails
        {
            get
            {
                if (IEnumerableGenericInnerType == null)
                    return null;
                if (iEnumerableGenericInnerTypeDetails == null)
                {
                    lock (this)
                    {
                        iEnumerableGenericInnerTypeDetails ??= TypeAnalyzer.GetTypeDetail(IEnumerableGenericInnerType);
                    }
                }
                return iEnumerableGenericInnerTypeDetails;
            }
        }

        public Func<object, object> TaskResultGetter { get; private set; }

        public Func<object> Creator { get; private set; }

        public override string ToString()
        {
            return Type.GetNiceFullName();
        }

        internal TypeDetail(Type type)
        {
            this.Type = type;

            this.Interfaces = type.GetInterfaces();

            var baseType = type;
            var baseTypes = new List<Type>();
            while (baseType != null)
            {
                baseTypes.Add(baseType);
                baseType = baseType.BaseType;
            }
            this.BaseTypes = baseTypes;

            this.IsNullable = type.Name == nullaleTypeName;

            this.IsIEnumerable = !TypeLookup.CoreTypes.Contains(type) && (type.IsArray || type.Name == enumberableTypeName || Interfaces.Select(x => x.Name).Contains(enumberableTypeName));
            this.IsICollection = !TypeLookup.CoreTypes.Contains(type) && (type.Name == collectionTypeName || Interfaces.Select(x => x.Name).Contains(collectionTypeName));
            this.IsICollectionGeneric = !TypeLookup.CoreTypes.Contains(type) && (type.Name == collectionGenericTypeName || Interfaces.Select(x => x.Name).Contains(collectionGenericTypeName));
            this.IsIList = !TypeLookup.CoreTypes.Contains(type) && (type.Name == listTypeName || type.Name == listGenericTypeName || Interfaces.Select(x => x.Name).Contains(listTypeName) || Interfaces.Select(x => x.Name).Contains(listGenericTypeName));
            this.IsISet = !TypeLookup.CoreTypes.Contains(type) && (type.Name == setGenericTypeName || Interfaces.Select(x => x.Name).Contains(setGenericTypeName));

            if (this.IsIEnumerable)
            {
                if (type.Name == enumberableGenericTypeName)
                {
                    this.IsIEnumerableGeneric = true;
                    this.IEnumerableGenericInnerType = type.GetGenericArguments()[0];
                }
                else
                {
                    var enumerableGeneric = Interfaces.Where(x => x.Name == enumberableGenericTypeName).ToArray();
                    if (enumerableGeneric.Length == 1)
                    {
                        this.IsIEnumerableGeneric = true;
                        this.IEnumerableGenericInnerType = enumerableGeneric[0].GetGenericArguments()[0];
                    }
                }
            }

            if (type.IsGenericType)
                this.InnerTypes = type.GetGenericArguments();
            else if (type.IsArray)
                this.InnerTypes = new Type[] { type.GetElementType() };
            else
                this.InnerTypes = Type.EmptyTypes;

            if (TypeLookup.CoreTypeLookup(type, out var coreTypeLookup))
                this.CoreType = coreTypeLookup;

            if (TypeLookup.SpecialTypeLookup(type, out var specialTypeLookup))
            {
                this.SpecialType = specialTypeLookup;
                switch (specialTypeLookup)
                {
                    case Reflection.SpecialType.Task:
                        this.IsTask = true;
                        break;
                    case Reflection.SpecialType.Dictionary:
                        var innerType = TypeAnalyzer.GetGenericType(keyValuePairType, (Type[])this.InnerTypes);
                        this.InnerTypes = new Type[] { innerType };
                        break;
                }
            }

            if (type.IsEnum)
            {
                var enumEnderlyingType = Enum.GetUnderlyingType(this.Type);
                if (!TypeLookup.CoreTypeLookup(enumEnderlyingType, out var enumCoreTypeLookup))
                    throw new NotImplementedException("Should not happen");
                this.EnumUnderlyingType = enumCoreTypeLookup;
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
                this.EnumUnderlyingType = enumCoreTypeLookup;
            }

            this.IsGraphLocalProperty = this.CoreType.HasValue || this.EnumUnderlyingType.HasValue || this.SpecialType.HasValue || this.Type.IsArray || (this.IsNullable && this.InnerTypes[0].IsArray);

            var methodDetails = new List<MethodDetail>();
            if (!type.IsGenericTypeDefinition)
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                    methodDetails.Add(new MethodDetail(method));
                if (type.IsInterface)
                {
                    foreach (var i in Interfaces)
                    {
                        var iMethods = i.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                        foreach (var method in iMethods)
                            methodDetails.Add(new MethodDetail(method));
                    }
                }
            }
            this.MethodDetails = methodDetails.ToArray();

            var constructorDetails = new List<ConstructorDetails>();
            if (!type.IsGenericTypeDefinition)
            {
                var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var constructor in constructors)
                    constructorDetails.Add(new ConstructorDetails(constructor));
            }
            this.ConstructorDetails = constructorDetails.ToArray();

            if (!type.IsAbstract && !type.IsGenericTypeDefinition)
            {
                var emptyConstructor = this.ConstructorDetails.FirstOrDefault(x => x.ParametersInfo.Count == 0);
                if (emptyConstructor != null)
                {
                    this.Creator = () => { return emptyConstructor.Creator(null); };
                }
                else if (type.IsValueType && type.Name != "Void")
                {
                    var constantExpression = Expression.Convert(Expression.Default(type), typeof(object));
                    var lambda = Expression.Lambda<Func<object>>(constantExpression).Compile();
                    this.Creator = lambda;
                }
                else if (type == typeof(string))
                {
                    this.Creator = () => { return String.Empty; };
                }
            }

            var test = type.Name;
            this.Attributes = type.GetCustomAttributes().ToArray();

            //if (!this.CoreType.HasValue)
            //{
                var typeMembers = new List<MemberDetail>();
                if (!type.IsGenericTypeDefinition)
                {
                    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (type.IsInterface)
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
                        var backingField = fields.FirstOrDefault(x => x.Name == $"<{property.Name}>k__BackingField");
                        if (backingField != null)
                            backingMember = new MemberDetail(backingField, null);
                        typeMembers.Add(new MemberDetail(property, backingMember));
                    }
                    foreach (var field in fields.Where(x => !x.Name.EndsWith("k__BackingField")))
                    {
                        typeMembers.Add(new MemberDetail(field, null));
                    }
                }

                this.MemberDetails = typeMembers.ToArray();
                this.membersByName = this.MemberDetails.ToDictionary(x => x.Name);
            //}
            //else
            //{
            //    var typeMembers = Array.Empty<MemberDetail>();
            //    this.MemberDetails = typeMembers;
            //    this.membersByName = this.MemberDetails.ToDictionary(x => x.Name);
            //}

            if (this.IsTask && this.Type.IsGenericType)
            {
                if (this.membersByName.TryGetValue("Result", out var resultMember))
                    this.TaskResultGetter = resultMember.Getter;
            }
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
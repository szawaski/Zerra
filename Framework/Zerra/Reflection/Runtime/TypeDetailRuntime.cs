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

namespace Zerra.Reflection.Runtime
{
    internal sealed class TypeDetailRuntime : TypeDetail
    {
        public override bool IsGenerated => false;

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
                if (innerTypes is null)
                {
                    lock (locker)
                    {
                        if (innerTypes is null)
                        {
                            if (Type.IsGenericType)
                            {
                                innerTypes = Type.GetGenericArguments();
                            }
                            else if (Type.IsArray)
                            {
                                innerTypes = [Type.GetElementType()!];
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
                if (baseTypes is null)
                {
                    lock (locker)
                    {
                        if (baseTypes is null)
                        {
                            var baseType = Type.BaseType;
                            var items = new List<Type>();
                            while (baseType is not null)
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
                if (interfaces is null)
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
                    hasIReadOnlySetGeneric = Type.Name == readOnlySetGenericTypeName || interfaces.Contains(readOnlySetGenericTypeName);
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
                    isIReadOnlySetGeneric = Type.Name == readOnlySetGenericTypeName;
                    isIDictionary = Type.Name == dictionaryTypeName;
                    isIDictionaryGeneric = Type.Name == dictionaryGenericTypeName;
                    isIReadOnlyDictionaryGeneric = Type.Name == readOnlyDictionaryGenericTypeName;

                    hasIsInterfaceLoaded = true;
                }
            }
        }

        private MemberDetail[]? memberDetails = null;
        public override IReadOnlyList<MemberDetail> MemberDetails
        {
            get
            {
                if (memberDetails is null)
                {
                    lock (locker)
                    {
                        if (memberDetails is null)
                        {
                            var items = new List<MemberDetail>();
                            if (!Type.IsGenericTypeDefinition)
                            {
                                var hasInterfaces = Interfaces.Count > 0;
                                var names = hasInterfaces ? new HashSet<string>() : null; //explicit declarations can create duplicates with interfaces

                                var properties = Type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                                var fields = Type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                                foreach (var property in properties)
                                {
                                    if (property.GetIndexParameters().Length > 0)
                                        continue;
                                    MemberDetail? backingMember = null;

                                    //<{property.Name}>k__BackingField
                                    //<{property.Name}>i__Field
                                    var backingName = $"<{property.Name}>";
                                    var backingField = fields.FirstOrDefault(x => x.Name.StartsWith(backingName));
                                    if (backingField is not null)
                                        backingMember = MemberDetailRuntime<object, object>.New(Type, property.PropertyType, property.Name, backingField, null, false, locker);

                                    items.Add(MemberDetailRuntime<object, object>.New(Type, property.PropertyType, property.Name, property, backingMember, false, locker));
                                    if (hasInterfaces)
                                        names!.Add(property.Name);
                                }

                                if (hasInterfaces)
                                {
                                    foreach (var @field in fields)
                                        names!.Add(@field.Name);

                                    foreach (var i in Interfaces)
                                    {
                                        var iProperties = i.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);//don't get static interface members

                                        foreach (var property in iProperties)
                                        {
                                            if (property.GetIndexParameters().Length > 0)
                                                continue;
                                            //MemberDetail? backingMember = null;

                                            ////<{property.Name}>k__BackingField
                                            ////<{property.Name}>i__Field
                                            //var backingName = $"<{property.Name}>";
                                            //var backingField = fields.FirstOrDefault(x => x.Name.StartsWith(backingName));
                                            //if (backingField is not null)
                                            //{
                                            //    var backingMemberName = $"{property.DeclaringType?.Namespace}.{property.DeclaringType?.Name}.{property.Name.Split('.').Last()}";
                                            //    backingMember = MemberDetailRuntime<object, object>.New(Type, property.PropertyType, backingMemberName, backingField, null, locker);
                                            //}

                                            string name;
                                            bool isExplicitFromInterface;
                                            if (Type.IsInterface && !names!.Contains(property.Name))
                                            {
                                                name = property.Name;
                                                isExplicitFromInterface = false;
                                            }
                                            else
                                            {
                                                name = $"{property.DeclaringType?.Namespace}.{property.DeclaringType?.Name}.{property.Name.Split('.').Last()}";
                                                isExplicitFromInterface = true;
                                            }

                                            if (!names!.Contains(name))
                                            {
                                                items.Add(MemberDetailRuntime<object, object>.New(Type, property.PropertyType, name, property, null, isExplicitFromInterface, locker));
                                                names!.Add(name);
                                            }
                                        }
                                    }
                                }

                                foreach (var @field in fields.Where(x => !items.Any(y => y.BackingFieldDetailBoxed?.MemberInfo == x)))
                                {
                                    items.Add(MemberDetailRuntime<object, object>.New(Type, @field.FieldType, @field.Name, @field, null, false, locker));
                                }
                            }

                            memberDetails = items.ToArray();
                        }
                    }
                }
                return memberDetails;
            }
        }

        private Dictionary<string, MemberDetail>? membersByName = null;
        private bool membersByNameLoadedAll = false;
        public override MemberDetail GetMember(string name)
        {
            if (membersByName is null)
            {
                lock (locker)
                {
                    if (membersByName is null)
                    {
                        if (memberDetails is not null)
                        {
                            membersByName = memberDetails.ToDictionary(x => x.Name);
                            membersByNameLoadedAll = true;
                        }
                        else
                        {
                            membersByName = new Dictionary<string, MemberDetail>();
                        }
                    }
                }
            }
            if (this.membersByName.TryGetValue(name, out var member))
                return member;

            if (!membersByNameLoadedAll)
            {
                if (TryLoadMembersByName(name, out member))
                    return member;
            }

            throw new Exception($"{Type.Name} does not contain member {name}");
        }
        public override bool TryGetMember(string name,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out MemberDetail member)
        {
            if (membersByName is null)
            {
                lock (locker)
                {
                    if (membersByName is null)
                    {
                        if (memberDetails is not null)
                        {
                            membersByName = memberDetails.ToDictionary(x => x.Name);
                            membersByNameLoadedAll = true;
                        }
                        else
                        {
                            membersByName = new Dictionary<string, MemberDetail>();
                        }
                    }
                }
            }
            if (this.membersByName.TryGetValue(name, out member))
                return true;

            if (!membersByNameLoadedAll)
            {
                if (TryLoadMembersByName(name, out member))
                    return true;
            }

            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryLoadMembersByName(string name,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out MemberDetail member)
        {
            lock (locker)
            {
                //256 is about where the number of members where performance drops off for individual loading
                if (memberDetails is not null || membersByName!.Count >= 256)
                {
                    foreach (var memberDetail in MemberDetails)
                    {
#if NETSTANDARD2_0
                        if (membersByName!.ContainsKey(memberDetail.Name))
                            membersByName!.Add(memberDetail.Name, memberDetail);
#else
                        _ = membersByName!.TryAdd(memberDetail.Name, memberDetail);
#endif
                    }
                    membersByNameLoadedAll = true;
                    if (this.membersByName!.TryGetValue(name, out member))
                        return true;
                    return false;
                }

                var property = Type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (property is not null && property.GetIndexParameters().Length == 0)
                {
                    MemberDetail? backingMember = null;
                    //<{property.Name}>k__BackingField    
                    //<{property.Name}>i__Field
                    var backingName = $"<{property.Name}>";
                    var backingField = Type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    if (backingField is not null)
                        backingMember = MemberDetailRuntime<object, object>.New(Type, property.PropertyType, property.Name, backingField, null, false, locker);

                    member = MemberDetailRuntime<object, object>.New(Type, property.PropertyType, property.Name, property, backingMember, false, locker);
                    membersByName!.Add(member.Name, member);
                    return true;
                }
                var field = Type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (field is not null)
                {
                    member = MemberDetailRuntime<object, object>.New(Type, field.FieldType, field.Name, field, null, false, locker);
                    membersByName!.Add(member.Name, member);
                    return true;
                }

                foreach (var memberDetail in MemberDetails)
                {
#if NETSTANDARD2_0
                    if (membersByName!.ContainsKey(memberDetail.Name))
                        membersByName!.Add(memberDetail.Name, memberDetail);
#else
                    _ = membersByName!.TryAdd(memberDetail.Name, memberDetail);
#endif
                }
                membersByNameLoadedAll = true;
                if (this.membersByName!.TryGetValue(name, out member))
                    return true;
            }
            return false;
        }

        private MethodDetail[]? methodDetailsBoxed = null;
        public override IReadOnlyList<MethodDetail> MethodDetailsBoxed
        {
            get
            {
                if (methodDetailsBoxed is null)
                {
                    lock (locker)
                    {
                        if (methodDetailsBoxed is null)
                        {
                            var items = new List<MethodDetail>();
                            var hasInterfaces = Interfaces.Count > 0;
                            var names = hasInterfaces ? new HashSet<string>() : null; //explicit declarations can create duplicates with interfaces
                            if (!Type.IsGenericTypeDefinition)
                            {
                                var methods = Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                                foreach (var method in methods)
                                {
                                    items.Add(MethodDetailRuntime<object>.New(Type, method.Name, method, false, locker));
                                    if (hasInterfaces)
                                        names!.Add(method.Name);
                                }

                                if (hasInterfaces)
                                {
                                    foreach (var i in Interfaces)
                                    {
                                        var iMethods = i.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); //don't get static interface methods
                                        foreach (var method in iMethods)
                                        {
                                            string name;
                                            bool isExplicitFromInterface;
                                            if (Type.IsInterface && !names!.Contains(method.Name))
                                            {
                                                name = method.Name;
                                                isExplicitFromInterface = false;
                                            }
                                            else
                                            {
                                                name = $"{method.DeclaringType?.Namespace}.{method.DeclaringType?.Name}.{method.Name.Split('.').Last()}";
                                                isExplicitFromInterface = true;
                                            }

                                            if (!names!.Contains(name))
                                            {
                                                items.Add(MethodDetailRuntime<object>.New(Type, name, method, isExplicitFromInterface, locker));
                                                names!.Add(name);
                                            }
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
                if (constructorDetailsBoxed is null)
                {
                    lock (locker)
                    {
                        if (constructorDetailsBoxed is null)
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
                if (attributes is null)
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
                if (innerTypesDetails is null)
                {
                    lock (locker)
                    {
                        if (innerTypesDetails is null)
                        {
                            var innerTypesRef = InnerTypes;
                            if (innerTypesRef is not null)
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

        private bool iEnumerableGenericInnerTypeLoaded = false;
        private Type? iEnumerableGenericInnerType = null;
        public override Type IEnumerableGenericInnerType
        {
            get
            {
                if (!iEnumerableGenericInnerTypeLoaded)
                {
                    lock (locker)
                    {
                        if (!iEnumerableGenericInnerTypeLoaded)
                        {
                            if (isIEnumerableGeneric)
                            {
                                iEnumerableGenericInnerType = Type.GetGenericArguments()[0];
                            }
                            else
                            {
                                var interfaceFound = Interfaces.Where(x => x.Name == enumberableGenericTypeName).ToArray();
                                if (interfaceFound.Length == 1)
                                {
                                    iEnumerableGenericInnerType = interfaceFound[0].GetGenericArguments()[0];
                                }
                            }
                            iEnumerableGenericInnerTypeLoaded = true;
                        }
                    }
                }
                return iEnumerableGenericInnerType ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name.GetType()} is not an IEnumerableGeneric or has multiple");
            }
        }

        private bool iEnumerableGenericInnerTypeDetailLoaded = false;
        private TypeDetail? iEnumerableGenericInnerTypeDetail = null;
        public override TypeDetail IEnumerableGenericInnerTypeDetail
        {
            get
            {
                if (!iEnumerableGenericInnerTypeDetailLoaded)
                {
                    lock (locker)
                    {
                        if (!iEnumerableGenericInnerTypeDetailLoaded)
                        {
                            if (IEnumerableGenericInnerType is not null)
                                iEnumerableGenericInnerTypeDetail ??= TypeAnalyzer.GetTypeDetail(IEnumerableGenericInnerType);
                            iEnumerableGenericInnerTypeDetailLoaded = true;
                        }
                    }
                }
                return iEnumerableGenericInnerTypeDetail ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name.GetType()} is not an IEnumerableGeneric or has multiple");
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
                            if (IsIDictionaryGeneric || IsIReadOnlyDictionaryGeneric)
                            {
                                dictionaryInnerType = TypeAnalyzer.GetGenericType(keyValuePairType, (Type[])InnerTypes);
                            }
                            else if (HasIDictionaryGeneric)
                            {
                                var interfaceFound = Interfaces.Where(x => x.Name == dictionaryGenericTypeName).ToArray();
                                if (interfaceFound.Length == 1)
                                {
                                    dictionaryInnerType = TypeAnalyzer.GetGenericType(keyValuePairType, interfaceFound[0].GetGenericArguments());
                                }
                            }
                            else if (HasIReadOnlyDictionaryGeneric)
                            {
                                var interfaceFound = Interfaces.Where(x => x.Name == readOnlyDictionaryGenericTypeName).ToArray();
                                if (interfaceFound.Length == 1)
                                {
                                    dictionaryInnerType = TypeAnalyzer.GetGenericType(keyValuePairType, interfaceFound[0].GetGenericArguments());
                                }
                            }
                            else if (HasIDictionary)
                            {
                                dictionaryInnerType = dictionaryEntryType;
                            }
                            dictionartyInnerTypeLoaded = true;
                        }
                    }
                }
                return dictionaryInnerType ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name.GetType()} is not a Dictionary or has multiple");
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
                            dictionaryInnerTypeDetailLoaded = true;
                        }
                    }
                }
                return dictionaryInnerTypesDetail ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name.GetType()} is not a Dictionary or has multiple");
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

                if (taskResultGetter is null)
                    LoadTaskResultGetter();
                return taskResultGetter is not null;
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
                return creatorBoxed is not null;
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
                        var emptyConstructor = this.ConstructorDetailsBoxed.FirstOrDefault(x => x.ParameterDetails.Count == 0);
                        if (emptyConstructor is not null && emptyConstructor.HasCreatorBoxed)
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

        public override Delegate? CreatorTyped => null;

        internal TypeDetailRuntime(Type type) : base(type)
        {
            this.IsNullable = type.Name == nullaleTypeName;
        }
    }
}
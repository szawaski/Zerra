// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zerra.Collections;

namespace Zerra.Reflection
{
    public sealed class TypeDetail<T> : TypeDetail
    {
        //private MemberDetail<T>[]? memberDetails = null;
        //public new IReadOnlyList<MemberDetail<T>> MemberDetails
        //{
        //    get
        //    {
        //        if (memberDetails == null)
        //        {
        //            lock (locker)
        //            {
        //                if (memberDetails == null)
        //                {
        //                    var items = new List<MemberDetail<T>>();
        //                    if (!Type.IsGenericTypeDefinition)
        //                    {
        //                        IEnumerable<PropertyInfo> properties = Type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        //                        IEnumerable<FieldInfo> fields = Type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        //                        if (Type.IsInterface)
        //                        {
        //                            foreach (var i in Interfaces)
        //                            {
        //                                var iProperties = i.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        //                                var iFields = i.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        //                                var existingPropertyNames = properties.Select(y => y.Name).ToArray();
        //                                properties = properties.Concat(iProperties.Where(x => !existingPropertyNames.Contains(x.Name)));
        //                                var existingFieldNames = fields.Select(y => y.Name).ToArray();
        //                                fields = fields.Concat(iFields.Where(x => !existingFieldNames.Contains(x.Name)));
        //                            }
        //                        }
        //                        foreach (var property in properties)
        //                        {
        //                            if (property.GetIndexParameters().Length > 0)
        //                                continue;
        //                            MemberDetail<T>? backingMember = null;

        //                            //<{property.Name}>k__BackingField
        //                            //<{property.Name}>i__Field
        //                            var backingName = $"<{property.Name}>";
        //                            var backingField = fields.FirstOrDefault(x => x.Name.StartsWith(backingName));
        //                            if (backingField != null)
        //                                backingMember = MemberDetail<T>.New(property.PropertyType, backingField, null, locker);

        //                            items.Add(MemberDetail<T>.New(property.PropertyType, property, backingMember, locker));
        //                        }
        //                        foreach (var field in fields.Where(x => !items.Any(y => y.BackingFieldDetail?.MemberInfo == x)))
        //                        {
        //                            items.Add(MemberDetail<T>.New(field.FieldType, field, null, locker));
        //                        }
        //                    }

        //                    memberDetails = items.ToArray();
        //                }
        //            }
        //        }
        //        return memberDetails;
        //    }
        //}

        private MethodDetail<T>[]? methodDetails = null;
        public new IReadOnlyList<MethodDetail<T>> MethodDetails
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
                                    items.Add(new MethodDetail<T>(method, locker));
                                if (Type.IsInterface)
                                {
                                    foreach (var i in Interfaces)
                                    {
                                        var iMethods = i.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                                        foreach (var method in iMethods)
                                            items.Add(new MethodDetail<T>(method, locker));
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
        public new IReadOnlyList<ConstructorDetail<T>> ConstructorDetails
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
                                var items = new ConstructorDetail<T>[constructors.Length];
                                for (var i = 0; i < items.Length; i++)
                                    items[i] = new ConstructorDetail<T>(constructors[i], locker);
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

//        private Dictionary<string, MemberDetail<T>>? membersByName = null;
//        public new MemberDetail<T> GetMember(string name)
//        {
//            if (membersByName == null)
//            {
//                lock (locker)
//                {
//                    membersByName ??= this.MemberDetails.ToDictionary(x => x.Name);
//                }
//            }
//            if (!this.membersByName.TryGetValue(name, out var member))
//                throw new Exception($"{Type.Name} does not contain member {name}");
//            return member;
//        }
//        public new bool TryGetMember(string name,
//#if !NETSTANDARD2_0
//            [MaybeNullWhen(false)]
//#endif
//        out MemberDetail<T> member)
//        {
//            if (membersByName == null)
//            {
//                lock (locker)
//                {
//                    membersByName ??= this.MemberDetails.ToDictionary(x => x.Name);
//                }
//            }
//            return this.membersByName.TryGetValue(name, out member);
//        }

//        private readonly ConcurrentFactoryDictionary<TypeKey, MethodDetail<T>?> methodLookups = new();
//        private MethodDetail<T>? GetMethodInternal(string name, Type[]? parameterTypes = null)
//        {
//            var key = new TypeKey(name, parameterTypes);
//            var method = methodLookups.GetOrAdd(key, (_) =>
//            {
//                foreach (var methodDetail in MethodDetails)
//                {
//                    if (methodDetail.Name == name && (parameterTypes == null || methodDetail.ParametersInfo.Count == parameterTypes.Length))
//                    {
//                        var match = true;
//                        if (parameterTypes != null)
//                        {
//                            for (var i = 0; i < parameterTypes.Length; i++)
//                            {
//                                if (parameterTypes[i].Name != methodDetail.ParametersInfo[i].ParameterType.Name || parameterTypes[i].Namespace != methodDetail.ParametersInfo[i].ParameterType.Namespace)
//                                {
//                                    match = false;
//                                    break;
//                                }
//                            }
//                        }
//                        if (match)
//                            return methodDetail;
//                    }
//                }
//                return null;
//            });
//            return method;
//        }
//        public new MethodDetail<T> GetMethod(string name, Type[]? parameterTypes = null)
//        {
//            var method = GetMethodInternal(name, parameterTypes);
//            if (method == null)
//                throw new MissingMethodException($"{Type.Name}.{name} method not found for the given parameters {(parameterTypes == null || parameterTypes.Length == 0 ? "(none)" : String.Join(",", parameterTypes.Select(x => x.GetNiceName())))}");
//            return method;
//        }
//        public new bool TryGetMethod(string name,
//#if !NETSTANDARD2_0
//            [MaybeNullWhen(false)]
//#endif
//        out MethodDetail<T> method)
//        {
//            method = GetMethodInternal(name, null);
//            return method != null;
//        }
//        public new bool TryGetMethod(string name, Type[] parameterTypes,
//#if !NETSTANDARD2_0
//            [MaybeNullWhen(false)]
//#endif
//        out MethodDetail<T> method)
//        {
//            method = GetMethodInternal(name, parameterTypes);
//            return method != null;
//        }

        private bool creatorLoaded = false;
        private Func<T>? creator = null;
        public new Func<T> Creator
        {
            get
            {
                LoadCreator();
                return creator ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name} does not have a {nameof(Creator)}");
            }
        }
        public new bool HasCreator
        {
            get
            {
                LoadCreator();
                return creator != null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadCreator()
        {
            if (!creatorLoaded)
            {
                lock (locker)
                {
                    if (!creatorLoaded)
                    {
                        if (!Type.IsAbstract && !Type.IsGenericTypeDefinition)
                        {
                            var emptyConstructor = this.ConstructorDetails.FirstOrDefault(x => x.ParametersInfo.Count == 0);
                            if (emptyConstructor != null && emptyConstructor.Creator != null)
                            {
                                creator = () => { return (T)emptyConstructor.Creator(null); };
                            }
                            else if (Type.IsValueType && Type.Name != "Void")
                            {
                                creator = () => { return default!; };
                            }
                            else
                            {
                                creator = () => { return (T)(object)String.Empty; };
                            }
                        }
                        creatorLoaded = true;
                    }
                }
            }
        }

        private readonly ConcurrentFactoryDictionary<TypeKey, ConstructorDetail<T>?> constructorLookups = new();
        private ConstructorDetail<T>? GetConstructorInternal(Type[]? parameterTypes)
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
        public new ConstructorDetail<T> GetConstructor(params Type[] parameterTypes)
        {
            var constructor = GetConstructorInternal(parameterTypes);
            if (constructor == null)
                throw new MissingMethodException($"{Type.Name} constructor not found for the given parameters {String.Join(",", parameterTypes.Select(x => x.GetNiceName()))}");
            return constructor;
        }
        public new bool TryGetConstructor(
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)] 
#endif
        out ConstructorDetail<T> constructor)
        {
            constructor = GetConstructorInternal(null);
            return constructor != null;
        }
        public new bool TryGetConstructor(Type[] parameterTypes,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out ConstructorDetail<T> constructor)
        {
            constructor = GetConstructorInternal(parameterTypes);
            return constructor != null;
        }

//        private IReadOnlyList<MemberDetail<T>>? serializableMemberDetails = null;
//        public new IReadOnlyList<MemberDetail<T>> SerializableMemberDetails
//        {
//            get
//            {
//                if (serializableMemberDetails == null)
//                {
//                    lock (locker)
//                    {
//                        serializableMemberDetails ??= MemberDetails.Where(x => x.IsBacked && IsSerializableType(x.TypeDetail)).ToArray();
//                    }
//                }
//                return serializableMemberDetails;
//            }
//        }

//        private Dictionary<string, MemberDetail<T>>? serializableMembersByNameLower = null;
//        public new MemberDetail<T> GetSerializableMemberCaseInsensitive(string name)
//        {
//            if (serializableMembersByNameLower == null)
//            {
//                lock (locker)
//                {
//                    serializableMembersByNameLower ??= this.SerializableMemberDetails.GroupBy(x => x.Name.ToLower()).Where(x => x.Count() == 1).ToDictionary(x => x.Key, x => x.First(), new StringOrdinalIgnoreCaseComparer());
//                }
//            }
//            if (!this.serializableMembersByNameLower.TryGetValue(name, out var member))
//                throw new Exception($"{Type.Name} does not contain member {name}");
//            return member;
//        }
//        public new bool TryGetSerializableMemberCaseInsensitive(string name,
//#if !NETSTANDARD2_0
//            [MaybeNullWhen(false)]
//#endif
//         out MemberDetail<T> member)
//        {
//            if (serializableMembersByNameLower == null)
//            {
//                lock (locker)
//                {
//                    serializableMembersByNameLower ??= this.SerializableMemberDetails.GroupBy(x => x.Name.ToLower()).Where(x => x.Count() == 1).ToDictionary(x => x.Key, x => x.First(), new StringOrdinalIgnoreCaseComparer());
//                }
//            }
//            return this.serializableMembersByNameLower.TryGetValue(name, out member);
//        }

//        private Dictionary<string, MemberDetail<T>>? membersFieldBackedByName = null;
//        public new MemberDetail<T> GetMemberFieldBacked(string name)
//        {
//            if (membersFieldBackedByName == null)
//            {
//                lock (locker)
//                {
//                    membersFieldBackedByName ??= MemberDetails.ToDictionary(x => x.Name);
//                }
//            }
//            if (!this.membersFieldBackedByName.TryGetValue(name, out var member))
//                throw new Exception($"{Type.Name} does not contain member {name}");
//            return member;
//        }
//        public new bool TryGetGetMemberFieldBacked(string name,
//#if !NETSTANDARD2_0
//            [MaybeNullWhen(false)]
//#endif
//        out MemberDetail<T> member)
//        {
//            if (membersFieldBackedByName == null)
//            {
//                lock (locker)
//                {
//                    membersFieldBackedByName ??= MemberDetails.ToDictionary(x => x.Name);
//                }
//            }
//            return this.membersFieldBackedByName.TryGetValue(name, out member);
//        }

        internal TypeDetail(Type type) : base(type) { }
    }
}
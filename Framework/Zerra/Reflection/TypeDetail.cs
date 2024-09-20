// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Zerra.Collections;

namespace Zerra.Reflection
{
    public abstract class TypeDetail
    {
        public Type Type { get; }
        public abstract bool IsNullable { get; }
        public abstract CoreType? CoreType { get; }
        public abstract SpecialType? SpecialType { get; }

        public abstract IReadOnlyList<Type> InnerTypes { get; }

        public abstract Type InnerType { get; }

        public abstract bool IsTask { get; }

        public abstract CoreEnumType? EnumUnderlyingType { get; }

        public abstract IReadOnlyList<Type> BaseTypes { get; }

        public abstract IReadOnlyList<Type> Interfaces { get; }

        public abstract bool HasIEnumerable { get; }
        public abstract bool HasIEnumerableGeneric { get; }
        public abstract bool HasICollection { get; }
        public abstract bool HasICollectionGeneric { get; }
        public abstract bool HasIReadOnlyCollectionGeneric { get; }
        public abstract bool HasIList { get; }
        public abstract bool HasIListGeneric { get; }
        public abstract bool HasIReadOnlyListGeneric { get; }
        public abstract bool HasISetGeneric { get; }
        public abstract bool HasIReadOnlySetGeneric { get; }
        public abstract bool HasIDictionary { get; }
        public abstract bool HasIDictionaryGeneric { get; }
        public abstract bool HasIReadOnlyDictionaryGeneric { get; }

        public abstract bool IsIEnumerable { get; }
        public abstract bool IsIEnumerableGeneric { get; }
        public abstract bool IsICollection { get; }
        public abstract bool IsICollectionGeneric { get; }
        public abstract bool IsIReadOnlyCollectionGeneric { get; }
        public abstract bool IsIList { get; }
        public abstract bool IsIListGeneric { get; }
        public abstract bool IsIReadOnlyListGeneric { get; }
        public abstract bool IsISetGeneric { get; }
        public abstract bool IsIReadOnlySetGeneric { get; }
        public abstract bool IsIDictionary { get; }
        public abstract bool IsIDictionaryGeneric { get; }
        public abstract bool IsIReadOnlyDictionaryGeneric { get; }

        public abstract IReadOnlyList<MemberDetail> MemberDetails { get; }

        public abstract IReadOnlyList<MethodDetail> MethodDetailsBoxed { get; }

        public abstract IReadOnlyList<ConstructorDetail> ConstructorDetailsBoxed { get; }

        public abstract IReadOnlyList<Attribute> Attributes { get; }

        public abstract IReadOnlyList<TypeDetail> InnerTypeDetails { get; }

        public abstract TypeDetail InnerTypeDetail { get; }

        public abstract Type IEnumerableGenericInnerType { get; }

        public abstract TypeDetail IEnumerableGenericInnerTypeDetail { get; }

        public abstract Type DictionaryInnerType { get; }

        public abstract TypeDetail DictionaryInnerTypeDetail { get; }

        public abstract Func<object, object?> TaskResultGetter { get; }
        public abstract bool HasTaskResultGetter { get; }

        public abstract Func<object> CreatorBoxed { get; }
        public abstract bool HasCreatorBoxed { get; }

        public abstract Delegate? CreatorTyped { get; }

        public abstract IReadOnlyList<MemberDetail> SerializableMemberDetails { get; }

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

        private ConcurrentFactoryDictionary<TypeKey, MethodDetail?>? methodLookupsBoxed = null;
        private MethodDetail? GetMethodBoxedInternal(string name, Type[]? parameterTypes = null)
        {
            var key = new TypeKey(name, parameterTypes);
            MethodDetail? found = null;
            methodLookupsBoxed ??= new();
            var method = methodLookupsBoxed.GetOrAdd(key, (_) =>
            {
                foreach (var methodDetail in MethodDetailsBoxed)
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
        public MethodDetail GetMethodBoxed(string name, Type[]? parameterTypes = null)
        {
            var method = GetMethodBoxedInternal(name, parameterTypes);
            if (method == null)
                throw new MissingMethodException($"{Type.Name}.{name} method not found for the given parameters {(parameterTypes == null || parameterTypes.Length == 0 ? "(none)" : String.Join(",", parameterTypes.Select(x => x.GetNiceName())))}");
            return method;
        }
        public bool TryGetMethodBoxed(string name,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out MethodDetail method)
        {
            method = GetMethodBoxedInternal(name, null);
            return method != null;
        }
        public bool TryGetMethodBoxed(string name, Type[] parameterTypes,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out MethodDetail method)
        {
            method = GetMethodBoxedInternal(name, parameterTypes);
            return method != null;
        }

        private ConcurrentFactoryDictionary<TypeKey, ConstructorDetail?>? constructorLookups = null;
        private ConstructorDetail? GetConstructorBoxedInternal(Type[]? parameterTypes)
        {
            var key = new TypeKey(parameterTypes);
            ConstructorDetail? found = null;
            constructorLookups ??= new();
            var constructor = constructorLookups.GetOrAdd(key, (_) =>
            {
                foreach (var constructorDetail in ConstructorDetailsBoxed)
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
        public ConstructorDetail GetConstructorBoxed(params Type[] parameterTypes)
        {
            var constructor = GetConstructorBoxedInternal(parameterTypes);
            if (constructor == null)
                throw new MissingMethodException($"{Type.Name} constructor not found for the given parameters {String.Join(",", parameterTypes.Select(x => x.GetNiceName()))}");
            return constructor;
        }
        public bool TryGetConstructorBoxed(
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)] 
#endif
        out ConstructorDetail constructor)
        {
            constructor = GetConstructorBoxedInternal(null);
            return constructor != null;
        }
        public bool TryGetConstructorBoxed(Type[] parameterTypes,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out ConstructorDetail constructor)
        {
            constructor = GetConstructorBoxedInternal(parameterTypes);
            return constructor != null;
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

        protected readonly object locker;
        protected TypeDetail(Type type)
        {
            this.locker = new();
            this.Type = type;
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
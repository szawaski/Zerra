// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Zerra.Collections;
using Zerra.Reflection.Runtime;

namespace Zerra.Reflection
{
    public abstract class TypeDetail
    {
        public abstract bool IsGenerated { get; }

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

        private IReadOnlyList<MemberDetail>? serializableMemberDetails = null;
        public IReadOnlyList<MemberDetail> SerializableMemberDetails
        {
            get
            {
                if (serializableMemberDetails is null)
                {
                    lock (locker)
                    {
                        serializableMemberDetails ??= MemberDetails.Where(x => !x.IsStatic && x.IsBacked && IsSerializableType(x.TypeDetailBoxed)).ToArray();
                    }
                }
                return serializableMemberDetails;
            }
        }

        private Dictionary<string, MemberDetail>? membersByName = null;
        public MemberDetail GetMember(string name)
        {
            if (membersByName is null)
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
            if (membersByName is null)
            {
                lock (locker)
                {
                    membersByName ??= this.MemberDetails.ToDictionary(x => x.Name);
                }
            }
            return this.membersByName.TryGetValue(name, out member);
        }

        private ConcurrentFactoryDictionary<TypeKey, MethodDetail?>? methodLookupsBoxed = null;
        private MethodDetail? GetMethodBoxedInternal(string name, int? parameterCount, Type[]? parameterTypes)
        {
            if (parameterCount is not null && parameterTypes is not null && parameterTypes.Length != parameterCount)
                throw new InvalidOperationException($"Number of parameters does not match the specified count");

            var key = new TypeKey(name, parameterCount, parameterTypes);
            MethodDetail? found = null;
            methodLookupsBoxed ??= new();
            var method = methodLookupsBoxed.GetOrAdd(key, (_) =>
            {
                foreach (var methodDetail in MethodDetailsBoxed)
                {
                    if (SignatureCompare(name, parameterCount, parameterTypes, methodDetail))
                    {
                        if (found is not null)
                            throw new InvalidOperationException($"More than one method found for {name}");
                        found = methodDetail;
                    }
                }
                return found;
            });
            return method;
        }
        public MethodDetail GetMethodBoxed(string name)
        {
            var method = GetMethodBoxedInternal(name, null, null);
            if (method is null)
                throw new MissingMethodException($"{Type.Name}.{name} method not found");
            return method;
        }
        public MethodDetail GetMethodBoxed(string name, int parameterCount)
        {
            var method = GetMethodBoxedInternal(name, parameterCount, null);
            if (method is null)
                throw new MissingMethodException($"{Type.Name}.{name} method not found");
            return method;
        }
        public MethodDetail GetMethodBoxed(string name, Type[] parameterTypes)
        {
            var method = GetMethodBoxedInternal(name, null, parameterTypes);
            if (method is null)
                throw new MissingMethodException($"{Type.Name}.{name} method not found");
            return method;
        }
        public bool TryGetMethodBoxed(string name,
#if !NETSTANDARD2_0
    [MaybeNullWhen(false)]
#endif
        out MethodDetail method)
        {
            method = GetMethodBoxedInternal(name, null, null);
            return method is not null;
        }
        public bool TryGetMethodBoxed(string name, int parameterCount,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out MethodDetail method)
        {
            method = GetMethodBoxedInternal(name, parameterCount, null);
            return method is not null;
        }
        public bool TryGetMethodBoxed(string name, Type[] parameterTypes,
#if !NETSTANDARD2_0
    [MaybeNullWhen(false)]
#endif
        out MethodDetail method)
        {
            method = GetMethodBoxedInternal(name, null, parameterTypes);
            return method is not null;
        }

        private ConcurrentFactoryDictionary<TypeKey, ConstructorDetail?>? constructorLookups = null;
        private ConstructorDetail? GetConstructorBoxedInternal(int? parameterCount, Type[]? parameterTypes)
        {
            var key = new TypeKey(parameterTypes);
            ConstructorDetail? found = null;
            constructorLookups ??= new();
            var constructor = constructorLookups.GetOrAdd(key, (_) =>
            {
                foreach (var constructorDetail in ConstructorDetailsBoxed)
                {
                    if (SignatureCompare(parameterCount, parameterTypes, constructorDetail))
                    {
                        if (found is not null)
                            throw new InvalidOperationException($"More than one constructor found");
                        found = constructorDetail;
                    }
                }
                return found;
            });
            return constructor;
        }
        public ConstructorDetail GetConstructorBoxed()
        {
            var constructor = GetConstructorBoxedInternal(null, null);
            if (constructor is null)
                throw new MissingMethodException($"{Type.Name} constructor not found");
            return constructor;
        }
        public ConstructorDetail GetConstructorBoxed(int parameterCount)
        {
            var constructor = GetConstructorBoxedInternal(parameterCount, null);
            if (constructor is null)
                throw new MissingMethodException($"{Type.Name} constructor not found");
            return constructor;
        }
        public ConstructorDetail GetConstructorBoxed(Type[] parameterTypes)
        {
            var constructor = GetConstructorBoxedInternal(null, parameterTypes);
            if (constructor is null)
                throw new MissingMethodException($"{Type.Name} constructor not found");
            return constructor;
        }
        public bool TryGetConstructorBoxed(
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)] 
#endif
        out ConstructorDetail constructor)
        {
            constructor = GetConstructorBoxedInternal(null, null);
            return constructor is not null;
        }
        public bool TryGetConstructorBoxed(int parameterCount,
#if !NETSTANDARD2_0
    [MaybeNullWhen(false)]
#endif
        out ConstructorDetail constructor)
        {
            constructor = GetConstructorBoxedInternal(parameterCount, null);
            return constructor is not null;
        }
        public bool TryGetConstructorBoxed(Type[] parameterTypes,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out ConstructorDetail constructor)
        {
            constructor = GetConstructorBoxedInternal(null, parameterTypes);
            return constructor is not null;
        }

        private Dictionary<string, MemberDetail>? serializableMembersByNameLower = null;
        public MemberDetail GetSerializableMemberCaseInsensitive(string name)
        {
            if (serializableMembersByNameLower is null)
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
            if (serializableMembersByNameLower is null)
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
            if (membersFieldBackedByName is null)
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
            if (membersFieldBackedByName is null)
            {
                lock (locker)
                {
                    membersFieldBackedByName ??= MemberDetails.ToDictionary(x => x.Name);
                }
            }
            return this.membersFieldBackedByName.TryGetValue(name, out member);
        }

        protected readonly object locker;
        public TypeDetail(Type type)
        {
            this.locker = new();
            this.Type = type;
        }

        public override string ToString()
        {
            return $"{Type.Name}";
        }

        public TypeDetail GetRuntimeTypeDetailBoxed()
        {
            if (!this.IsGenerated)
                return this;
            var runtimeTypeDetail = TypeDetailRuntime<object>.New(this.Type);
            TypeAnalyzer.ReplaceTypeDetail(runtimeTypeDetail);
            return runtimeTypeDetail;
        }

        protected static bool SignatureCompare(string name1, int? parameterCount, Type[]? parameters1, MethodDetail methodDetail2)
        {
            if (name1 != methodDetail2.Name)
                return false;

            if (parameterCount is not null)
            {
                if (parameterCount != methodDetail2.ParameterDetails.Count)
                    return false;
            }

            if (parameters1 is not null)
            {
                if (parameters1.Length != methodDetail2.ParameterDetails.Count)
                    return false;
                for (var i = 0; i < parameters1.Length; i++)
                {
                    var type1 = parameters1[i];
                    var type2 = methodDetail2.ParameterDetails[i].Type;
                    if (type1 != type2)
                        return false;
                }
            }

            return true;
        }
        protected static bool SignatureCompare(int? parameterCount, Type[]? parameters1, ConstructorDetail constructorDetail2)
        {
            if (parameterCount is not null)
            {
                if (parameterCount != constructorDetail2.ParameterDetails.Count)
                    return false;
            }

            if (parameters1 is not null)
            {
                if (parameters1.Length != constructorDetail2.ParameterDetails.Count)
                    return false;
                for (var i = 0; i < parameters1.Length; i++)
                {
                    if (parameters1[i] != constructorDetail2.ParameterDetails[i].Type)
                        return false;
                }
            }

            return true;
        }

        protected static bool SignatureCompareForLoadMethodInfo(string name1, ParameterInfo[] parameters1, MethodDetail methodDetail2)
        {
            if (name1 != methodDetail2.Name)
                return false;

            if (parameters1.Length != methodDetail2.ParameterDetails.Count)
                return false;
            for (var i = 0; i < parameters1.Length; i++)
            {
                var parameter1 = parameters1[i];
                var parameter2 = methodDetail2.ParameterDetails[i];
                if (parameter1.Name != parameter2.Name)
                    return false;

                var type1 = parameter1.ParameterType;
                var type2 = parameter2.Type;
                if (type2 is null)
                    continue; //could not generate typeof statement, allow it to pass
                if (type1.IsGenericType && !type1.IsGenericTypeDefinition)
                    type1 = type1.GetGenericTypeDefinition();
                if (type2.IsGenericType && !type2.IsGenericTypeDefinition)
                    type2 = type2.GetGenericTypeDefinition();
                if (type1 != type2)
                    return false;
            }

            return true;
        }
        protected static bool SignatureCompareForLoadConstructorInfo(ParameterInfo[] parameters1, ConstructorDetail constructorDetail2)
        {
            if (parameters1.Length != constructorDetail2.ParameterDetails.Count)
                return false;
            for (var i = 0; i < parameters1.Length; i++)
            {
                var parameter1 = parameters1[i];
                var parameter2 = constructorDetail2.ParameterDetails[i];
                if (parameter1.Name != parameter2.Name)
                    return false;

                //constructors won't have generic arguments like methods
                if (parameter1.ParameterType != parameter2.Type)
                    return false;
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
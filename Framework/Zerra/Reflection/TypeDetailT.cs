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
    public abstract class TypeDetail<T> : TypeDetail
    {
        public abstract IReadOnlyList<MethodDetail<T>> MethodDetails { get; }

        public abstract IReadOnlyList<ConstructorDetail<T>> ConstructorDetails { get; }

        public abstract Func<T> Creator { get; }
        public abstract bool HasCreator { get; }

        public override sealed Delegate? CreatorTyped => Creator;

        private ConcurrentFactoryDictionary<TypeKey, MethodDetail<T>?>? methodLookups = null;
        private MethodDetail<T>? GetMethodInternal(string name, Type[]? parameterTypes = null)
        {
            var key = new TypeKey(name, parameterTypes);
            MethodDetail<T>? found = null;
            methodLookups ??= new();
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
        public MethodDetail<T> GetMethod(string name, Type[]? parameterTypes = null)
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
        out MethodDetail<T> method)
        {
            method = GetMethodInternal(name, null);
            return method != null;
        }
        public bool TryGetMethod(string name, Type[] parameterTypes,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out MethodDetail<T> method)
        {
            method = GetMethodInternal(name, parameterTypes);
            return method != null;
        }

        private ConcurrentFactoryDictionary<TypeKey, ConstructorDetail<T>?>? constructorLookups = null;
        private ConstructorDetail<T>? GetConstructorInternal(Type[]? parameterTypes)
        {
            var key = new TypeKey(parameterTypes);
            constructorLookups ??= new();
            var constructor = constructorLookups.GetOrAdd(key, (_) =>
            {
                foreach (var constructorDetail in ConstructorDetails)
                {
                    if (parameterTypes == null || constructorDetail.Parameters.Count == parameterTypes.Length)
                    {
                        var match = true;
                        if (parameterTypes != null)
                        {
                            for (var i = 0; i < parameterTypes.Length; i++)
                            {
                                if (parameterTypes[i].Name != constructorDetail.Parameters[i].Type.Name || parameterTypes[i].Namespace != constructorDetail.Parameters[i].Type.Namespace)
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
        public ConstructorDetail<T> GetConstructor(params Type[] parameterTypes)
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
        out ConstructorDetail<T> constructor)
        {
            constructor = GetConstructorInternal(null);
            return constructor != null;
        }
        public bool TryGetConstructor(Type[] parameterTypes,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out ConstructorDetail<T> constructor)
        {
            constructor = GetConstructorInternal(parameterTypes);
            return constructor != null;
        }

        public TypeDetail(Type type) : base(type) { }
    }
}
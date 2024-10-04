// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Zerra.Collections;
using Zerra.Reflection.Runtime;

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
        private MethodDetail<T>? GetMethodInternal(string name, int? parameterCount, Type[]? parameterTypes)
        {
            var key = new TypeKey(name, parameterTypes);
            MethodDetail<T>? found = null;
            methodLookups ??= new();
            var method = methodLookups.GetOrAdd(key, (_) =>
            {
                foreach (var methodDetail in MethodDetails)
                {
                    if (SignatureCompare(name, parameterCount, parameterTypes, methodDetail))
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
        public MethodDetail<T> GetMethod(string name)
        {
            var method = GetMethodInternal(name, null, null);
            if (method == null)
                throw new MissingMethodException($"{Type.Name}.{name} method not found");
            return method;
        }
        public MethodDetail<T> GetMethod(string name, int parameterCount)
        {
            var method = GetMethodInternal(name, parameterCount, null);
            if (method == null)
                throw new MissingMethodException($"{Type.Name}.{name} method not found");
            return method;
        }
        public MethodDetail<T> GetMethod(string name, Type[] parameterTypes)
        {
            var method = GetMethodInternal(name, null, parameterTypes);
            if (method == null)
                throw new MissingMethodException($"{Type.Name}.{name} method not found");
            return method;
        }
        public bool TryGetMethod(string name,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out MethodDetail<T> method)
        {
            method = GetMethodInternal(name, null, null);
            return method != null;
        }
        public bool TryGetMethod(string name, int parameterCount,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out MethodDetail<T> method)
        {
            method = GetMethodInternal(name, parameterCount, null);
            return method != null;
        }
        public bool TryGetMethod(string name, Type[] parameterTypes,
#if !NETSTANDARD2_0
    [MaybeNullWhen(false)]
#endif
        out MethodDetail<T> method)
        {
            method = GetMethodInternal(name, null, parameterTypes);
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
                    if (parameterTypes == null || constructorDetail.ParameterDetails.Count == parameterTypes.Length)
                    {
                        var match = true;
                        if (parameterTypes != null)
                        {
                            for (var i = 0; i < parameterTypes.Length; i++)
                            {
                                if (parameterTypes[i].Name != constructorDetail.ParameterDetails[i].Type.Name || parameterTypes[i].Namespace != constructorDetail.ParameterDetails[i].Type.Namespace)
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

        public TypeDetail<T> GetRuntimeTypeDetail()
        {
            if (!this.IsGenerated)
                return this;
            var runtimeTypeDetail = new TypeDetailRuntime<T>(this.Type);
            TypeAnalyzer.ReplaceTypeDetail(runtimeTypeDetail);
            return runtimeTypeDetail;
        }
    }
}
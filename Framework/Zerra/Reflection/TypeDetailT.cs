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
        private MethodDetail<T>[]? methodDetails = null;
        public IReadOnlyList<MethodDetail<T>> MethodDetails
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
                                        {
                                            var methodDetail = new MethodDetail<T>(method, locker);
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

        private ConstructorDetail<T>[]? constructorDetails = null;
        public IReadOnlyList<ConstructorDetail<T>> ConstructorDetails
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

        private bool creatorLoaded = false;
        private Func<T>? creator = null;
        public Func<T> Creator
        {
            get
            {
                if (!creatorLoaded)
                    LoadCreator();
                return creator ?? throw new NotSupportedException($"{nameof(TypeDetail)} {Type.Name} does not have a {nameof(Creator)}");
            }
        }
        public bool HasCreator
        {
            get
            {
                if (!creatorLoaded)
                    LoadCreator();
                return creator != null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadCreator()
        {
            lock (locker)
            {
                if (!creatorLoaded)
                {
                    if (!Type.IsAbstract && !Type.IsGenericTypeDefinition)
                    {
                        var emptyConstructor = this.ConstructorDetails.FirstOrDefault(x => x.ParametersInfo.Count == 0);
                        if (emptyConstructor != null && emptyConstructor.HasCreator)
                        {
                            creator = emptyConstructor.Creator;
                        }
                        else if (Type.IsValueType && Type.Name != "Void")
                        {
                            creator = () => { return default!; };
                        }
                        else if (Type.Name == "String")
                        {
                            creator = () => { return (T)(object)String.Empty; };
                        }
                    }
                    creatorLoaded = true;
                }
            }
        }

        public override Delegate? CreatorTyped => Creator;

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

        internal TypeDetail(Type type) : base(type) { }
    }
}
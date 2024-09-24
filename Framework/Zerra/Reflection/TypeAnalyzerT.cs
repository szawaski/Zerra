// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using Zerra.Collections;
using Zerra.Reflection.Runtime;

namespace Zerra.Reflection
{
    public static class TypeAnalyzer<T>
    {
        private static readonly object typeDetailLock = new object();
        private static TypeDetail<T>? typeDetail = null;
        public static TypeDetail<T> GetTypeDetail()
        {
            if (typeDetail is null)
            {
                lock (typeDetailLock)
                {
                    typeDetail ??= (TypeDetail<T>)TypeAnalyzer.GetTypeDetail(typeof(T));
                }
            }
            return typeDetail;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, MethodDetail<T>?> methodDetailsByType = new();
        public static MethodDetail<T> GetMethodDetail(string name, Type[]? parameterTypes = null)
        {
            var key = new TypeKey(name, parameterTypes);
            var method = methodDetailsByType.GetOrAdd(key, (_) =>
            {
                var typeDetails = GetTypeDetail();
                foreach (var methodDetail in typeDetails.MethodDetails.OrderBy(x => x.Parameters.Count))
                {
                    if (methodDetail.Name == name && (parameterTypes == null || methodDetail.Parameters.Count == parameterTypes.Length))
                    {
                        var match = true;
                        if (parameterTypes != null)
                        {
                            for (var i = 0; i < parameterTypes.Length; i++)
                            {
                                if (parameterTypes[i].Name != methodDetail.Parameters[i].Type.Name || parameterTypes[i].Namespace != methodDetail.Parameters[i].Type.Namespace)
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
            return method ?? throw new ArgumentException($"{typeof(T).GetNiceName()}.{name} method not found");
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, ConstructorDetail<T>?> constructorDetailsByType = new();
        public static ConstructorDetail<T> GetConstructorDetail(Type[]? parameterTypes = null)
        {
            var key = new TypeKey(parameterTypes);
            var constructor = constructorDetailsByType.GetOrAdd(key, (_) =>
            {
                var typeDetails = GetTypeDetail();
                foreach (var constructorDetail in typeDetails.ConstructorDetails)
                {
                    if (parameterTypes == null || constructorDetail.Parameters.Count == parameterTypes.Length)
                    {
                        var match = true;
                        if (parameterTypes != null)
                        {
                            for (var i = 0; i < parameterTypes.Length; i++)
                            {
                                if (parameterTypes[i] != constructorDetail.Parameters[i].Type)
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
            return constructor ?? throw new ArgumentException($"{typeof(T).GetNiceName()} constructor not found");
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, MethodDetail<T>> genericMethodDetails = new();
        public static MethodDetail<T> GetGenericMethodDetail(MethodDetail<T> methodDetail, params Type[] types)
        {
            var key = new TypeKey(methodDetail.MethodInfo.ToString(), types);
            var genericMethod = genericMethodDetails.GetOrAdd(key, (_) =>
            {
                return new MethodDetailRuntime<T>(methodDetail.MethodInfo, types);
            });
            return genericMethod;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, TypeDetail<T>> genericTypeDetailsByType = new();
        public static TypeDetail<T> GetGenericTypeDetail(params Type[] types)
        {
            var key = new TypeKey(types);
            var genericTypeDetail = genericTypeDetailsByType.GetOrAdd(key, (_) =>
            {
                return new TypeDetailRuntime<T>(typeof(T).MakeGenericType(types));
            });
            return genericTypeDetail;
        }
    }
}
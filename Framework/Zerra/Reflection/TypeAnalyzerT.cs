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
            var method = methodDetailsByType.GetOrAdd(key, name, parameterTypes, static (name, parameterTypes) => (MethodDetail<T>)TypeAnalyzer.GetMethodDetail(typeof(T), name, parameterTypes));
            return method ?? throw new ArgumentException($"{typeof(T).GetNiceName()}.{name} method not found");
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, ConstructorDetail<T>?> constructorDetailsByType = new();
        public static ConstructorDetail<T> GetConstructorDetail(Type[]? parameterTypes = null)
        {
            var key = new TypeKey(parameterTypes);
            var constructor = constructorDetailsByType.GetOrAdd(key, parameterTypes, static (parameterTypes) => (ConstructorDetail<T>)TypeAnalyzer.GetConstructorDetail(typeof(T), parameterTypes));
            return constructor ?? throw new ArgumentException($"{typeof(T).GetNiceName()} constructor not found");
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, MethodDetail<T>> genericMethodDetails = new();
        public static MethodDetail<T> GetGenericMethodDetail(MethodDetail<T> methodDetail, params Type[] types)
        {
            var key = new TypeKey(methodDetail.MethodInfo.ToString(), types);
            var genericMethod = genericMethodDetails.GetOrAdd(key, methodDetail, types, static (methodDetail, types) => (MethodDetail<T>)TypeAnalyzer.GetGenericMethodDetail(methodDetail, types));
            return genericMethod;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, TypeDetail<T>> genericTypeDetailsByType = new();
        public static TypeDetail<T> GetGenericTypeDetail(params Type[] types)
        {
            var key = new TypeKey(types);
            var genericTypeDetail = genericTypeDetailsByType.GetOrAdd(key, types, static (types) => (TypeDetail<T>)TypeAnalyzer.GetGenericTypeDetail(typeof(T), types));
            return genericTypeDetail;
        }
    }
}
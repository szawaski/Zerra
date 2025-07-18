// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using Zerra.Collections;

namespace Zerra.Reflection
{
    public static class Instantiator
    {
        private static readonly ConcurrentFactoryDictionary<Type, object> singleInstancesByType = new();

        public static T GetSingle<T>()
            where T : class
        {
            var type = typeof(T);
            var instance = (T)singleInstancesByType.GetOrAdd(type, Create);
            return instance;
        }
        public static T GetSingle<T>(Func<T> factory)
            where T : class
        {
            var type = typeof(T);
            var instance = (T)singleInstancesByType.GetOrAdd(type, factory);
            return instance;
        }
        public static T GetSingle<T, TArg1>(TArg1 arg1, Func<TArg1, T> factory)
            where T : class
        {
            var type = typeof(T);
            var instance = (T)singleInstancesByType.GetOrAdd(type, arg1, factory);
            return instance;
        }
        public static T GetSingle<T, TArg1, TArg2>(TArg1 arg1, TArg2 arg2, Func<TArg1, TArg2, T> factory)
            where T : class
        {
            var type = typeof(T);
            var instance = (T)singleInstancesByType.GetOrAdd(type, arg1, arg2, factory);
            return instance;
        }
        public static T GetSingle<T, TArg1, TArg2, TArg3>(TArg1 arg1, TArg2 arg2, TArg3 arg3, Func<TArg1, TArg2, TArg3, T> factory)
            where T : class
        {
            var type = typeof(T);
            var instance = (T)singleInstancesByType.GetOrAdd(type, arg1, arg2, arg3, factory);
            return instance;
        }
        public static T GetSingle<T, TArg1, TArg2, TArg3, TArg4>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, Func<TArg1, TArg2, TArg3, TArg4, T> factory)
            where T : class
        {
            var type = typeof(T);
            var instance = (T)singleInstancesByType.GetOrAdd(type, arg1, arg2, arg3, arg4, factory);
            return instance;
        }
        public static T GetSingle<T, TArg1, TArg2, TArg3, TArg4, TArg5>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, Func<TArg1, TArg2, TArg3, TArg4, TArg5, T> factory)
            where T : class
        {
            var type = typeof(T);
            var instance = (T)singleInstancesByType.GetOrAdd(type, arg1, arg2, arg3, arg4, arg5, factory);
            return instance;
        }

        public static void SetSingle<T>(T value)
        {
            var type = typeof(T);
            singleInstancesByType[type] = value;
        }

        public static object GetSingle(Type type)
        {
            if (!type.IsClass)
                throw new ArgumentException("Must be a class", nameof(type));
            var instance = singleInstancesByType.GetOrAdd(type, Create);
            return instance;
        }
        public static object GetSingle<TArg1>(Type type, TArg1 arg1, Func<TArg1, object> factory)
        {
            if (!type.IsClass)
                throw new ArgumentException("Must be a class", nameof(type));
            var instance = singleInstancesByType.GetOrAdd(type, arg1, factory);
            return instance;
        }
        public static object GetSingle<TArg1, TArg2>(Type type, TArg1 arg1, TArg2 arg2, Func<TArg1, TArg2, object> factory)
        {
            if (!type.IsClass)
                throw new ArgumentException("Must be a class", nameof(type));
            var instance = singleInstancesByType.GetOrAdd(type, arg1, arg2, factory);
            return instance;
        }
        public static object GetSingle<TArg1, TArg2, TArg3>(Type type, TArg1 arg1, TArg2 arg2, TArg3 arg3, Func<TArg1, TArg2, TArg3, object> factory)
        {
            if (!type.IsClass)
                throw new ArgumentException("Must be a class", nameof(type));
            var instance = singleInstancesByType.GetOrAdd(type, arg1, arg2, arg3, factory);
            return instance;
        }
        public static object GetSingle<TArg1, TArg2, TArg3, TArg4>(Type type, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, Func<TArg1, TArg2, TArg3, TArg4, object> factory)
        {
            if (!type.IsClass)
                throw new ArgumentException("Must be a class", nameof(type));
            var instance = singleInstancesByType.GetOrAdd(type, arg1, arg2, arg3, arg4, factory);
            return instance;
        }
        public static object GetSingle<TArg1, TArg2, TArg3, TArg4, TArg5>(Type type, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, Func<TArg1, TArg2, TArg3, TArg4, TArg5, object> factory)
        {
            if (!type.IsClass)
                throw new ArgumentException("Must be a class", nameof(type));
            var instance = singleInstancesByType.GetOrAdd(type, arg1, arg2, arg3, arg4, arg5, factory);
            return instance;
        }

        public static void SetSingle(Type type, object value)
        {
            singleInstancesByType[type] = value;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object?[]?, object>?> creatorsByType = new();
        public static T Create<T>() => (T)Create(typeof(T), null, null);
        public static T Create<T>(Type[]? parameterTypes, params object?[]? args) => (T)Create(typeof(T), parameterTypes, args);
        public static object Create(Type type) => Create(type, null, null);
        public static object Create(Type type, Type[]? parameterTypes, params object?[]? args) => GetCreator(type, parameterTypes)(args);
        private static Func<object?[]?, object> GetCreator(Type type, Type[]? parameterTypes)
        {
            var key = new TypeKey(type, parameterTypes);
            var creator = creatorsByType.GetOrAdd(key, GetCreator);
            if (creator is null)
                throw new MissingMethodException($"Constructor for {type.GetNiceName()} not available for the given parameters {(parameterTypes is null || parameterTypes.Length == 0 ? "(none)" : String.Join(",", parameterTypes.Select(x => x.GetNiceName())))}");

            return creator;
        }
        private static Func<object?[]?, object>? GetCreator(TypeKey typeKey)
        {
            var type = typeKey.Type1!;
            var parameterTypes = typeKey.TypeArray!;
            var typeDetail = TypeAnalyzer.GetTypeDetail(type);
            foreach (var constructorDetail in typeDetail.ConstructorDetailsBoxed)
            {
                if (!constructorDetail.HasCreatorWithArgsBoxed)
                    continue;
                if (constructorDetail.ParameterDetails.Count != (parameterTypes?.Length ?? 0))
                    continue;

                if (parameterTypes is null || parameterTypes.Length == 0)
                {
                    return constructorDetail.CreatorWithArgsBoxed;
                }
                else
                {
                    var match = true;
                    for (var i = 0; i < parameterTypes.Length; i++)
                    {
                        if (parameterTypes[i] != constructorDetail.ParameterDetails[i].Type)
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        return constructorDetail.CreatorWithArgsBoxed;
                    }
                }
            }
            return null;
        }
    }
}

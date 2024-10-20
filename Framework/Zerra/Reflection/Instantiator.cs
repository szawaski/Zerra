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
        private static readonly ConcurrentFactoryDictionary<string, object> singleInstancesByKey = new();
        public static T GetSingle<T>(Func<T>? factory = null)
            where T : class
        {
            var type = typeof(T);
            T instance;
            if (factory is null)
                instance = (T)singleInstancesByType.GetOrAdd(type, Create);
            else
                instance = (T)singleInstancesByType.GetOrAdd(type, factory);
            return instance;
        }
        public static object GetSingle(Type type, Func<object>? factory = null)
        {
            if (!type.IsClass)
                throw new ArgumentException("Must be a class", nameof(type));
            object instance;
            if (factory is null)
                instance = singleInstancesByType.GetOrAdd(type, Create);
            else
                instance = singleInstancesByType.GetOrAdd(type, factory);
            return instance;
        }
        public static object GetSingle(string key, Func<object> factory)
        {
            var instance = singleInstancesByKey.GetOrAdd(key, factory);
            return instance;
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

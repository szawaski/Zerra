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
            factory ??= Create<T>;
            var instance = (T)singleInstancesByType.GetOrAdd(type, (_) => { return factory(); });
            return instance;
        }
        public static object GetSingle(Type type, Func<object>? factory = null)
        {
            if (!type.IsClass)
                throw new ArgumentException("Must be a class", nameof(type));
            factory ??= () => { return Create(type); };
            var instance = singleInstancesByType.GetOrAdd(type, (_) => { return factory(); });
            return instance;
        }
        public static object GetSingle(string key, Func<object> factory)
        {
            var instance = singleInstancesByKey.GetOrAdd(key, (_) => { return factory.Invoke(); });
            return instance;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object?[]?, object>?> creatorsByType = new();
        public static T Create<T>() { return (T)Create(typeof(T), null, null); }
        public static T Create<T>(Type[]? parameterTypes, params object?[]? args) { return (T)Create(typeof(T), parameterTypes, args); }
        public static object Create(Type type) { return Create(type, null, null); }
        public static object Create(Type type, Type[]? parameterTypes, params object?[]? args)
        {
            return GetCreator(type, parameterTypes)(args);
        }
        private static Func<object?[]?, object> GetCreator(Type type, Type[]? parameterTypes)
        {
            var key = new TypeKey(type, parameterTypes);
            var creator = creatorsByType.GetOrAdd(key, (_) =>
            {
                var typeDetail = TypeAnalyzer.GetTypeDetail(type);
                foreach (var constructorDetail in typeDetail.ConstructorDetailsBoxed)
                {
                    if (!constructorDetail.HasCreatorWithArgsBoxed)
                        continue;
                    if (constructorDetail.ParameterDetails.Count != (parameterTypes?.Length ?? 0))
                        continue;

                    if (parameterTypes == null || parameterTypes.Length == 0)
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
            });
            if (creator == null)
                throw new MissingMethodException($"Constructor for {type.GetNiceName()} not available for the given parameters {(parameterTypes == null || parameterTypes.Length == 0 ? "(none)" : String.Join(",", parameterTypes.Select(x => x.GetNiceName())))}");

            return creator;
        }
    }
}

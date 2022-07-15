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
        public static T GetSingleInstance<T>(Func<T> factory = null)
        {
            var type = typeof(T);
            if (factory == null)
                factory = CreateInstance<T>;
            var instance = (T)singleInstancesByType.GetOrAdd(type, (key) => { return factory(); });
            return instance;
        }
        public static object GetSingleInstance(Type type, Func<object> factory = null)
        {
            if (factory == null)
                factory = () => { return CreateInstance(type); };
            var instance = singleInstancesByType.GetOrAdd(type, (key) => { return factory(); });
            return instance;
        }
        public static object GetSingleInstance(string key, Func<object> factory)
        {
            var instance = singleInstancesByKey.GetOrAdd(key, (factoryKey) => { return factory.Invoke(); });
            return instance;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object[], object>> creatorsByType = new();
        public static T CreateInstance<T>() { return (T)CreateInstance(typeof(T), Type.EmptyTypes, null); }
        public static T CreateInstance<T>(Type[] parameterTypes, params object[] args) { return (T)CreateInstance(typeof(T), parameterTypes, args); }
        public static object CreateInstance(Type type) { return CreateInstance(type, Type.EmptyTypes, null); }
        public static object CreateInstance(Type type, Type[] parameterTypes, params object[] args)
        {
            return GetCreator(type, parameterTypes)(args);
        }
        public static Func<object[], object> GetCreator(Type type, Type[] parameterTypes)
        {
            var key = new TypeKey(type, parameterTypes);
            var creator = creatorsByType.GetOrAdd(key, (keyArg) =>
            {
                var typeDetail = TypeAnalyzer.GetTypeDetail(type);
                if (parameterTypes.Length == 0)
                {
                    if (typeDetail.Creator == null)
                        return null;
                    Func<object[], object> c = (a) => { return typeDetail.Creator(); };
                    return c;
                }
                foreach (var constructorDetail in typeDetail.ConstructorDetails)
                {
                    if (constructorDetail.ParametersInfo.Count == parameterTypes.Length)
                    {
                        var match = true;
                        if (parameterTypes != null)
                        {
                            for (var i = 0; i < parameterTypes.Length; i++)
                            {
                                if (parameterTypes[i] != constructorDetail.ParametersInfo[i].ParameterType)
                                {
                                    match = false;
                                    break;
                                }
                            }
                        }
                        if (match)
                        {
                            return constructorDetail.Creator;
                        }
                    }
                }
                return null;
            });
            if (creator == null)
                throw new MissingMethodException($"Constructor for {type.GetNiceName()} not available for the given parameters {String.Join(",", parameterTypes.Select(x => x.GetNiceName()))}");

            return creator;
        }

        //private static class LinqObjectFactory
        //{
        //    private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object[], object>> creators = new ConcurrentFactoryDictionary<TypeKey, Func<object[], object>>();
        //    public static Func<object[], object> GetCreator(Type type, Type[] parameterTypes)
        //    {
        //        var key = new TypeKey(type, parameterTypes);
        //        var instantiator = creators.GetOrAdd(key, (k) =>
        //        {
        //            if (type.IsValueType)
        //            {
        //                var constantExpression = Expression.Constant(Activator.CreateInstance(type), typeof(object));
        //                var parameterExpression = Expression.Parameter(typeof(object[]), "args");
        //                var lambda = Expression.Lambda<Func<object[], object>>(constantExpression, new ParameterExpression[] { parameterExpression }).Compile();
        //                return lambda;
        //            }
        //            else
        //            {
        //                var constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, parameterTypes, null);
        //                if (constructor == null)
        //                    throw new Exception($"Constructor for {type.GetNiceName()} not found for parameter types {String.Join(",", parameterTypes.Select(x => x.GetNiceName()))}");

        //                var parameterInfos = constructor.GetParameters();
        //                var parameterExpression = Expression.Parameter(typeof(object[]), "args");

        //                var argExpressions = new Expression[parameterTypes.Length];
        //                for (var i = 0; i < parameterTypes.Length; i++)
        //                {
        //                    var indexedAcccess = Expression.ArrayIndex(parameterExpression, Expression.Constant(i));

        //                    if (!parameterTypes[i].IsClass && !parameterTypes[i].IsInterface)
        //                    {
        //                        var localVariable = Expression.Variable(parameterTypes[i], "arg" + i);

        //                        var block = Expression.Block(new[] { localVariable },
        //                                Expression.IfThenElse(Expression.Equal(indexedAcccess, Expression.Constant(null)),
        //                                    Expression.Assign(localVariable, Expression.Default(parameterTypes[i])),
        //                                    Expression.Assign(localVariable, Expression.Convert(indexedAcccess, parameterTypes[i]))
        //                                ),
        //                                localVariable
        //                            );

        //                        argExpressions[i] = block;

        //                    }
        //                    else
        //                    {
        //                        argExpressions[i] = Expression.Convert(indexedAcccess, parameterTypes[i]);
        //                    }
        //                }
        //                var newExpression = Expression.New(constructor, argExpressions);
        //                var lambda = Expression.Lambda<Func<object[], object>>(newExpression, new ParameterExpression[] { parameterExpression }).Compile();
        //                return lambda;
        //            }
        //        });

        //        return instantiator;
        //    }
        //}
    }
}

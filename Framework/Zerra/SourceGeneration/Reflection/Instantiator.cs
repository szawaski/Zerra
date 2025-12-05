// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using Zerra.Collections;

namespace Zerra.SourceGeneration.Reflection
{
    /// <summary>
    /// Provides singleton and factory pattern instantiation capabilities for creating and caching instances of types.
    /// Supports both generic and non-generic object creation with optional factory functions and custom arguments.
    /// </summary>
    /// <remarks>
    /// Uses a factory dictionary to cache and reuse creator functions for improved performance on repeated instantiations.
    /// Singleton instances are cached per type and can be manually set or retrieved.
    /// </remarks>
    [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
    public static class Instantiator
    {
        private static readonly ConcurrentFactoryDictionary<Type, object> singleInstancesByType = new();

        /// <summary>
        /// Gets or creates a singleton instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to instantiate. Must be a class.</typeparam>
        /// <returns>A singleton instance of type T.</returns>
        public static T GetSingle<T>()
            where T : class
        {
            var type = typeof(T);
            var instance = (T)singleInstancesByType.GetOrAdd(type, Create);
            return instance;
        }
        /// <summary>
        /// Gets or creates a singleton instance of the specified type using a provided factory function.
        /// </summary>
        /// <typeparam name="T">The type to instantiate. Must be a class.</typeparam>
        /// <param name="factory">A factory function to create the instance if one does not already exist.</param>
        /// <returns>A singleton instance of type T.</returns>
        public static T GetSingle<T>(Func<T> factory)
            where T : class
        {
            var type = typeof(T);
            var instance = (T)singleInstancesByType.GetOrAdd(type, factory);
            return instance;
        }
        /// <summary>
        /// Gets or creates a singleton instance of the specified type using a provided factory function with one argument.
        /// </summary>
        /// <typeparam name="T">The type to instantiate. Must be a class.</typeparam>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <param name="arg1">The first argument to pass to the factory function.</param>
        /// <param name="factory">A factory function to create the instance if one does not already exist.</param>
        /// <returns>A singleton instance of type T.</returns>
        public static T GetSingle<T, TArg1>(TArg1 arg1, Func<TArg1, T> factory)
            where T : class
        {
            var type = typeof(T);
            var instance = (T)singleInstancesByType.GetOrAdd(type, arg1, factory);
            return instance;
        }
        /// <summary>
        /// Gets or creates a singleton instance of the specified type using a provided factory function with two arguments.
        /// </summary>
        /// <typeparam name="T">The type to instantiate. Must be a class.</typeparam>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second factory argument.</typeparam>
        /// <param name="arg1">The first argument to pass to the factory function.</param>
        /// <param name="arg2">The second argument to pass to the factory function.</param>
        /// <param name="factory">A factory function to create the instance if one does not already exist.</param>
        /// <returns>A singleton instance of type T.</returns>
        public static T GetSingle<T, TArg1, TArg2>(TArg1 arg1, TArg2 arg2, Func<TArg1, TArg2, T> factory)
            where T : class
        {
            var type = typeof(T);
            var instance = (T)singleInstancesByType.GetOrAdd(type, arg1, arg2, factory);
            return instance;
        }
        /// <summary>
        /// Gets or creates a singleton instance of the specified type using a provided factory function with three arguments.
        /// </summary>
        /// <typeparam name="T">The type to instantiate. Must be a class.</typeparam>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second factory argument.</typeparam>
        /// <typeparam name="TArg3">The type of the third factory argument.</typeparam>
        /// <param name="arg1">The first argument to pass to the factory function.</param>
        /// <param name="arg2">The second argument to pass to the factory function.</param>
        /// <param name="arg3">The third argument to pass to the factory function.</param>
        /// <param name="factory">A factory function to create the instance if one does not already exist.</param>
        /// <returns>A singleton instance of type T.</returns>
        public static T GetSingle<T, TArg1, TArg2, TArg3>(TArg1 arg1, TArg2 arg2, TArg3 arg3, Func<TArg1, TArg2, TArg3, T> factory)
            where T : class
        {
            var type = typeof(T);
            var instance = (T)singleInstancesByType.GetOrAdd(type, arg1, arg2, arg3, factory);
            return instance;
        }
        /// <summary>
        /// Gets or creates a singleton instance of the specified type using a provided factory function with four arguments.
        /// </summary>
        /// <typeparam name="T">The type to instantiate. Must be a class.</typeparam>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second factory argument.</typeparam>
        /// <typeparam name="TArg3">The type of the third factory argument.</typeparam>
        /// <typeparam name="TArg4">The type of the fourth factory argument.</typeparam>
        /// <param name="arg1">The first argument to pass to the factory function.</param>
        /// <param name="arg2">The second argument to pass to the factory function.</param>
        /// <param name="arg3">The third argument to pass to the factory function.</param>
        /// <param name="arg4">The fourth argument to pass to the factory function.</param>
        /// <param name="factory">A factory function to create the instance if one does not already exist.</param>
        /// <returns>A singleton instance of type T.</returns>
        public static T GetSingle<T, TArg1, TArg2, TArg3, TArg4>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, Func<TArg1, TArg2, TArg3, TArg4, T> factory)
            where T : class
        {
            var type = typeof(T);
            var instance = (T)singleInstancesByType.GetOrAdd(type, arg1, arg2, arg3, arg4, factory);
            return instance;
        }
        /// <summary>
        /// Gets or creates a singleton instance of the specified type using a provided factory function with five arguments.
        /// </summary>
        /// <typeparam name="T">The type to instantiate. Must be a class.</typeparam>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second factory argument.</typeparam>
        /// <typeparam name="TArg3">The type of the third factory argument.</typeparam>
        /// <typeparam name="TArg4">The type of the fourth factory argument.</typeparam>
        /// <typeparam name="TArg5">The type of the fifth factory argument.</typeparam>
        /// <param name="arg1">The first argument to pass to the factory function.</param>
        /// <param name="arg2">The second argument to pass to the factory function.</param>
        /// <param name="arg3">The third argument to pass to the factory function.</param>
        /// <param name="arg4">The fourth argument to pass to the factory function.</param>
        /// <param name="arg5">The fifth argument to pass to the factory function.</param>
        /// <param name="factory">A factory function to create the instance if one does not already exist.</param>
        /// <returns>A singleton instance of type T.</returns>
        public static T GetSingle<T, TArg1, TArg2, TArg3, TArg4, TArg5>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, Func<TArg1, TArg2, TArg3, TArg4, TArg5, T> factory)
            where T : class
        {
            var type = typeof(T);
            var instance = (T)singleInstancesByType.GetOrAdd(type, arg1, arg2, arg3, arg4, arg5, factory);
            return instance;
        }

        /// <summary>
        /// Sets the singleton instance for the specified type.
        /// </summary>
        /// <typeparam name="T">The type to set the singleton for. Must not be null.</typeparam>
        /// <param name="value">The singleton instance to cache.</param>
        public static void SetSingle<T>(T value)
            where T : notnull
        {
            var type = typeof(T);
            singleInstancesByType[type] = value;
        }

        /// <summary>
        /// Gets or creates a singleton instance of the specified type.
        /// </summary>
        /// <param name="type">The type to instantiate. Must be a class.</param>
        /// <returns>A singleton instance of the specified type.</returns>
        /// <exception cref="ArgumentException">Thrown when type is not a class.</exception>
        public static object GetSingle(Type type)
        {
            if (!type.IsClass)
                throw new ArgumentException("Must be a class", nameof(type));
            var instance = singleInstancesByType.GetOrAdd(type, Create);
            return instance;
        }
        /// <summary>
        /// Gets or creates a singleton instance of the specified type using a provided factory function with one argument.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <param name="type">The type to instantiate. Must be a class.</param>
        /// <param name="arg1">The first argument to pass to the factory function.</param>
        /// <param name="factory">A factory function to create the instance if one does not already exist.</param>
        /// <returns>A singleton instance of the specified type.</returns>
        /// <exception cref="ArgumentException">Thrown when type is not a class.</exception>
        public static object GetSingle<TArg1>(Type type, TArg1 arg1, Func<TArg1, object> factory)
        {
            if (!type.IsClass)
                throw new ArgumentException("Must be a class", nameof(type));
            var instance = singleInstancesByType.GetOrAdd(type, arg1, factory);
            return instance;
        }
        /// <summary>
        /// Gets or creates a singleton instance of the specified type using a provided factory function with two arguments.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second factory argument.</typeparam>
        /// <param name="type">The type to instantiate. Must be a class.</param>
        /// <param name="arg1">The first argument to pass to the factory function.</param>
        /// <param name="arg2">The second argument to pass to the factory function.</param>
        /// <param name="factory">A factory function to create the instance if one does not already exist.</param>
        /// <returns>A singleton instance of the specified type.</returns>
        /// <exception cref="ArgumentException">Thrown when type is not a class.</exception>
        public static object GetSingle<TArg1, TArg2>(Type type, TArg1 arg1, TArg2 arg2, Func<TArg1, TArg2, object> factory)
        {
            if (!type.IsClass)
                throw new ArgumentException("Must be a class", nameof(type));
            var instance = singleInstancesByType.GetOrAdd(type, arg1, arg2, factory);
            return instance;
        }
        /// <summary>
        /// Gets or creates a singleton instance of the specified type using a provided factory function with three arguments.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second factory argument.</typeparam>
        /// <typeparam name="TArg3">The type of the third factory argument.</typeparam>
        /// <param name="type">The type to instantiate. Must be a class.</param>
        /// <param name="arg1">The first argument to pass to the factory function.</param>
        /// <param name="arg2">The second argument to pass to the factory function.</param>
        /// <param name="arg3">The third argument to pass to the factory function.</param>
        /// <param name="factory">A factory function to create the instance if one does not already exist.</param>
        /// <returns>A singleton instance of the specified type.</returns>
        /// <exception cref="ArgumentException">Thrown when type is not a class.</exception>
        public static object GetSingle<TArg1, TArg2, TArg3>(Type type, TArg1 arg1, TArg2 arg2, TArg3 arg3, Func<TArg1, TArg2, TArg3, object> factory)
        {
            if (!type.IsClass)
                throw new ArgumentException("Must be a class", nameof(type));
            var instance = singleInstancesByType.GetOrAdd(type, arg1, arg2, arg3, factory);
            return instance;
        }
        /// <summary>
        /// Gets or creates a singleton instance of the specified type using a provided factory function with four arguments.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second factory argument.</typeparam>
        /// <typeparam name="TArg3">The type of the third factory argument.</typeparam>
        /// <typeparam name="TArg4">The type of the fourth factory argument.</typeparam>
        /// <param name="type">The type to instantiate. Must be a class.</param>
        /// <param name="arg1">The first argument to pass to the factory function.</param>
        /// <param name="arg2">The second argument to pass to the factory function.</param>
        /// <param name="arg3">The third argument to pass to the factory function.</param>
        /// <param name="arg4">The fourth argument to pass to the factory function.</param>
        /// <param name="factory">A factory function to create the instance if one does not already exist.</param>
        /// <returns>A singleton instance of the specified type.</returns>
        /// <exception cref="ArgumentException">Thrown when type is not a class.</exception>
        public static object GetSingle<TArg1, TArg2, TArg3, TArg4>(Type type, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, Func<TArg1, TArg2, TArg3, TArg4, object> factory)
        {
            if (!type.IsClass)
                throw new ArgumentException("Must be a class", nameof(type));
            var instance = singleInstancesByType.GetOrAdd(type, arg1, arg2, arg3, arg4, factory);
            return instance;
        }
        /// <summary>
        /// Gets or creates a singleton instance of the specified type using a provided factory function with five arguments.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second factory argument.</typeparam>
        /// <typeparam name="TArg3">The type of the third factory argument.</typeparam>
        /// <typeparam name="TArg4">The type of the fourth factory argument.</typeparam>
        /// <typeparam name="TArg5">The type of the fifth factory argument.</typeparam>
        /// <param name="type">The type to instantiate. Must be a class.</param>
        /// <param name="arg1">The first argument to pass to the factory function.</param>
        /// <param name="arg2">The second argument to pass to the factory function.</param>
        /// <param name="arg3">The third argument to pass to the factory function.</param>
        /// <param name="arg4">The fourth argument to pass to the factory function.</param>
        /// <param name="arg5">The fifth argument to pass to the factory function.</param>
        /// <param name="factory">A factory function to create the instance if one does not already exist.</param>
        /// <returns>A singleton instance of the specified type.</returns>
        /// <exception cref="ArgumentException">Thrown when type is not a class.</exception>
        public static object GetSingle<TArg1, TArg2, TArg3, TArg4, TArg5>(Type type, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, Func<TArg1, TArg2, TArg3, TArg4, TArg5, object> factory)
        {
            if (!type.IsClass)
                throw new ArgumentException("Must be a class", nameof(type));
            var instance = singleInstancesByType.GetOrAdd(type, arg1, arg2, arg3, arg4, arg5, factory);
            return instance;
        }

        /// <summary>
        /// Sets the singleton instance for the specified type.
        /// </summary>
        /// <param name="type">The type to set the singleton for.</param>
        /// <param name="value">The singleton instance to cache.</param>
        public static void SetSingle(Type type, object value)
        {
            singleInstancesByType[type] = value;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, Func<object?[]?, object>?> creatorsByType = new();
        /// <summary>
        /// Creates a new instance of the specified type using the parameterless constructor.
        /// </summary>
        /// <typeparam name="T">The type to instantiate.</typeparam>
        /// <returns>A new instance of type T.</returns>
        /// <exception cref="MissingMethodException">Thrown when no suitable constructor is found.</exception>
        public static T Create<T>() => (T)Create(typeof(T), null, null);
        /// <summary>
        /// Creates a new instance of the specified type using a constructor matching the specified parameter types.
        /// </summary>
        /// <typeparam name="T">The type to instantiate.</typeparam>
        /// <param name="parameterTypes">The types of the constructor parameters, or null for parameterless constructor.</param>
        /// <param name="args">The arguments to pass to the constructor.</param>
        /// <returns>A new instance of type T.</returns>
        /// <exception cref="MissingMethodException">Thrown when no suitable constructor is found.</exception>
        public static T Create<T>(Type[]? parameterTypes, params object?[]? args) => (T)Create(typeof(T), parameterTypes, args);
        /// <summary>
        /// Creates a new instance of the specified type using the parameterless constructor.
        /// </summary>
        /// <param name="type">The type to instantiate.</param>
        /// <returns>A new instance of the specified type.</returns>
        /// <exception cref="MissingMethodException">Thrown when no suitable constructor is found.</exception>
        public static object Create(Type type) => Create(type, null, null);
        /// <summary>
        /// Creates a new instance of the specified type using a constructor matching the specified parameter types.
        /// </summary>
        /// <param name="type">The type to instantiate.</param>
        /// <param name="parameterTypes">The types of the constructor parameters, or null for parameterless constructor.</param>
        /// <param name="args">The arguments to pass to the constructor.</param>
        /// <returns>A new instance of the specified type.</returns>
        /// <exception cref="MissingMethodException">Thrown when no suitable constructor is found.</exception>
        public static object Create(Type type, Type[]? parameterTypes, params object?[]? args) => GetCreator(type, parameterTypes)(args);
        private static Func<object?[]?, object> GetCreator(Type type, Type[]? parameterTypes)
        {
            var key = new TypeKey(type, parameterTypes);
            var creator = creatorsByType.GetOrAdd(key, GetCreator);
            if (creator is null)
                throw new MissingMethodException($"Constructor for {type.Name} not available for the given parameters {(parameterTypes is null || parameterTypes.Length == 0 ? "(none)" : String.Join(",", parameterTypes.Select(x => x.Name)))}");

            return creator;
        }
        private static Func<object?[]?, object>? GetCreator(TypeKey typeKey)
        {
            var type = typeKey.Type1!;
            var parameterTypes = typeKey.TypeArray!;
            var typeDetail = TypeAnalyzer.GetTypeDetail(type);
            foreach (var constructorDetail in typeDetail.Constructors)
            {
                if (constructorDetail.Parameters.Count != (parameterTypes?.Length ?? 0))
                    continue;

                if (parameterTypes is null || parameterTypes.Length == 0)
                {
                    return constructorDetail.CreatorBoxed;
                }
                else
                {
                    var match = true;
                    for (var i = 0; i < parameterTypes.Length; i++)
                    {
                        if (parameterTypes[i] != constructorDetail.Parameters[i].Type)
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        return constructorDetail.CreatorBoxed;
                    }
                }
            }
            return null;
        }
    }
}

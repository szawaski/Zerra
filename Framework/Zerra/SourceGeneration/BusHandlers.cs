// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Collections;
using Zerra.SourceGeneration.Reflection;

namespace Zerra.SourceGeneration
{
    internal static class BusHandlers
    {
        private static readonly ConcurrentFactoryDictionary<Type, ConcurrentFactoryDictionary<string, MethodForHandler>> methodsByType = new();

        public static MethodForHandler GetMethod(Type interfaceType, string methodName)
        {
            var methods = methodsByType.GetOrAdd(interfaceType, static () => new());
            var methodForHandler = methods.GetOrAdd(methodName, interfaceType, methodName, GenerateMethodForHandler);
            return methodForHandler;
        }

        public static object? Invoke(Type interfaceType, object instance, string methodName, object[] args)
        {
            var methods = methodsByType.GetOrAdd(interfaceType, static () => new());
            var methodForHandler = methods.GetOrAdd(methodName, interfaceType, methodName, GenerateMethodForHandler);
            var result = methodForHandler.Method.Invoke(instance, args);
            return result;
        }

        public static void Register(Type interfaceType, string name, bool isTask, Type? taskInnerType, Type[] parameterTypes, Func<object, object?[]?, object?> method, Func<object, object?>? taskResult)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.Name} is not an interface");
            var methodForHandler = new MethodForHandler(interfaceType, name, isTask, taskInnerType, parameterTypes, method, taskResult);
            var methods = methodsByType.GetOrAdd(interfaceType, (key) => new());
            if (!methods.TryAdd(methodForHandler.Name, methodForHandler))
                throw new InvalidOperationException($"Method for {interfaceType.Name} is already registered");
        }

        private static MethodForHandler GenerateMethodForHandler(Type interfaceType, string methodName)
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                throw new NotSupportedException($"Cannot generate method for handler for {interfaceType.Name}.{methodName}. Dynamic code generation is not supported in this build configuration.");

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
            return BusHandlerGenerator.GenerateMethodForHandler(interfaceType, methodName);
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
        }

        public sealed class MethodForHandler
        {
            public readonly Type InterfaceType;
            public readonly string Name;
            public readonly bool IsTask;
            public readonly Type? TaskInnerType;
            public readonly IReadOnlyList<Type> ParameterTypes;
            public readonly Func<object, object?[]?, object?> Method;
            public readonly Func<object, object?>? TaskResult;
            public MethodForHandler(Type interfaceType, string name, bool isTask, Type? taskInnerType, IReadOnlyList<Type> parameterTypes, Func<object, object?[]?, object?> method, Func<object, object?>? taskResult)
            {
                this.InterfaceType = interfaceType;
                this.Name = name;
                this.IsTask = isTask;
                this.TaskInnerType = taskInnerType;
                this.ParameterTypes = parameterTypes;
                this.Method = method;
                this.TaskResult = taskResult;
            }
        }
    }
}

// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using Zerra.Reflection.Dynamic;
using static Zerra.CQRS.Reflection.BusHandlers;

namespace Zerra.CQRS.Reflection.Dynamic
{
    [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
    internal static class BusHandlerGenerator
    {
        public static MethodForHandler GenerateMethodForHandler(Type interfaceType, string methodName)
        {
            var methodNameSplit = methodName.Split('-');

            var allMethods = interfaceType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).ToList();
            foreach (var i in interfaceType.GetInterfaces())
                allMethods.AddRange(i.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance));
            var methods = allMethods.Where(x => x.Name == methodNameSplit[0]);
            if (methodNameSplit.Length > 1)
                methods = methods.Where(x => x.GetParameters().FirstOrDefault()?.ParameterType.Name == methodNameSplit[1]);

            var method = methods.FirstOrDefault() ?? throw new ArgumentException($"Method '{methodName}' not found in interface '{interfaceType.FullName}'");

            var isTask = method.ReturnType.Name == nameof(Task);
            var isTaskGeneric = method.ReturnType.Name == "Task`1";
            Type? taskInnerType = null;
            if (isTaskGeneric)
                taskInnerType = method.ReturnType.GetGenericArguments()[0];

            var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();

            Func<object, object?[]?, object?>? caller = AccessorGenerator.GenerateCaller(method);
            if (caller == null)
                throw new InvalidOperationException($"Could not generate caller for method '{method.Name}' in interface '{interfaceType.FullName}'");

            Func<object, object?>? taskResult = null;
            if (isTaskGeneric && taskInnerType != null)
            {
                var taskResultProperty = typeof(Task<>).MakeGenericType(taskInnerType).GetProperty(nameof(Task<>.Result))!;
                taskResult = AccessorGenerator.GenerateGetter(taskResultProperty);
            }

            var methodInfo = new MethodForHandler(interfaceType, method.Name, isTask || isTaskGeneric, taskInnerType, parameterTypes, caller, taskResult);
            return methodInfo;
        }
    }
}

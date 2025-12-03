// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using Zerra.Collections;
using Zerra.CQRS;
using static Zerra.SourceGeneration.BusRouters;

namespace Zerra.SourceGeneration.Reflection
{
    [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
    internal static class BusRouterGenerator
    {
        private static readonly ConstructorInfo objectConstructor = typeof(object).GetConstructor(Array.Empty<Type>()) ?? throw new Exception($"{nameof(Object)} constructor not found");
        private static readonly MethodInfo callGenericTask = typeof(IBusInternal).GetMethod(nameof(IBusInternal._CallMethodTaskGeneric))!;
        private static readonly MethodInfo callTask = typeof(IBusInternal).GetMethod(nameof(IBusInternal._CallMethodTask))!;
        private static readonly MethodInfo call = typeof(IBusInternal).GetMethod(nameof(IBusInternal._CallMethod))!;
        private static readonly MethodInfo typeOfMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)) ?? throw new Exception($"{nameof(Type)}.{nameof(Type.GetTypeFromHandle)} not found");

        public static Func<IBusInternal, string, object> GenerateBusCaller(Type interfaceType)
        {
            var callerType = GenerateBusCallerClass(interfaceType);
            var constructor = callerType.GetConstructor([typeof(IBusInternal), typeof(string)])!;
            var creator = AccessorGenerator.GenerateCreator(constructor)!;
            object caller(IBusInternal bus, string source) => creator.Invoke([bus, source])!;
            return caller;
        }
        private static Type GenerateBusCallerClass(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.Name} is not an interface");

            var methods = new List<MethodInfo>();
            methods.AddRange(interfaceType.GetMethods());
            foreach (var @interface in interfaceType.GetInterfaces())
                methods.AddRange(@interface.GetMethods());

            var typeSignature = "Caller_" + interfaceType.FullName;

            var moduleBuilder = GeneratedAssembly.GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout | TypeAttributes.Sealed, null);

            typeBuilder.AddInterfaceImplementation(interfaceType);

            var busField = typeBuilder.DefineField("bus", typeof(IBusInternal), FieldAttributes.Private | FieldAttributes.InitOnly);
            var sourceField = typeBuilder.DefineField("source", typeof(string), FieldAttributes.Private | FieldAttributes.InitOnly);

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard, [typeof(IBusInternal), typeof(string)]);
            {
                _ = constructorBuilder.DefineParameter(1, ParameterAttributes.None, "bus");
                _ = constructorBuilder.DefineParameter(2, ParameterAttributes.None, "source");

                var il = constructorBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, objectConstructor);
                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Nop);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, busField);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Stfld, sourceField);

                il.Emit(OpCodes.Ret);
            }

            foreach (var method in methods)
            {
                var returnType = method.ReturnType;
                var voidMethod = returnType.Name == "Void";
                var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();

                MethodInfo callMethod;
                if (method.ReturnType.Name == "Task`1")
                    callMethod = callGenericTask.MakeGenericMethod(method.ReturnType.GetGenericArguments()[0]);
                else if (method.ReturnType.Name == nameof(Task))
                    callMethod = callTask;
                else
                    callMethod = call.MakeGenericMethod(voidMethod ? typeof(object) : method.ReturnType);

                var methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.Standard | CallingConventions.HasThis,
                    voidMethod ? null : returnType,
                    parameterTypes);
                methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

                var il = methodBuilder.GetILGenerator();

                //_Call***(Type interfaceType, string methodName, object[] arguments, string source)

                il.Emit(OpCodes.Nop);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, busField); //bus

                il.Emit(OpCodes.Ldtoken, interfaceType);
                il.Emit(OpCodes.Call, typeOfMethod); //typeof(TInterface)

                il.Emit(OpCodes.Ldstr, method.Name); //methodName

                il.Emit(OpCodes.Ldc_I4, parameterTypes.Length);
                il.Emit(OpCodes.Newarr, typeof(object)); //new object[#]
                for (var j = 0; j < parameterTypes.Length; j++)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4, j);
                    il.Emit(OpCodes.Ldarg, j + 1);
                    if (parameterTypes[j].IsValueType)
                        il.Emit(OpCodes.Box, parameterTypes[j]);
                    il.Emit(OpCodes.Stelem_Ref);
                }

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, sourceField); //source

                il.Emit(OpCodes.Callvirt, callMethod);

                if (voidMethod)
                {
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Ret);
                }
                else
                {
                    il.Emit(OpCodes.Ret);
                }

                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }

            Type objectType = typeBuilder.CreateTypeInfo() ?? throw new Exception("Failed to CreateTypeInfo");
            return objectType;
        }

        private static readonly MethodInfo dispatchCommandWithResultInternalAsyncMethod = typeof(IBusInternal).GetMethod(nameof(IBusInternal._DispatchCommandWithResultInternalAsync))!;
        private static readonly ConcurrentFactoryDictionary<Type, MethodInfo> dispatchCommandWithResultInternalAsyncMethods = new();
        public static DispatchToBus GenerateBusDispatcher(Type commandType)
        {
            static Task method(IBusInternal bus, ICommand command, Type commandType, string source, CancellationToken cancellationToken)
            {
                var method = dispatchCommandWithResultInternalAsyncMethods.GetOrAdd(commandType, static (commandType) =>
                {
                    var taskInnerType = commandType.GetInterface("ICommand`1")!.GetGenericArguments()[0];
                    return dispatchCommandWithResultInternalAsyncMethod.MakeGenericMethod(taskInnerType);
                });
                var task = (Task)method.Invoke(bus, [command, commandType, source, cancellationToken])!;
                return task;
            }
            var taskInnerType = commandType.GetInterface("ICommand`1")!.GetGenericArguments()[0];
            var taskResultProperty = typeof(Task<>).MakeGenericType(taskInnerType).GetProperty(nameof(Task<>.Result))!;
            var taskResult = AccessorGenerator.GenerateGetter(taskResultProperty)!;

            var dispatchToBus = new DispatchToBus(method, taskResult);
            return dispatchToBus;
        }
    }
}

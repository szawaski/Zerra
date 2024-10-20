// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra.CQRS
{
    internal static class BusRouters
    {
        private static readonly ConcurrentFactoryDictionary<Type, Type> callerClasses = new();
        public static object GetProviderToCallMethodInternalInstance(Type interfaceType, NetworkType networkType, string source)
        {
            var callerClassType = callerClasses.GetOrAdd(interfaceType, GenerateProviderToCallMethodInternalClass);
            var instance = Instantiator.GetSingle(interfaceType.Name + ((byte)networkType) + source, callerClassType, networkType, source, static (callerClassType, networkType, source) => Instantiator.Create(callerClassType, [typeof(NetworkType), typeof(string)], networkType, source));
            return instance;
        }
        private static Type GenerateProviderToCallMethodInternalClass(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            var methods = new List<MethodInfo>();
            methods.AddRange(interfaceType.GetMethods());
            foreach (var @interface in interfaceType.GetInterfaces())
                methods.AddRange(@interface.GetMethods());

            var typeSignature = "Caller_" + interfaceType.FullName;

            var moduleBuilder = GeneratedAssembly.GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout | TypeAttributes.Sealed, null);

            typeBuilder.AddInterfaceImplementation(interfaceType);

            var networkTypeField = typeBuilder.DefineField("networkType", typeof(NetworkType), FieldAttributes.Private | FieldAttributes.InitOnly);
            var sourceField = typeBuilder.DefineField("source", typeof(string), FieldAttributes.Private | FieldAttributes.InitOnly);

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard, [typeof(NetworkType), typeof(string)]);
            {
                constructorBuilder.DefineParameter(0, ParameterAttributes.None, "networkType");
                constructorBuilder.DefineParameter(1, ParameterAttributes.None, "source");

                var il = constructorBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, BusRouterMethods.ObjectConstructor);
                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Nop);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, networkTypeField);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Stfld, sourceField);

                il.Emit(OpCodes.Ret);
            }

            foreach (var method in methods)
            {
                var returnType = method.ReturnType;
                var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();

                var voidMethod = false;
                if (returnType.Name == "Void")
                {
                    returnType = typeof(object);
                    voidMethod = true;
                }

                var callMethod = BusRouterMethods.CallInternalMethodNonGeneric.MakeGenericMethod(returnType);

                var methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.Standard | CallingConventions.HasThis,
                    voidMethod ? null : returnType,
                    parameterTypes);
                methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

                var il = methodBuilder.GetILGenerator();

                _ = il.DeclareLocal(returnType);

                il.Emit(OpCodes.Nop);

                //_CallInternal<TReturn>(Type interfaceType, string methodName, object[] arguments, NetworkType networkType, string source)

                il.Emit(OpCodes.Ldtoken, interfaceType);
                il.Emit(OpCodes.Call, BusRouterMethods.TypeOfMethod); //typeof(TInterface)

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
                il.Emit(OpCodes.Ldfld, networkTypeField); //networkType

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, sourceField); //source

                il.Emit(OpCodes.Call, callMethod);

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

        public static void RegisterCaller(Type interfaceType, Type type)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");
            if (!callerClasses.TryAdd(interfaceType, type))
                throw new InvalidOperationException($"Caller for {interfaceType.GetNiceName()} is already registered");
        }

        private static readonly ConcurrentFactoryDictionary<Type, Type> dispatcherClasses = new();
        public static object GetHandlerToDispatchInternalInstance(Type interfaceType, bool requireAffirmation, NetworkType networkType, string source, BusLogging busLogging)
        {
            var dispatcherClassType = dispatcherClasses.GetOrAdd(interfaceType, GenerateHandlerToDispatchInternalClass);
            var instance = Instantiator.GetSingle(interfaceType.Name + (requireAffirmation ? 1 : 0) + (byte)networkType + source, dispatcherClassType, requireAffirmation, networkType, source, busLogging, static (dispatcherClassType, requireAffirmation, networkType, source, busLogging) => Instantiator.Create(dispatcherClassType, [typeof(bool), typeof(NetworkType), typeof(string), typeof(BusLogging)], requireAffirmation, networkType, source, busLogging));
            return instance;
        }
        private static Type GenerateHandlerToDispatchInternalClass(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            var methods = new List<MethodInfo>();
            methods.AddRange(interfaceType.GetMethods());
            foreach (var @interface in interfaceType.GetInterfaces())
                methods.AddRange(@interface.GetMethods());

            var typeSignature = "Dispatcher_" + interfaceType.FullName;

            var moduleBuilder = GeneratedAssembly.GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout | TypeAttributes.Sealed, null);

            typeBuilder.AddInterfaceImplementation(interfaceType);

            var requireAffirmationField = typeBuilder.DefineField("requireAffirmation", typeof(bool), FieldAttributes.Private | FieldAttributes.InitOnly);
            var networkTypeField = typeBuilder.DefineField("networkType", typeof(NetworkType), FieldAttributes.Private | FieldAttributes.InitOnly);
            var sourceField = typeBuilder.DefineField("source", typeof(string), FieldAttributes.Private | FieldAttributes.InitOnly);
            var busLoggingField = typeBuilder.DefineField("busLogging", typeof(BusLogging), FieldAttributes.Private | FieldAttributes.InitOnly);

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard, [typeof(bool), typeof(NetworkType), typeof(string), typeof(BusLogging)]);
            {
                constructorBuilder.DefineParameter(0, ParameterAttributes.None, "requireAffirmation");
                constructorBuilder.DefineParameter(1, ParameterAttributes.None, "networkType");
                constructorBuilder.DefineParameter(2, ParameterAttributes.None, "source");
                constructorBuilder.DefineParameter(3, ParameterAttributes.None, "busLogging");

                var il = constructorBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, BusRouterMethods.ObjectConstructor);
                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Nop);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, requireAffirmationField);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Stfld, networkTypeField);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Stfld, sourceField);

                il.Emit(OpCodes.Ret);
            }

            foreach (var method in methods)
            {
                var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();

                var methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.Standard | CallingConventions.HasThis,
                    method.ReturnType,
                    parameterTypes);
                methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

                var il = methodBuilder.GetILGenerator();

                var genericInterface = method.DeclaringType?.IsGenericType == true ? method.DeclaringType.GetGenericTypeDefinition() : null;

                if (genericInterface == BusRouterMethods.CommandHandlerType)
                {
                    // Bus._DispatchCommandInternalAsync(command, commandType, requireAffirmation, networkType, source, busLogging)

                    il.Emit(OpCodes.Ldarg_1); //command

                    il.Emit(OpCodes.Ldtoken, parameterTypes[0]);
                    il.Emit(OpCodes.Call, BusRouterMethods.TypeOfMethod); //commandType

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, networkTypeField); //requireAffirmation

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, networkTypeField); //networkType

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, sourceField); //source

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, busLoggingField); //busLogging

                    il.Emit(OpCodes.Call, BusRouterMethods.DispatchCommandInternalAsyncMethod);
                    il.Emit(OpCodes.Ret);

                    typeBuilder.DefineMethodOverride(methodBuilder, method);
                }
                else if (genericInterface == BusRouterMethods.CommandHandlerWithResultType)
                {
                    // Bus._DispatchCommandWithResultInternalAsync<>(command, commandType, networkType, source, busLogging)

                    il.Emit(OpCodes.Ldarg_1); //command

                    il.Emit(OpCodes.Ldtoken, parameterTypes[0]);
                    il.Emit(OpCodes.Call, BusRouterMethods.TypeOfMethod); //commandType

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, networkTypeField); //networkType

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, sourceField); //source

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, busLoggingField); //busLogging

                    il.Emit(OpCodes.Call, BusRouterMethods.DispatchCommandInternalAsyncMethod);
                    il.Emit(OpCodes.Ret);

                    typeBuilder.DefineMethodOverride(methodBuilder, method);
                }
                else if (genericInterface == BusRouterMethods.EventHandlerType)
                {
                    // Bus._DispatchEventInternalAsync(@event, eventType, networkType, source, busLogging)

                    il.Emit(OpCodes.Ldarg_1); //@event

                    il.Emit(OpCodes.Ldtoken, parameterTypes[0]);
                    il.Emit(OpCodes.Call, BusRouterMethods.TypeOfMethod); //eventType

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, networkTypeField); //networkType

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, sourceField); //source

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, busLoggingField); //busLogging

                    il.Emit(OpCodes.Call, BusRouterMethods.DispatchCommandInternalAsyncMethod);
                    il.Emit(OpCodes.Ret);

                    typeBuilder.DefineMethodOverride(methodBuilder, method);
                }
                else
                {
                    il.Emit(OpCodes.Ldstr, $"Interface method does not inherit {BusRouterMethods.CommandHandlerType.Name} or {BusRouterMethods.CommandHandlerWithResultType.Name} or {BusRouterMethods.EventHandlerType.Name}");
                    il.Emit(OpCodes.Newobj, BusRouterMethods.NotSupportedExceptionConstructor);
                    il.Emit(OpCodes.Throw);

                    typeBuilder.DefineMethodOverride(methodBuilder, method);
                }
            }

            Type objectType = typeBuilder.CreateTypeInfo() ?? throw new Exception("Failed to CreateTypeInfo");
            return objectType;
        }

        public static void RegisterDispatcher(Type interfaceType, Type type)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");
            if (!dispatcherClasses.TryAdd(interfaceType, type))
                throw new InvalidOperationException($"Dispatcher for {interfaceType.GetNiceName()} is already registered");
        }
    }
}

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
        private static readonly ConstructorInfo notSupportedExceptionConstructor = typeof(NotSupportedException).GetConstructor([typeof(string)]) ?? throw new Exception($"{nameof(NotSupportedException)} constructor not found");
        private static readonly ConstructorInfo objectConstructor = typeof(object).GetConstructor(Array.Empty<Type>()) ?? throw new Exception($"{nameof(Object)} constructor not found");
        private static readonly MethodInfo typeOfMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)) ?? throw new Exception($"{nameof(Type)}.{nameof(Type.GetTypeFromHandle)} not found");
        
        private static readonly MethodInfo callInternalMethodNonGeneric = typeof(Bus).GetMethod(nameof(Bus._CallMethod), BindingFlags.Static | BindingFlags.Public) ?? throw new Exception($"{nameof(Bus)}.{nameof(Bus._CallMethod)} not found");
        private static readonly ConcurrentFactoryDictionary<Type, Type> callerClasses = new();
        public static object GetProviderToCallMethodInternalInstance(Type interfaceType, NetworkType networkType, string source)
        {
            var callerClassType = callerClasses.GetOrAdd(interfaceType, (interfaceType) => GenerateProviderToCallMethodInternalClass(interfaceType));
            var instance = Instantiator.GetSingle(interfaceType.Name + ((byte)networkType) + source, () => Instantiator.Create(callerClassType, [typeof(NetworkType), typeof(string)], networkType, source));
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
                il.Emit(OpCodes.Call, objectConstructor);
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

                var callMethod = callInternalMethodNonGeneric.MakeGenericMethod(returnType);

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

        private static readonly Type commandHandlerType = typeof(ICommandHandler<>);
        private static readonly Type commandHandlerWithResultType = typeof(ICommandHandler<,>);
        private static readonly Type eventHandlerType = typeof(IEventHandler<>);
        private static readonly MethodInfo dispatchCommandInternalAsyncMethod = typeof(Bus).GetMethod(nameof(Bus._DispatchCommandInternalAsync), BindingFlags.Static | BindingFlags.Public) ?? throw new Exception($"{nameof(Bus)}.{nameof(Bus._DispatchCommandInternalAsync)} not found");
        private static readonly MethodInfo dispatchCommandWithResultInternalAsyncMethod = typeof(Bus).GetMethod(nameof(Bus._DispatchCommandWithResultInternalAsync), BindingFlags.Static | BindingFlags.Public) ?? throw new Exception($"{nameof(Bus)}.{nameof(Bus._DispatchCommandInternalAsync)} not found");
        private static readonly MethodInfo dispatchEventInternalAsyncMethod = typeof(Bus).GetMethod(nameof(Bus._DispatchEventInternalAsync), BindingFlags.Static | BindingFlags.Public) ?? throw new Exception($"{nameof(Bus)}.{nameof(Bus._DispatchEventInternalAsync)} not found");
        private static readonly ConcurrentFactoryDictionary<Type, Type> dispatcherClasses = new();
        public static object GetHandlerToDispatchInternalInstance(Type interfaceType, bool requireAffirmation, NetworkType networkType, string source, BusLogging busLogging)
        {
            var dispatcherClassType = dispatcherClasses.GetOrAdd(interfaceType, (interfaceType) => GenerateHandlerToDispatchInternalClass(interfaceType));
            var instance = Instantiator.GetSingle(interfaceType.Name + (requireAffirmation ? 1 : 0) + (byte)networkType + source, () => Instantiator.Create(dispatcherClassType, [typeof(bool), typeof(NetworkType), typeof(string), typeof(BusLogging)], requireAffirmation, networkType, source, busLogging));
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
                il.Emit(OpCodes.Call, objectConstructor);
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

                if (genericInterface == commandHandlerType)
                {
                    // Bus._DispatchCommandInternalAsync(command, commandType, requireAffirmation, networkType, source, busLogging)

                    il.Emit(OpCodes.Ldarg_1); //command

                    il.Emit(OpCodes.Ldtoken, parameterTypes[0]);
                    il.Emit(OpCodes.Call, typeOfMethod); //commandType

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, networkTypeField); //requireAffirmation

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, networkTypeField); //networkType

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, sourceField); //source

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, busLoggingField); //busLogging

                    il.Emit(OpCodes.Call, dispatchCommandInternalAsyncMethod);
                    il.Emit(OpCodes.Ret);

                    typeBuilder.DefineMethodOverride(methodBuilder, method);
                }
                else if (genericInterface == commandHandlerWithResultType)
                {
                    // Bus._DispatchCommandWithResultInternalAsync<>(command, commandType, networkType, source, busLogging)

                    il.Emit(OpCodes.Ldarg_1); //command

                    il.Emit(OpCodes.Ldtoken, parameterTypes[0]);
                    il.Emit(OpCodes.Call, typeOfMethod); //commandType

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, networkTypeField); //networkType

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, sourceField); //source

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, busLoggingField); //busLogging

                    il.Emit(OpCodes.Call, dispatchCommandInternalAsyncMethod);
                    il.Emit(OpCodes.Ret);

                    typeBuilder.DefineMethodOverride(methodBuilder, method);
                }
                else if (genericInterface == eventHandlerType)
                {
                    // Bus._DispatchEventInternalAsync(@event, eventType, networkType, source, busLogging)

                    il.Emit(OpCodes.Ldarg_1); //@event

                    il.Emit(OpCodes.Ldtoken, parameterTypes[0]);
                    il.Emit(OpCodes.Call, typeOfMethod); //eventType

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, networkTypeField); //networkType

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, sourceField); //source

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, busLoggingField); //busLogging

                    il.Emit(OpCodes.Call, dispatchCommandInternalAsyncMethod);
                    il.Emit(OpCodes.Ret);

                    typeBuilder.DefineMethodOverride(methodBuilder, method);
                }
                else
                {
                    il.Emit(OpCodes.Ldstr, $"Interface method does not inherit {commandHandlerType.Name} or {commandHandlerWithResultType.Name} or {eventHandlerType.Name}");
                    il.Emit(OpCodes.Newobj, notSupportedExceptionConstructor);
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

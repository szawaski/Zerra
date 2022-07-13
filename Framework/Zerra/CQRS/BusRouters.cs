// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Zerra.Collections;
using Zerra.Providers;
using Zerra.Reflection;

namespace Zerra.CQRS
{
    internal static class BusRouters
    {
        private static readonly Type taskType = typeof(Task);
        private static readonly ConstructorInfo notSupportedExceptionConstructor = typeof(NotSupportedException).GetConstructor(new Type[] { typeof(string) });

        private static readonly Type baseProviderType = typeof(IBaseProvider);
        private static MethodInfo callInternalMethodNonGeneric = typeof(Bus).GetMethod(nameof(Bus._CallInternal), BindingFlags.Static | BindingFlags.Public);
        private static readonly ConcurrentFactoryDictionary<Type, Type> callerClasses = new ConcurrentFactoryDictionary<Type, Type>();
        public static object GetProviderToCallInternalInstance(Type interfaceType)
        {
            var callerClassType = callerClasses.GetOrAdd(interfaceType, (t) =>
            {
                return GenerateProviderToCallInternalClass(t);
            });
            var instance = Instantiator.GetSingleInstance(callerClassType);
            return instance;
        }
        private static Type GenerateProviderToCallInternalClass(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            var inheritsBaseProvider = false;
            var methods = new List<MethodInfo>();
            var notSupportedMethods = new List<MethodInfo>();
            methods.AddRange(interfaceType.GetMethods());
            foreach (var @interface in interfaceType.GetInterfaces())
            {
                if (!inheritsBaseProvider && @interface == baseProviderType)
                    inheritsBaseProvider = true;
                if (@interface.GetInterfaces().Contains(baseProviderType))
                    methods.AddRange(@interface.GetMethods());
                else
                    notSupportedMethods.AddRange(@interface.GetMethods());
            }

            if (!inheritsBaseProvider)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} does not inherit {baseProviderType.GetNiceName()}");

            var typeSignature = interfaceType.Name + "_Caller";

            var moduleBuilder = GeneratedAssembly.GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, null);

            typeBuilder.AddInterfaceImplementation(interfaceType);

            _ = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            foreach (var method in methods)
            {
                var methodName = method.Name;
                var returnType = method.ReturnType;
                var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();

                var voidMethod = false;
                if (returnType.Name == "Void")
                {
                    returnType = typeof(object);
                    voidMethod = true;
                }

                var callMethod = callInternalMethodNonGeneric.MakeGenericMethod(interfaceType, returnType);

                var methodBuilder = typeBuilder.DefineMethod(
                    methodName,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.HasThis,
                    voidMethod ? null : returnType,
                    parameterTypes);
                methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

                var il = methodBuilder.GetILGenerator();

                _ = il.DeclareLocal(returnType);

                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Ldstr, methodName);

                il.Emit(OpCodes.Ldc_I4, parameterTypes.Length);
                il.Emit(OpCodes.Newarr, typeof(object));

                for (var j = 0; j < parameterTypes.Length; j++)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4, j);
                    il.Emit(OpCodes.Ldarg, j + 1);
                    if (parameterTypes[j].IsValueType)
                        il.Emit(OpCodes.Box, parameterTypes[j]);
                    il.Emit(OpCodes.Stelem_Ref);
                }

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

            foreach (var method in notSupportedMethods)
            {
                var methodName = method.Name;
                var returnType = method.ReturnType;
                var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();

                var voidMethod = false;
                if (returnType.Name == "Void")
                {
                    returnType = typeof(object);
                    voidMethod = true;
                }

                var methodBuilder = typeBuilder.DefineMethod(
                    methodName,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.HasThis,
                    voidMethod ? null : returnType,
                    parameterTypes);
                methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

                var il = methodBuilder.GetILGenerator();

                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Ldstr, $"Interface method does not inherit {baseProviderType.Name}");
                il.Emit(OpCodes.Newobj, notSupportedExceptionConstructor);
                il.Emit(OpCodes.Throw);

                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }

            Type objectType = typeBuilder.CreateTypeInfo();
            return objectType;
        }

        private static readonly MethodInfo getTypeMethod = typeof(object).GetMethod(nameof(Object.GetType));

        private static readonly Type commandHandlerType = typeof(ICommandHandler<>);
        private static readonly MethodInfo dispatchCommandInternalAsyncMethod = typeof(Bus).GetMethod(nameof(Bus._DispatchCommandInternalAsync), BindingFlags.Static | BindingFlags.Public);
        private static readonly ConcurrentFactoryDictionary<Type, Type> commandDispatcherClasses = new ConcurrentFactoryDictionary<Type, Type>();
        public static object GetCommandHandlerToDispatchInternalInstance(Type interfaceType)
        {
            var dispatcherClassType = commandDispatcherClasses.GetOrAdd(interfaceType, (t) =>
            {
                return GenerateCommandHandlerToDispatchInternalClass(t);
            });
            var instance = Instantiator.GetSingleInstance(dispatcherClassType);
            return instance;
        }
        private static Type GenerateCommandHandlerToDispatchInternalClass(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            var inheritsBaseProvider = false;
            var methods = new List<MethodInfo>();
            var notSupportedMethods = new List<MethodInfo>();
            foreach (var @interface in interfaceType.GetInterfaces())
            {
                if (!inheritsBaseProvider && @interface == baseProviderType)
                    inheritsBaseProvider = true;
                if (@interface.Name == commandHandlerType.Name)
                    methods.AddRange(@interface.GetMethods());
                else
                    notSupportedMethods.AddRange(@interface.GetMethods());
            }

            if (!inheritsBaseProvider)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} does not inherit {baseProviderType.GetNiceName()}");

            var typeSignature = interfaceType.Name + "_CommandDispatcher";

            var moduleBuilder = GeneratedAssembly.GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, null);

            typeBuilder.AddInterfaceImplementation(interfaceType);

            _ = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            foreach (var method in methods)
            {
                var methodName = method.Name;
                var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();

                var methodBuilder = typeBuilder.DefineMethod(
                    methodName,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.HasThis,
                    taskType,
                    parameterTypes);
                methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

                var il = methodBuilder.GetILGenerator();

                _ = il.DeclareLocal(typeof(Type));

                il.Emit(OpCodes.Nop);

                // Type messageType = command.GetType();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Callvirt, getTypeMethod);
                il.Emit(OpCodes.Stloc_0);

                // Bus.DispatchInternal(command, commandType, false)
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldc_I4_0); //il.Emit(OpCodes.Ldc_I4_1); for true, 
                il.Emit(OpCodes.Call, dispatchCommandInternalAsyncMethod);
                il.Emit(OpCodes.Ret);

                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }

            foreach (var method in notSupportedMethods)
            {
                var methodName = method.Name;
                var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();

                var methodBuilder = typeBuilder.DefineMethod(
                    methodName,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.HasThis,
                    taskType,
                    parameterTypes);
                methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

                var il = methodBuilder.GetILGenerator();

                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Ldstr, $"Interface method does not inherit {commandHandlerType.Name}");
                il.Emit(OpCodes.Newobj, notSupportedExceptionConstructor);
                il.Emit(OpCodes.Throw);

                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }

            Type objectType = typeBuilder.CreateTypeInfo();
            return objectType;
        }

        private static readonly Type eventHandlerType = typeof(IEventHandler<>);
        private static readonly MethodInfo dispatchEventInternalAsyncMethod = typeof(Bus).GetMethod(nameof(Bus._DispatchEventInternalAsync), BindingFlags.Static | BindingFlags.Public);
        private static readonly ConcurrentFactoryDictionary<Type, Type> eventDispatcherClasses = new ConcurrentFactoryDictionary<Type, Type>();
        public static object GetEventHandlerToDispatchInternalInstance(Type interfaceType)
        {
            var dispatcherClassType = eventDispatcherClasses.GetOrAdd(interfaceType, (t) =>
            {
                return GenerateEventHandlerToDispatchInternalClass(t);
            });
            var instance = Instantiator.GetSingleInstance(dispatcherClassType);
            return instance;
        }
        private static Type GenerateEventHandlerToDispatchInternalClass(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            var inheritsBaseProvider = false;
            var methods = new List<MethodInfo>();
            var notSupportedMethods = new List<MethodInfo>();
            foreach (var @interface in interfaceType.GetInterfaces())
            {
                if (!inheritsBaseProvider && @interface == baseProviderType)
                    inheritsBaseProvider = true;
                if (@interface.Name == eventHandlerType.Name)
                    methods.AddRange(@interface.GetMethods());
                else
                    notSupportedMethods.AddRange(@interface.GetMethods());
            }

            if (!inheritsBaseProvider)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} does not inherit {baseProviderType.GetNiceName()}");

            var typeSignature = interfaceType.Name + "_EventDispatcher";

            var moduleBuilder = GeneratedAssembly.GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, null);

            typeBuilder.AddInterfaceImplementation(interfaceType);

            _ = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            foreach (var method in methods)
            {
                var methodName = method.Name;
                var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();

                var methodBuilder = typeBuilder.DefineMethod(
                    methodName,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.HasThis,
                    taskType,
                    parameterTypes);
                methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

                var il = methodBuilder.GetILGenerator();

                _ = il.DeclareLocal(typeof(Type));

                il.Emit(OpCodes.Nop);

                // Type messageType = @event.GetType();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Callvirt, getTypeMethod);
                il.Emit(OpCodes.Stloc_0);

                // Bus._DispatchEventInternalAsync(@event, eventType)
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Call, dispatchEventInternalAsyncMethod);
                il.Emit(OpCodes.Ret);

                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }

            foreach (var method in notSupportedMethods)
            {
                var methodName = method.Name;
                var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();

                var methodBuilder = typeBuilder.DefineMethod(
                    methodName,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.HasThis,
                    taskType,
                    parameterTypes);
                methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

                var il = methodBuilder.GetILGenerator();

                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Ldstr, $"Interface method does not inherit {eventHandlerType.Name}");
                il.Emit(OpCodes.Newobj, notSupportedExceptionConstructor);
                il.Emit(OpCodes.Throw);

                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }

            Type objectType = typeBuilder.CreateTypeInfo();
            return objectType;
        }
    }
}

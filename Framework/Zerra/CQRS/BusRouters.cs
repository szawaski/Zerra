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
        private static readonly ConstructorInfo objectConstructor = typeof(object).GetConstructor(new Type[0]);
        private static readonly MethodInfo getTypeMethod = typeof(object).GetMethod(nameof(Object.GetType));
        private static readonly MethodInfo typeofMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));

        private static readonly MethodInfo callInternalMethodNonGeneric = typeof(Bus).GetMethod(nameof(Bus._CallMethod), BindingFlags.Static | BindingFlags.Public);
        private static readonly ConcurrentFactoryDictionary<Type, Type> callerClasses = new();
        public static object GetProviderToCallMethodInternalInstance(Type interfaceType, NetworkType networkType, string source)
        {
            var callerClassType = callerClasses.GetOrAdd(interfaceType, (t) =>
            {
                return GenerateProviderToCallMethodInternalClass(t);
            });
            var instance = Instantiator.GetSingle(interfaceType.Name + ((byte)networkType) + source, () => Instantiator.Create(callerClassType, new Type[] { typeof(NetworkType), typeof(string) }, networkType, source));
            return instance;
        }
        private static Type GenerateProviderToCallMethodInternalClass(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            var methods = new List<MethodInfo>();
            //var notSupportedMethods = new List<MethodInfo>();
            methods.AddRange(interfaceType.GetMethods());
            foreach (var @interface in interfaceType.GetInterfaces())
            {
                methods.AddRange(@interface.GetMethods());
            }

            var typeSignature = interfaceType.Name + "_Caller";

            var moduleBuilder = GeneratedAssembly.GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout | TypeAttributes.Sealed, null);

            typeBuilder.AddInterfaceImplementation(interfaceType);

            var networkTypeField = typeBuilder.DefineField("networkType", typeof(NetworkType), FieldAttributes.Private | FieldAttributes.InitOnly);
            var sourceField = typeBuilder.DefineField("source", typeof(string), FieldAttributes.Private | FieldAttributes.InitOnly);

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Any, new Type[] { typeof(NetworkType), typeof(string) });
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
                var methodName = method.Name;
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
                    methodName,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.HasThis,
                    voidMethod ? null : returnType,
                    parameterTypes);
                methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

                var il = methodBuilder.GetILGenerator();

                _ = il.DeclareLocal(returnType);

                il.Emit(OpCodes.Nop);

                //_CallInternal<TReturn>(Type interfaceType, string methodName, object[] arguments, string externallyReceived, string source)

                il.Emit(OpCodes.Ldtoken, interfaceType);
                il.Emit(OpCodes.Call, typeofMethod); //typeof(TInterface)

                il.Emit(OpCodes.Ldstr, methodName); //methodName

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
                il.Emit(OpCodes.Ldfld, networkTypeField); //externallyReceived

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

            //foreach (var method in notSupportedMethods)
            //{
            //    var methodName = method.Name;
            //    var returnType = method.ReturnType;
            //    var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();

            //    var voidMethod = false;
            //    if (returnType.Name == "Void")
            //    {
            //        returnType = typeof(object);
            //        voidMethod = true;
            //    }

            //    var methodBuilder = typeBuilder.DefineMethod(
            //        methodName,
            //        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
            //        CallingConventions.HasThis,
            //        voidMethod ? null : returnType,
            //        parameterTypes);
            //    methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

            //    var il = methodBuilder.GetILGenerator();

            //    il.Emit(OpCodes.Nop);
            //    il.Emit(OpCodes.Ldstr, $"Interface method does not inherit {baseProviderType.Name}");
            //    il.Emit(OpCodes.Newobj, notSupportedExceptionConstructor);
            //    il.Emit(OpCodes.Throw);

            //    typeBuilder.DefineMethodOverride(methodBuilder, method);
            //}

            Type objectType = typeBuilder.CreateTypeInfo();
            return objectType;
        }

        private static readonly Type commandHandlerType = typeof(ICommandHandler<>);
        private static readonly MethodInfo dispatchCommandInternalAsyncMethod = typeof(Bus).GetMethod(nameof(Bus._DispatchCommandInternalAsync), BindingFlags.Static | BindingFlags.Public);
        private static readonly ConcurrentFactoryDictionary<Type, Type> commandDispatcherClasses = new();
        public static object GetCommandHandlerToDispatchInternalInstance(Type interfaceType, bool requireAffirmation, NetworkType networkType, string source)
        {
            var dispatcherClassType = commandDispatcherClasses.GetOrAdd(interfaceType, (t) =>
            {
                return GenerateCommandHandlerToDispatchInternalClass(t);
            });
            var instance = Instantiator.GetSingle(interfaceType.Name + (requireAffirmation ? 1 : 0) + (byte)networkType + source, () => Instantiator.Create(dispatcherClassType, new Type[] { typeof(bool), typeof(NetworkType), typeof(string) }, requireAffirmation, networkType, source));
            return instance;
        }
        private static Type GenerateCommandHandlerToDispatchInternalClass(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            var methods = new List<MethodInfo>();
            //var notSupportedMethods = new List<MethodInfo>();
            foreach (var @interface in interfaceType.GetInterfaces())
            {
                methods.AddRange(@interface.GetMethods());
            }

            var typeSignature = interfaceType.Name + "_CommandDispatcher";

            var moduleBuilder = GeneratedAssembly.GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout | TypeAttributes.Sealed, null);

            typeBuilder.AddInterfaceImplementation(interfaceType);

            var requireAffirmationField = typeBuilder.DefineField("requireAffirmation", typeof(bool), FieldAttributes.Private | FieldAttributes.InitOnly);
            var networkTypeField = typeBuilder.DefineField("networkType", typeof(NetworkType), FieldAttributes.Private | FieldAttributes.InitOnly);
            var sourceField = typeBuilder.DefineField("source", typeof(string), FieldAttributes.Private | FieldAttributes.InitOnly);

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Any, new Type[] { typeof(bool), typeof(NetworkType), typeof(string) });
            {
                constructorBuilder.DefineParameter(0, ParameterAttributes.None, "requireAffirmation");
                constructorBuilder.DefineParameter(1, ParameterAttributes.None, "networkType");
                constructorBuilder.DefineParameter(2, ParameterAttributes.None, "source");

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

                // Bus.DispatchInternal(command, commandType, requireAffirmation, externallyReceived, source)
                il.Emit(OpCodes.Ldarg_1); //@event
                il.Emit(OpCodes.Ldloc_0); //eventType

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, networkTypeField); //requireAffirmation

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, networkTypeField); //externallyReceived

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, sourceField); //source

                il.Emit(OpCodes.Call, dispatchCommandInternalAsyncMethod);
                il.Emit(OpCodes.Ret);

                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }

            //foreach (var method in notSupportedMethods)
            //{
            //    var methodName = method.Name;
            //    var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();

            //    var methodBuilder = typeBuilder.DefineMethod(
            //        methodName,
            //        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
            //        CallingConventions.HasThis,
            //        taskType,
            //        parameterTypes);
            //    methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

            //    var il = methodBuilder.GetILGenerator();

            //    il.Emit(OpCodes.Nop);
            //    il.Emit(OpCodes.Ldstr, $"Interface method does not inherit {commandHandlerType.Name}");
            //    il.Emit(OpCodes.Newobj, notSupportedExceptionConstructor);
            //    il.Emit(OpCodes.Throw);

            //    typeBuilder.DefineMethodOverride(methodBuilder, method);
            //}

            Type objectType = typeBuilder.CreateTypeInfo();
            return objectType;
        }

        private static readonly Type eventHandlerType = typeof(IEventHandler<>);
        private static readonly MethodInfo dispatchEventInternalAsyncMethod = typeof(Bus).GetMethod(nameof(Bus._DispatchEventInternalAsync), BindingFlags.Static | BindingFlags.Public);
        private static readonly ConcurrentFactoryDictionary<Type, Type> eventDispatcherClasses = new();
        public static object GetEventHandlerToDispatchInternalInstance(Type interfaceType, NetworkType networkType, string source)
        {
            var dispatcherClassType = eventDispatcherClasses.GetOrAdd(interfaceType, (t) =>
            {
                return GenerateEventHandlerToDispatchInternalClass(t);
            });
            var instance = Instantiator.GetSingle(interfaceType.Name + (byte)networkType + source, () => Instantiator.Create(dispatcherClassType, new Type[] { typeof(NetworkType), typeof(string) }, networkType, source));
            return instance;
        }
        private static Type GenerateEventHandlerToDispatchInternalClass(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            var inheritsBaseProvider = false;
            var methods = new List<MethodInfo>();
            //var notSupportedMethods = new List<MethodInfo>();
            foreach (var @interface in interfaceType.GetInterfaces())
            {
                methods.AddRange(@interface.GetMethods());
            }

            var typeSignature = interfaceType.Name + "_EventDispatcher";

            var moduleBuilder = GeneratedAssembly.GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout | TypeAttributes.Sealed, null);

            typeBuilder.AddInterfaceImplementation(interfaceType);

            var networkTypeField = typeBuilder.DefineField("networkType", typeof(NetworkType), FieldAttributes.Private | FieldAttributes.InitOnly);
            var sourceField = typeBuilder.DefineField("source", typeof(string), FieldAttributes.Private | FieldAttributes.InitOnly);

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Any, new Type[] { typeof(NetworkType), typeof(string) });
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

                // Bus._DispatchEventInternalAsync(@event, eventType, externallyReceived, source)
                il.Emit(OpCodes.Ldarg_1); //@event
                il.Emit(OpCodes.Ldloc_0); //eventType
                il.Emit(OpCodes.Ldc_I4_0); //false; il.Emit(OpCodes.Ldc_I4_1); for true, 

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, networkTypeField); //externallyReceived

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, sourceField); //source

                il.Emit(OpCodes.Call, dispatchEventInternalAsyncMethod);
                il.Emit(OpCodes.Ret);

                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }

            //foreach (var method in notSupportedMethods)
            //{
            //    var methodName = method.Name;
            //    var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();

            //    var methodBuilder = typeBuilder.DefineMethod(
            //        methodName,
            //        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
            //        CallingConventions.HasThis,
            //        taskType,
            //        parameterTypes);
            //    methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

            //    var il = methodBuilder.GetILGenerator();

            //    il.Emit(OpCodes.Nop);
            //    il.Emit(OpCodes.Ldstr, $"Interface method does not inherit {eventHandlerType.Name}");
            //    il.Emit(OpCodes.Newobj, notSupportedExceptionConstructor);
            //    il.Emit(OpCodes.Throw);

            //    typeBuilder.DefineMethodOverride(methodBuilder, method);
            //}

            Type objectType = typeBuilder.CreateTypeInfo();
            return objectType;
        }
    }
}

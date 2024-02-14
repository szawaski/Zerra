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
using Zerra.Reflection;

namespace Zerra.CQRS
{
    internal sealed class BusRouters
    {
        private readonly ConcurrentFactoryDictionary<LookupKey, object> callerInstances = new();
        private readonly ConcurrentFactoryDictionary<LookupKey, object> commandDispatcherInstances = new();
        private readonly ConcurrentFactoryDictionary<LookupKey, object> eventDispatcherInstances = new();
        private readonly BusInstance bus;
        public BusRouters(BusInstance bus)
        {
            this.bus = bus;
        }

        private static readonly Type taskType = typeof(Task);
        private static readonly ConstructorInfo notSupportedExceptionConstructor = typeof(NotSupportedException).GetConstructor(new Type[] { typeof(string) }) ?? throw new Exception($"{nameof(NotSupportedException)} constructor not found");
        private static readonly ConstructorInfo objectConstructor = typeof(object).GetConstructor(Array.Empty<Type>()) ?? throw new Exception($"{nameof(Object)} constructor not found");
        private static readonly MethodInfo getTypeMethod = typeof(object).GetMethod(nameof(Object.GetType)) ?? throw new Exception($"{nameof(Object)}.{nameof(Object.GetType)} not found");
        private static readonly MethodInfo typeofMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)) ?? throw new Exception($"{nameof(Type)}.{nameof(Type.GetTypeFromHandle)} not found");
        private static readonly MethodInfo callInternalMethodNonGeneric = typeof(BusInstance).GetMethod(nameof(BusInstance._CallMethod), BindingFlags.Instance | BindingFlags.Public) ?? throw new Exception($"{nameof(Bus)}.{nameof(BusInstance._CallMethod)} not found");
        private static readonly Type commandHandlerType = typeof(ICommandHandler<>);
        private static readonly MethodInfo dispatchCommandInternalAsyncMethod = typeof(BusInstance).GetMethod(nameof(BusInstance._DispatchCommandInternalAsync), BindingFlags.Instance | BindingFlags.Public) ?? throw new Exception($"{nameof(Bus)}.{nameof(BusInstance._DispatchCommandInternalAsync)} not found");
        private static readonly Type eventHandlerType = typeof(IEventHandler<>);
        private static readonly MethodInfo dispatchEventInternalAsyncMethod = typeof(BusInstance).GetMethod(nameof(BusInstance._DispatchEventInternalAsync), BindingFlags.Instance | BindingFlags.Public) ?? throw new Exception($"{nameof(Bus)}.{nameof(BusInstance._DispatchEventInternalAsync)} not found");

        private static readonly ConcurrentFactoryDictionary<Type, Type> callerClasses = new();
        private static readonly ConcurrentFactoryDictionary<Type, Type> commandDispatcherClasses = new();
        private static readonly ConcurrentFactoryDictionary<Type, Type> eventDispatcherClasses = new();

        public object GetProviderToCallMethodInternalInstance(Type interfaceType, NetworkType networkType, string source)
        {
            var callerClassType = callerClasses.GetOrAdd(interfaceType, (interfaceType) =>
            {
                return GenerateProviderToCallMethodInternalClass(interfaceType);
            });
            var key = new LookupKey(interfaceType, false, networkType, source);
            var instance = callerInstances.GetOrAdd(key, (key) => Instantiator.Create(callerClassType, new Type[] { typeof(BusInstance), typeof(NetworkType), typeof(string) }, bus, networkType, source));
            return instance;
        }
        private static Type GenerateProviderToCallMethodInternalClass(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            var methods = new List<MethodInfo>();
            methods.AddRange(interfaceType.GetMethods());
            foreach (var @interface in interfaceType.GetInterfaces())
            {
                methods.AddRange(@interface.GetMethods());
            }

            var typeSignature = interfaceType.Name + "_Caller";

            var moduleBuilder = GeneratedAssembly.GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout | TypeAttributes.Sealed, null);

            typeBuilder.AddInterfaceImplementation(interfaceType);

            var busTypeField = typeBuilder.DefineField("bus", typeof(BusInstance), FieldAttributes.Private | FieldAttributes.InitOnly);
            var networkTypeField = typeBuilder.DefineField("networkType", typeof(NetworkType), FieldAttributes.Private | FieldAttributes.InitOnly);
            var sourceField = typeBuilder.DefineField("source", typeof(string), FieldAttributes.Private | FieldAttributes.InitOnly);

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard, new Type[] { typeof(BusInstance), typeof(NetworkType), typeof(string) });
            {
                constructorBuilder.DefineParameter(1, ParameterAttributes.None, "bus");
                constructorBuilder.DefineParameter(2, ParameterAttributes.None, "networkType");
                constructorBuilder.DefineParameter(3, ParameterAttributes.None, "source");

                var il = constructorBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, objectConstructor);
                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Nop);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, busTypeField);

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
                    CallingConventions.Standard | CallingConventions.HasThis,
                    voidMethod ? null : returnType,
                    parameterTypes);
                methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

                var il = methodBuilder.GetILGenerator();

                _ = il.DeclareLocal(returnType);

                il.Emit(OpCodes.Nop);

                //bus._CallMethod<TReturn>(Type interfaceType, string methodName, object[] arguments, NetworkType networkType, string source)

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, busTypeField); //bus

                il.Emit(OpCodes.Ldtoken, interfaceType); //typeof(TInterface)
                il.Emit(OpCodes.Call, typeofMethod); //interfaceType

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

        public object GetCommandHandlerToDispatchInternalInstance(Type interfaceType, bool requireAffirmation, NetworkType networkType, string source, BusLogging busLogging)
        {
            var dispatcherClassType = commandDispatcherClasses.GetOrAdd(interfaceType, (interfaceType) =>
            {
                return GenerateCommandHandlerToDispatchInternalClass(interfaceType);
            });
            var key = new LookupKey(interfaceType, requireAffirmation, networkType, source);
            var instance = commandDispatcherInstances.GetOrAdd(key, (key) => Instantiator.Create(dispatcherClassType, new Type[] { typeof(BusInstance), typeof(bool), typeof(NetworkType), typeof(string), typeof(BusLogging) }, bus, requireAffirmation, networkType, source, busLogging));
            return instance;
        }
        private static Type GenerateCommandHandlerToDispatchInternalClass(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            var methods = new List<MethodInfo>();
            foreach (var @interface in interfaceType.GetInterfaces())
            {
                methods.AddRange(@interface.GetMethods());
            }

            var typeSignature = interfaceType.Name + "_CommandDispatcher";

            var moduleBuilder = GeneratedAssembly.GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout | TypeAttributes.Sealed, null);

            typeBuilder.AddInterfaceImplementation(interfaceType);

            var busTypeField = typeBuilder.DefineField("bus", typeof(BusInstance), FieldAttributes.Private | FieldAttributes.InitOnly);
            var requireAffirmationField = typeBuilder.DefineField("requireAffirmation", typeof(bool), FieldAttributes.Private | FieldAttributes.InitOnly);
            var networkTypeField = typeBuilder.DefineField("networkType", typeof(NetworkType), FieldAttributes.Private | FieldAttributes.InitOnly);
            var sourceField = typeBuilder.DefineField("source", typeof(string), FieldAttributes.Private | FieldAttributes.InitOnly);
            var busLoggingField = typeBuilder.DefineField("busLogging", typeof(BusLogging), FieldAttributes.Private | FieldAttributes.InitOnly);

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard, new Type[] { typeof(BusInstance), typeof(bool), typeof(NetworkType), typeof(string), typeof(BusLogging) });
            {
                constructorBuilder.DefineParameter(1, ParameterAttributes.None, "bus");
                constructorBuilder.DefineParameter(2, ParameterAttributes.None, "requireAffirmation");
                constructorBuilder.DefineParameter(3, ParameterAttributes.None, "networkType");
                constructorBuilder.DefineParameter(4, ParameterAttributes.None, "source");
                constructorBuilder.DefineParameter(5, ParameterAttributes.None, "busLogging");

                var il = constructorBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, objectConstructor);
                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Nop);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, busTypeField);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Stfld, requireAffirmationField);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Stfld, networkTypeField);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg, 4);
                il.Emit(OpCodes.Stfld, sourceField);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg, 5);
                il.Emit(OpCodes.Stfld, busLoggingField);

                il.Emit(OpCodes.Ret);
            }

            foreach (var method in methods)
            {
                var methodName = method.Name;
                var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();

                var methodBuilder = typeBuilder.DefineMethod(
                    methodName,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.Standard | CallingConventions.HasThis,
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

                // bus._DispatchCommandInternalAsync(command, commandType, requireAffirmation, networkType, source, busLogging)
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, busTypeField); //bus

                il.Emit(OpCodes.Ldarg_1); //command
                il.Emit(OpCodes.Ldloc_0); //commandType

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, requireAffirmationField); //requireAffirmation

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

            Type objectType = typeBuilder.CreateTypeInfo() ?? throw new Exception("Failed to CreateTypeInfo");
            return objectType;
        }

        public object GetEventHandlerToDispatchInternalInstance(Type interfaceType, NetworkType networkType, string source, BusLogging busLogging)
        {
            var dispatcherClassType = eventDispatcherClasses.GetOrAdd(interfaceType, (interfaceType) =>
            {
                return GenerateEventHandlerToDispatchInternalClass(interfaceType);
            });
            var key = new LookupKey(interfaceType, false, networkType, source);
            var instance = eventDispatcherInstances.GetOrAdd(key, (key) => Instantiator.Create(dispatcherClassType, new Type[] { typeof(BusInstance), typeof(NetworkType), typeof(string), typeof(BusLogging) }, bus, networkType, source, busLogging));
            return instance;
        }
        private static Type GenerateEventHandlerToDispatchInternalClass(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            var methods = new List<MethodInfo>();
            foreach (var @interface in interfaceType.GetInterfaces())
            {
                methods.AddRange(@interface.GetMethods());
            }

            var typeSignature = interfaceType.Name + "_EventDispatcher";

            var moduleBuilder = GeneratedAssembly.GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout | TypeAttributes.Sealed, null);

            typeBuilder.AddInterfaceImplementation(interfaceType);

            var busTypeField = typeBuilder.DefineField("bus", typeof(BusInstance), FieldAttributes.Private | FieldAttributes.InitOnly);
            var networkTypeField = typeBuilder.DefineField("networkType", typeof(NetworkType), FieldAttributes.Private | FieldAttributes.InitOnly);
            var sourceField = typeBuilder.DefineField("source", typeof(string), FieldAttributes.Private | FieldAttributes.InitOnly);
            var busLoggingField = typeBuilder.DefineField("busLogging", typeof(BusLogging), FieldAttributes.Private | FieldAttributes.InitOnly);

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard, new Type[] { typeof(BusInstance), typeof(NetworkType), typeof(string), typeof(BusLogging) });
            {
                constructorBuilder.DefineParameter(1, ParameterAttributes.None, "bus");
                constructorBuilder.DefineParameter(2, ParameterAttributes.None, "networkType");
                constructorBuilder.DefineParameter(3, ParameterAttributes.None, "source");
                constructorBuilder.DefineParameter(4, ParameterAttributes.None, "busLogging");

                var il = constructorBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, objectConstructor);
                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Nop);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, busTypeField);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Stfld, networkTypeField);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Stfld, sourceField);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg, 4);
                il.Emit(OpCodes.Stfld, busLoggingField);

                il.Emit(OpCodes.Ret);
            }

            foreach (var method in methods)
            {
                var methodName = method.Name;
                var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();

                var methodBuilder = typeBuilder.DefineMethod(
                    methodName,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.Standard | CallingConventions.HasThis,
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

                // bus._DispatchEventInternalAsync(@event, eventType, networkType, source, busLogging)
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, busTypeField); //bus

                il.Emit(OpCodes.Ldarg_1); //@event
                il.Emit(OpCodes.Ldloc_0); //eventType

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, networkTypeField); //networkType

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, sourceField); //source

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, busLoggingField); //busLogging

                il.Emit(OpCodes.Call, dispatchEventInternalAsyncMethod);
                il.Emit(OpCodes.Ret);

                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }

            Type objectType = typeBuilder.CreateTypeInfo() ?? throw new Exception("Failed to CreateTypeInfo");
            return objectType;
        }

        private sealed class LookupKey
        {
            public Type InterfaceType { get; private set; }
            public bool RequireAffirmation { get; private set; }
            public NetworkType NetworkType { get; private set; }
            public string Source { get; private set; }

            public LookupKey(Type interfaceType, bool requireAffirmation, NetworkType networkType, string source)
            {
                this.InterfaceType = interfaceType;
                this.RequireAffirmation = requireAffirmation;
                this.NetworkType = networkType;
                this.Source = source;
            }

            public override bool Equals(object? obj)
            {
                if (obj is not LookupKey objCasted)
                    return false;

                return
                    InterfaceType == objCasted.InterfaceType &&
                    RequireAffirmation == objCasted.RequireAffirmation &&
                    NetworkType == objCasted.NetworkType &&
                    Source == objCasted.Source;
            }
            public override int GetHashCode()
            {
#if NETSTANDARD2_0
                unchecked
                {
                    return InterfaceType.GetHashCode() * RequireAffirmation.GetHashCode() * NetworkType.GetHashCode() * Source.GetHashCode();
                }
#else
                return HashCode.Combine(InterfaceType, RequireAffirmation, NetworkType, Source);
#endif
            }
        }
    }
}

// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Zerra.Collections;

namespace Zerra.Reflection
{
    public static class EmptyImplementations
    {
        private static readonly Type taskType = typeof(Task);
        private static readonly Type taskGenericType = typeof(Task<>);
        private static readonly ConcurrentFactoryDictionary<Type, Type> emptyImplementations = new();
        public static Type GetEmptyImplementationType<TInterface>()
        {
            return GetEmptyImplementationType(typeof(TInterface));
        }
        public static Type GetEmptyImplementationType(Type interfaceType)
        {
            var classType = emptyImplementations.GetOrAdd(interfaceType, (interfaceType) =>
            {
                return GenerateEmptyImplementation(interfaceType);
            });
            return classType;
        }
        public static TInterface GetEmptyImplementation<TInterface>()
        {
            return (TInterface)GetEmptyImplementation(typeof(TInterface));
        }
        public static object GetEmptyImplementation(Type interfaceType)
        {
            var classType = emptyImplementations.GetOrAdd(interfaceType, (interfaceType) =>
            {
                return GenerateEmptyImplementation(interfaceType);
            });
            var instance = Instantiator.Create(classType);
            return instance;
        }
        private static Type GenerateEmptyImplementation(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            var typeSignature = interfaceType.FullName + "_EmptyImplementation";

            var moduleBuilder = GeneratedAssembly.GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, null);

            typeBuilder.AddInterfaceImplementation(interfaceType);

            _ = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            var methods = interfaceType.GetMethods().ToList();
            var properties = interfaceType.GetProperties().ToList();
            foreach (var @interface in interfaceType.GetInterfaces())
            {
                methods.AddRange(@interface.GetMethods());
                properties.AddRange(@interface.GetProperties());
            }

            foreach (var method in methods)
            {
                if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                    continue;

                var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();

                var voidMethod = method.ReturnType.Name == "Void";

                var methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.HasThis,
                    voidMethod ? null : method.ReturnType,
                    parameterTypes
                );
                methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

                var methodBuilderIL = methodBuilder.GetILGenerator();

                if (voidMethod)
                {
                    methodBuilderIL.Emit(OpCodes.Ret);
                }
                else if (method.ReturnType == taskType)
                {
                    var getCompletedTaskMethod = taskType.GetProperty(nameof(Task.CompletedTask))!.GetGetMethod()!;
                    methodBuilderIL.Emit(OpCodes.Call, getCompletedTaskMethod);
                    methodBuilderIL.Emit(OpCodes.Ret);
                }
                else if (method.ReturnType.Name == taskGenericType.Name)
                {
                    var taskInnerType = method.ReturnType.GetGenericArguments()[0];
                    var getFromResultsTaskMethod = taskType.GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(taskInnerType);

                    EmitDefault(methodBuilderIL, method.ReturnType.GetGenericArguments()[0]);
                    methodBuilderIL.Emit(OpCodes.Call, getFromResultsTaskMethod);
                    methodBuilderIL.Emit(OpCodes.Ret);
                }
                else
                {
                    EmitDefault(methodBuilderIL, method.ReturnType);
                    methodBuilderIL.Emit(OpCodes.Ret);
                }

                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }

            foreach (var property in properties)
            {
                var fieldBuilder = typeBuilder.DefineField(
                    $"<{property.Name}>k__BackingField",
                    property.PropertyType,
                    FieldAttributes.Private
                );

                var propertyBuilder = typeBuilder.DefineProperty(
                    property.Name,
                    PropertyAttributes.HasDefault,
                    property.PropertyType,
                    null
                );

                var getMethodName = "get_" + property.Name;
                var getMethodBuilder = typeBuilder.DefineMethod(
                    getMethodName,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    property.PropertyType,
                    Type.EmptyTypes
                );
                var getMethodBuilderIL = getMethodBuilder.GetILGenerator();
                getMethodBuilderIL.Emit(OpCodes.Ldarg_0);
                getMethodBuilderIL.Emit(OpCodes.Ldfld, fieldBuilder);
                getMethodBuilderIL.Emit(OpCodes.Ret);

                var getMethod = methods.FirstOrDefault(x => x.Name == getMethodName);
                if (getMethod is not null)
                    typeBuilder.DefineMethodOverride(getMethodBuilder, getMethod);

                var setMethodName = "set_" + property.Name;
                var setMethodBuilder = typeBuilder.DefineMethod(
                    setMethodName,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    null,
                    [property.PropertyType]
                );
                var setMethodBuilderIL = setMethodBuilder.GetILGenerator();
                setMethodBuilderIL.Emit(OpCodes.Ldarg_0);
                setMethodBuilderIL.Emit(OpCodes.Ldarg_1);
                setMethodBuilderIL.Emit(OpCodes.Stfld, fieldBuilder);
                setMethodBuilderIL.Emit(OpCodes.Ret);

                var setMethod = methods.FirstOrDefault(x => x.Name == setMethodName);
                if (setMethod is not null)
                    typeBuilder.DefineMethodOverride(setMethodBuilder, setMethod);

                propertyBuilder.SetGetMethod(getMethodBuilder);
                propertyBuilder.SetSetMethod(setMethodBuilder);
            }

            Type objectType = typeBuilder.CreateTypeInfo()!;
            return objectType;
        }
        private static void EmitDefault(ILGenerator il, Type type)
        {
            if (TypeLookup.CoreTypeLookup(type, out var coreType))
            {
                switch (coreType)
                {
                    case CoreType.Boolean:
                    case CoreType.Byte:
                    case CoreType.SByte:
                    case CoreType.Int16:
                    case CoreType.UInt16:
                    case CoreType.Int32:
                    case CoreType.UInt32:
                        il.Emit(OpCodes.Ldc_I4, 0);
                        return;
                    case CoreType.Int64:
                    case CoreType.UInt64:
                        il.Emit(OpCodes.Ldc_I4, 0);
                        il.Emit(OpCodes.Conv_I8);
                        return;
                    case CoreType.Single:
                        il.Emit(OpCodes.Ldc_R4, 0f);
                        return;
                    case CoreType.Double:
                        il.Emit(OpCodes.Ldc_R8, 0d);
                        return;
                    case CoreType.Decimal:
                        il.Emit(OpCodes.Ldc_I4, 0);
                        return;
                    case CoreType.Char:
                        il.Emit(OpCodes.Ldc_I4, 0);
                        return;
                    case CoreType.DateTime:
                    case CoreType.DateTimeOffset:
                    case CoreType.TimeSpan:
#if NET6_0_OR_GREATER
                    case CoreType.DateOnly:
                    case CoreType.TimeOnly:
#endif
                    case CoreType.Guid:
                        _ = il.DeclareLocal(type);
                        _ = il.DeclareLocal(type);
                        il.Emit(OpCodes.Ldloca_S, 0);
                        il.Emit(OpCodes.Initobj, type);
                        il.Emit(OpCodes.Ldloc_0);
                        il.Emit(OpCodes.Stloc_1);
                        il.Emit(OpCodes.Ldloc_1);
                        return;
                    case CoreType.String:
                    default:
                        il.Emit(OpCodes.Ldnull);
                        return;
                }
            }

            il.Emit(OpCodes.Ldnull);
        }
    }
}

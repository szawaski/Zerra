// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra.Repository.Reflection
{
    public static class CoreTypeSetterGenerator
    {
        private static readonly ConcurrentFactoryDictionary<MemberInfo, Type> generatedTypes = new ConcurrentFactoryDictionary<MemberInfo, Type>();
        public static object Get(MemberInfo memberInfo, CoreType? coreType, bool isByteArray)
        {
            var generatedType = generatedTypes.GetOrAdd(memberInfo, (t) =>
            {
                return Generate(memberInfo, coreType, isByteArray);
            });
            var instance = Instantiator.CreateInstance(generatedType);
            return instance;
        }

        private static readonly Type byteArrayType = typeof(byte[]);
        private static readonly Type iCoreTypeSetterType = typeof(ICoreTypeSetter<>);
        private static readonly ConstructorInfo coreTypeNullableConstructor = typeof(CoreType?).GetConstructors()[0];
        private static readonly ConstructorInfo notSupportedExceptionConstructor = typeof(NotSupportedException).GetConstructors()[0];
        private static Type Generate(MemberInfo memberInfo, CoreType? coreType, bool isByteArray)
        {
            var interfaceType = TypeAnalyzer.GetGenericType(iCoreTypeSetterType, memberInfo.ReflectedType);

            var typeSignature = $"{interfaceType.FullName}_{memberInfo.Name}_CoreTypeSetterGenerator";

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

                var methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.HasThis,
                    null,
                    parameterTypes
                );
                methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

                var il = methodBuilder.GetILGenerator();

                CoreType? methodCoreType;
                bool methodIsByteArray;
                if (TypeLookup.CoreTypeLookup(parameterTypes[1], out var coreTypeLookup))
                {
                    methodCoreType = coreTypeLookup;
                    methodIsByteArray = false;
                }
                else
                {
                    methodCoreType = null;
                    methodIsByteArray = parameterTypes[1] == byteArrayType;
                }

                if (coreType == methodCoreType && isByteArray == methodIsByteArray)
                {
                    if (memberInfo.MemberType == MemberTypes.Property)
                    {
                        var propertyInfo = (PropertyInfo)memberInfo;
                        var setMethod = propertyInfo.GetSetMethod(true);
                        if (!setMethod.IsStatic)
                            il.Emit(OpCodes.Ldarg_1);

                        il.Emit(OpCodes.Ldarg_2);

                        if (setMethod.IsFinal || !setMethod.IsVirtual)
                            il.Emit(OpCodes.Call, setMethod);
                        else
                            il.Emit(OpCodes.Callvirt, setMethod);

                        il.Emit(OpCodes.Ret);
                    }
                    else if (memberInfo.MemberType == MemberTypes.Field)
                    {
                        var fieldInfo = (FieldInfo)memberInfo;
                        if (!fieldInfo.IsStatic)
                        {
                            il.Emit(OpCodes.Ldarg_1);
                            if (fieldInfo.ReflectedType.IsValueType)
                                il.Emit(OpCodes.Unbox, fieldInfo.ReflectedType);
                        }

                        il.Emit(OpCodes.Ldarg_2);

                        if (!fieldInfo.IsStatic)
                            il.Emit(OpCodes.Stfld, fieldInfo);
                        else
                            il.Emit(OpCodes.Stsfld, fieldInfo);

                        il.Emit(OpCodes.Ret);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    il.Emit(OpCodes.Newobj, notSupportedExceptionConstructor);
                    il.Emit(OpCodes.Throw);
                }

                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }

            foreach (var property in properties)
            {
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


                if (property.Name == nameof(ICoreTypeSetter<object>.CoreType))
                {
                    if (coreType.HasValue)
                    {
                        getMethodBuilderIL.Emit(OpCodes.Ldc_I4, (int)coreType.Value);
                        getMethodBuilderIL.Emit(OpCodes.Newobj, coreTypeNullableConstructor);
                    }
                    else
                    {
                        getMethodBuilderIL.Emit(OpCodes.Ldnull);
                    }
                }
                else if (property.Name == nameof(ICoreTypeSetter<object>.IsByteArray))
                {
                    if (isByteArray)
                        getMethodBuilderIL.Emit(OpCodes.Ldc_I4_1);
                    else
                        getMethodBuilderIL.Emit(OpCodes.Ldc_I4_0);
                }
                else
                {
                    throw new NotImplementedException();
                }

                getMethodBuilderIL.Emit(OpCodes.Ret);

                var getMethod = methods.FirstOrDefault(x => x.Name == getMethodName);
                typeBuilder.DefineMethodOverride(getMethodBuilder, getMethod);

                propertyBuilder.SetGetMethod(getMethodBuilder);
            }

            Type objectType = typeBuilder.CreateTypeInfo();
            return objectType;
        }
    }
}

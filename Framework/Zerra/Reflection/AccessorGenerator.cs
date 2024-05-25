// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

// Includes method modified from origional in Newtonsoft.Json Copyright © 2007 James Newton-King.
// https://github.com/JamesNK/Newtonsoft.Json/blob/master/Src/Newtonsoft.Json/Utilities/DynamicReflectionDelegateFactory.cs
// Copyright (c) 2007 James Newton-King

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Zerra.Reflection
{
    internal static class AccessorGenerator
    {
        public static Func<object, object?>? GenerateGetter(PropertyInfo propertyInfo)
        {
            if (propertyInfo.ReflectedType == null)
                return null;

            if (!propertyInfo.CanRead)
                return null;

            var dynamicMethod = new DynamicMethod($"{propertyInfo.ReflectedType.Name}.{propertyInfo.Name}.Getter", typeof(object), new Type[] { typeof(object) }, typeof(object), true);
            var il = dynamicMethod.GetILGenerator();

            var getMethod = propertyInfo.GetGetMethod(true);
            if (getMethod == null)
                return null;

            if (!getMethod.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                if (propertyInfo.ReflectedType.IsValueType)
                    il.Emit(OpCodes.Unbox, propertyInfo.ReflectedType);
                else
                    il.Emit(OpCodes.Castclass, propertyInfo.ReflectedType);
            }

            if (getMethod.IsFinal || !getMethod.IsVirtual)
                il.Emit(OpCodes.Call, getMethod);
            else
                il.Emit(OpCodes.Callvirt, getMethod);

            if (propertyInfo.PropertyType.IsValueType)
                il.Emit(OpCodes.Box, propertyInfo.PropertyType);
            else
                il.Emit(OpCodes.Castclass, propertyInfo.PropertyType);

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Func<object, object?>));
            return (Func<object, object?>)method;
        }
        public static Func<T, TValue?>? GenerateGetter<T, TValue>(PropertyInfo propertyInfo)
        {
            if (propertyInfo.ReflectedType == null)
                return null;

            if (!propertyInfo.CanRead)
                return null;

            var dynamicMethod = new DynamicMethod($"{propertyInfo.ReflectedType.Name}.{propertyInfo.Name}.Getter`2", propertyInfo.PropertyType, new Type[] { propertyInfo.ReflectedType }, propertyInfo.ReflectedType, true);
            var il = dynamicMethod.GetILGenerator();

            var getMethod = propertyInfo.GetGetMethod(true);
            if (getMethod == null)
                return null;

            if (!getMethod.IsStatic)
                il.Emit(OpCodes.Ldarg_0);

            if (getMethod.IsFinal || !getMethod.IsVirtual)
                il.Emit(OpCodes.Call, getMethod);
            else
                il.Emit(OpCodes.Callvirt, getMethod);

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Func<T, TValue?>));
            return (Func<T, TValue?>)method;
        }

        public static Action<object, object?>? GenerateSetter(PropertyInfo propertyInfo)
        {
            if (propertyInfo.ReflectedType == null)
                return null;

            if (!propertyInfo.CanWrite)
                return null;

            var dynamicMethod = new DynamicMethod($"{propertyInfo.ReflectedType.Name}.{propertyInfo.Name}.Setter", null, new Type[] { typeof(object), typeof(object) }, typeof(object), true);
            var il = dynamicMethod.GetILGenerator();

            var setMethod = propertyInfo.GetSetMethod(true);
            if (setMethod == null)
                return null;

            if (!setMethod.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                if (propertyInfo.ReflectedType.IsValueType)
                    il.Emit(OpCodes.Unbox, propertyInfo.ReflectedType);
                else
                    il.Emit(OpCodes.Castclass, propertyInfo.ReflectedType);
            }

            il.Emit(OpCodes.Ldarg_1);
            if (propertyInfo.PropertyType.IsValueType)
                il.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);
            else
                il.Emit(OpCodes.Castclass, propertyInfo.PropertyType);

            if (setMethod.IsFinal || !setMethod.IsVirtual)
                il.Emit(OpCodes.Call, setMethod);
            else
                il.Emit(OpCodes.Callvirt, setMethod);

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Action<object, object?>));
            return (Action<object, object?>)method;
        }
        public static Action<T, TValue?>? GenerateSetter<T, TValue>(PropertyInfo propertyInfo)
        {
            if (propertyInfo.ReflectedType == null)
                return null;

            if (!propertyInfo.CanWrite)
                return null;

            var dynamicMethod = new DynamicMethod($"{propertyInfo.ReflectedType.Name}.{propertyInfo.Name}.Setter`2", null, new Type[] { propertyInfo.ReflectedType, propertyInfo.PropertyType }, propertyInfo.ReflectedType, true);
            var il = dynamicMethod.GetILGenerator();

            var setMethod = propertyInfo.GetSetMethod(true);
            if (setMethod == null)
                return null;

            if (!setMethod.IsStatic)
                il.Emit(OpCodes.Ldarg_0);

            il.Emit(OpCodes.Ldarg_1);

            if (setMethod.IsFinal || !setMethod.IsVirtual)
                il.Emit(OpCodes.Call, setMethod);
            else
                il.Emit(OpCodes.Callvirt, setMethod);

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Action<T, TValue?>));
            return (Action<T, TValue?>)method;
        }

        public static Func<object, object?>? GenerateGetter(FieldInfo fieldInfo)
        {
            if (fieldInfo.ReflectedType == null)
                return null;

            var dynamicMethod = new DynamicMethod($"{fieldInfo.ReflectedType.Name}.{fieldInfo.Name}.Getter", typeof(object), new Type[] { typeof(object) }, typeof(object), true);
            var il = dynamicMethod.GetILGenerator();

            if (!fieldInfo.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                if (fieldInfo.ReflectedType.IsValueType)
                    il.Emit(OpCodes.Unbox, fieldInfo.ReflectedType);
                else
                    il.Emit(OpCodes.Castclass, fieldInfo.ReflectedType);
            }

            if (!fieldInfo.IsStatic)
                il.Emit(OpCodes.Ldfld, fieldInfo);
            else
                il.Emit(OpCodes.Ldsfld, fieldInfo);

            if (fieldInfo.FieldType.IsValueType)
                il.Emit(OpCodes.Box, fieldInfo.FieldType);
            else
                il.Emit(OpCodes.Castclass, fieldInfo.FieldType);

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Func<object, object?>));
            return (Func<object, object?>)method;
        }
        public static Func<T, TValue?>? GenerateGetter<T, TValue>(FieldInfo fieldInfo)
        {
            if (fieldInfo.ReflectedType == null || fieldInfo.DeclaringType == null)
                return null;

            var dynamicMethod = new DynamicMethod($"{fieldInfo.ReflectedType.Name}.{fieldInfo.Name}.Getter`2", fieldInfo.FieldType, new Type[] { fieldInfo.ReflectedType }, fieldInfo.ReflectedType, true);
            var il = dynamicMethod.GetILGenerator();

            if (!fieldInfo.IsStatic)
                il.Emit(OpCodes.Ldarg_0);

            if (!fieldInfo.IsStatic)
                il.Emit(OpCodes.Ldfld, fieldInfo);
            else
                il.Emit(OpCodes.Ldsfld, fieldInfo);

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Func<T, TValue?>));
            return (Func<T, TValue?>)method;
        }

        public static Action<object, object?>? GenerateSetter(FieldInfo fieldInfo)
        {
            if (fieldInfo.ReflectedType == null)
                return null;

            var dynamicMethod = new DynamicMethod($"{fieldInfo.ReflectedType.Name}.{fieldInfo.Name}.Setter", null, new Type[] { typeof(object), typeof(object) }, typeof(object), true);
            var il = dynamicMethod.GetILGenerator();

            if (!fieldInfo.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                if (fieldInfo.ReflectedType.IsValueType)
                    il.Emit(OpCodes.Unbox, fieldInfo.ReflectedType);
                else
                    il.Emit(OpCodes.Castclass, fieldInfo.ReflectedType);
            }

            il.Emit(OpCodes.Ldarg_1);
            if (fieldInfo.FieldType.IsValueType)
                il.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
            else
                il.Emit(OpCodes.Castclass, fieldInfo.FieldType);

            if (!fieldInfo.IsStatic)
                il.Emit(OpCodes.Stfld, fieldInfo);
            else
                il.Emit(OpCodes.Stsfld, fieldInfo);

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Action<object, object?>));
            return (Action<object, object?>)method;
        }
        public static Action<T, TValue?>? GenerateSetter<T, TValue>(FieldInfo fieldInfo)
        {
            if (fieldInfo.ReflectedType == null)
                return null;

            var dynamicMethod = new DynamicMethod($"{fieldInfo.ReflectedType.Name}.{fieldInfo.Name}.Setter`2", null, new Type[] { fieldInfo.ReflectedType, fieldInfo.FieldType }, fieldInfo.ReflectedType, true);
            var il = dynamicMethod.GetILGenerator();

            if (!fieldInfo.IsStatic)
                il.Emit(OpCodes.Ldarg_0);

            il.Emit(OpCodes.Ldarg_1);

            if (!fieldInfo.IsStatic)
                il.Emit(OpCodes.Stfld, fieldInfo);
            else
                il.Emit(OpCodes.Stsfld, fieldInfo);

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Action<T, TValue?>));
            return (Action<T, TValue?>)method;
        }

        public static Func<object?[]?, object>? GenerateCreator(ConstructorInfo constructorInfo)
        {
            if (constructorInfo.DeclaringType == null)
                return null;

            var dynamicMethod = new DynamicMethod($"{constructorInfo.DeclaringType.Name}.{constructorInfo.Name}.Creator", typeof(object), new Type[] { typeof(object[]) }, true);
            var il = dynamicMethod.GetILGenerator();

            var success = GenerateMethod(il, constructorInfo);
            if (!success)
                return null;

            var creator = dynamicMethod.CreateDelegate(typeof(Func<object?[]?, object>));
            return (Func<object?[]?, object>)creator;
        }
        public static Func<object?[]?, T>? GenerateCreator<T>(ConstructorInfo constructorInfo)
        {
            if (constructorInfo.DeclaringType == null)
                return null;

            var dynamicMethod = new DynamicMethod($"{constructorInfo.DeclaringType.Name}.{constructorInfo.Name}.Creator`1", typeof(T), new Type[] { typeof(object[]) }, true);
            var il = dynamicMethod.GetILGenerator();

            var success = GenerateMethod<T>(il, constructorInfo);
            if (!success)
                return null;

            var creator = dynamicMethod.CreateDelegate(typeof(Func<object?[]?, T>));
            return (Func<object?[]?, T>)creator;
        }

        public static Func<object?, object?[]?, object?>? GenerateCaller(MethodInfo methodInfo)
        {
            if (methodInfo.ReflectedType == null)
                return null;

            var dynamicMethod = new DynamicMethod($"{methodInfo.ReflectedType.Name}.{methodInfo.Name}.Caller", typeof(object), new Type[] { typeof(object), typeof(object[]) }, true);
            var il = dynamicMethod.GetILGenerator();

            var success = GenerateMethod(il, methodInfo);
            if (!success)
                return null;

            var caller = dynamicMethod.CreateDelegate(typeof(Func<object?, object?[]?, object?>));
            return (Func<object?, object?[]?, object?>)caller;
        }
        public static Func<T?, object?[]?, object?>? GenerateCaller<T>(MethodInfo methodInfo)
        {
            if (methodInfo.ReflectedType == null)
                return null;

            var dynamicMethod = new DynamicMethod($"{methodInfo.ReflectedType.Name}.{methodInfo.Name}.Caller`1", typeof(object), new Type[] { methodInfo.ReflectedType, typeof(object[]) }, true);
            var il = dynamicMethod.GetILGenerator();

            var success = GenerateMethod<T>(il, methodInfo);
            if (!success)
                return null;

            var caller = dynamicMethod.CreateDelegate(typeof(Func<T?, object?[]?, object?>));
            return (Func<T?, object?[]?, object?>)caller;
        }

        // Modified from origional method in Newtonsoft.Json Copyright © 2007 James Newton-King.
        // https://github.com/JamesNK/Newtonsoft.Json/blob/master/Src/Newtonsoft.Json/Utilities/DynamicReflectionDelegateFactory.cs
        // Copyright (c) 2007 James Newton-King
        //
        // Permission is hereby granted, free of charge, to any person
        // obtaining a copy of this software and associated documentation
        // files (the "Software"), to deal in the Software without
        // restriction, including without limitation the rights to use,
        // copy, modify, merge, publish, distribute, sublicense, and/or sell
        // copies of the Software, and to permit persons to whom the
        // Software is furnished to do so, subject to the following
        // conditions:
        //
        // The above copyright notice and this permission notice shall be
        // included in all copies or substantial portions of the Software.
        //
        // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
        // EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
        // OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
        // NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
        // HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
        // WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
        // FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
        // OTHER DEALINGS IN THE SOFTWARE.
        public static bool GenerateMethod(ILGenerator il, MethodBase methodBase)
        {
            //Constructor: object Thing(object[] args)
            //Method: object DoSomething(object instance, object[] args)
            var loadArgsIndex = methodBase.IsConstructor ? 0 : 1;

            var parameters = methodBase.GetParameters();

            // throw an error if the number of argument values doesn't match method parameters
            var argsOk = il.DefineLabel();
            var argsNull = il.DefineLabel();
            var argsLengthCheck = il.DefineLabel();

            il.Emit(OpCodes.Ldarg, loadArgsIndex);
            il.Emit(OpCodes.Brfalse_S, argsNull);

            //Set argument count as length of argument object[]
            il.Emit(OpCodes.Ldarg, loadArgsIndex);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Br_S, argsLengthCheck);

            //Set argument count as 0 because argument object[] is null
            il.MarkLabel(argsNull);
            il.Emit(OpCodes.Ldc_I4_0);

            il.MarkLabel(argsLengthCheck);
            il.Emit(OpCodes.Ldc_I4, parameters.Length);
            il.Emit(OpCodes.Beq, argsOk);
            il.Emit(OpCodes.Newobj, typeof(TargetParameterCountException).GetConstructor(Type.EmptyTypes)!);
            il.Emit(OpCodes.Throw);
            il.MarkLabel(argsOk);

            if (!methodBase.IsConstructor && !methodBase.IsStatic)
            {
                if (methodBase.DeclaringType == null)
                    return false;

                il.Emit(OpCodes.Ldarg_0);

                if (methodBase.DeclaringType.IsValueType)
                    il.Emit(OpCodes.Unbox, methodBase.DeclaringType);
                else
                    il.Emit(OpCodes.Castclass, methodBase.DeclaringType);
            }

            var localConvertible = il.DeclareLocal(typeof(IConvertible));
            var localObject = il.DeclareLocal(typeof(object));

            var variableAddressOpCode = parameters.Length < 256 ? OpCodes.Ldloca_S : OpCodes.Ldloca;
            var variableLoadOpCode = parameters.Length < 256 ? OpCodes.Ldloc_S : OpCodes.Ldloc;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var parameterType = parameter.ParameterType;

                if (parameterType.IsByRef)
                {
                    parameterType = parameterType.GetElementType();
                    if (parameterType == null)
                        return false;

                    var localVariable = il.DeclareLocal(parameterType);

                    // don't need to set variable for 'out' parameter
                    if (!parameter.IsOut)
                    {
                        il.Emit(OpCodes.Ldarg, loadArgsIndex);
                        il.Emit(OpCodes.Ldc_I4, i);
                        il.Emit(OpCodes.Ldelem_Ref);

                        if (parameterType.IsValueType)
                        {
                            var skipSettingDefault = il.DefineLabel();
                            var finishedProcessingParameter = il.DefineLabel();

                            // check if parameter is not null
                            il.Emit(OpCodes.Brtrue_S, skipSettingDefault);

                            // parameter has no value, initialize to default
                            il.Emit(variableAddressOpCode, localVariable);
                            il.Emit(OpCodes.Initobj, parameterType);
                            il.Emit(OpCodes.Br_S, finishedProcessingParameter);

                            // parameter has value, get value from array again and unbox and set to variable
                            il.MarkLabel(skipSettingDefault);
                            il.Emit(OpCodes.Ldarg, loadArgsIndex);
                            il.Emit(OpCodes.Ldc_I4, i);
                            il.Emit(OpCodes.Ldelem_Ref);

                            il.Emit(OpCodes.Unbox_Any, parameterType);

                            il.Emit(OpCodes.Stloc_S, localVariable);

                            il.MarkLabel(finishedProcessingParameter);
                        }
                        else
                        {
                            il.Emit(OpCodes.Castclass, parameterType);
                            il.Emit(OpCodes.Stloc_S, localVariable);
                        }
                    }

                    il.Emit(variableAddressOpCode, localVariable);
                }
                else if (parameterType.IsValueType)
                {
                    il.Emit(OpCodes.Ldarg, loadArgsIndex);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);
                    il.Emit(OpCodes.Stloc_S, localObject);

                    // have to check that value type parameters aren't null
                    // otherwise they will error when unboxed
                    var skipSettingDefault = il.DefineLabel();
                    var finishedProcessingParameter = il.DefineLabel();

                    // check if parameter is not null
                    il.Emit(OpCodes.Ldloc_S, localObject);
                    il.Emit(OpCodes.Brtrue_S, skipSettingDefault);

                    // parameter has no value, initialize to default
                    var localVariable = il.DeclareLocal(parameterType);
                    il.Emit(variableAddressOpCode, localVariable);
                    il.Emit(OpCodes.Initobj, parameterType);
                    il.Emit(variableLoadOpCode, localVariable);
                    il.Emit(OpCodes.Br_S, finishedProcessingParameter);

                    // argument has value, try to convert it to parameter type
                    il.MarkLabel(skipSettingDefault);

                    if (parameterType.IsPrimitive)
                    {
                        // for primitive types we need to handle type widening (e.g. short -> int)
                        var toParameterTypeMethod = typeof(IConvertible).GetMethod("To" + parameterType.Name, new[] { typeof(IFormatProvider) });

                        if (toParameterTypeMethod != null)
                        {
                            var skipConvertible = il.DefineLabel();

                            // check if argument type is an exact match for parameter type
                            // in this case we may use cheap unboxing instead
                            il.Emit(OpCodes.Ldloc_S, localObject);
                            il.Emit(OpCodes.Isinst, parameterType);
                            il.Emit(OpCodes.Brtrue_S, skipConvertible);

                            // types don't match, check if argument implements IConvertible
                            il.Emit(OpCodes.Ldloc_S, localObject);
                            il.Emit(OpCodes.Isinst, typeof(IConvertible));
                            il.Emit(OpCodes.Stloc_S, localConvertible);
                            il.Emit(OpCodes.Ldloc_S, localConvertible);
                            il.Emit(OpCodes.Brfalse_S, skipConvertible);

                            // convert argument to parameter type
                            il.Emit(OpCodes.Ldloc_S, localConvertible);
                            il.Emit(OpCodes.Ldnull);
                            il.Emit(OpCodes.Callvirt, toParameterTypeMethod);
                            il.Emit(OpCodes.Br_S, finishedProcessingParameter);

                            il.MarkLabel(skipConvertible);
                        }
                    }

                    // we got here because either argument type matches parameter (conversion will succeed),
                    // or argument type doesn't match parameter, but we're out of options (conversion will fail)
                    il.Emit(OpCodes.Ldloc_S, localObject);

                    if (parameterType.IsValueType)
                        il.Emit(OpCodes.Unbox_Any, parameterType);
                    else
                        il.Emit(OpCodes.Castclass, parameterType);

                    il.MarkLabel(finishedProcessingParameter);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg, loadArgsIndex);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);

                    if (parameterType.IsValueType)
                        il.Emit(OpCodes.Unbox_Any, parameterType);
                    else
                        il.Emit(OpCodes.Castclass, parameterType);
                }
            }

            if (methodBase.IsConstructor)
            {
                il.Emit(OpCodes.Newobj, (ConstructorInfo)methodBase);
            }
            else
            {
                if (methodBase.IsFinal || !methodBase.IsVirtual)
                    il.Emit(OpCodes.Call, (MethodInfo)methodBase);
                else
                    il.Emit(OpCodes.Callvirt, (MethodInfo)methodBase);
            }

            var returnType = methodBase.IsConstructor ? methodBase.DeclaringType : ((MethodInfo)methodBase).ReturnType;

            if (returnType != null && returnType != typeof(void))
            {
                if (returnType.IsValueType)
                    il.Emit(OpCodes.Box, returnType);
                else
                    il.Emit(OpCodes.Castclass, returnType);
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
            }

            il.Emit(OpCodes.Ret);

            return true;
        }
        public static bool GenerateMethod<T>(ILGenerator il, MethodBase methodBase)
        {
            //Constructor: object Thing(object[] args)
            //Method: object DoSomething(object instance, object[] args)
            var loadArgsIndex = methodBase.IsConstructor ? 0 : 1;

            var parameters = methodBase.GetParameters();

            // throw an error if the number of argument values doesn't match method parameters
            var argsOk = il.DefineLabel();
            var argsNull = il.DefineLabel();
            var argsLengthCheck = il.DefineLabel();

            il.Emit(OpCodes.Ldarg, loadArgsIndex);
            il.Emit(OpCodes.Brfalse_S, argsNull);

            //Set argument count as length of argument object[]
            il.Emit(OpCodes.Ldarg, loadArgsIndex);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Br_S, argsLengthCheck);

            //Set argument count as 0 because argument object[] is null
            il.MarkLabel(argsNull);
            il.Emit(OpCodes.Ldc_I4_0);

            il.MarkLabel(argsLengthCheck);
            il.Emit(OpCodes.Ldc_I4, parameters.Length);
            il.Emit(OpCodes.Beq, argsOk);
            il.Emit(OpCodes.Newobj, typeof(TargetParameterCountException).GetConstructor(Type.EmptyTypes)!);
            il.Emit(OpCodes.Throw);
            il.MarkLabel(argsOk);

            if (!methodBase.IsConstructor && !methodBase.IsStatic)
            {
                if (methodBase.DeclaringType == null)
                    return false;

                il.Emit(OpCodes.Ldarg_0);
            }

            var localConvertible = il.DeclareLocal(typeof(IConvertible));
            var localObject = il.DeclareLocal(typeof(T));

            var variableAddressOpCode = parameters.Length < 256 ? OpCodes.Ldloca_S : OpCodes.Ldloca;
            var variableLoadOpCode = parameters.Length < 256 ? OpCodes.Ldloc_S : OpCodes.Ldloc;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var parameterType = parameter.ParameterType;

                if (parameterType.IsByRef)
                {
                    parameterType = parameterType.GetElementType();
                    if (parameterType == null)
                        return false;

                    var localVariable = il.DeclareLocal(parameterType);

                    // don't need to set variable for 'out' parameter
                    if (!parameter.IsOut)
                    {
                        il.Emit(OpCodes.Ldarg, loadArgsIndex);
                        il.Emit(OpCodes.Ldc_I4, i);
                        il.Emit(OpCodes.Ldelem_Ref);

                        if (parameterType.IsValueType)
                        {
                            var skipSettingDefault = il.DefineLabel();
                            var finishedProcessingParameter = il.DefineLabel();

                            // check if parameter is not null
                            il.Emit(OpCodes.Brtrue_S, skipSettingDefault);

                            // parameter has no value, initialize to default
                            il.Emit(variableAddressOpCode, localVariable);
                            il.Emit(OpCodes.Initobj, parameterType);
                            il.Emit(OpCodes.Br_S, finishedProcessingParameter);

                            // parameter has value, get value from array again and unbox and set to variable
                            il.MarkLabel(skipSettingDefault);
                            il.Emit(OpCodes.Ldarg, loadArgsIndex);
                            il.Emit(OpCodes.Ldc_I4, i);
                            il.Emit(OpCodes.Ldelem_Ref);

                            il.Emit(OpCodes.Unbox_Any, parameterType);

                            il.Emit(OpCodes.Stloc_S, localVariable);

                            il.MarkLabel(finishedProcessingParameter);
                        }
                        else
                        {
                            il.Emit(OpCodes.Castclass, parameterType);
                            il.Emit(OpCodes.Stloc_S, localVariable);
                        }
                    }

                    il.Emit(variableAddressOpCode, localVariable);
                }
                else if (parameterType.IsValueType)
                {
                    il.Emit(OpCodes.Ldarg, loadArgsIndex);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);
                    il.Emit(OpCodes.Stloc_S, localObject);

                    // have to check that value type parameters aren't null
                    // otherwise they will error when unboxed
                    var skipSettingDefault = il.DefineLabel();
                    var finishedProcessingParameter = il.DefineLabel();

                    // check if parameter is not null
                    il.Emit(OpCodes.Ldloc_S, localObject);
                    il.Emit(OpCodes.Brtrue_S, skipSettingDefault);

                    // parameter has no value, initialize to default
                    var localVariable = il.DeclareLocal(parameterType);
                    il.Emit(variableAddressOpCode, localVariable);
                    il.Emit(OpCodes.Initobj, parameterType);
                    il.Emit(variableLoadOpCode, localVariable);
                    il.Emit(OpCodes.Br_S, finishedProcessingParameter);

                    // argument has value, try to convert it to parameter type
                    il.MarkLabel(skipSettingDefault);

                    if (parameterType.IsPrimitive)
                    {
                        // for primitive types we need to handle type widening (e.g. short -> int)
                        var toParameterTypeMethod = typeof(IConvertible).GetMethod("To" + parameterType.Name, new[] { typeof(IFormatProvider) });

                        if (toParameterTypeMethod != null)
                        {
                            var skipConvertible = il.DefineLabel();

                            // check if argument type is an exact match for parameter type
                            // in this case we may use cheap unboxing instead
                            il.Emit(OpCodes.Ldloc_S, localObject);
                            il.Emit(OpCodes.Isinst, parameterType);
                            il.Emit(OpCodes.Brtrue_S, skipConvertible);

                            // types don't match, check if argument implements IConvertible
                            il.Emit(OpCodes.Ldloc_S, localObject);
                            il.Emit(OpCodes.Isinst, typeof(IConvertible));
                            il.Emit(OpCodes.Stloc_S, localConvertible);
                            il.Emit(OpCodes.Ldloc_S, localConvertible);
                            il.Emit(OpCodes.Brfalse_S, skipConvertible);

                            // convert argument to parameter type
                            il.Emit(OpCodes.Ldloc_S, localConvertible);
                            il.Emit(OpCodes.Ldnull);
                            il.Emit(OpCodes.Callvirt, toParameterTypeMethod);
                            il.Emit(OpCodes.Br_S, finishedProcessingParameter);

                            il.MarkLabel(skipConvertible);
                        }
                    }

                    // we got here because either argument type matches parameter (conversion will succeed),
                    // or argument type doesn't match parameter, but we're out of options (conversion will fail)
                    il.Emit(OpCodes.Ldloc_S, localObject);

                    if (parameterType.IsValueType)
                        il.Emit(OpCodes.Unbox_Any, parameterType);
                    else
                        il.Emit(OpCodes.Castclass, parameterType);

                    il.MarkLabel(finishedProcessingParameter);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg, loadArgsIndex);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);

                    if (parameterType.IsValueType)
                        il.Emit(OpCodes.Unbox_Any, parameterType);
                    else
                        il.Emit(OpCodes.Castclass, parameterType);
                }
            }

            if (methodBase.IsConstructor)
            {
                il.Emit(OpCodes.Newobj, (ConstructorInfo)methodBase);
            }
            else
            {
                if (methodBase.IsFinal || !methodBase.IsVirtual)
                    il.Emit(OpCodes.Call, (MethodInfo)methodBase);
                else
                    il.Emit(OpCodes.Callvirt, (MethodInfo)methodBase);
            }

            var returnType = methodBase.IsConstructor ? methodBase.DeclaringType : ((MethodInfo)methodBase).ReturnType;

            if (returnType != null && returnType != typeof(void))
            {
                if (returnType.IsValueType)
                    il.Emit(OpCodes.Box, returnType);
                else
                    il.Emit(OpCodes.Castclass, returnType);
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
            }

            il.Emit(OpCodes.Ret);

            return true;
        }
    }
}
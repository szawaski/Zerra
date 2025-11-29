// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

// Includes method modified from origional in Newtonsoft.Json Copyright © 2007 James Newton-King.
// https://github.com/JamesNK/Newtonsoft.Json/blob/master/Src/Newtonsoft.Json/Utilities/DynamicReflectionDelegateFactory.cs
// Copyright (c) 2007 James Newton-King

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace Zerra.SourceGeneration.Reflection
{
    [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
    internal static class AccessorGenerator
    {
        public static Func<object, object?>? GenerateGetter(PropertyInfo propertyInfo)
        {
            if (propertyInfo.ReflectedType is null)
                return null;

            if (!propertyInfo.CanRead)
                return null;

            var dynamicMethod = new DynamicMethod($"{propertyInfo.ReflectedType.Name}.{propertyInfo.Name}.Getter", typeof(object), [typeof(object)], typeof(object), true);
            var il = dynamicMethod.GetILGenerator();

            var getMethod = propertyInfo.GetGetMethod(true);
            if (getMethod is null)
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
        public static Func<object, TValue?>? GenerateGetter<TValue>(PropertyInfo propertyInfo)
        {
            if (propertyInfo.ReflectedType is null)
                return null;

            if (!propertyInfo.CanRead)
                return null;

            var dynamicMethod = new DynamicMethod($"{propertyInfo.ReflectedType.Name}.{propertyInfo.Name}.Getter`2", propertyInfo.PropertyType, [typeof(object)], true);
            var il = dynamicMethod.GetILGenerator();

            var getMethod = propertyInfo.GetGetMethod(true);
            if (getMethod is null)
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

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Func<object, TValue?>));
            return (Func<object, TValue?>)method;
        }
        public static Func<T, TValue?>? GenerateGetter<T, TValue>(PropertyInfo propertyInfo)
        {
            if (propertyInfo.ReflectedType is null)
                return null;

            if (!propertyInfo.CanRead)
                return null;

            var dynamicMethod = new DynamicMethod($"{propertyInfo.ReflectedType.Name}.{propertyInfo.Name}.Getter`2", propertyInfo.PropertyType, [propertyInfo.ReflectedType], true);
            var il = dynamicMethod.GetILGenerator();

            var getMethod = propertyInfo.GetGetMethod(true);
            if (getMethod is null)
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
        public static Delegate? GenerateGetter(PropertyInfo propertyInfo, Type valueType)
        {
            if (propertyInfo.ReflectedType is null)
                return null;

            if (!propertyInfo.CanRead)
                return null;

            var dynamicMethod = new DynamicMethod($"{propertyInfo.ReflectedType.Name}.{propertyInfo.Name}.Getter`2", propertyInfo.PropertyType, [typeof(object)], true);
            var il = dynamicMethod.GetILGenerator();

            var getMethod = propertyInfo.GetGetMethod(true);
            if (getMethod is null)
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

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(object), valueType));
            return method;
        }
        public static Delegate? GenerateGetter(PropertyInfo propertyInfo, Type objectType, Type valueType)
        {
            if (propertyInfo.ReflectedType is null)
                return null;

            if (!propertyInfo.CanRead)
                return null;

            var dynamicMethod = new DynamicMethod($"{propertyInfo.ReflectedType.Name}.{propertyInfo.Name}.Getter`2", propertyInfo.PropertyType, [propertyInfo.ReflectedType], true);
            var il = dynamicMethod.GetILGenerator();

            var getMethod = propertyInfo.GetGetMethod(true);
            if (getMethod is null)
                return null;

            if (!getMethod.IsStatic)
                il.Emit(OpCodes.Ldarg_0);

            if (getMethod.IsFinal || !getMethod.IsVirtual)
                il.Emit(OpCodes.Call, getMethod);
            else
                il.Emit(OpCodes.Callvirt, getMethod);

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(objectType, valueType));
            return method;
        }

        public static Action<object, object?>? GenerateSetter(PropertyInfo propertyInfo)
        {
            if (propertyInfo.ReflectedType is null)
                return null;

            if (!propertyInfo.CanWrite)
                return null;

            var dynamicMethod = new DynamicMethod($"{propertyInfo.ReflectedType.Name}.{propertyInfo.Name}.Setter", null, [typeof(object), typeof(object)], true);
            var il = dynamicMethod.GetILGenerator();

            var setMethod = propertyInfo.GetSetMethod(true);
            if (setMethod is null)
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
        public static Action<object, TValue?>? GenerateSetter<TValue>(PropertyInfo propertyInfo)
        {
            if (propertyInfo.ReflectedType is null)
                return null;

            if (!propertyInfo.CanWrite)
                return null;

            var dynamicMethod = new DynamicMethod($"{propertyInfo.ReflectedType.Name}.{propertyInfo.Name}.Setter`2", null, [typeof(object), propertyInfo.PropertyType], true);
            var il = dynamicMethod.GetILGenerator();

            var setMethod = propertyInfo.GetSetMethod(true);
            if (setMethod is null)
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
            if (setMethod.IsFinal || !setMethod.IsVirtual)
                il.Emit(OpCodes.Call, setMethod);
            else
                il.Emit(OpCodes.Callvirt, setMethod);

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Action<object, TValue?>));
            return (Action<object, TValue?>)method;
        }
        public static Action<T, TValue?>? GenerateSetter<T, TValue>(PropertyInfo propertyInfo)
        {
            if (propertyInfo.ReflectedType is null)
                return null;

            if (!propertyInfo.CanWrite)
                return null;

            var dynamicMethod = new DynamicMethod($"{propertyInfo.ReflectedType.Name}.{propertyInfo.Name}.Setter`2", null, [propertyInfo.ReflectedType, propertyInfo.PropertyType], true);
            var il = dynamicMethod.GetILGenerator();

            var setMethod = propertyInfo.GetSetMethod(true);
            if (setMethod is null)
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
        public static Delegate? GenerateSetter(PropertyInfo propertyInfo, Type valueType)
        {
            if (propertyInfo.ReflectedType is null)
                return null;

            if (!propertyInfo.CanWrite)
                return null;

            var dynamicMethod = new DynamicMethod($"{propertyInfo.ReflectedType.Name}.{propertyInfo.Name}.Setter`2", null, [typeof(object), propertyInfo.PropertyType], true);
            var il = dynamicMethod.GetILGenerator();

            var setMethod = propertyInfo.GetSetMethod(true);
            if (setMethod is null)
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
            if (setMethod.IsFinal || !setMethod.IsVirtual)
                il.Emit(OpCodes.Call, setMethod);
            else
                il.Emit(OpCodes.Callvirt, setMethod);

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Action<,>).MakeGenericType(typeof(object), valueType));
            return method;
        }
        public static Delegate? GenerateSetter(PropertyInfo propertyInfo, Type objectType, Type valueType)
        {
            if (propertyInfo.ReflectedType is null)
                return null;

            if (!propertyInfo.CanWrite)
                return null;

            var dynamicMethod = new DynamicMethod($"{propertyInfo.ReflectedType.Name}.{propertyInfo.Name}.Setter`2", null, [propertyInfo.ReflectedType, propertyInfo.PropertyType], true);
            var il = dynamicMethod.GetILGenerator();

            var setMethod = propertyInfo.GetSetMethod(true);
            if (setMethod is null)
                return null;

            if (!setMethod.IsStatic)
                il.Emit(OpCodes.Ldarg_0);

            il.Emit(OpCodes.Ldarg_1);
            if (setMethod.IsFinal || !setMethod.IsVirtual)
                il.Emit(OpCodes.Call, setMethod);
            else
                il.Emit(OpCodes.Callvirt, setMethod);

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Action<,>).MakeGenericType(objectType, valueType));
            return method;
        }

        public static Func<object, object?>? GenerateGetter(FieldInfo fieldInfo)
        {
            if (fieldInfo.ReflectedType is null)
                return null;

            var dynamicMethod = new DynamicMethod($"{fieldInfo.ReflectedType.Name}.{fieldInfo.Name}.Getter", typeof(object), [typeof(object)], true);
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
        public static Func<object, TValue?>? GenerateGetter<TValue>(FieldInfo fieldInfo)
        {
            if (fieldInfo.ReflectedType is null || fieldInfo.DeclaringType is null)
                return null;

            var dynamicMethod = new DynamicMethod($"{fieldInfo.ReflectedType.Name}.{fieldInfo.Name}.Getter`2", fieldInfo.FieldType, [typeof(object)], true);
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

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Func<object, TValue?>));
            return (Func<object, TValue?>)method;
        }
        public static Func<T, TValue?>? GenerateGetter<T, TValue>(FieldInfo fieldInfo)
        {
            if (fieldInfo.ReflectedType is null || fieldInfo.DeclaringType is null)
                return null;

            var dynamicMethod = new DynamicMethod($"{fieldInfo.ReflectedType.Name}.{fieldInfo.Name}.Getter`2", fieldInfo.FieldType, [fieldInfo.ReflectedType], true);
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
        public static Delegate? GenerateGetter(FieldInfo fieldInfo, Type valueType)
        {
            if (fieldInfo.ReflectedType is null || fieldInfo.DeclaringType is null)
                return null;

            var dynamicMethod = new DynamicMethod($"{fieldInfo.ReflectedType.Name}.{fieldInfo.Name}.Getter`2", fieldInfo.FieldType, [typeof(object)], true);
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

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(object), valueType));
            return method;
        }
        public static Delegate? GenerateGetter(FieldInfo fieldInfo, Type objectType, Type valueType)
        {
            if (fieldInfo.ReflectedType is null || fieldInfo.DeclaringType is null)
                return null;

            var dynamicMethod = new DynamicMethod($"{fieldInfo.ReflectedType.Name}.{fieldInfo.Name}.Getter`2", fieldInfo.FieldType, [fieldInfo.ReflectedType], true);
            var il = dynamicMethod.GetILGenerator();

            if (!fieldInfo.IsStatic)
                il.Emit(OpCodes.Ldarg_0);

            if (!fieldInfo.IsStatic)
                il.Emit(OpCodes.Ldfld, fieldInfo);
            else
                il.Emit(OpCodes.Ldsfld, fieldInfo);

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(objectType, valueType));
            return method;
        }

        public static Action<object, object?>? GenerateSetter(FieldInfo fieldInfo)
        {
            if (fieldInfo.ReflectedType is null)
                return null;

            var dynamicMethod = new DynamicMethod($"{fieldInfo.ReflectedType.Name}.{fieldInfo.Name}.Setter", null, [typeof(object), typeof(object)], true);
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
        public static Action<object, TValue?>? GenerateSetter<TValue>(FieldInfo fieldInfo)
        {
            if (fieldInfo.ReflectedType is null)
                return null;

            var dynamicMethod = new DynamicMethod($"{fieldInfo.ReflectedType.Name}.{fieldInfo.Name}.Setter`2", null, [typeof(object), fieldInfo.FieldType], true);
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
            if (!fieldInfo.IsStatic)
                il.Emit(OpCodes.Stfld, fieldInfo);
            else
                il.Emit(OpCodes.Stsfld, fieldInfo);

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Action<object, TValue?>));
            return (Action<object, TValue?>)method;
        }
        public static Action<T, TValue?>? GenerateSetter<T, TValue>(FieldInfo fieldInfo)
        {
            if (fieldInfo.ReflectedType is null)
                return null;

            var dynamicMethod = new DynamicMethod($"{fieldInfo.ReflectedType.Name}.{fieldInfo.Name}.Setter`2", null, [fieldInfo.ReflectedType, fieldInfo.FieldType], true);
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
        public static Delegate? GenerateSetter(FieldInfo fieldInfo, Type valueType)
        {
            if (fieldInfo.ReflectedType is null)
                return null;

            var dynamicMethod = new DynamicMethod($"{fieldInfo.ReflectedType.Name}.{fieldInfo.Name}.Setter`2", null, [typeof(object), fieldInfo.FieldType], true);
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
            if (!fieldInfo.IsStatic)
                il.Emit(OpCodes.Stfld, fieldInfo);
            else
                il.Emit(OpCodes.Stsfld, fieldInfo);

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Action<,>).MakeGenericType(typeof(object), valueType));
            return method;
        }
        public static Delegate? GenerateSetter(FieldInfo fieldInfo, Type objectType, Type valueType)
        {
            if (fieldInfo.ReflectedType is null)
                return null;

            var dynamicMethod = new DynamicMethod($"{fieldInfo.ReflectedType.Name}.{fieldInfo.Name}.Setter`2", null, [fieldInfo.ReflectedType, fieldInfo.FieldType], true);
            var il = dynamicMethod.GetILGenerator();

            if (!fieldInfo.IsStatic)
                il.Emit(OpCodes.Ldarg_0);

            il.Emit(OpCodes.Ldarg_1);
            if (!fieldInfo.IsStatic)
                il.Emit(OpCodes.Stfld, fieldInfo);
            else
                il.Emit(OpCodes.Stsfld, fieldInfo);

            il.Emit(OpCodes.Ret);

            var method = dynamicMethod.CreateDelegate(typeof(Action<,>).MakeGenericType(objectType, valueType));
            return method;
        }

        public static Func<object?[]?, object>? GenerateCreator(ConstructorInfo constructorInfo)
        {
            if (constructorInfo.DeclaringType is null)
                return null;

            var dynamicMethod = new DynamicMethod($"{constructorInfo.DeclaringType.Name}.{constructorInfo.Name}.Creator", typeof(object), [typeof(object[])], true);
            var il = dynamicMethod.GetILGenerator();

            var success = GenerateMethod(il, constructorInfo);
            if (!success)
                return null;

            var creator = dynamicMethod.CreateDelegate(typeof(Func<object?[]?, object>));
            return (Func<object?[]?, object>)creator;
        }
        public static Func<object?[]?, T>? GenerateCreator<T>(ConstructorInfo constructorInfo)
        {
            if (constructorInfo.DeclaringType is null)
                return null;

            var dynamicMethod = new DynamicMethod($"{constructorInfo.DeclaringType.Name}.{constructorInfo.Name}.Creator`1", typeof(T), [typeof(object[])], true);
            var il = dynamicMethod.GetILGenerator();

            var success = GenerateMethod<T>(il, constructorInfo);
            if (!success)
                return null;

            var creator = dynamicMethod.CreateDelegate(typeof(Func<object?[]?, T>));
            return (Func<object?[]?, T>)creator;
        }

        public static Func<object>? GenerateCreatorNoArgs(ConstructorInfo constructorInfo)
        {
            if (constructorInfo.DeclaringType is null)
                return null;

            var dynamicMethod = new DynamicMethod($"{constructorInfo.DeclaringType.Name}.{constructorInfo.Name}.Creator", typeof(object), [typeof(object[])], true);
            var il = dynamicMethod.GetILGenerator();

            var success = GenerateMethodNoArgs(il, constructorInfo);
            if (!success)
                return null;

            var creator = dynamicMethod.CreateDelegate(typeof(Func<object>));
            return (Func<object>)creator;
        }
        public static Func<T>? GenerateCreatorNoArgs<T>(ConstructorInfo constructorInfo)
        {
            if (constructorInfo.DeclaringType is null)
                return null;

            var dynamicMethod = new DynamicMethod($"{constructorInfo.DeclaringType.Name}.{constructorInfo.Name}.Creator`1", typeof(T), [typeof(object[])], true);
            var il = dynamicMethod.GetILGenerator();

            var success = GenerateMethodNoArgs<T>(il, constructorInfo);
            if (!success)
                return null;

            var creator = dynamicMethod.CreateDelegate(typeof(Func<T>));
            return (Func<T>)creator;
        }

        public static Func<object?, object?[]?, object?>? GenerateCaller(MethodInfo methodInfo)
        {
            if (methodInfo.ReflectedType is null)
                return null;

            var dynamicMethod = new DynamicMethod($"{methodInfo.ReflectedType.Name}.{methodInfo.Name}.Caller", typeof(object), [typeof(object), typeof(object[])], true);
            var il = dynamicMethod.GetILGenerator();

            var success = GenerateMethod(il, methodInfo);
            if (!success)
                return null;

            var caller = dynamicMethod.CreateDelegate(typeof(Func<object?, object?[]?, object?>));
            return (Func<object?, object?[]?, object?>)caller;
        }
        public static Func<T?, object?[]?, object?>? GenerateCaller<T>(MethodInfo methodInfo)
        {
            if (methodInfo.ReflectedType is null)
                return null;

            var dynamicMethod = new DynamicMethod($"{methodInfo.ReflectedType.Name}.{methodInfo.Name}.Caller`1", typeof(object), [methodInfo.ReflectedType, typeof(object[])], true);
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

            if (!methodBase.IsConstructor && !methodBase.IsStatic)
            {
                if (methodBase.DeclaringType is null)
                    return false;

                il.Emit(OpCodes.Ldarg_0);

                if (methodBase.DeclaringType.IsValueType)
                    il.Emit(OpCodes.Unbox, methodBase.DeclaringType);
                else
                    il.Emit(OpCodes.Castclass, methodBase.DeclaringType);
            }

            var localObject = il.DeclareLocal(typeof(object));

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var parameterType = parameter.ParameterType;
                if (parameterType.IsPointer)
                    return false;

                if (parameterType.IsByRef)
                {
                    parameterType = parameterType.GetElementType();
                    if (parameterType is null)
                        return false;
                    if (parameterType.IsPointer)
                        return false;

                    var localVariable = il.DeclareLocal(parameterType);

                    if (!parameter.IsOut)
                    {
                        il.Emit(OpCodes.Ldarg, loadArgsIndex);
                        il.Emit(OpCodes.Ldc_I4, i);
                        il.Emit(OpCodes.Ldelem_Ref);

                        if (parameterType.IsValueType)
                            il.Emit(OpCodes.Unbox_Any, parameterType);
                        else
                            il.Emit(OpCodes.Castclass, parameterType);

                        il.Emit(OpCodes.Stloc_S, localVariable);
                    }

                    il.Emit(OpCodes.Ldloca_S, localVariable);
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

            if (returnType is not null && returnType != typeof(void))
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

            if (!methodBase.IsConstructor && !methodBase.IsStatic)
            {
                if (methodBase.DeclaringType is null)
                    return false;

                il.Emit(OpCodes.Ldarg_0);
            }

            var localObject = il.DeclareLocal(typeof(T));

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var parameterType = parameter.ParameterType;
                if (parameterType.IsPointer)
                    return false;

                if (parameterType.IsByRef)
                {
                    parameterType = parameterType.GetElementType();
                    if (parameterType is null)
                        return false;
                    if (parameterType.IsPointer)
                        return false;

                    var localVariable = il.DeclareLocal(parameterType);

                    if (!parameter.IsOut)
                    {
                        il.Emit(OpCodes.Ldarg, loadArgsIndex);
                        il.Emit(OpCodes.Ldc_I4, i);
                        il.Emit(OpCodes.Ldelem_Ref);

                        if (parameterType.IsValueType)
                            il.Emit(OpCodes.Unbox_Any, parameterType);
                        else
                            il.Emit(OpCodes.Castclass, parameterType);
                        il.Emit(OpCodes.Stloc_S, localVariable);
                    }

                    il.Emit(OpCodes.Ldloca_S, localVariable);
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

            if (returnType is not null && returnType != typeof(void))
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
        public static bool GenerateMethodNoArgs(ILGenerator il, MethodBase methodBase)
        {
            //Constructor: object Thing()
            //Method: object DoSomething(object instance)

            if (!methodBase.IsConstructor && !methodBase.IsStatic)
            {
                if (methodBase.DeclaringType is null)
                    return false;

                il.Emit(OpCodes.Ldarg_0);

                if (methodBase.DeclaringType.IsValueType)
                    il.Emit(OpCodes.Unbox, methodBase.DeclaringType);
                else
                    il.Emit(OpCodes.Castclass, methodBase.DeclaringType);
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

            if (returnType is not null && returnType != typeof(void))
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
        public static bool GenerateMethodNoArgs<T>(ILGenerator il, MethodBase methodBase)
        {
            //Constructor: object Thing()
            //Method: object DoSomething(object instance)

            if (!methodBase.IsConstructor && !methodBase.IsStatic)
            {
                if (methodBase.DeclaringType is null)
                    return false;

                il.Emit(OpCodes.Ldarg_0);
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

            if (returnType is not null && returnType != typeof(void))
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
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Zerra.Collections;

namespace Zerra.Reflection
{
    public sealed class MemberAccessor
    {
        private static readonly ConcurrentFactoryDictionary<Type, MemberAccessor> instances = new ConcurrentFactoryDictionary<Type, MemberAccessor>();
        public static MemberAccessor Get(Type type)
        {
            var instance = instances.GetOrAdd(type, (t) =>
            {
                return new MemberAccessor(t);
            });
            return instance;
        }

        public Type Type { get; private set; }
        private readonly Func<object, int, object> getter;
        private readonly Action<object, int, object> setter;
        private readonly Dictionary<string, int> indexMap;
        private readonly string[] memberNames;
        private MemberAccessor(Type type)
        {
            this.Type = type;
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var fields = new List<FieldInfo>();

            var baseType = type;
            while (baseType != null)
            {
                var baseFields = baseType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var newFields = baseFields.Where(x => !fields.Select(y => y.Name).Contains(x.Name));
                fields.AddRange(newFields);
                baseType = baseType.BaseType;
            }

            var members = new Dictionary<string, MemberInfo>();

            foreach (var property in properties)
            {
                if (property.GetIndexParameters().Length > 0)
                    continue;

                var backingName = $"<{property.Name}>k__BackingField";
                var field = fields.FirstOrDefault(x => x.Name == backingName);
                if (field != null)
                {
                    members.Add(property.Name, field);
                    members.Add(field.Name, field);
                }
                else
                {
                    members.Add(property.Name, property);
                }
            }

            foreach (var field in fields.Where(x => !x.Name.EndsWith("k__BackingField")))
            {
                members.Add(field.Name, field);
            }

            var membersArray = members.ToArray();

            indexMap = new Dictionary<string, int>();
            var i = 0;
            foreach (var member in membersArray)
            {
                indexMap.Add(member.Key, i);
                i++;
            }

            memberNames = indexMap.Select(x => x.Key).ToArray();

            getter = GenerateGetter(type, membersArray);
            setter = GenerateSetter(type, membersArray);
        }

        private static readonly ConstructorInfo argumentOutOfRangeExceptionConstructor = typeof(ArgumentOutOfRangeException).GetConstructor(Array.Empty<Type>());
        private static Func<object, int, object> GenerateGetter(Type type, KeyValuePair<string, MemberInfo>[] members)
        {
            var dynamicMethod = new DynamicMethod($"{type.FullName}.Getter", typeof(object), new Type[] { typeof(object), typeof(int) }, true);
            var il = dynamicMethod.GetILGenerator();

            var fail = il.DefineLabel();
            var labels = new Label[members.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                labels[i] = il.DefineLabel();
            }

            il.Emit(OpCodes.Nop);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Switch, labels);

            il.MarkLabel(fail);
            il.Emit(OpCodes.Newobj, argumentOutOfRangeExceptionConstructor);
            il.Emit(OpCodes.Throw);

            for (var i = 0; i < members.Length; i++)
            {
                var member = members[i].Value;

                if (member.MemberType == MemberTypes.Field)
                {
                    il.MarkLabel(labels[i]);

                    var fieldInfo = (FieldInfo)member;

                    il.Emit(OpCodes.Ldarg_0);
                    Cast(il, type, true);

                    il.Emit(OpCodes.Ldfld, fieldInfo);

                    if (fieldInfo.FieldType.IsValueType)
                        il.Emit(OpCodes.Box, fieldInfo.FieldType);

                    il.Emit(OpCodes.Ret);
                }
                else if (member.MemberType == MemberTypes.Property)
                {
                    il.MarkLabel(labels[i]);

                    var propertyInfo = (PropertyInfo)member;
                    if (propertyInfo.CanRead)
                    {
                        var propertyAccessor = propertyInfo.GetGetMethod(true);

                        il.Emit(OpCodes.Ldarg_0);
                        Cast(il, type, true);

                        if (propertyAccessor.IsFinal || !propertyAccessor.IsVirtual)
                            il.EmitCall(OpCodes.Call, propertyAccessor, null);
                        else
                            il.EmitCall(OpCodes.Callvirt, propertyAccessor, null);

                        if (propertyInfo.PropertyType.IsByRef)
                            il.Emit(OpCodes.Ldobj, propertyInfo.PropertyType);

                        if (propertyInfo.PropertyType.IsValueType)
                            il.Emit(OpCodes.Box, propertyInfo.PropertyType);

                        il.Emit(OpCodes.Ret);
                    }
                    else
                    {
                        il.Emit(OpCodes.Br, fail);
                    }
                }
            }

            var accessor = (Func<object, int, object>)dynamicMethod.CreateDelegate(typeof(Func<object, int, object>));
            return accessor;
        }
        private static Action<object, int, object> GenerateSetter(Type type, KeyValuePair<string, MemberInfo>[] members)
        {
            var dynamicMethod = new DynamicMethod($"{type.FullName}.Setter", null, new Type[] { typeof(object), typeof(int), typeof(object) }, true);
            var il = dynamicMethod.GetILGenerator();

            var fail = il.DefineLabel();
            var labels = new Label[members.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                labels[i] = il.DefineLabel();
            }

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Switch, labels);

            il.MarkLabel(fail);
            il.Emit(OpCodes.Newobj, argumentOutOfRangeExceptionConstructor);
            il.Emit(OpCodes.Throw);

            for (var i = 0; i < members.Length; i++)
            {
                var member = members[i].Value;

                if (member.MemberType == MemberTypes.Field)
                {
                    il.MarkLabel(labels[i]);

                    var fieldInfo = (FieldInfo)member;

                    il.Emit(OpCodes.Ldarg_0);
                    Cast(il, type, true);

                    il.Emit(OpCodes.Ldarg_2);
                    Cast(il, fieldInfo.FieldType, false);
                    il.Emit(OpCodes.Stfld, fieldInfo);
                    il.Emit(OpCodes.Ret);
                }
                else if (member.MemberType == MemberTypes.Property)
                {
                    il.MarkLabel(labels[i]);

                    var propertyInfo = (PropertyInfo)member;
                    if (propertyInfo.CanWrite)
                    {
                        var propertyAccessor = propertyInfo.GetSetMethod(true);

                        il.Emit(OpCodes.Ldarg_0);
                        Cast(il, type, true);

                        if (propertyInfo.PropertyType.IsByRef)
                        {
                            if (propertyAccessor.IsFinal || !propertyAccessor.IsVirtual)
                                il.EmitCall(OpCodes.Call, propertyAccessor, null);
                            else
                                il.EmitCall(OpCodes.Callvirt, propertyAccessor, null);
                        }

                        il.Emit(OpCodes.Ldarg_2);
                        Cast(il, propertyInfo.PropertyType, false);

                        if (propertyInfo.PropertyType.IsByRef)
                        {
                            il.Emit(OpCodes.Stobj, propertyInfo.PropertyType);
                        }
                        else
                        {
                            if (propertyAccessor.IsFinal || !propertyAccessor.IsVirtual)
                                il.EmitCall(OpCodes.Call, propertyAccessor, null);
                            else
                                il.EmitCall(OpCodes.Callvirt, propertyAccessor, null);
                        }
                        il.Emit(OpCodes.Ret);
                    }
                    else
                    {
                        il.Emit(OpCodes.Br, fail);
                    }
                }
            }

            var accessor = (Action<object, int, object>)dynamicMethod.CreateDelegate(typeof(Action<object, int, object>));
            return accessor;
        }

        private static void Cast(ILGenerator il, Type type, bool valueAsPointer)
        {
            if (type == typeof(object))
                return;
            if (type.IsValueType)
            {
                if (valueAsPointer)
                    il.Emit(OpCodes.Unbox, type);
                else
                    il.Emit(OpCodes.Unbox_Any, type);
            }
            else
            {
                il.Emit(OpCodes.Castclass, type);
            }
        }

        public IReadOnlyList<string> Members
        {
            get
            {
                return memberNames;
            }
        }

        public int this[string name]
        {
            get
            {
                if (!indexMap.TryGetValue(name, out int index))
                    throw new ArgumentOutOfRangeException("name", name, $"{Type.Name}.{name} getter not found");
                return index;
            }
        }

        public object this[object target, string name]
        {
            get
            {
                if (!indexMap.TryGetValue(name, out int index))
                    throw new ArgumentOutOfRangeException("name", name, $"{Type.Name}.{name} member not found");
                return getter(target, index);
            }
            set
            {
                if (!indexMap.TryGetValue(name, out int index))
                    throw new ArgumentOutOfRangeException("name", name, $"{Type.Name}.{name} member not found");
                setter(target, index, value);
            }
        }

        public object this[object target, int index]
        {
            get
            {
                return getter(target, index);
            }
            set
            {
                setter(target, index, value);
            }
        }
    }
}

// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Zerra.Reflection.Runtime
{
    internal sealed class MemberDetailRuntime<T, V> : MemberDetail<T, V>
    {
        public override bool IsGenerated => false;

        public override MemberDetail? BackingFieldDetailBoxed { get; }

        public override MemberInfo MemberInfo { get; }
        public override string Name { get; }
        public override Type Type { get; }
        public override bool IsBacked { get; }
        public override bool IsStatic { get; }

        private Attribute[]? attributes = null;
        public override IReadOnlyList<Attribute> Attributes
        {
            get
            {
                if (this.attributes is null)
                {
                    lock (locker)
                    {
                        this.attributes ??= MemberInfo.GetCustomAttributes().ToArray();
                    }
                }
                return this.attributes;
            }
        }

        private bool getterBoxedLoaded = false;
        private Func<object, object?>? getterBoxed = null;
        public override Func<object, object?> GetterBoxed
        {
            get
            {
                if (!getterBoxedLoaded)
                    LoadGetterBoxed();
                return this.getterBoxed ?? throw new NotSupportedException($"{nameof(MemberDetail)} {Name} does not have a {nameof(GetterBoxed)}");
            }
        }
        public override bool HasGetterBoxed
        {
            get
            {
                if (!getterBoxedLoaded)
                    LoadGetterBoxed();
                return this.getterBoxed is not null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadGetterBoxed()
        {
            lock (locker)
            {
                if (!getterBoxedLoaded)
                {
                    if (MemberInfo.MemberType == MemberTypes.Property)
                    {
                        var property = (PropertyInfo)MemberInfo;
                        if (!property.PropertyType.IsPointer)
                        {
                            if (BackingFieldDetail is null)
                            {
                                this.getterBoxed = AccessorGenerator.GenerateGetter(property);
                            }
                            else
                            {
                                this.getterBoxed = BackingFieldDetail.GetterBoxed;
                            }
                        }
                    }
                    else if (MemberInfo.MemberType == MemberTypes.Field)
                    {
                        var field = (FieldInfo)MemberInfo;
                        if (!field.FieldType.IsPointer)
                        {
                            this.getterBoxed = AccessorGenerator.GenerateGetter(field);
                        }
                    }
                    getterBoxedLoaded = true;
                }
            }
        }

        private bool setterBoxedLoaded = false;
        private Action<object, object?>? setterBoxed = null;
        public override Action<object, object?> SetterBoxed
        {
            get
            {
                if (!setterBoxedLoaded)
                    LoadSetterBoxed();
                return this.setterBoxed ?? throw new NotSupportedException($"{nameof(MemberDetail)} {Name} does not have a {nameof(SetterBoxed)}");
            }
        }
        public override bool HasSetterBoxed
        {
            get
            {
                if (!setterBoxedLoaded)
                    LoadSetterBoxed();
                return this.setterBoxed is not null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadSetterBoxed()
        {
            lock (locker)
            {
                if (!setterBoxedLoaded)
                {
                    if (MemberInfo.MemberType == MemberTypes.Property)
                    {
                        var property = (PropertyInfo)MemberInfo;
                        if (!property.PropertyType.IsPointer)
                        {
                            if (BackingFieldDetail is null)
                            {
                                this.setterBoxed = AccessorGenerator.GenerateSetter(property);
                            }
                            else
                            {
                                this.setterBoxed = BackingFieldDetail.SetterBoxed;
                            }
                        }
                    }
                    else if (MemberInfo.MemberType == MemberTypes.Field)
                    {
                        var field = (FieldInfo)MemberInfo;
                        if (!field.FieldType.IsPointer)
                        {
                            this.setterBoxed = AccessorGenerator.GenerateSetter(field);
                        }
                    }
                    setterBoxedLoaded = true;
                }
            }
        }

        private TypeDetail? typeDetailBoxed = null;
        public override TypeDetail TypeDetailBoxed
        {
            get
            {
                if (typeDetailBoxed is null)
                    LoadTypeDetail();
                return typeDetailBoxed!;
            }
        }

        public override MemberDetail<T, V>? BackingFieldDetail { get; }

        private bool getterLoaded = false;
        private Func<T, V?>? getter = null;
        public override Func<T, V?> Getter
        {
            get
            {
                if (!getterLoaded)
                    LoadGetter();
                return this.getter ?? throw new NotSupportedException($"{nameof(MemberDetail)} {Name} does not have a {nameof(Getter)}");
            }
        }
        public override bool HasGetter
        {
            get
            {
                if (!getterLoaded)
                    LoadGetter();
                return this.getter is not null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadGetter()
        {
            lock (locker)
            {
                if (!getterLoaded)
                {
                    if (MemberInfo.MemberType == MemberTypes.Property)
                    {
                        var property = (PropertyInfo)MemberInfo;
                        if (!property.PropertyType.IsPointer)
                        {
                            if (BackingFieldDetail is null)
                            {
                                this.getter = AccessorGenerator.GenerateGetter<T, V?>(property);
                            }
                            else
                            {
                                this.getter = BackingFieldDetail.Getter;
                            }
                        }
                    }
                    else if (MemberInfo.MemberType == MemberTypes.Field)
                    {
                        var field = (FieldInfo)MemberInfo;
                        if (!field.FieldType.IsPointer)
                        {
                            this.getter = AccessorGenerator.GenerateGetter<T, V?>(field);
                        }
                    }
                    getterLoaded = true;
                }
            }
        }

        private bool setterLoaded = false;
        private Action<T, V?>? setter = null;
        public override Action<T, V?> Setter
        {
            get
            {
                if (!setterLoaded)
                    LoadSetter();
                return this.setter ?? throw new NotSupportedException($"{nameof(MemberDetail)} {Name} does not have a {nameof(Setter)}");
            }
        }
        public override bool HasSetter
        {
            get
            {
                if (!setterLoaded)
                    LoadSetter();
                return this.setter is not null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadSetter()
        {
            lock (locker)
            {
                if (!setterLoaded)
                {
                    if (MemberInfo.MemberType == MemberTypes.Property)
                    {
                        var property = (PropertyInfo)MemberInfo;
                        if (!property.PropertyType.IsPointer)
                        {
                            if (BackingFieldDetail is null)
                            {
                                this.setter = AccessorGenerator.GenerateSetter<T, V?>(property);
                            }
                            else
                            {
                                this.setter = BackingFieldDetail.Setter;
                            }
                        }
                    }
                    else if (MemberInfo.MemberType == MemberTypes.Field)
                    {
                        var field = (FieldInfo)MemberInfo;
                        if (!field.FieldType.IsPointer)
                        {
                            this.setter = AccessorGenerator.GenerateSetter<T, V?>(field);
                        }
                    }
                    setterLoaded = true;
                }
            }
        }

        private TypeDetail<V>? typeDetail = null;
        public override TypeDetail<V> TypeDetail
        {
            get
            {
                if (typeDetail is null)
                    LoadTypeDetail();
                return typeDetail!;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadTypeDetail()
        {
            lock (locker)
            {
                typeDetail ??= TypeAnalyzer<V>.GetTypeDetail();
                typeDetailBoxed = typeDetail;
            }
        }

        private readonly object locker;
        internal MemberDetailRuntime(string name, MemberInfo member, MemberDetail<T, V>? backingFieldDetail, object locker)
        {
            this.locker = locker;
            this.BackingFieldDetail = backingFieldDetail;
            this.BackingFieldDetailBoxed = backingFieldDetail;
            this.MemberInfo = member;
            this.Name = name;

            if (member.MemberType == MemberTypes.Property)
            {
                var property = (PropertyInfo)MemberInfo;
                this.Type = property.PropertyType;
                this.IsStatic = property.GetMethod?.IsStatic ?? property.SetMethod?.IsStatic ?? false;
            }
            else if (member.MemberType == MemberTypes.Field)
            {
                var field = (FieldInfo)MemberInfo;
                this.Type = field.FieldType;
                this.IsStatic = field.IsStatic;
            }
            else
            {
                throw new NotSupportedException($"{nameof(MemberDetail)} does not support {member.MemberType}");
            }

            this.IsBacked = member.MemberType == MemberTypes.Field || backingFieldDetail is not null;
        }

        private static readonly Type typeDetailT = typeof(MemberDetailRuntime<,>);
        internal static MemberDetail New(Type type, Type valueType, string name, MemberInfo member, MemberDetail? backingFieldDetail, object locker)
        {
            if (!valueType.ContainsGenericParameters && !type.IsPointer && !type.IsByRef
#if !NETSTANDARD2_0
                && !valueType.IsByRefLike
            #endif
            )
            {
                var typeDetailGeneric = typeDetailT.MakeGenericType(type, valueType);
                var obj = typeDetailGeneric.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0].Invoke([name, member, backingFieldDetail, locker]);
                return (MemberDetail)obj;
            }
            else
            {
                return new MemberDetailRuntime(name, member, backingFieldDetail, locker);
            }
        }

        internal override void SetMemberInfo(MemberInfo memberInfo, MemberInfo? backingField) => throw new NotSupportedException();
    }
}

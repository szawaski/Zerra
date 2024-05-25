// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Zerra.Reflection
{
    public abstract class MemberDetail
    {
        public MemberDetail? BackingFieldDetail { get; }

        public MemberInfo MemberInfo { get; }
        public string Name { get; }
        public Type Type { get; }
        public bool IsBacked { get; }

        private Attribute[]? attributes = null;
        public IReadOnlyList<Attribute> Attributes
        {
            get
            {
                if (this.attributes == null)
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
        public Func<object, object?> GetterBoxed
        {
            get
            {
                if (!getterBoxedLoaded)
                    LoadGetterBoxed();
                return this.getterBoxed ?? throw new NotSupportedException($"{nameof(MemberDetail)} {Name} does not have a {nameof(GetterBoxed)}");
            }
        }
        public bool HasGetterBoxed
        {
            get
            {
                if (!getterBoxedLoaded)
                    LoadGetterBoxed();
                return this.getterBoxed != null;
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
                            if (BackingFieldDetail == null)
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
        public Action<object, object?> SetterBoxed
        {
            get
            {
                if (!setterBoxedLoaded)
                    LoadSetterBoxed();
                return this.setterBoxed ?? throw new NotSupportedException($"{nameof(MemberDetail)} {Name} does not have a {nameof(SetterBoxed)}");
            }
        }
        public bool HasSetterBoxed
        {
            get
            {
                if (!setterBoxedLoaded)
                    LoadSetterBoxed();
                return this.setterBoxed != null;
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
                            if (BackingFieldDetail == null)
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

        public abstract Delegate? GetterTyped { get; }
        public abstract Delegate? SetterTyped { get; }

        private TypeDetail? typeDetail = null;
        public TypeDetail TypeDetail
        {
            get
            {
                if (typeDetail == null)
                {
                    lock (locker)
                    {
                        typeDetail ??= TypeAnalyzer.GetTypeDetail(Type);
                    }
                }
                return typeDetail;
            }
        }

        public override string ToString()
        {
            return $"{Type.Name} {Name}";
        }

        protected readonly object locker;
        protected MemberDetail(MemberInfo member, MemberDetail? backingFieldDetail, object locker)
        {
            this.locker = locker;
            this.BackingFieldDetail = backingFieldDetail;
            this.MemberInfo = member;
            this.Name = member.Name;

            if (member.MemberType == MemberTypes.Property)
            {
                var property = (PropertyInfo)MemberInfo;
                this.Type = property.PropertyType;
            }
            else if (member.MemberType == MemberTypes.Field)
            {
                var field = (FieldInfo)MemberInfo;
                this.Type = field.FieldType;
            }
            else
            {
                throw new NotSupportedException($"{nameof(MemberDetail)} does not support {member.MemberType}");
            }

            this.IsBacked = member.MemberType == MemberTypes.Field || backingFieldDetail != null;
        }

        private static readonly Type typeDetailT = typeof(MemberDetail<,>);
        internal static MemberDetail New(Type type, Type valueType, MemberInfo member, MemberDetail? backingFieldDetail, object locker)
        {
            var typeDetailGeneric = typeDetailT.MakeGenericType(type, valueType);
            var obj = typeDetailGeneric.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0].Invoke(new object?[] { member, backingFieldDetail, locker });
            return (MemberDetail)obj;
        }
    }
}

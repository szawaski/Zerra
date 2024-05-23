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

        private bool getterLoaded = false;
        private Func<object, object?>? getter = null;
        public Func<object, object?> Getter
        {
            get
            {
                LoadGetter();
                return this.getter ?? throw new NotSupportedException($"{nameof(MemberDetail)} {Name} does not have a {nameof(Getter)}");
            }
        }
        public bool HasGetter
        {
            get
            {
                LoadGetter();
                return this.getter != null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadGetter()
        {
            if (!getterLoaded)
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
                                if (BackingFieldDetail == null)
                                {
                                    this.getter = AccessorGenerator.GenerateGetter(property);
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
                                this.getter = AccessorGenerator.GenerateGetter(field);
                            }
                        }
                        getterLoaded = true;
                    }
                }
            }
        }

        private bool setterLoaded = false;
        private Action<object, object?>? setter = null;
        public Action<object, object?> Setter
        {
            get
            {
                LoadSetter();
                return this.setter ?? throw new NotSupportedException($"{nameof(MemberDetail)} {Name} does not have a {nameof(Setter)}");
            }
        }
        public bool HasSetter
        {
            get
            {
                LoadSetter();
                return this.setter != null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadSetter()
        {
            if (!setterLoaded)
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
                                if (BackingFieldDetail == null)
                                {
                                    this.setter = AccessorGenerator.GenerateSetter(property);
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
                                this.setter = AccessorGenerator.GenerateSetter(field);
                            }
                        }
                        setterLoaded = true;
                    }
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

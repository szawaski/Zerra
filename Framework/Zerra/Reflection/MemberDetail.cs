// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Zerra.Reflection
{
    public sealed class MemberDetail
    {
        public MemberDetail? BackingFieldDetail { get; private set; }

        public MemberInfo MemberInfo { get; private set; }
        public string Name { get; private set; }
        public Type Type { get; private set; }
        public bool IsBacked { get; private set; }

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
        public Func<object, object?>? Getter
        {
            get
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
                return this.getter;
            }
        }

        private bool setterLoaded = false;
        private Action<object, object?>? setter = null;
        public Action<object, object?>? Setter
        {
            get
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
                return this.setter;
            }
        }

        private bool getterTypedLoaded = false;
        private object? getterTyped = null;
        public object? GetterTyped
        {
            get
            {
                if (!getterTypedLoaded)
                {
                    lock (locker)
                    {
                        if (!getterTypedLoaded)
                        {
                            if (MemberInfo.MemberType == MemberTypes.Property)
                            {
                                var property = (PropertyInfo)MemberInfo;
                                if (!property.PropertyType.IsPointer)
                                {
                                    if (BackingFieldDetail == null)
                                    {
                                        this.getterTyped = AccessorGenerator.GenerateGetterTyped(property);
                                    }
                                    else
                                    {
                                        this.getterTyped = BackingFieldDetail.GetterTyped;
                                    }
                                }
                            }
                            else if (MemberInfo.MemberType == MemberTypes.Field)
                            {
                                var field = (FieldInfo)MemberInfo;
                                if (!field.FieldType.IsPointer)
                                {
                                    this.getterTyped = AccessorGenerator.GenerateGetterTyped(field);
                                }
                            }
                            getterTypedLoaded = true;
                        }
                    }
                }
                return this.getterTyped;
            }
        }

        private bool setterTypedLoaded = false;
        private object? setterTyped = null;
        public object? SetterTyped
        {
            get
            {
                if (!setterTypedLoaded)
                {
                    lock (locker)
                    {
                        if (!setterTypedLoaded)
                        {
                            if (MemberInfo.MemberType == MemberTypes.Property)
                            {
                                var property = (PropertyInfo)MemberInfo;
                                if (!property.PropertyType.IsPointer)
                                {
                                    if (BackingFieldDetail == null)
                                    {
                                        this.setterTyped = AccessorGenerator.GenerateSetterTyped(property);
                                    }
                                    else
                                    {
                                        this.setterTyped = BackingFieldDetail.SetterTyped;
                                    }
                                }
                            }
                            else if (MemberInfo.MemberType == MemberTypes.Field)
                            {
                                var field = (FieldInfo)MemberInfo;
                                if (!field.FieldType.IsPointer)
                                {
                                    this.setterTyped = AccessorGenerator.GenerateSetterTyped(field);
                                }
                            }
                            setterTypedLoaded = true;
                        }
                    }
                }
                return this.setterTyped;
            }
        }

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

        private readonly object locker;
        internal MemberDetail(MemberInfo member, MemberDetail? backingFieldDetail, object locker)
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
    }
}

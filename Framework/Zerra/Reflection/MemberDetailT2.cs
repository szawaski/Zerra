// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Zerra.Reflection
{
    public sealed class MemberDetail<T, V> : MemberDetail
    {
        public new MemberDetail<T, V>? BackingFieldDetail { get; }

        private bool getterLoaded = false;
        private Func<T, V?>? getter = null;
        public Func<T, V?> Getter
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
        }

        private bool setterLoaded = false;
        private Action<T, V?>? setter = null;
        public Action<T, V?> Setter
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
        }

        public override Delegate? GetterTyped => Getter;
        public override Delegate? SetterTyped => Setter;

        private TypeDetail<V>? typeDetail = null;
        public new TypeDetail<V> TypeDetail
        {
            get
            {
                if (typeDetail == null)
                {
                    lock (locker)
                    {
                        typeDetail ??= TypeAnalyzer<V>.GetTypeDetail();
                    }
                }
                return typeDetail;
            }
        }

        internal MemberDetail(MemberInfo member, MemberDetail<T, V>? backingFieldDetail, object locker) : base(member, backingFieldDetail, locker)
        {
            this.BackingFieldDetail = backingFieldDetail;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Zerra.Reflection.Generation
{
    public abstract class PrivateMemberDetailGenerationBase<T, V> : MemberDetail<T, V>
    {
        protected readonly object locker;
        private readonly Action loadMemberInfo;
        public PrivateMemberDetailGenerationBase(object locker, Action loadMemberInfo)
        {
            this.locker = locker;
            this.loadMemberInfo = loadMemberInfo;
        }

        public override sealed TypeDetail<V> TypeDetail => TypeAnalyzer<V>.GetTypeDetail();
        public override sealed TypeDetail TypeDetailBoxed => TypeDetail;

        private MemberInfo? memberInfo = null;
        public override sealed MemberInfo MemberInfo
        {
            get
            {
                if (memberInfo is null)
                {
                    lock (locker)
                    {
                        if (memberInfo is null)
                        {
                            loadMemberInfo();
                            backingFieldDetailLoaded = true;
                        }
                    }
                }
                return memberInfo!;
            }
        }

        protected abstract Func<MemberDetail<T, V>?> CreateBackingFieldDetail { get; }
        private bool backingFieldDetailLoaded = false;
        private MemberDetail<T, V>? backingFieldDetail = null;
        private MemberDetail? backingFieldDetailBoxed = null;
        public override sealed MemberDetail<T, V>? BackingFieldDetail
        {
            get
            {
                if (!backingFieldDetailLoaded)
                    LoadBackingField();
                return backingFieldDetail;
            }
        }
        public override sealed MemberDetail? BackingFieldDetailBoxed
        {
            get
            {
                if (!backingFieldDetailLoaded)
                    LoadBackingField();
                return backingFieldDetailBoxed;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadBackingField()
        {
            lock (locker)
            {
                if (!backingFieldDetailLoaded)
                {
                    backingFieldDetail = CreateBackingFieldDetail();
                    backingFieldDetailBoxed = backingFieldDetail;
                    backingFieldDetailLoaded = true;
                }
            }
        }

        protected abstract Func<Attribute[]> CreateAttributes { get; }
        private Attribute[]? attributes = null;
        public override sealed IReadOnlyList<Attribute> Attributes
        {
            get
            {
                if (attributes is null)
                {
                    lock (locker)
                    {
                        attributes ??= CreateAttributes();
                    }
                }
                return attributes;
            }
        }

        internal override sealed void SetMemberInfo(MemberInfo memberInfo)
        {
            this.memberInfo = memberInfo;
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
        public override sealed Action<object, object?> SetterBoxed
        {
            get
            {
                if (!setterBoxedLoaded)
                    LoadSetterBoxed();
                return this.setterBoxed ?? throw new NotSupportedException($"{nameof(MemberDetail)} {Name} does not have a {nameof(SetterBoxed)}");
            }
        }
        public override sealed bool HasSetterBoxed
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

        private bool getterLoaded = false;
        private Func<T, V?>? getter = null;
        public override sealed Func<T, V?> Getter
        {
            get
            {
                if (!getterLoaded)
                    LoadGetter();
                return this.getter ?? throw new NotSupportedException($"{nameof(MemberDetail)} {Name} does not have a {nameof(Getter)}");
            }
        }
        public override sealed bool HasGetter
        {
            get
            {
                if (!getterLoaded)
                    LoadGetter();
                return this.getter != null;
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

        private bool setterLoaded = false;
        private Action<T, V?>? setter = null;
        public override sealed Action<T, V?> Setter
        {
            get
            {
                if (!setterLoaded)
                    LoadSetter();
                return this.setter ?? throw new NotSupportedException($"{nameof(MemberDetail)} {Name} does not have a {nameof(Setter)}");
            }
        }
        public override sealed bool HasSetter
        {
            get
            {
                if (!setterLoaded)
                    LoadSetter();
                return this.setter != null;
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
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zerra.Reflection.Runtime;

namespace Zerra.Reflection.Compiletime
{
    public abstract class PrivateTypeMemberDetailCompiletimeBase : MemberDetail
    {
        protected readonly object locker;
        private readonly Action loadMemberInfo;
        public PrivateTypeMemberDetailCompiletimeBase(object locker, Action loadMemberInfo)
        {
            this.locker = locker;
            this.loadMemberInfo = loadMemberInfo;
        }

        public override sealed bool IsGenerated => true;

        private TypeDetail? typeDetail = null;
        public override sealed TypeDetail TypeDetailBoxed
        {
            get
            {
                if (typeDetail is null)
                {
                    lock (locker)
                        typeDetail ??= TypeAnalyzer.GetTypeDetail(Type);
                }
                return typeDetail;
            }
        }

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

        private Type? type = null;
        public override sealed Type Type
        {
            get
            {
                if (type is null)
                {
                    lock (locker)
                    {
                        if (type is null)
                        {
                            var member = MemberInfo;
                            if (member.MemberType == MemberTypes.Property)
                                type = ((PropertyInfo)member).PropertyType;
                            else
                                type = ((FieldInfo)member).FieldType;
                        }
                    }
                }
                return type;
            }
        }

        private bool backingFieldDetailLoaded = false;
        private MemberDetail? backingFieldDetailBoxed = null;
        public override sealed MemberDetail? BackingFieldDetailBoxed
        {
            get
            {
                if (!backingFieldDetailLoaded)
                {
                    lock (locker)
                    {
                        if (!backingFieldDetailLoaded)
                        {
                            loadMemberInfo();
                            backingFieldDetailLoaded = true;
                        }
                    }
                }
                return backingFieldDetailBoxed;
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

        internal override sealed void SetMemberInfo(MemberInfo memberInfo, MemberInfo? backingField)
        {
            this.memberInfo = memberInfo;
            if (backingField is not null)
                this.backingFieldDetailBoxed = new MemberDetailRuntime(memberInfo.Name, backingField, null, false, locker);
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
                            if (BackingFieldDetailBoxed is null)
                            {
                                this.getterBoxed = AccessorGenerator.GenerateGetter(property);
                            }
                            else
                            {
                                this.getterBoxed = BackingFieldDetailBoxed.GetterBoxed;
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
                            if (BackingFieldDetailBoxed is null)
                            {
                                this.setterBoxed = AccessorGenerator.GenerateSetter(property);
                            }
                            else
                            {
                                this.setterBoxed = BackingFieldDetailBoxed.SetterBoxed;
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

        public override sealed Delegate GetterTyped => throw new NotSupportedException();
        public override sealed Delegate SetterTyped => throw new NotSupportedException();
    }
}

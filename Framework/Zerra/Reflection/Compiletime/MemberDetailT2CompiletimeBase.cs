﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Zerra.Reflection.Compiletime
{
    public abstract class MemberDetailCompiletimeBase<T, V> : MemberDetail<T, V>
    {
        protected readonly object locker;
        protected readonly Action loadMemberInfo;
        public MemberDetailCompiletimeBase(object locker, Action loadMemberInfo)
        {
            this.locker = locker;
            this.loadMemberInfo = loadMemberInfo;
        }

        public override sealed bool IsGenerated => true;

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

        internal override sealed void SetMemberInfo(MemberInfo memberInfo, MemberInfo? backingField)
        {
            this.memberInfo = memberInfo;
            if (backingField is not null)
                BackingFieldDetail?.SetMemberInfo(backingField, null);
        }
    }
}

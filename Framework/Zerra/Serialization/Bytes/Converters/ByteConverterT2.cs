// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using Zerra.IO;

namespace Zerra.Serialization
{
    internal abstract class ByteConverter<TParent, TValue> : ByteConverter<TParent>, IByteConverter<TParent>
    {
        protected new TypeDetail<TValue> TypeDetail { get; private set; } = null!;
        private Func<TParent, TValue?>? getter;
        private Action<TParent, TValue?>? setter;

        public override sealed void Setup()
        {
            if (base.TypeDetail == null)
                throw new InvalidOperationException();
            if (base.memberDetail == null)
                throw new InvalidOperationException();

            this.TypeDetail = (TypeDetail<TValue>)base.TypeDetail;
            var memberTyped = (MemberDetail<TParent, TValue>)base.memberDetail;
            this.getter = memberTyped.Getter;
            this.setter = memberTyped.Setter;
            SetupAdditional();
        }

        public override sealed void Read(ref ByteReader reader, ref ReadState state, TParent? parent)
        {
            if (Read(ref reader, ref state, out var value))
            {
                if (setter != null && parent != null)
                    setter(parent, value);
                state.EndFrame();
            }
        }
        public override sealed void Write(ref ByteWriter writer, ref WriteState state, TParent? parent)
        {
            if (getter == null || parent == null)
            {
                state.EndFrame();
                return;
            }

            var value = getter(parent);
            if (Write(ref writer, ref state, value))
            {
                state.EndFrame();
                return;
            }
        }

        public virtual void SetupAdditional() { }

        protected abstract bool Read(ref ByteReader reader, ref ReadState state, out TValue? value);
        protected abstract bool Write(ref ByteWriter writer, ref WriteState state, TValue? value);
    }
}
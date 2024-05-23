// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using Zerra.IO;

namespace Zerra.Serialization
{
    internal abstract class ByteConverter<TParent, TValue> : ByteConverter<TParent>
    {
        protected new TypeDetail<TValue> typeDetail { get; private set; } = null!;
        private new Func<TParent, TValue?>? getter;
        private new Action<TParent, TValue?>? setter;

        protected override sealed void SetupRoot()
        {
            if (base.typeDetail == null)
                throw new InvalidOperationException();
            if (base.getter == null)
                throw new InvalidOperationException();
            if (base.setter == null)
                throw new InvalidOperationException();

            this.typeDetail = (TypeDetail<TValue>)base.typeDetail;
            this.getter = (Func<TParent, TValue?>?)base.getter;
            this.setter = (Action<TParent, TValue?>?)base.setter;
            Setup();
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

        public virtual void Setup() { }

        protected abstract bool Read(ref ByteReader reader, ref ReadState state, out TValue? value);
        protected abstract bool Write(ref ByteWriter writer, ref WriteState state, TValue? value);
    }
}
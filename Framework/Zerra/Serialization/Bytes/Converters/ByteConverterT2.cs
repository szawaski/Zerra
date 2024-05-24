// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using Zerra.IO;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization
{
    internal abstract class ByteConverter<TParent, TValue> : ByteConverter<TParent>, IByteConverterDiscoverable<TValue>
    {
        protected new TypeDetail<TValue> typeDetail { get; private set; } = null!;
        private new Func<TParent, TValue?>? getter;
        private new Action<TParent, TValue?>? setter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override sealed void SetupRoot()
        {
            if (base.typeDetail == null)
                throw new InvalidOperationException();

            this.typeDetail = (TypeDetail<TValue>)base.typeDetail;
            this.getter = (Func<TParent, TValue?>?)base.getter;
            this.setter = (Action<TParent, TValue?>?)base.setter;
            Setup();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool Read(ref ByteReader reader, ref ReadState state, TParent? parent)
        {
            if (!Read(ref reader, ref state, out var value))
                return false;

            if (setter != null && parent != null)
                setter(parent, value);

            state.EndFrame();
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool Write(ref ByteWriter writer, ref WriteState state, TParent? parent)
        {
            if (getter != null && parent != null)
            {
                var value = getter(parent);
                if (!Write(ref writer, ref state, value))
                    return false;
            }

            state.EndFrame();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Setup() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool Read(ref ByteReader reader, ref ReadState state, out TValue? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool Write(ref ByteWriter writer, ref WriteState state, TValue? value);
    }
}
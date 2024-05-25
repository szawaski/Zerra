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
        protected TypeDetail<TValue> typeDetail { get; private set; } = null!;
        private Func<TParent, TValue?>? getter;
        private Action<TParent, TValue?>? setter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override sealed void Setup(TypeDetail? typeDetail, Delegate? getterBoxed, Delegate? setterBoxed)
        {
            if (typeDetail == null)
                throw new ArgumentNullException(nameof(typeDetail));

            this.typeDetail = (TypeDetail<TValue>)typeDetail;
            this.getter = (Func<TParent, TValue?>?)getterBoxed;
            this.setter = (Action<TParent, TValue?>?)setterBoxed;

            Setup();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryRead(ref ByteReader reader, ref ReadState state, TParent? parent)
        {
            if (state.Stack.Count > maxStackDepth)
                return false;

            if (!TryRead(ref reader, ref state, out var value))
                return false;

            if (setter != null && parent != null)
                setter(parent, value);

            state.EndFrame(value);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryWrite(ref ByteWriter writer, ref WriteState state, TParent? parent)
        {
            if (state.Stack.Count > maxStackDepth)
                return false;

            if (parent != null)
            {
                if (getter != null)
                {
                    var value = getter(parent);
                    if (!TryWrite(ref writer, ref state, value))
                        return false;
                }
            }
            else
            {
                var value = (TValue?)state.Object;
                if (!TryWrite(ref writer, ref state, value))
                    return false;
            }

            state.EndFrame();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void Setup() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryRead(ref ByteReader reader, ref ReadState state, out TValue? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryWrite(ref ByteWriter writer, ref WriteState state, TValue? value);
    }
}
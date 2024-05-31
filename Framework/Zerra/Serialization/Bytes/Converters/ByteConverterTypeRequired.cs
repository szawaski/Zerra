// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using System;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterTypeRequired<TParent> : ByteConverter<TParent>
    {
        public override sealed void Setup(TypeDetail? typeDetail, string? memberKey, Delegate? getterBoxed, Delegate? setterBoxed) { }

        public override sealed bool TryReadValueBoxed(ref ByteReader reader, ref ReadState state, out object? value)
            => throw new NotSupportedException("Cannot deserialize without type information");
        public override sealed bool TryWriteValueBoxed(ref ByteWriter writer, ref WriteState state, object? value)
            => throw new NotSupportedException("Cannot deserialize without type information");

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryReadBoxed(ref ByteReader reader, ref ReadState state, out object? returnValue)
            => throw new NotSupportedException("Cannot deserialize without type information");
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryWriteBoxed(ref ByteWriter writer, ref WriteState state, object? value)
            => throw new NotSupportedException("Cannot deserialize without type information");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryReadFromParent(ref ByteReader reader, ref ReadState state, TParent? parent)
            => throw new NotSupportedException("Cannot deserialize without type information");
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryWriteFromParent(ref ByteWriter writer, ref WriteState state, TParent parent)
            => throw new NotSupportedException("Cannot deserialize without type information");
    }
}
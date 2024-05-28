// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;

namespace Zerra.Serialization
{
    public abstract class ByteConverter<TParent> : ByteConverter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryReadFromParent(ref ByteReader reader, ref ReadState state, TParent? parent);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryWriteFromParent(ref ByteWriter writer, ref WriteState state, TParent parent);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryReadValueBoxed(ref ByteReader reader, ref ReadState state, out object? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryWriteValueBoxed(ref ByteWriter writer, ref WriteState state, object? value);
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters
{
    public abstract class ByteConverter<TParent> : ByteConverter
    {
        public abstract bool TryReadFromParent(ref ByteReader reader, ref ReadState state, TParent? parent, bool nullFlags, bool drainBytes = false);
        public abstract bool TryWriteFromParent(ref ByteWriter writer, ref WriteState state, TParent parent, bool nullFlags, ushort indexProperty = default, string? indexPropertyName = default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryReadValueBoxed(ref ByteReader reader, ref ReadState state, out object? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryWriteValueBoxed(ref ByteWriter writer, ref WriteState state, object? value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract void CollectedValuesSetter(TParent? parent, object? value);
    }
}
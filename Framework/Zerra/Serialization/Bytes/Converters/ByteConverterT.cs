// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    public abstract class ByteConverter<TParent> : ByteConverter
    {
        protected readonly TypeDetail parentTypeDetail = TypeAnalyzer<TParent>.GetTypeDetail();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryReadFromParentObject(ref ByteReader reader, ref ReadState state, object? parent)
            => TryReadFromParent(ref reader, ref state, (TParent?)parent);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryWriteFromParentObject(ref ByteWriter writer, ref WriteState state, object? parent)
            => TryWriteFromParent(ref writer, ref state, (TParent?)parent);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryReadFromParent(ref ByteReader reader, ref ReadState state, TParent? parent);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryWriteFromParent(ref ByteWriter writer, ref WriteState state, TParent? parent);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryReadValueObject(ref ByteReader reader, ref ReadState state, out object? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryWriteValueObject(ref ByteWriter writer, ref WriteState state, object? value);
    }
}
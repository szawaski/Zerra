// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    public abstract class ByteConverter<TParent> : ByteConverter
    {
        protected readonly TypeDetail parentTypeDetail = TypeAnalyzer<TParent>.GetTypeDetail();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryRead(ref ByteReader reader, ref ReadState state, object? parent)
            => TryRead(ref reader, ref state, (TParent?)parent);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryWrite(ref ByteWriter writer, ref WriteState state, object? parent)
            => TryWrite(ref writer, ref state, (TParent?)parent);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryRead(ref ByteReader reader, ref ReadState state, TParent? parent);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryWrite(ref ByteWriter writer, ref WriteState state, TParent? parent);
    }
}
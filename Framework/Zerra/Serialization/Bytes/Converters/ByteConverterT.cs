// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal abstract class ByteConverter<TParent> : ByteConverter
    {
        protected readonly TypeDetail parentTypeDetail = TypeAnalyzer<TParent>.GetTypeDetail();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed void Read(ref ByteReader reader, ref ReadState state)
        {
            _ = Read(ref reader, ref state, (TParent?)state.Current.Parent);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed void Write(ref ByteWriter writer, ref WriteState state)
        {
            _ = Write(ref writer, ref state, (TParent?)state.Current.Parent);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool Read(ref ByteReader reader, ref ReadState state, TParent? parent);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool Write(ref ByteWriter writer, ref WriteState state, TParent? parent);
    }
}
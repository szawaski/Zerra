// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET6_0_OR_GREATER

using System;
using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterDateOnlyNullable<TParent> : ByteConverter<TParent, DateOnly?>
    {
        protected override bool Read(ref ByteReader reader, ref ReadState state, out DateOnly? value)
        {
            if (!reader.TryReadDateOnlyNullable(state.CurrentFrame.NullFlags, out value, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, DateOnly? value)
        {
            if (!writer.TryWrite(value, state.CurrentFrame.NullFlags, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }
    }
}

#endif
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterTimeSpanNullable<TParent> : ByteConverter<TParent, TimeSpan?>
    {
        protected override bool TryRead(ref ByteReader reader, ref ReadState state, out TimeSpan? value)
        {
            if (!reader.TryReadTimeSpanNullable(state.Current.NullFlags, out value, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }

        protected override bool TryWrite(ref ByteWriter writer, ref WriteState state, TimeSpan? value)
        {
            if (!writer.TryWrite(value, state.Current.NullFlags, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }
    }
}
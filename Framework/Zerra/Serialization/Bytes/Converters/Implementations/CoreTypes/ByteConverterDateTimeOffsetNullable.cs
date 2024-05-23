// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterDateTimeOffsetNullable<TParent> : ByteConverter<TParent, DateTimeOffset?>
    {
        protected override bool Read(ref ByteReader reader, ref ReadState state, out DateTimeOffset? value)
        {
            if (!reader.TryReadDateTimeOffsetNullable(state.CurrentFrame.NullFlags, out value, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, DateTimeOffset? value)
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
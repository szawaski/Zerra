﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterUInt64<TParent> : ByteConverter<TParent, ulong>
    {
        protected override bool TryRead(ref ByteReader reader, ref ReadState state, out ulong value)
        {
            if (!reader.TryReadUInt64(out value, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }

        protected override bool TryWrite(ref ByteWriter writer, ref WriteState state, ulong value)
        {
            if (!writer.TryWrite(value, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }
    }
}
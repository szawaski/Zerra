﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET6_0_OR_GREATER

using System;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterTimeOnly<TParent> : ByteConverter<TParent, TimeOnly>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out TimeOnly value)
        {
            if (!reader.TryReadTimeOnly(out value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TimeOnly value)
        {
            if (!writer.TryWrite(value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }
    }
}

#endif
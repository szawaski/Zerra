﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET6_0_OR_GREATER

using System;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterTimeOnlyNullable<TParent> : ByteConverter<TParent, TimeOnly?>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out TimeOnly? value)
        {
            if (!reader.TryReadTimeOnlyNullable(state.Current.NullFlags, out value, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TimeOnly? value)
        {
            if (!writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded))
            {
                return false;
            }
            return true;
        }
    }
}

#endif
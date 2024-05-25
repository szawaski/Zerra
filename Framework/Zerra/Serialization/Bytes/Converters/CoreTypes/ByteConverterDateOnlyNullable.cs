﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET6_0_OR_GREATER

using System;
using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterDateOnlyNullable<TParent> : ByteConverter<TParent, DateOnly?>
    {
        protected override bool TryRead(ref ByteReader reader, ref ReadState state, out DateOnly? value)
        {
            if (!reader.TryReadDateOnlyNullable(state.Current.NullFlags, out value, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }

        protected override bool TryWrite(ref ByteWriter writer, ref WriteState state, DateOnly? value)
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

#endif
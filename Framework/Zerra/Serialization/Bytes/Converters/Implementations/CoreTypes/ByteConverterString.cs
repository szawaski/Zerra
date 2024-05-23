﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterString<TParent> : ByteConverter<TParent, string?>
    {
        protected override bool Read(ref ByteReader reader, ref ReadState state, out string? value)
        {
            int sizeNeeded;
            if (!state.CurrentFrame.StringLength.HasValue)
            {
                if (!reader.TryReadStringLength(state.CurrentFrame.NullFlags, out var stringLength, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    value = null;
                    return false;
                }
                if (stringLength == null)
                {
                    value = null;
                    return true;
                }
                if (stringLength == 0)
                {
                    value = String.Empty;
                    return true;
                }
                state.CurrentFrame.StringLength = stringLength;
            }

            if (!reader.TryReadString(state.CurrentFrame.StringLength.Value, out value, out sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }

            return true;
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, string? value)
        {
            if (!writer.TryWrite((string?)state.CurrentFrame.Object, state.CurrentFrame.NullFlags, out var sizeNeeded))
            {
                state.BytesNeeded = sizeNeeded;
                return false;
            }
            return true;
        }
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterString<TParent> : ByteConverter<TParent, string?>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out string? value)
        {
            if (!state.Current.StringLength.HasValue)
            {
                if (!reader.TryReadStringLength(state.Current.NullFlags, out state.Current.StringLength, out state.BytesNeeded))
                {
                    value = null;
                    return false;
                }
                if (state.Current.StringLength == null)
                {
                    value = null;
                    return true;
                }
                if (state.Current.StringLength == 0)
                {
                    value = String.Empty;
                    return true;
                }
            }

            return reader.TryReadString(state.Current.StringLength.Value, out value, out state.BytesNeeded);
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, string? value)
            => writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded);
    }
}
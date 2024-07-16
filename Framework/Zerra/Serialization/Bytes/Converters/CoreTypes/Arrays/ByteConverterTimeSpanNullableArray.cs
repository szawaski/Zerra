// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.Arrays
{
    internal sealed class ByteConverterTimeSpanNullableArray<TParent> : ByteConverter<TParent, TimeSpan?[]>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TimeSpan?[]? value)
        {
            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryRead(out state.Current.EnumerableLength, out state.BytesNeeded))
                {
                    value = default;
                    return false;
                }
            }

            if (!reader.TryRead(state.Current.EnumerableLength!.Value, out value, out state.BytesNeeded))
            {
                return false;
            }

            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TimeSpan?[] value)
        {
            if (!state.Current.HasWrittenLength)
            {
                if (!writer.TryWrite(value.Length, out state.BytesNeeded))
                {
                    return false;
                }
            }

            if (!writer.TryWrite(value, value.Length, out state.BytesNeeded))
            {
                state.Current.HasWrittenLength = true;
                return false;
            }

            return true;
        }
    }
}
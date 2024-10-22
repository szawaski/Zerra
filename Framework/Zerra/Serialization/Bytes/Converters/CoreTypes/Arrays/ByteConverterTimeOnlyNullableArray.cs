// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET6_0_OR_GREATER

using System;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.Arrays
{
    internal sealed class ByteConverterTimeOnlyNullableArray<TParent> : ByteConverter<TParent, TimeOnly?[]>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TimeOnly?[]? value)
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

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TimeOnly?[] value)
            => writer.TryWrite(value, value.Length, out state.BytesNeeded);
    }
}

#endif
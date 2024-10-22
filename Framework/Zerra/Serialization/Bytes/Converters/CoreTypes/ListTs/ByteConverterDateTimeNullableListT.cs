// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.ListTs
{
    internal sealed class ByteConverterDateTimeNullableList<TParent> : ByteConverter<TParent, List<DateTime?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out List<DateTime?>? value)
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

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in List<DateTime?> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}
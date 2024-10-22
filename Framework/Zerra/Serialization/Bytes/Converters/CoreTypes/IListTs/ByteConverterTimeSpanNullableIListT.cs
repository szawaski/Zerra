// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.IListTs
{
    internal sealed class ByteConverterTimeSpanNullableIList<TParent> : ByteConverter<TParent, IList<TimeSpan?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IList<TimeSpan?>? value)
        {
            if (!reader.TryRead(out List<TimeSpan?>? valueTyped, out state.BytesNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in IList<TimeSpan?> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}
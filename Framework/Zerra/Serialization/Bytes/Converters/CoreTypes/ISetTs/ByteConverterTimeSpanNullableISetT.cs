// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.ISetTs
{
    internal sealed class ByteConverterTimeSpanNullableISet<TParent> : ByteConverter<TParent, ISet<TimeSpan?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out ISet<TimeSpan?>? value)
        {
            if (!reader.TryRead(out HashSet<TimeSpan?>? valueTyped, out state.BytesNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in ISet<TimeSpan?> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}
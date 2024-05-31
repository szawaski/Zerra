// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterDateTimeOffsetNullable<TParent> : ByteConverter<TParent, DateTimeOffset?>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out DateTimeOffset? value)
            => reader.TryReadDateTimeOffsetNullable(state.Current.NullFlags, out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, DateTimeOffset? value)
            => writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded);
    }
}
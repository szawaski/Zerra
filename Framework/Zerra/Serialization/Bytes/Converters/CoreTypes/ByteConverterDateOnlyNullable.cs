// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET6_0_OR_GREATER

using System;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterDateOnlyNullable<TParent> : ByteConverter<TParent, DateOnly?>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out DateOnly? value)
            => reader.TryReadDateOnlyNullable(state.Current.NullFlags, out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, DateOnly? value)
            => writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded);
    }
}

#endif
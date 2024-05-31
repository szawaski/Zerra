// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterGuidNullable<TParent> : ByteConverter<TParent, Guid?>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out Guid? value)
            => reader.TryReadGuidNullable(state.Current.NullFlags, out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, Guid? value)
            => writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded);
    }
}
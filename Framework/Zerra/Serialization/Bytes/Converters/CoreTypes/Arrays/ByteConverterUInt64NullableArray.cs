// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.Arrays
{
    internal sealed class ByteConverterUInt64NullableArray<TParent> : ByteConverter<TParent, ulong?[]>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out ulong?[]? value)
            => reader.TryRead(out value, out state.SizeNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in ulong?[] value)
            => writer.TryWrite(value, value.Length, out state.BytesNeeded);
    }
}
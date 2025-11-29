// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.ListTs
{
    internal sealed class ByteConverterSByteNullableList : ByteConverter<List<sbyte?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out List<sbyte?>? value)
            => reader.TryRead(out value, out state.SizeNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in List<sbyte?> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}
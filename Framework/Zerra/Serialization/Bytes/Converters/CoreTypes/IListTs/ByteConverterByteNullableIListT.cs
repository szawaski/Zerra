// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.IListTs
{
    internal sealed class ByteConverterByteNullableIList : ByteConverter<IList<byte?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IList<byte?>? value)
        {
            if (!reader.TryRead(out List<byte?>? valueTyped, out state.SizeNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in IList<byte?> value)
            => writer.TryWrite(value, value.Count, out state.SizeNeeded);
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.IListTs
{
    internal sealed class ByteConverterInt64IList : ByteConverter<IList<long>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IList<long>? value)
        {
            if (!reader.TryRead(out List<long>? valueTyped, out state.SizeNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in IList<long> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if !NETSTANDARD2_0

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.IListTs
{
    internal sealed class ByteConverterTimeOnlyIList : ByteConverter<IList<TimeOnly>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IList<TimeOnly>? value)
        {
            if (!reader.TryRead(out List<TimeOnly>? valueTyped, out state.SizeNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in IList<TimeOnly> value)
            => writer.TryWrite(value, value.Count, out state.SizeNeeded);
    }
}

#endif
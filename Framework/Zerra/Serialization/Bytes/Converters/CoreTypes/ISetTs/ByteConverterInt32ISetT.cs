// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.ISetTs
{
    internal sealed class ByteConverterInt32ISet : ByteConverter<ISet<int>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out ISet<int>? value)
        {
            if (!reader.TryRead(out HashSet<int>? valueTyped, out state.SizeNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in ISet<int> value)
            => writer.TryWrite(value, value.Count, out state.SizeNeeded);
    }
}
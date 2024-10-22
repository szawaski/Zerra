// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.IReadOnlyListTs
{
    internal sealed class ByteConverterInt32NullableIReadOnlyList<TParent> : ByteConverter<TParent, IReadOnlyList<int?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IReadOnlyList<int?>? value)
        {
            if (!reader.TryRead(out List<int?>? valueTyped, out state.BytesNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in IReadOnlyList<int?> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.IReadOnlyListTs
{
    internal sealed class ByteConverterDoubleIReadOnlyList<TParent> : ByteConverter<TParent, IReadOnlyList<double>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IReadOnlyList<double>? value)
        {
            if (!reader.TryRead(out List<double>? valueTyped, out state.BytesNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in IReadOnlyList<double> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}
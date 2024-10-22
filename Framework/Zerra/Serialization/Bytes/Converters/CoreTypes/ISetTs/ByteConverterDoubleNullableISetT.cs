// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.ISetTs
{
    internal sealed class ByteConverterDoubleNullableISet<TParent> : ByteConverter<TParent, ISet<double?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out ISet<double?>? value)
        {
            if (!reader.TryRead(out HashSet<double?>? valueTyped, out state.BytesNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in ISet<double?> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}
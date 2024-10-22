// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.IReadOnlyCollectionTs
{
    internal sealed class ByteConverterBooleanNullableIReadOnlyCollection<TParent> : ByteConverter<TParent, IReadOnlyCollection<bool?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IReadOnlyCollection<bool?>? value)
        {
            if (!reader.TryRead(out List<bool?>? valueTyped, out state.BytesNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in IReadOnlyCollection<bool?> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}
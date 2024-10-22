// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.ICollectionTs
{
    internal sealed class ByteConverterCharICollection<TParent> : ByteConverter<TParent, ICollection<char>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out ICollection<char>? value)
        {
            if (!reader.TryRead(out List<char>? valueTyped, out state.BytesNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in ICollection<char> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}
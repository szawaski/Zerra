// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.ListTs
{
    internal sealed class ByteConverterInt32List<TParent> : ByteConverter<TParent, List<int>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out List<int>? value)
            => reader.TryRead(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in List<int> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}
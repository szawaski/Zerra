// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes
{
    internal sealed class ByteConverterInt16<TParent> : ByteConverter<TParent, short>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out short value)
            => reader.TryReadInt16(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, short value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes
{
    internal sealed class ByteConverterUInt16<TParent> : ByteConverter<TParent, ushort>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out ushort value)
            => reader.TryReadUInt16(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, ushort value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}
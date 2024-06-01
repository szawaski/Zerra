// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.Values
{
    internal sealed class ByteConverterSingle<TParent> : ByteConverter<TParent, float>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out float value)
            => reader.TryRead(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, float value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}
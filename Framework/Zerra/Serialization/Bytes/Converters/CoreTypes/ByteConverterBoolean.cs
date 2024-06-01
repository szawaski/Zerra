// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes
{
    internal sealed class ByteConverterBoolean<TParent> : ByteConverter<TParent, bool>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out bool value)
            => reader.TryReadBoolean(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, bool value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}
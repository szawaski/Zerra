// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.Values
{
    internal sealed class ByteConverterInt32Nullable<TParent> : ByteConverter<TParent, int?>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out int? value)
            => reader.TryRead(state.Current.NullFlags, out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, int? value)
            => writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded);
    }
}
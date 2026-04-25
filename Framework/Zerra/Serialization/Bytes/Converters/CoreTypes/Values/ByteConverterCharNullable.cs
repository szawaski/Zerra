// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.Values
{
    internal sealed class ByteConverterCharNullable : ByteConverter<char?>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out char? value)
            => reader.TryRead(out value, out state.SizeNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in char? value)
            => writer.TryWrite(value!.Value, out state.SizeNeeded);
    }
}
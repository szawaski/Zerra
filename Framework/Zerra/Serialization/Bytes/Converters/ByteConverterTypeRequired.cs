// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters
{
    //we should never get to the read or writes because a type specified will override this converter
    internal sealed class ByteConverterTypeRequired : ByteConverter<object>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out object? value)
            => throw new NotSupportedException($"Cannot deserialize {TypeDetail.Type.Name} without type information");
        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in object? value)
            => throw new NotSupportedException($"Cannot deserialize {TypeDetail.Type.Name} without type information");
    }
}
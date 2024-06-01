// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET6_0_OR_GREATER

using System;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes
{
    internal sealed class ByteConverterTimeOnly<TParent> : ByteConverter<TParent, TimeOnly>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TimeOnly value)
            => reader.TryReadTimeOnly(out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TimeOnly value)
            => writer.TryWrite(value, out state.BytesNeeded);
    }
}

#endif
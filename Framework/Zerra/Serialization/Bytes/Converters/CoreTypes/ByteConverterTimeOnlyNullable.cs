// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET6_0_OR_GREATER

using System;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes
{
    internal sealed class ByteConverterTimeOnlyNullable<TParent> : ByteConverter<TParent, TimeOnly?>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TimeOnly? value)
            => reader.TryReadTimeOnlyNullable(state.Current.NullFlags, out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TimeOnly? value)
            => writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded);
    }
}

#endif
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET6_0_OR_GREATER

using System;
using Zerra.Serialization.Bytes.Converters;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterDateOnlyNullable<TParent> : ByteConverter<TParent, DateOnly?>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out DateOnly? value)
            => reader.TryRead(out value, out state.SizeNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in DateOnly? value)
            => writer.TryWrite(value!.Value, out state.BytesNeeded);
    }
}

#endif
﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterTimeSpanNullable<TParent> : ByteConverter<TParent, TimeSpan?>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TimeSpan? value)
            => reader.TryReadTimeSpanNullable(state.Current.NullFlags, out value, out state.BytesNeeded);

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TimeSpan? value)
            => writer.TryWrite(value, state.Current.NullFlags, out state.BytesNeeded);
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters
{
    internal sealed class ByteConverterTypeRequired<TParent> : ByteConverter<TParent, object>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, bool nullFlags, out object? value)
            => throw new NotSupportedException($"Cannot deserialize {typeDetail.Type.GetNiceName()} without type information");
        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, bool nullFlags, object? value)
            => throw new NotSupportedException($"Cannot deserialize {typeDetail.Type.GetNiceName()} without type information");
    }
}
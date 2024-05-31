// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using System;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterTypeRequired<TParent> : ByteConverter<TParent, object>
    {
        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out object? value)
         => throw new NotSupportedException("Cannot deserialize without type information");
        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, object? value)
     => throw new NotSupportedException("Cannot deserialize without type information");
    }
}
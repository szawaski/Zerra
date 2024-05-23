// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.IO;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterDictionary<TParent, TDictionary, TKey, TValue> : ByteConverter<TParent, TDictionary>
    {
        protected override bool Read(ref ByteReader reader, ref ReadState state, out TDictionary? value)
        {
            throw new NotImplementedException();
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, TDictionary? value)
        {
            throw new NotImplementedException();
        }
    }
}
﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.IReadOnlyCollectionTs
{
    internal sealed class ByteConverterInt16NullableIReadOnlyCollection<TParent> : ByteConverter<TParent, IReadOnlyCollection<short?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IReadOnlyCollection<short?>? value)
        {
            if (!reader.TryRead(out List<short?>? valueTyped, out state.SizeNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in IReadOnlyCollection<short?> value)
            => writer.TryWrite(value, value.Count, out state.BytesNeeded);
    }
}
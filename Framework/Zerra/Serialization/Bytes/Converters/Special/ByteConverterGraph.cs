// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Special
{
    internal sealed class ByteConverterGraph<TParent, TValue> : ByteConverter<TParent, TValue>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TValue? value)
        {
            if (!reader.TryRead(out string? signature, out state.SizeNeeded))
            {
                value = default;
                return false;
            }
            if (signature is null)
            {
                value = default;
                return true;
            }

            var graph = typeDetail.Creator();
            if (!Graph.TryParseSignature(signature, (Graph)(object)graph!))
                throw new FormatException($"Invalid graph signature {signature}");
            value = graph;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TValue value)
            => writer.TryWrite(((Graph)(object)value!).Signature, out state.BytesNeeded);
    }
}

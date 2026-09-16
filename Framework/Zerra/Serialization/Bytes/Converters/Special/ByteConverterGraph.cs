// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Special
{
    //a graph keeps its members in private fields so it is written as the signature and read back by parsing it
    internal sealed class ByteConverterGraph<TValue> : ByteConverter<TValue>
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

            var graph = TypeDetail.Creator!();
            Graph.ParseSignature(signature, (Graph)(object)graph!);
            value = graph;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TValue value)
            => writer.TryWrite(((Graph)(object)value!).Signature, out state.SizeNeeded);
    }
}

// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Special
{
    internal sealed class JsonConverterGraph<TParent, TValue> : JsonConverter<TParent, TValue>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out TValue? value)
        {
            if (valueType != JsonValueType.String)
            {
                if (state.ErrorOnTypeMismatch)
                    ThrowCannotConvert(ref reader);

                value = default;
                return Drain(ref reader, ref state, valueType);
            }

            if (!reader.TryReadStringEscapedQuoted(true, out var signature, out state.SizeNeeded))
            {
                value = default;
                return false;
            }

            var graph = typeDetail.Creator();
            if (!Graph.TryParseSignature(signature, (Graph)(object)graph!))
            {
                if (state.ErrorOnTypeMismatch)
                    ThrowCannotConvert(ref reader);
                value = default;
                return true;
            }
            value = graph;
            return true;
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in TValue value)
            => writer.TryWriteEscapedQuoted(((Graph)(object)value!).Signature, out state.SizeNeeded);
    }
}

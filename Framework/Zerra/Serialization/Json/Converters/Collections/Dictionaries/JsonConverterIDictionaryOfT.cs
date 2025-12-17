// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Dictionaries
{
    internal sealed class JsonConverterIDictionaryOfT<TDictionary> : JsonConverter<TDictionary>
    {
        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out TDictionary? value)
        {
            throw new NotSupportedException($"{nameof(JsonConverter)} does not support IDictionary because it cannot determine the types to deserialize");
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in TDictionary value)
        {
            throw new NotSupportedException($"{nameof(JsonConverter)} does not support IDictionary because it cannot determine the types to deserialize");
        }
    }
}
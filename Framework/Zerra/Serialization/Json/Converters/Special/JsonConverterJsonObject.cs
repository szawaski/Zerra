// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Special
{
    internal sealed class JsonConverterJsonObject : JsonConverter<JsonObject>
    {
        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out JsonObject? value)
            => ReadJsonObject(ref reader, ref state, token, out value);

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in JsonObject value)
        {
            var json = (string?)state.Current.Object ?? value.ToString();
            if (!writer.TryWrite(json.AsSpan(), out state.SizeNeeded))
            {
                state.Current.Object = json;
                return false;
            }
            return true;
        }
    }
}

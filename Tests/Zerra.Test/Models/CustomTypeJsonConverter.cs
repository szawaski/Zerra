// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Serialization.Json.Converters;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Test
{
    public class CustomTypeJsonConverter<TParent> : JsonConverter<TParent, CustomType>
    {
        protected override bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out CustomType value)
        {
            switch (valueType)
            {
                case JsonValueType.String:
                    if (!reader.TryReadStringEscapedQuoted(true, out string str, out state.SizeNeeded))
                    {
                        value = null;
                        return false;
                    }
                    var values = str.Split(" - ");
                    value = new CustomType()
                    {
                        Things1 = values[0],
                        Things2 = values.Length > 0 ? values[1] : null
                    };
                    return true;
                case JsonValueType.Null_Completed:
                    value = null;
                    return true;
                case JsonValueType.Number:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = null;
                    return true;
                case JsonValueType.False_Completed:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = null;
                    return true;
                case JsonValueType.True_Completed:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = null;
                    return true;
                case JsonValueType.Object:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = null;
                    return DrainObject(ref reader, ref state);
                case JsonValueType.Array:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = null;
                    return DrainArray(ref reader, ref state);
                default:
                    throw new NotImplementedException();
            }
        }

        protected override bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in CustomType value)
        {
            var str = $"{value.Things1} - {value.Things2}";
            return writer.TryWriteEscapedQuoted(str, out state.SizeNeeded);
        }
    }
}

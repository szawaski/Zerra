// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.CoreTypes.Values
{
    internal sealed class JsonConverterByte<TParent> : JsonConverter<TParent, byte>
    {
        protected override sealed bool TryReadValue(ref CharReader reader, ref ReadState state, out byte value)
        {
            switch (state.Current.ValueType)
            {
                case JsonValueType.Object_Started:
                    value = default;
                    return DrainObject(ref reader, ref state);
                case JsonValueType.Array_Started:
                    value = default;
                    return DrainArray(ref reader, ref state);
                case JsonValueType.String_Started:
                    if (!ReadString(ref reader, ref state, out var str))
                    {
                        value = default;
                        return false;
                    }
                    _ = Byte.TryParse(str, out value);
                    return true;
                case JsonValueType.Number_Started:
                    if (!ReadNumberAsInt64(ref reader, ref state, out var number))
                    {
                        value = default;
                        return false;
                    }
                    value = (byte)number;
                    return true;
                case JsonValueType.Null_Completed:
                    value = default;
                    return true;
                case JsonValueType.False_Completed:
                    value = (byte)0;
                    return true;
                case JsonValueType.True_Completed:
                    value = (byte)1;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override sealed bool TryWriteValue(ref CharWriter writer, ref WriteState state, byte value)
            => writer.TryWrite(value, out state.CharsNeeded);
    }
}
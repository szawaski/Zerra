// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;
using System.Collections.Generic;

namespace Zerra.Serialization.Json.Converters.Collections
{
    internal sealed class JsonConverterByteArray<TParent> : JsonConverter<TParent, byte[]>
    {
        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out byte[]? value)
        {
            if (valueType != JsonValueType.String)
            {
                if (state.ErrorOnTypeMismatch)
                    throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");

                value = default;
                return Drain(ref reader, ref state, valueType);
            }

            if (!ReadString(ref reader, ref state, true, out var str))
            {
                value = default;
                return false;
            }

            if (str.Length == 0)
            {
                value = Array.Empty<byte>();
                return true;
            }

            value = Convert.FromBase64String(str);
            return true;
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, byte[] value)
        {
            string str;
            if (!state.Current.HasWrittenStart)
            {
                if (value.Length == 0)
                {
                    if (!writer.TryWriteEmptyString(out state.CharsNeeded))
                    {
                        return false;
                    }
                    return true;
                }

                str = Convert.ToBase64String(value);
            }
            else
            {
                str = (string)state.Current.Object!;
            }

            if (!writer.TryWriteQuoted(str, out state.CharsNeeded))
            {
                state.Current.Object = str;
                return false;
            }
            return true;
        }
    }
}
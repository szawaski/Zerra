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
        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, out byte[]? value)
        {
            if (state.Current.ValueType == JsonValueType.Null_Completed)
            {
                value = default;
                return true;
            }

            if (state.Current.ValueType != JsonValueType.String)
            {
                if (state.ErrorOnTypeMismatch)
                    throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");

                value = default;
                return Drain(ref reader, ref state);
            }

            if (!ReadString(ref reader, ref state, out var str))
            {
                value = default;
                return false;
            }

            value = Convert.FromBase64String(str);
            return true;
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, byte[] value)
        {
            string str;
            if (!state.Current.HasWrittenStart)
            {
                str = Convert.ToBase64String(value);
            }
            else
            {
                str = (string)state.Current.Object!;
            }

            if (!writer.TryWrite(str, out state.CharsNeeded))
            {
                state.Current.Object = str;
                return false;
            }
            return true;
        }
    }
}
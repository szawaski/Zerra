﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Special
{
    internal sealed class JsonConverterByteArray<TParent> : JsonConverter<TParent, byte[]>
    {
        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out byte[]? value)
        {
            if (valueType != JsonValueType.String)
            {
                if (state.ErrorOnTypeMismatch)
                    ThrowCannotConvert(ref reader);

                value = default;
                return Drain(ref reader, ref state, valueType);
            }

            if (!reader.TryReadStringEscapedQuoted(true, out var str, out state.SizeNeeded))
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in byte[] value)
        {
            string str;
            if (!state.Current.HasWrittenStart)
            {
                if (value.Length == 0)
                {
                    if (!writer.TryWriteEmptyString(out state.SizeNeeded))
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

            if (!writer.TryWriteQuoted(str, out state.SizeNeeded))
            {
                state.Current.Object = str;
                return false;
            }
            return true;
        }
    }
}
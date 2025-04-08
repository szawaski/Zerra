// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Special
{
    internal sealed class JsonConverterCancellationTokenNullable<TParent> : JsonConverter<TParent, CancellationToken?>
    {
        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out CancellationToken? value)
        {
            switch (valueType)
            {
                case JsonValueType.Object:
                    value = CancellationToken.None;
                    return DrainObject(ref reader, ref state);
                case JsonValueType.Array:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = null;
                    return DrainArray(ref reader, ref state);
                case JsonValueType.String:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = null;
                    return DrainString(ref reader, ref state);
                case JsonValueType.Number:
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);
                    value = null;
                    return DrainNumber(ref reader, ref state);
                case JsonValueType.Null_Completed:
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
                default:
                    throw new NotImplementedException();
            }
        }

        private static readonly char[] emptyObjectChars = ['{', '}'];
        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in CancellationToken? value)
            => value is null ? writer.TryWriteNull(out state.SizeNeeded) : writer.TryWrite(emptyObjectChars, out state.SizeNeeded);
    }
}
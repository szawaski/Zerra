// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections
{
    internal sealed class ByteConverterArrayT<TValue> : ByteConverter<TValue[]>
    {
        private ByteConverter<TValue> converter = null!;

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            converter = (ByteConverter<TValue>)ByteConverterFactory.Get(valueTypeDetail, nameof(ByteConverterArrayT<TValue>), null, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TValue[]? value)
        {
            TValue[]? array;
            int length;

            if (!state.Current.HasCreated)
            {
                if (!reader.TryRead(out length, out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
                    array = new TValue[length];
                    value = array;
                    if (length == 0)
                        return true;
                }
                else
                {
                    value = default;
                    if (length == 0)
                        return true;
                    array = null;
                }
            }
            else
            {
                array = (TValue[]?)state.Current.Object;
                length = state.Current.EnumerableLength!.Value;
                value = array;
            }

            var index = state.Current.EnumeratorIndex;
            for (; index < length; index++)
            {
                if (!converter.TryReadToValue(ref reader, ref state, out var item))
                {
                    state.Current.HasCreated = true;
                    state.Current.Object = array;
                    state.Current.EnumerableLength = length;
                    state.Current.EnumeratorIndex = index;
                    return false;
                }
                if (array is not null)
                    array[index] = item!;
            }
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TValue[] value)
        {
            if (!state.Current.EnumeratorInProgress)
            {
                if (!writer.TryWrite(value.Length, out state.SizeNeeded))
                {
                    return false;
                }
                if (value.Length == 0)
                {
                    return true;
                }
            }

            var index = state.Current.EnumeratorIndex;
            for (; index < value.Length; index++)
            {
                if (!converter.TryWriteFromValue(ref writer, ref state, value[index]))
                {
                    state.Current.EnumeratorInProgress = true;
                    state.Current.EnumeratorIndex = index;
                    return false;
                }
            }

            return true;
        }
    }
}
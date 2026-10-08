// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Lists
{
    internal sealed class ByteConverterListT<TValue> : ByteConverter<List<TValue>>
    {
        private ByteConverter<TValue> converter = null!;

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            converter = (ByteConverter<TValue>)ByteConverterFactory.Get(valueTypeDetail, nameof(ByteConverterListT<TValue>), null, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out List<TValue>? value)
        {
            if (state.Current.DrainBytes)
            {
                value = default;
                if (!state.Current.EnumerableLength.HasValue)
                {
                    if (!reader.TryRead(out state.Current.EnumerableLength, out state.SizeNeeded))
                        return false;
                }

                var length = state.Current.EnumerableLength!.Value;
                for (; state.Current.EnumeratorIndex < length; state.Current.EnumeratorIndex++)
                {
                    if (!converter.TryReadToValue(ref reader, ref state, out _))
                        return false;
                }
                return true;
            }

            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryRead(out state.Current.EnumerableLength, out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }

                value = new List<TValue>(state.Current.EnumerableLength!.Value);
                if (state.Current.EnumerableLength!.Value == 0)
                    return true;
            }
            else
            {
                value = (List<TValue>)state.Current.Object!;
            }

            if (value.Count == state.Current.EnumerableLength.Value)
                return true;

            for (; ; )
            {
                if (!converter.TryReadToValue(ref reader, ref state, out var item))
                {
                    state.Current.Object = value;
                    return false;
                }
                value.Add(item!);

                if (value.Count == state.Current.EnumerableLength!.Value)
                    return true;
            }
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in List<TValue> value)
        {
            if (!state.Current.EnumeratorInProgress)
            {
                if (!writer.TryWrite(value.Count, out state.SizeNeeded))
                {
                    return false;
                }
                if (value.Count == 0)
                {
                    return true;
                }
            }

            var index = state.Current.EnumeratorIndex;
            for (; index < value.Count; index++)
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
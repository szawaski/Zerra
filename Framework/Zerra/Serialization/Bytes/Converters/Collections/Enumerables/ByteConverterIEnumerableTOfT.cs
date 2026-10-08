// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Enumerables
{
    internal sealed class ByteConverterIEnumerableTOfT<TEnumerable, TValue> : ByteConverter<TEnumerable>
    {
        private ByteConverter<TValue> converter = null!;

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            converter = (ByteConverter<TValue>)ByteConverterFactory.Get(valueTypeDetail, nameof(ByteConverterIEnumerableTOfT<TEnumerable, TValue>), null, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TEnumerable? value)
        {
            if (!state.Current.DrainBytes)
                throw new NotSupportedException($"Cannot deserialize {TypeDetail.Type.Name} because no interface to populate the collection");

            value = default;
            int length;
            if (!state.Current.HasCreated)
            {
                if (!reader.TryRead(out length, out state.SizeNeeded))
                    return false;
                if (length == 0)
                    return true;
            }
            else
            {
                length = state.Current.EnumerableLength!.Value;
            }

            var index = state.Current.EnumeratorIndex;
            for (; index < length; index++)
            {
                if (!converter.TryReadToValue(ref reader, ref state, out _))
                {
                    state.Current.HasCreated = true;
                    state.Current.EnumerableLength = length;
                    state.Current.EnumeratorIndex = index;
                    return false;
                }
            }
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TEnumerable value)
        {
            IEnumerator<TValue> enumerator;

            if (state.Current.Object is null)
            {
                if (value is ICollection<TValue> collection1)
                {
                    if (!writer.TryWrite(collection1.Count, out state.SizeNeeded))
                    {
                        return false;
                    }
                    if (collection1.Count == 0)
                    {
                        return true;
                    }

                    enumerator = collection1.GetEnumerator();
                }
                else if (value is IReadOnlyCollection<TValue> collection2)
                {
                    if (!writer.TryWrite(collection2.Count, out state.SizeNeeded))
                    {
                        return false;
                    }
                    if (collection2.Count == 0)
                    {
                        return true;
                    }

                    enumerator = collection2.GetEnumerator();
                }
                else
                {
                    var enumerable = (IEnumerable<TValue>)value!;

                    var count = 0;
                    foreach (var item in enumerable)
                        count++;

                    if (!writer.TryWrite(count, out state.SizeNeeded))
                    {
                        return false;
                    }
                    if (count == 0)
                    {
                        return true;
                    }

                    enumerator = enumerable.GetEnumerator();
                }
            }
            else
            {
                enumerator = (IEnumerator<TValue>)state.Current.Object!;
            }

            while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            {
                if (!converter.TryWriteFromValue(ref writer, ref state, enumerator.Current))
                {
                    state.Current.Object = enumerator;
                    state.Current.EnumeratorInProgress = true;
                    return false;
                }

                state.Current.EnumeratorInProgress = false;
            }

            return true;
        }
    }
}
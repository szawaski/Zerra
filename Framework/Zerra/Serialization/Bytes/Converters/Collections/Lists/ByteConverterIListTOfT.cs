// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Lists
{
    internal sealed class ByteConverterIListTOfT<TList, TValue> : ByteConverter<TList>
    {
        private ByteConverter<TValue> converter = null!;

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            converter = (ByteConverter<TValue>)ByteConverterFactory.Get(valueTypeDetail, nameof(ByteConverterIListTOfT<TList, TValue>), null, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TList? value)
        {
            IList<TValue> list;

            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryRead(out state.Current.EnumerableLength, out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
                    if (!TypeDetail.HasCreator)
                        throw new InvalidOperationException($"{TypeDetail.Type} does not have a parameterless constructor.");
                    value = TypeDetail.Creator!();
                    list = (IList<TValue>)value!;
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                }
                else
                {
                    value = default;
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                    list = new IListTCounter<TValue>();
                }
            }
            else
            {
                list = (IList<TValue>)state.Current.Object!;
                if (!state.Current.DrainBytes)
                    value = (TList?)state.Current.Object;
                else
                    value = default;
            }

            if (list.Count == state.Current.EnumerableLength.Value)
                return true;

            for (; ; )
            {
                if (!converter.TryReadToValue(ref reader, ref state, out var item))
                {
                    state.Current.Object = list;
                    return false;
                }
                list.Add(item!);

                if (list.Count == state.Current.EnumerableLength!.Value)
                    return true;
            }
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TList value)
        {
            IEnumerator<TValue> enumerator;

            if (state.Current.Object is null)
            {
                var collection = (IList<TValue>)value!;

                if (!writer.TryWrite(collection.Count, out state.SizeNeeded))
                {
                    return false;
                }
                if (collection.Count == 0)
                {
                    return true;
                }

                enumerator = collection.GetEnumerator();
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
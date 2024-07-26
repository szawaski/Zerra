// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Collections
{
    internal sealed class ByteConverterICollectionTOfT<TParent, TCollection, TValue> : ByteConverter<TParent, TCollection>
    {
        private ByteConverter<ICollection<TValue>> readConverter = null!;
        private ByteConverter<IEnumerator<TValue>> writeConverter = null!;

        private static TValue Getter(IEnumerator<TValue> parent) => parent.Current;
        private static void Setter(ICollection<TValue> parent, TValue value) => parent.Add(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            readConverter = ByteConverterFactory<ICollection<TValue>>.Get(valueTypeDetail, nameof(ByteConverterICollectionTOfT<TParent, TCollection, TValue>), null, Setter);
            writeConverter = ByteConverterFactory<IEnumerator<TValue>>.Get(valueTypeDetail, nameof(ByteConverterICollectionTOfT<TParent, TCollection, TValue>), Getter, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TCollection? value)
        {
            ICollection<TValue> Collection;

            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryRead(out state.Current.EnumerableLength, out state.BytesNeeded))
                {
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
                    value = typeDetail.Creator();
                    Collection = (ICollection<TValue>)value!;
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                }
                else
                {
                    value = default;
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                    Collection = new ICollectionTCounter<TValue>();
                }
            }
            else
            {
                Collection = (ICollection<TValue>)state.Current.Object!;
                if (!state.Current.DrainBytes)
                    value = (TCollection?)state.Current.Object;
                else
                    value = default;
            }

            if (Collection.Count == state.Current.EnumerableLength.Value)
                return true;

            for (; ; )
            {
                if (!readConverter.TryReadFromParent(ref reader, ref state, Collection, true))
                {
                    state.Current.Object = Collection;
                    return false;
                }

                if (Collection.Count == state.Current.EnumerableLength!.Value)
                    return true;
            }
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TCollection value)
        {
            IEnumerator<TValue> enumerator;

            if (state.Current.Object is null)
            {
                var collection = (ICollection<TValue>)value!;

                if (!writer.TryWrite(collection.Count, out state.BytesNeeded))
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
                if (!writeConverter.TryWriteFromParent(ref writer, ref state, enumerator, true))
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
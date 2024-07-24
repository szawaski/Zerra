// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Enumerables
{
    internal sealed class ByteConverterIEnumerableTOfT<TParent, TEnumerable, TValue> : ByteConverter<TParent, TEnumerable>
    {
        private ByteConverter<ArrayAccessor<TValue>> readConverter = null!;
        private ByteConverter<IEnumerator<TValue>> writeConverter = null!;

        private static TValue Getter(IEnumerator<TValue> parent) => parent.Current;
        private static void Setter(ArrayAccessor<TValue> parent, TValue value) => parent.Set(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            readConverter = ByteConverterFactory<ArrayAccessor<TValue>>.Get(valueTypeDetail, null, null, Setter);
            writeConverter = ByteConverterFactory<IEnumerator<TValue>>.Get(valueTypeDetail, null, Getter, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TEnumerable? value)
            => throw new NotSupportedException($"Cannot deserialize {typeDetail.Type.GetNiceName()} because no interface to populate the collection");

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TEnumerable value)
        {
            IEnumerator<TValue> enumerator;

            if (state.Current.Object is null)
            {
                if (value is ICollection<TValue> collection1)
                {
                    if (!writer.TryWrite(collection1.Count, out state.BytesNeeded))
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
                    if (!writer.TryWrite(collection2.Count, out state.BytesNeeded))
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

                    if (!writer.TryWrite(count, out state.BytesNeeded))
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
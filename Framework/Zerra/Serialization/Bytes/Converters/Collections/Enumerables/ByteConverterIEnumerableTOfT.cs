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
        private ByteConverter converter = null!;

        private static TValue Getter(object parent) => ((IEnumerator<TValue>)parent).Current;
        private static void Setter(object parent, TValue value) => ((ArrayAccessor<TValue>)parent).Set(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            converter = ByteConverterFactory.Get(valueTypeDetail, nameof(ByteConverterIEnumerableTOfT<TEnumerable, TValue>), Getter, Setter);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TEnumerable? value)
            => throw new NotSupportedException($"Cannot deserialize {typeDetail.Type.Name} because no interface to populate the collection");

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TEnumerable value)
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
                if (!converter.TryWriteFromParent(ref writer, ref state, enumerator, true))
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
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Enumerables
{
    internal sealed class ByteConverterIEnumerableOfT<TEnumerable> : ByteConverter<TEnumerable>
    {
        private ByteConverter converter = null!;

        private static object Getter(object parent) => ((IEnumerator)parent).Current;
        private static void Setter(object parent, object value) => ((ArrayAccessor<object>)parent).Set(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<object>.GetTypeDetail();
            converter = ByteConverterFactory.Get(valueTypeDetail, nameof(ByteConverterIEnumerableOfT<TEnumerable>), Getter, Setter);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TEnumerable? value)
            => throw new NotSupportedException($"Cannot deserialize {typeDetail.Type.Name} because no interface to populate the collection");

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TEnumerable value)
        {
            IEnumerator enumerator;

            if (state.Current.Object is null)
            {
                if (value is ICollection collection)
                {
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
                    var enumerable = (IEnumerable)value!;

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
                enumerator = (IEnumerator)state.Current.Object!;
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
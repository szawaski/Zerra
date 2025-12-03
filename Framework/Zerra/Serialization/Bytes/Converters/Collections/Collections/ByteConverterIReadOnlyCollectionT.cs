// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;
using Zerra.SourceGeneration;

namespace Zerra.Serialization.Bytes.Converters.Collections.Collections
{
    internal sealed class ByteConverterIReadOnlyCollectionT<TValue> : ByteConverter<IReadOnlyCollection<TValue>>
    {
        private ByteConverter converter = null!;

        private static TValue Getter(object parent) => ((IEnumerator<TValue>)parent).Current;
        private static void Setter(object parent, TValue value) => ((ArrayAccessor<TValue>)parent).Set(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            converter = ByteConverterFactory.Get(valueTypeDetail, nameof(ByteConverterIReadOnlyCollectionT<TValue>), Getter, Setter);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IReadOnlyCollection<TValue>? value)
        {
            ArrayAccessor<TValue> accessor;

            if (state.Current.Object is null)
            {
                if (!reader.TryRead(out int length, out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
                    if (typeDetail.Type.IsInterface)
                    {
                        accessor = new ArrayAccessor<TValue>(new TValue[length]);
                        value = accessor.Array;
                    }
                    else
                    {
                        throw new InvalidOperationException($"{nameof(ByteSerializer)} cannot deserialize {typeDetail.Type.Name}");
                    }
                    if (length == 0)
                        return true;
                }
                else
                {
                    value = default;
                    if (length == 0)
                        return true;
                    accessor = new ArrayAccessor<TValue>(length);
                }
            }
            else
            {
                accessor = (ArrayAccessor<TValue>)state.Current.Object!;
                value = accessor.Array;
            }

            if (accessor.Index == accessor.Length)
                return true;

            for (; ; )
            {
                if (!converter.TryReadFromParent(ref reader, ref state, accessor, true))
                {
                    state.Current.Object = accessor;
                    return false;
                }
                accessor.Index++;
                if (accessor.Index == accessor.Length)
                    return true;
            }
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in IReadOnlyCollection<TValue> value)
        {
            IEnumerator<TValue> enumerator;

            if (state.Current.Object is null)
            {
                if (!writer.TryWrite(value.Count, out state.BytesNeeded))
                {
                    return false;
                }
                if (value.Count == 0)
                {
                    return true;
                }

                enumerator = value.GetEnumerator();
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
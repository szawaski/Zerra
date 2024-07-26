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
    internal sealed class ByteConverterIReadOnlyCollectionT<TParent, TValue> : ByteConverter<TParent, IReadOnlyCollection<TValue>>
    {
        private ByteConverter<ArrayAccessor<TValue>> readConverter = null!;
        private ByteConverter<IEnumerator<TValue>> writeConverter = null!;

        private static TValue Getter(IEnumerator<TValue> parent) => parent.Current;
        private static void Setter(ArrayAccessor<TValue> parent, TValue value) => parent.Set(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            readConverter = ByteConverterFactory<ArrayAccessor<TValue>>.Get(valueTypeDetail, nameof(ByteConverterIReadOnlyCollectionT<TParent, TValue>), null, Setter);
            writeConverter = ByteConverterFactory<IEnumerator<TValue>>.Get(valueTypeDetail, nameof(ByteConverterIReadOnlyCollectionT<TParent, TValue>), Getter, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IReadOnlyCollection<TValue>? value)
        {
            ArrayAccessor<TValue> accessor;

            if (state.Current.Object is null)
            {
                if (!reader.TryRead(out int length, out state.BytesNeeded))
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
                        throw new InvalidOperationException($"{nameof(ByteSerializer)} cannot deserialize {typeDetail.Type.GetNiceName()}");
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
                if (!readConverter.TryReadFromParent(ref reader, ref state, accessor, true))
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
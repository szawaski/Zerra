// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Enumerables
{
    internal sealed class ByteConverterIEnumerable<TParent> : ByteConverter<TParent, IEnumerable>
    {
        private ByteConverter<ArrayAccessor<object>> readConverter = null!;
        private ByteConverter<IEnumerator> writeConverter = null!;

        private static object Getter(IEnumerator parent) => parent.Current;
        private static void Setter(ArrayAccessor<object> parent, object value) => parent.Set(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<object>.GetTypeDetail();
            readConverter = ByteConverterFactory<ArrayAccessor<object>>.Get(valueTypeDetail, nameof(ByteConverterIEnumerable<TParent>), null, Setter);
            writeConverter = ByteConverterFactory<IEnumerator>.Get(valueTypeDetail, nameof(ByteConverterIEnumerable<TParent>), Getter, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IEnumerable? value)
        {
            ArrayAccessor<object> accessor;

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
                        accessor = new ArrayAccessor<object>(new object[length]);
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
                    accessor = new ArrayAccessor<object>(length);
                }
            }
            else
            {
                accessor = (ArrayAccessor<object>)state.Current.Object!;
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

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in IEnumerable value)
        {
            IEnumerator enumerator;

            if (state.Current.Object is null)
            {
                var count = 0;
                foreach (var item in value)
                    count++;

                if (!writer.TryWrite(count, out state.BytesNeeded))
                {
                    return false;
                }
                if (count == 0)
                {
                    return true;
                }

                enumerator = value.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator)state.Current.Object!;
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
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Enumerables
{
    internal sealed class ByteConverterIEnumerable : ByteConverter<IEnumerable>
    {
        private ByteConverter converter = null!;

        private static object Getter(object parent) => ((IEnumerator)parent).Current;
        private static void Setter(object parent, object value) => ((ArrayAccessor<object>)parent).Set(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<object>.GetTypeDetail();
            converter = ByteConverterFactory.Get(valueTypeDetail, nameof(ByteConverterIEnumerable), Getter, Setter);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IEnumerable? value)
        {
            ArrayAccessor<object> accessor;

            if (state.Current.Object is null)
            {
                if (!reader.TryRead(out int length, out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
                    if (TypeDetail.Type.IsInterface)
                    {
                        accessor = new ArrayAccessor<object>(new object[length]);
                        value = accessor.Array;
                    }
                    else
                    {
                        throw new InvalidOperationException($"{nameof(ByteSerializer)} cannot deserialize {TypeDetail.Type.Name}");
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
                if (!converter.TryReadFromParent(ref reader, ref state, accessor))
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
                if (!converter.TryWriteFromParent(ref writer, ref state, enumerator))
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
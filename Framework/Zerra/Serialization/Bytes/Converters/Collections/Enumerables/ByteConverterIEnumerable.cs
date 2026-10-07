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
        private ByteConverter<object> converter = null!;

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<object>.GetTypeDetail();
            converter = (ByteConverter<object>)ByteConverterFactory.Get(valueTypeDetail, nameof(ByteConverterIEnumerable), null, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IEnumerable? value)
        {
            object[]? array;
            int length;

            if (!state.Current.HasCreated)
            {
                if (!reader.TryRead(out length, out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
                    if (TypeDetail.Type.IsInterface)
                    {
                        array = new object[length];
                        value = array;
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
                    array = null;
                }
            }
            else
            {
                array = (object[]?)state.Current.Object;
                length = state.Current.EnumerableLength!.Value;
                value = array;
            }

            var index = state.Current.EnumeratorIndex;
            for (; index < length; index++)
            {
                if (!converter.TryReadToValue(ref reader, ref state, out var item))
                {
                    state.Current.HasCreated = true;
                    state.Current.Object = array;
                    state.Current.EnumerableLength = length;
                    state.Current.EnumeratorIndex = index;
                    return false;
                }
                if (array is not null)
                    array[index] = item!;
            }
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in IEnumerable value)
        {
            IEnumerator enumerator;

            if (state.Current.Object is null)
            {
                var count = 0;
                foreach (var item in value)
                    count++;

                if (!writer.TryWrite(count, out state.SizeNeeded))
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
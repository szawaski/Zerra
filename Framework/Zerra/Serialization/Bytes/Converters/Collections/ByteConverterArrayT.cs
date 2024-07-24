// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections
{
    internal sealed class ByteConverterArrayT<TParent, TValue> : ByteConverter<TParent, TValue[]>
    {
        private ByteConverter<ArrayAccessor<TValue>> converter = null!;

        private static TValue Getter(ArrayAccessor<TValue> parent) => parent.Get();
        private static void Setter(ArrayAccessor<TValue> parent, TValue value) => parent.Set(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            converter = ByteConverterFactory<ArrayAccessor<TValue>>.Get(valueTypeDetail, nameof(ByteConverterArrayT<TParent, TValue>), Getter, Setter);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TValue[]? value)
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
                    accessor = new ArrayAccessor<TValue>(new TValue[length]);
                    value = accessor.Array;
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

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TValue[] value)
        {
            ArrayAccessor<TValue> accessor;

            if (state.Current.Object is null)
            {
                if (!writer.TryWrite(value.Length, out state.BytesNeeded))
                {
                    return false;
                }
                if (value.Length == 0)
                {
                    return true;
                }

                accessor = new ArrayAccessor<TValue>(value);
            }
            else
            {
                accessor = (ArrayAccessor<TValue>)state.Current.Object!;
            }


            while (accessor.Index < accessor.Array!.Length)
            {
                if (!converter.TryWriteFromParent(ref writer, ref state, accessor, true))
                {
                    state.Current.Object = accessor;
                    return false;
                }
                accessor.Index++;
            }

            return true;
        }
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterList<TParent, TValue> : ByteConverter<TParent, List<TValue?>>
    {
        private static readonly ConstructorDetail<List<TValue?>> constructor = TypeAnalyzer<List<TValue?>>.GetTypeDetail().GetConstructor(new Type[] { typeof(int) });

        private Func<int, List<TValue?>> creator = null!;
        private ByteConverter<List<TValue?>> readConverter = null!;
        private ByteConverter<IEnumerator<TValue?>> writeConverter = null!;

        private bool valueIsNullable;

        public override void Setup()
        {
            var parent = TypeAnalyzer<List<TValue?>>.GetTypeDetail();

            var args = new object[1];
            creator = (length) =>
            {
                args[0] = length;
                return constructor.Creator(args);
            };

            Action<List<TValue?>, TValue?> setter = (parent, value) => parent.Add(value);
            var readConverterRoot = ByteConverterFactory<List<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, null, null, setter);
            readConverter = ByteConverterFactory<List<TValue?>>.GetMayNeedTypeInfo(options, typeDetail.IEnumerableGenericInnerTypeDetail, readConverterRoot);

            Func<IEnumerator<TValue?>, TValue?> getter = (parent) => parent.Current;
            var writeConverterRoot = ByteConverterFactory<IEnumerator<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, null, getter, null);
            writeConverter = ByteConverterFactory<IEnumerator<TValue?>>.GetMayNeedTypeInfo(options, typeDetail.IEnumerableGenericInnerTypeDetail, writeConverterRoot);

            valueIsNullable = !typeDetail.Type.IsValueType || typeDetail.InnerTypeDetails[0].IsNullable;
        }

        protected override bool Read(ref ByteReader reader, ref ReadState state, out List<TValue?>? value)
        {
            int sizeNeeded;
            if (state.CurrentFrame.NullFlags && !state.CurrentFrame.HasNullChecked)
            {
                if (!reader.TryReadIsNull(out var isNull, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    value = default;
                    return false;
                }

                if (isNull)
                {
                    value = default;
                    return true;
                }

                state.CurrentFrame.HasNullChecked = true;
            }

            int length;
            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                if (!reader.TryReadInt32(out length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    value = default;
                    return false;
                }

                state.CurrentFrame.EnumerableLength = length;

                if (!state.CurrentFrame.DrainBytes)
                {
                    value = creator(length);
                    state.CurrentFrame.ResultObject = value;
                }
                else
                {
                    value = default;
                }

                if (length == 0)
                {
                    return true;
                }
            }
            else
            {
                value = (List<TValue?>?)state.CurrentFrame.ResultObject;
            }

            length = state.CurrentFrame.EnumerableLength.Value;
            if (value.Count == length)
                return true;

            for (; ; )
            {
                state.PushFrame(readConverter, valueIsNullable, value);
                var read = readConverter.Read(ref reader, ref state, value);
                if (!read)
                    return false;

                if (value.Count == length)
                    return true;
            }
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, List<TValue?>? value)
        {
            int sizeNeeded;
            if (state.CurrentFrame.NullFlags && !state.CurrentFrame.HasWrittenIsNull)
            {
                if (value == null)
                {
                    if (!writer.TryWriteNull(out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return false;
                    }
                    return true;
                }
                if (!writer.TryWriteNotNull(out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return false;
                }
                state.CurrentFrame.HasWrittenIsNull = true;
            }

            if (value == null)
                throw new InvalidOperationException("Bad State");

            IEnumerator<TValue?> enumerator;

            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                var length = value.Count;

                if (!writer.TryWrite(length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return false;
                }
                if (length == 0)
                {
                    return true;
                }
                state.CurrentFrame.Object = length;

                enumerator = value.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator<TValue?>)state.CurrentFrame.Object!;
            }

            while (state.CurrentFrame.EnumeratorInProgress || enumerator.MoveNext())
            {
                state.CurrentFrame.EnumeratorInProgress = true;

                state.PushFrame(writeConverter, valueIsNullable, value);
                var write = writeConverter.Write(ref writer, ref state, enumerator);
                if (!write)
                    return false;

                state.CurrentFrame.EnumeratorInProgress = false;
            }

            return true;
        }
    }
}
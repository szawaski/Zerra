// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
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
            var readConverterRoot = ByteConverterFactory<List<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, parent, null, setter);
            readConverter = ByteConverterFactory<List<TValue?>>.GetMayNeedTypeInfo(options, typeDetail.IEnumerableGenericInnerTypeDetail, readConverterRoot);

            Func<IEnumerator<TValue?>, TValue?> getter = (parent) => parent.Current;
            var writeConverterRoot = ByteConverterFactory<IEnumerator<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, parent, getter, null);
            writeConverter = ByteConverterFactory<IEnumerator<TValue?>>.GetMayNeedTypeInfo(options, typeDetail.IEnumerableGenericInnerTypeDetail, writeConverterRoot);

            valueIsNullable = !typeDetail.Type.IsValueType || typeDetail.InnerTypeDetails[0].IsNullable;
        }

        protected override bool Read(ref ByteReader reader, ref ReadState state, out List<TValue?>? value)
        {
            int length;
            int sizeNeeded;
            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                if (!reader.TryReadInt32(out length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    value = default;
                    return false;
                }

                if (length == 0)
                {
                    value = (List<TValue?>?)state.CurrentFrame.ResultObject;
                    return true;
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
            }
            else
            {
                value = (List<TValue?>?)state.CurrentFrame.ResultObject;
            }

            length = state.CurrentFrame.EnumerableLength.Value;

            for (; ; )
            {
                state.PushFrame(readConverter, valueIsNullable);
                readConverter.Read(ref reader, ref state, value);
                if (state.BytesNeeded > 0)
                    return false;

                state.CurrentFrame.EnumerablePosition++;

                if (state.CurrentFrame.EnumerablePosition == length)
                    return true;
            }
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, List<TValue?>? value)
        {
            if (value == null)
                throw new NotSupportedException();

            IEnumerator<TValue?> enumerator;

            int sizeNeeded;

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
                writeConverter.Write(ref writer, ref state, enumerator);
                if (state.BytesNeeded > 0)
                    return false;

                state.CurrentFrame.EnumeratorInProgress = false;
            }

            return true;
        }
    }
}
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
    internal sealed class ByteConverterHashSet<TParent, TValue> : ByteConverter<TParent, HashSet<TValue?>>
    {
        private static readonly ConstructorDetail<HashSet<TValue?>> constructor = TypeAnalyzer<HashSet<TValue?>>.GetTypeDetail().GetConstructor(new Type[] { typeof(int) });

        private Func<int, HashSet<TValue?>> creator = null!;
        private ByteConverter<HashSet<TValue?>> readConverter = null!;
        private ByteConverter<IEnumerator<TValue?>> writeConverter = null!;

        private bool valueIsNullable;

        public override void Setup()
        {
            var parent = TypeAnalyzer<HashSet<TValue?>>.GetTypeDetail();

            var args = new object[1];
            creator = (length) =>
            {
                args[0] = length;
                return constructor.Creator(args);
            };

            Action<HashSet<TValue?>, TValue?> setter = (parent, value) => parent.Add(value);
            var readConverterRoot = ByteConverterFactory<HashSet<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, parent, null, setter);
            readConverter = ByteConverterFactory<HashSet<TValue?>>.GetMayNeedTypeInfo(options, typeDetail.IEnumerableGenericInnerTypeDetail, readConverterRoot);

            Func<IEnumerator<TValue?>, TValue?> getter = (parent) => parent.Current;
            var writeConverterRoot = ByteConverterFactory<IEnumerator<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, parent, getter, null);
            writeConverter = ByteConverterFactory<IEnumerator<TValue?>>.GetMayNeedTypeInfo(options, typeDetail.IEnumerableGenericInnerTypeDetail, writeConverterRoot);

            valueIsNullable = !typeDetail.Type.IsValueType || typeDetail.InnerTypeDetails[0].IsNullable;
        }

        protected override bool Read(ref ByteReader reader, ref ReadState state, out HashSet<TValue?>? obj)
        {
            int length;
            int sizeNeeded;
            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                if (!reader.TryReadInt32(out length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    obj = default;
                    return false;
                }

                if (length == 0)
                {
                    obj = (HashSet<TValue?>?)state.CurrentFrame.ResultObject;
                    return true;
                }

                state.CurrentFrame.EnumerableLength = length;

                if (!state.CurrentFrame.DrainBytes)
                {
                    obj = creator(length);
                    state.CurrentFrame.ResultObject = obj;
                }
                else
                {
                    obj = default;
                }
            }
            else
            {
                obj = (HashSet<TValue?>?)state.CurrentFrame.ResultObject;
            }

            length = state.CurrentFrame.EnumerableLength.Value;

            for (; ; )
            {
                state.PushFrame(readConverter, valueIsNullable);
                readConverter.Read(ref reader, ref state, obj);
                if (state.BytesNeeded > 0)
                    return false;

                state.CurrentFrame.EnumerablePosition++;

                if (state.CurrentFrame.EnumerablePosition == length)
                    return true;
            }
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, HashSet<TValue?>? obj)
        {
            if (obj == null)
                throw new NotSupportedException();

            int sizeNeeded;

            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                var length = obj.Count;

                if (!writer.TryWrite(length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return false;
                }
                state.CurrentFrame.EnumerableLength = length;
            }

            if (state.CurrentFrame.EnumerableLength > 0)
            {
                state.CurrentFrame.Enumerator ??= obj.GetEnumerator();

                while (state.CurrentFrame.EnumeratorInProgress || state.CurrentFrame.Enumerator!.MoveNext())
                {
                    state.CurrentFrame.EnumeratorInProgress = true;

                    state.PushFrame(writeConverter, valueIsNullable, obj);
                    readConverter.Write(ref writer, ref state, obj);
                    if (state.BytesNeeded > 0)
                        return false;

                    state.CurrentFrame.EnumeratorInProgress = false;
                }
            }

            return true;
        }
    }
}
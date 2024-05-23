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
        }

        protected override bool Read(ref ByteReader reader, ref ReadState state, out List<TValue?>? obj)
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

                if (length == 0)
                {
                    obj = (List<TValue?>?)state.CurrentFrame.ResultObject;
                    return true;
                }
            }
            else
            {
                obj = (List<TValue?>?)state.CurrentFrame.ResultObject;
            }

            length = state.CurrentFrame.EnumerableLength.Value;

            for (; ; )
            {
                if (!state.CurrentFrame.HasNullChecked)
                {
                    if (!reader.TryReadIsNull(out var isNull, out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        obj = default;
                        return false;
                    }

                    if (isNull)
                    {
                        obj!.Add(default);
                        state.CurrentFrame.EnumerablePosition++;
                        if (state.CurrentFrame.EnumerablePosition == length)
                        {
                            obj = default;
                            return true;
                        }
                        continue;
                    }
                    state.CurrentFrame.HasNullChecked = true;
                }

                if (!state.CurrentFrame.HasObjectStarted)
                {
                    state.PushFrame(readConverter, false);
                    readConverter.Read(ref reader, ref state, obj);
                    if (state.BytesNeeded > 0)
                        return false;
                }

                state.CurrentFrame.HasObjectStarted = false;
                state.CurrentFrame.HasNullChecked = false;
                state.CurrentFrame.EnumerablePosition++;

                if (state.CurrentFrame.EnumerablePosition == length)
                {
                    return true;
                }
            }
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, List<TValue?>? obj)
        {
            throw new NotImplementedException();
        }
    }
}
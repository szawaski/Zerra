// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
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
                    obj = (HashSet<TValue?>?)state.CurrentFrame.ResultObject;
                    return true;
                }
            }
            else
            {
                obj = (HashSet<TValue?>?)state.CurrentFrame.ResultObject;
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

        protected override bool Write(ref ByteWriter writer, ref WriteState state, HashSet<TValue?>? obj)
        {
            throw new NotImplementedException();
        }
    }
}
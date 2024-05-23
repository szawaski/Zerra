// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterEnumerable<TParent, TValue> : ByteConverter<TParent, TValue>
    {
        private static readonly ConstructorDetail<List<TValue?>> listTypeConstructor = TypeAnalyzer<List<TValue?>>.GetTypeDetail().GetConstructor(new Type[] { typeof(int) });
        private static readonly ConstructorDetail<HashSet<TValue?>> hashSetTypeConstructor = TypeAnalyzer<HashSet<TValue?>>.GetTypeDetail().GetConstructor(new Type[] { typeof(int) });

        private bool asCollection;
        private Func<int, ICollection<TValue?>> collectionCreator = null!;
        private ByteConverter converter = null!;


        public override void Setup()
        {
            var asList = !typeDetail.Type.IsArray && typeDetail.IsIList;
            var asSet = !typeDetail.Type.IsArray && typeDetail.IsISet;

            asCollection = asList || asSet;
            if (asList)
            {
                Func<List<TValue?>, TValue?> getter;
                Action<List<TValue?>, TValue?> setter = (parent, value) => parent.Add(value);

                converter = ByteConverterFactory<List<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, getter, setter);
                collectionCreator = (length) => listTypeConstructor.Creator(new object[] { length });
            }
            else if (asSet)
            {
                Func<HashSet<TValue?>, TValue?> getter;
                Action<HashSet<TValue?>, TValue?> setter = (parent, value) => parent.Add(value);

                converter = ByteConverterFactory<HashSet<TValue?>>.Get(options, typeDetail.IEnumerableGenericInnerTypeDetail, getter, setter);
                collectionCreator = (length) => hashSetTypeConstructor.Creator(new object[] { length });
            }
            else
            {

            }
        }

        protected override bool Read(ref ByteReader reader, ref ReadState state, out TValue? obj)
        {
            ICollection<TValue?>? collection = null;
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
                    if (asCollection)
                    {
                        collection = collectionCreator(length);
                        state.CurrentFrame.ResultObject = collection;
                    }
                    else
                    {
                        collection = null;
                        state.CurrentFrame.EnumerableArray = Array.CreateInstance(typeDetail.Type, length);
                        state.CurrentFrame.ResultObject = state.CurrentFrame.EnumerableArray;
                    }
                }

                if (length == 0)
                {
                    obj = default;
                    return true;
                }
            }
            else
            {
                if (asCollection)
                {
                    collection = (ICollection<TValue?>?)state.CurrentFrame.ResultObject;
                }
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
                        if (asCollection)
                        {
                            collection!.Add(default);
                        }
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
                    state.CurrentFrame.HasObjectStarted = true;
                    var frame = ReadFrameFromType(ref state, typeDetail, false, false);
                    state.PushFrame(frame);
                    return;
                }

                if (!state.CurrentFrame.DrainBytes)
                {
                    if (asList)
                    {
                        state.CurrentFrame.AddMethodArgs![0] = state.LastFrameResultObject;
                        _ = state.CurrentFrame.AddMethod!.Caller(state.CurrentFrame.ResultObject, state.CurrentFrame.AddMethodArgs);
                    }
                    else if (asSet)
                    {
                        state.CurrentFrame.AddMethodArgs![0] = state.LastFrameResultObject;
                        _ = state.CurrentFrame.AddMethod!.Caller(state.CurrentFrame.ResultObject, state.CurrentFrame.AddMethodArgs);
                    }
                    else
                    {
                        state.CurrentFrame.EnumerableArray!.SetValue(state.LastFrameResultObject, state.CurrentFrame.EnumerablePosition);
                    }
                }
                state.CurrentFrame.HasObjectStarted = false;
                state.CurrentFrame.HasNullChecked = false;
                state.CurrentFrame.EnumerablePosition++;

                if (state.CurrentFrame.EnumerablePosition == length)
                {
                    state.EndFrame();
                    return;
                }
            }
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, TValue? obj)
        {
            throw new NotImplementedException();
        }
    }
}
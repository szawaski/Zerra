// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterDictionary<TParent, TDictionary, TKey, TValue> : ByteConverter<TParent, TDictionary>
    {
        private static readonly Type dictionaryType = typeof(Dictionary<,>);

        private ByteConverter<IDictionary<TKey, TValue?>> readConverter = null!;
        private ByteConverter<IEnumerator<KeyValuePair<TKey, TValue?>>> writeConverter = null!;

        private bool valueIsNullable;

        public override void Setup()
        {
            var enumerableType = TypeAnalyzer<KeyValuePair<TKey, TValue?>>.GetTypeDetail();

            var readParent = TypeAnalyzer<IDictionary<TKey, TValue?>>.GetTypeDetail();
            Action<IDictionary<TKey, TValue?>, KeyValuePair<TKey, TValue?>> setter = (parent, value) => parent.Add(value.Key, value.Value);
            readConverter = ByteConverterFactory<IDictionary<TKey, TValue?>>.Get(options, enumerableType, readParent, null, setter);

            var writeParent = TypeAnalyzer<IEnumerator<KeyValuePair<TKey, TValue?>>>.GetTypeDetail();
            Func<IEnumerator<KeyValuePair<TKey, TValue?>>, KeyValuePair<TKey, TValue?>> getter = (parent) => parent.Current;
            writeConverter = ByteConverterFactory<IEnumerator<KeyValuePair<TKey, TValue?>>>.Get(options, enumerableType, writeParent, getter, null);

            var valueTypeDetail = typeDetail.InnerTypeDetails[0].InnerTypeDetails[1];
            valueIsNullable = !valueTypeDetail.Type.IsValueType || valueTypeDetail.InnerTypeDetails[0].IsNullable;
        }

        protected override bool Read(ref ByteReader reader, ref ReadState state, out TDictionary? value)
        {
            IDictionary<TKey, TValue?>? dictionary;

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
                    value = (TDictionary?)state.CurrentFrame.ResultObject;
                    return true;
                }

                state.CurrentFrame.EnumerableLength = length;

                if (!state.CurrentFrame.DrainBytes)
                {
                    if (typeDetail.Type.IsInterface)
                    {
                        var dictionaryGenericType = TypeAnalyzer.GetGenericTypeDetail(dictionaryType, (Type[])typeDetail.IEnumerableGenericInnerTypeDetail.InnerTypes);
                        var obj = dictionaryGenericType.CreatorBoxed();
                        value = (TDictionary?)obj;
                        dictionary = (IDictionary<TKey, TValue?>?)obj;
                        state.CurrentFrame.ResultObject = obj;
                    }
                    else
                    {
                        var obj = typeDetail.Creator();
                        value = obj;
                        dictionary = (IDictionary<TKey, TValue?>?)obj;
                        state.CurrentFrame.ResultObject = obj;
                    }
                }
                else
                {
                    value = default;
                    dictionary = null;
                }
            }
            else
            {
                value = (TDictionary?)state.CurrentFrame.ResultObject;
                dictionary = (IDictionary<TKey, TValue?>?)state.CurrentFrame.ResultObject;
            }

            length = state.CurrentFrame.EnumerableLength.Value;

            for (; ; )
            {
                state.PushFrame(readConverter, valueIsNullable);
                readConverter.Read(ref reader, ref state, dictionary);
                if (state.BytesNeeded > 0)
                    return false;

                state.CurrentFrame.EnumerablePosition++;

                if (state.CurrentFrame.EnumerablePosition == length)
                    return true;
            }
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, TDictionary? value)
        {
            if (value == null)
                throw new NotSupportedException();

            IEnumerator<KeyValuePair<TKey, TValue?>> enumerator;

            int sizeNeeded;

            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                var collection = (ICollection<KeyValuePair<TKey, TValue?>>)value;

                var length = collection.Count;

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

                enumerator = collection.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator<KeyValuePair<TKey, TValue?>>)state.CurrentFrame.Object!;
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
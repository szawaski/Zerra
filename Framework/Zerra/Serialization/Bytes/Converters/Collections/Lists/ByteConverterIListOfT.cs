// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Lists
{
    internal sealed class ByteConverterIListOfT<TParent, TList> : ByteConverter<TParent, TList>
    {
        private ByteConverter<IList> readConverter = null!;
        private ByteConverter<IEnumerator> writeConverter = null!;

        private static object Getter(IEnumerator parent) => parent.Current;
        private static void Setter(IList parent, object value) => parent.Add(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<object>.GetTypeDetail();
            readConverter = ByteConverterFactory<IList>.Get(valueTypeDetail, nameof(ByteConverterIListOfT<TParent, TList>), null, Setter);
            writeConverter = ByteConverterFactory<IEnumerator>.Get(valueTypeDetail, nameof(ByteConverterIListOfT<TParent, TList>), Getter, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TList? value)
        {
            IList list;

            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryRead(out state.Current.EnumerableLength, out state.BytesNeeded))
                {
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
                    value = typeDetail.Creator();
                    list = (IList)value!;
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                }
                else
                {
                    value = default;
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                    list = new IListCounter();
                }
            }
            else
            {
                list = (IList)state.Current.Object!;
                if (!state.Current.DrainBytes)
                    value = (TList?)state.Current.Object;
                else
                    value = default;
            }

            if (list.Count == state.Current.EnumerableLength.Value)
                return true;

            for (; ; )
            {
                if (!readConverter.TryReadFromParent(ref reader, ref state, list, true))
                {
                    state.Current.Object = list;
                    return false;
                }

                if (list.Count == state.Current.EnumerableLength!.Value)
                    return true;
            }
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TList value)
        {
            IEnumerator enumerator;

            if (state.Current.Object is null)
            {
                var collection = (ICollection)value!;

                if (!writer.TryWrite(collection.Count, out state.BytesNeeded))
                {
                    return false;
                }
                if (collection.Count == 0)
                {
                    return true;
                }

                enumerator = collection.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator)state.Current.Object!;
            }

            while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            {
                if (!writeConverter.TryWriteFromParent(ref writer, ref state, enumerator, true))
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
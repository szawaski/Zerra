// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Lists
{
    internal sealed class ByteConverterIListOfT<TList> : ByteConverter<TList>
    {
        private ByteConverter converter = null!;

        private static object Getter(object parent) => ((IEnumerator)parent).Current;
        private static void Setter(object parent, object value) => ((IList)parent).Add(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<object>.GetTypeDetail();
            converter = ByteConverterFactory.Get(valueTypeDetail, nameof(ByteConverterIListOfT<TList>), Getter, Setter);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TList? value)
        {
            IList list;

            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryRead(out state.Current.EnumerableLength, out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
                    if (!TypeDetail.HasCreator)
                        throw new InvalidOperationException($"{TypeDetail.Type} does not have a parameterless constructor.");
                    value = TypeDetail.Creator!();
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
                if (!converter.TryReadFromParent(ref reader, ref state, list))
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
                if (!converter.TryWriteFromParent(ref writer, ref state, enumerator))
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
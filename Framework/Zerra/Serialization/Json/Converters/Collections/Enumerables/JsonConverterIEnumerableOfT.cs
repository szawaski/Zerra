// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Enumerables
{
    internal sealed class JsonConverterIEnumerableOfT<TParent, TEnumerable> : JsonConverter<TParent, TEnumerable>
    {
        private JsonConverter<ArrayOrListAccessor<object>> readConverter = null!;
        private JsonConverter<IEnumerator> writeConverter = null!;

        private static object Getter(IEnumerator parent) => parent.Current;
        private static void Setter(ArrayOrListAccessor<object> parent, object value) => parent.Add(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<object>.GetTypeDetail();
            readConverter = JsonConverterFactory<ArrayOrListAccessor<object>>.Get(valueTypeDetail, nameof(JsonConverterIEnumerableOfT<TParent, TEnumerable>), null, Setter);
            writeConverter = JsonConverterFactory<IEnumerator>.Get(valueTypeDetail, nameof(JsonConverterIEnumerableOfT<TParent, TEnumerable>), Getter, null);
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out TEnumerable? value)
            => throw new NotSupportedException($"Cannot deserialize {typeDetail.Type.GetNiceName()} because no interface to populate the collection");

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, TEnumerable value)
        {
            IEnumerator enumerator;

            if (!state.Current.HasWrittenStart)
            {
                var enumerable = (IEnumerable)value!;
                //var count = 0;
                //foreach (var item in enumerable)
                //    count++;
                //if (count == 0)
                //{
                //    if (!writer.TryWriteEmptyBracket(out state.CharsNeeded))
                //    {
                //        return false;
                //    }
                //    return true;
                //}

                if (!writer.TryWriteOpenBracket(out state.CharsNeeded))
                {
                    return false;
                }
                enumerator = enumerable.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator)state.Current.Object!;
            }

            while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            {
                if (state.Current.HasWrittenFirst && !state.Current.HasWrittenSeperator)
                {
                    if (!writer.TryWriteComma(out state.CharsNeeded))
                    {
                        state.Current.HasWrittenStart = true;
                        state.Current.EnumeratorInProgress = true;
                        state.Current.Object = enumerator;
                        return false;
                    }
                }

                if (!writeConverter.TryWriteFromParent(ref writer, ref state, enumerator))
                {
                    state.Current.HasWrittenStart = true;
                    state.Current.HasWrittenSeperator = true;
                    state.Current.EnumeratorInProgress = true;
                    state.Current.Object = enumerator;
                    return false;
                }

                if (!state.Current.HasWrittenFirst)
                    state.Current.HasWrittenFirst = true;
                if (state.Current.HasWrittenSeperator)
                    state.Current.HasWrittenSeperator = false;
                if (state.Current.EnumeratorInProgress)
                    state.Current.EnumeratorInProgress = false;
            }

            if (!writer.TryWriteCloseBracket(out state.CharsNeeded))
            {
                state.Current.HasWrittenStart = true;
                state.Current.Object = enumerator;
                return false;
            }
            return true;
        }
    }
}
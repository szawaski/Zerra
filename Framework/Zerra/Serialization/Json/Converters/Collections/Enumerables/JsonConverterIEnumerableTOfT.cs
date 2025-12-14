// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Enumerables
{
    internal sealed class JsonConverterIEnumerableTOfT<TEnumerable, TValue> : JsonConverter<TEnumerable>
    {
        private JsonConverter converter = null!;

        private static TValue Getter(object parent) => ((IEnumerator<TValue>)parent).Current;
        private static void Setter(object parent, TValue value) => ((ArrayOrListAccessor<TValue>)parent).Add(value);

        protected override sealed void Setup()
        {
            var valueTypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            converter = JsonConverterFactory.Get(valueTypeDetail, nameof(JsonConverterIEnumerableTOfT<TEnumerable, TValue>), Getter, Setter);
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out TEnumerable? value)
            => throw new NotSupportedException($"Cannot deserialize {TypeDetail.Type.Name} because no interface to populate the collection");

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in TEnumerable value)
        {
            IEnumerator<TValue> enumerator;

            if (!state.Current.HasWrittenStart)
            {
                var enumerable = (IEnumerable<TValue>)value!;
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

                if (!writer.TryWriteOpenBracket(out state.SizeNeeded))
                {
                    return false;
                }
                enumerator = enumerable.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator<TValue>)state.Current.Object!;
            }

            while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            {
                if (state.Current.HasWrittenFirst && !state.Current.HasWrittenSeperator)
                {
                    if (!writer.TryWriteComma(out state.SizeNeeded))
                    {
                        state.Current.HasWrittenStart = true;
                        state.Current.EnumeratorInProgress = true;
                        state.Current.Object = enumerator;
                        return false;
                    }
                }

                if (!converter.TryWriteFromParent(ref writer, ref state, enumerator))
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

            if (!writer.TryWriteCloseBracket(out state.SizeNeeded))
            {
                state.Current.HasWrittenStart = true;
                state.Current.Object = enumerator;
                return false;
            }
            return true;
        }
    }
}
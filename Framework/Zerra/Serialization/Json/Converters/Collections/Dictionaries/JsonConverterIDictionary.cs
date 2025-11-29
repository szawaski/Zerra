// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.Collections.Dictionaries
{
    internal sealed class JsonConverterIDictionary : JsonConverter<IDictionary>
    {
        //private JsonConverter readConverter = null!;
        //private JsonConverter valueConverter = null!;

        //private JsonConverter converter = null!;

        //private static object? ValueGetter(IDictionaryEnumerator parent) => parent.Entry.Value;
        //private static void KeySetter(DictionaryAccessor<object, object> parent, object value) => parent.SetKey(value);
        //private static void ValueSetter(DictionaryAccessor<object, object> parent, object value) => parent.Add(value);

        //private static DictionaryEntry Getter(IDictionaryEnumerator parent) => parent.Entry;
        //private static void Setter(IDictionary parent, DictionaryEntry value) => parent.Add(value.Key, value.Value);

        //private bool canWriteAsProperties;

        protected override sealed void Setup()
        {
            //var keyDetail = TypeAnalyzer<object>.GetTypeDetail();
            //var valueDetail = TypeAnalyzer<object>.GetTypeDetail();

            //canWriteAsProperties = keyDetail.CoreType.HasValue;

            //if (canWriteAsProperties)
            //{
            //    readConverter = JsonConverterFactory.Create(keyDetail, $"{nameof(JsonConverterIDictionary)}_Key", null, KeySetter);
            //    valueConverter = JsonConverterFactory.Create(valueDetail, $"{nameof(JsonConverterIDictionary)}_Value", ValueGetter, ValueSetter);
            //}
            //else
            //{
            //    converter = JsonConverterFactory.Create(keyDetail, nameof(JsonConverterIDictionary), Getter, Setter);
            //}
        }

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out IDictionary? value)
        {
            throw new NotSupportedException($"{nameof(JsonConverter)} does not support IDictionary because it cannot determine the types to deserialize");
           
        }

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in IDictionary value)
        {
            throw new NotSupportedException($"{nameof(JsonConverter)} does not support IDictionary because it cannot determine the types to deserialize");
            //if (canWriteAsProperties)
            //{
            //    IDictionaryEnumerator enumerator;
            //    if (!state.Current.HasWrittenStart)
            //    {
            //        if (value.Count == 0)
            //        {
            //            if (!writer.TryWriteEmptyBrace(out state.CharsNeeded))
            //            {
            //                return false;
            //            }
            //            return true;
            //        }

            //        if (!writer.TryWriteOpenBrace(out state.CharsNeeded))
            //        {
            //            return false;
            //        }
            //        enumerator = value.GetEnumerator();
            //    }
            //    else
            //    {
            //        enumerator = (IDictionaryEnumerator)state.Current.Enumerator!;
            //    }

            //    while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            //    {
            //        if (!writeValueConverter.TryWriteFromParent(ref writer, ref state, enumerator, enumerator.Entry.Key.ToString(), true))
            //        {
            //            state.Current.HasWrittenStart = true;
            //            state.Current.Enumerator = enumerator;
            //            state.Current.EnumeratorInProgress = true;
            //            return false;
            //        }

            //        if (state.Current.EnumeratorInProgress)
            //            state.Current.EnumeratorInProgress = false;
            //    }

            //    if (!writer.TryWriteCloseBrace(out state.CharsNeeded))
            //    {
            //        state.Current.HasWrittenStart = true;
            //        return false;
            //    }
            //    return true;
            //}
            //else
            //{
            //    IDictionaryEnumerator enumerator;
            //    if (!state.Current.HasWrittenStart)
            //    {
            //        if (value.Count == 0)
            //        {
            //            if (!writer.TryWriteEmptyBracket(out state.CharsNeeded))
            //            {
            //                return false;
            //            }
            //            return true;
            //        }

            //        if (!writer.TryWriteOpenBracket(out state.CharsNeeded))
            //        {
            //            return false;
            //        }
            //        enumerator = value.GetEnumerator();
            //    }
            //    else
            //    {
            //        enumerator = (IDictionaryEnumerator)state.Current.Enumerator!;
            //    }

            //    while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            //    {
            //        if (state.Current.HasWrittenFirst && !state.Current.HasWrittenSeperator)
            //        {
            //            if (!writer.TryWriteComma(out state.CharsNeeded))
            //            {
            //                state.Current.HasWrittenStart = true;
            //                state.Current.Enumerator = enumerator;
            //                state.Current.EnumeratorInProgress = true;
            //                return false;
            //            }
            //        }

            //        if (!writeConverter.TryWriteFromParent(ref writer, ref state, enumerator))
            //        {
            //            state.Current.HasWrittenStart = true;
            //            state.Current.Enumerator = enumerator;
            //            state.Current.EnumeratorInProgress = true;
            //            state.Current.HasWrittenSeperator = true;
            //            state.Current.HasWrittenPropertyName = true;
            //            return false;
            //        }

            //        if (!state.Current.HasWrittenFirst)
            //            state.Current.HasWrittenFirst = true;
            //        if (state.Current.HasWrittenSeperator)
            //            state.Current.HasWrittenSeperator = false;
            //        if (state.Current.HasWrittenPropertyName)
            //            state.Current.HasWrittenPropertyName = false;
            //        if (state.Current.EnumeratorInProgress)
            //            state.Current.EnumeratorInProgress = false;
            //    }

            //    if (!writer.TryWriteCloseBracket(out state.CharsNeeded))
            //    {
            //        state.Current.HasWrittenStart = true;
            //        return false;
            //    }
            //    return true;
            //}
        }
    }
}
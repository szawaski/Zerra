// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;
using System.Runtime.CompilerServices;
using Zerra.Serialization.Json.Converters.Collections;
using System.Buffers.Text;

namespace Zerra.Serialization.Json.Converters
{
    public abstract partial class JsonConverter
    {
        /// <summary>
        /// Attempts to read a <see cref="JsonObject"/> from the JSON reader.
        /// </summary>
        /// <param name="reader">The JSON reader to read from.</param>
        /// <param name="state">The current read state.</param>
        /// <param name="value">The deserialized <see cref="JsonObject"/> if successful; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the read operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadJsonObject(ref JsonReader reader, ref ReadState state, out JsonObject? value)
        {
            if (state.EntryToken == JsonToken.NotDetermined)
            {
                if (!reader.TryReadToken(out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }
                state.EntryToken = reader.Token;
            }

            switch (state.EntryToken)
            {
                case JsonToken.ObjectStart:
                    state.PushFrame(null);
                    if (!ReadJsonObjectObject(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        value = default;
                        return false;
                    }
                    state.EndFrame();
                    return true;
                case JsonToken.ArrayStart:
                    state.PushFrame(null);
                    if (!ReadJsonObjectArray(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        value = default;
                        return false;
                    }
                    state.EndFrame();
                    return true;
                case JsonToken.String:
                    if (reader.UseBytes)
                        value = new JsonObject(reader.UnescapeStringBytes());
                    else
                        value = new JsonObject(reader.PositionOfFirstEscape == -1 ? reader.ValueChars.ToString() : reader.UnescapeStringChars());
                    return true;
                case JsonToken.Null:
                    value = new JsonObject();
                    return true;
                case JsonToken.False:
                    value = new JsonObject(false);
                    return true;
                case JsonToken.True:
                    value = new JsonObject(true);
                    return true;
                case JsonToken.Number:
                    decimal number;
                    if (reader.UseBytes)
                    {
                        if ((!Utf8Parser.TryParse(reader.ValueBytes, out number, out var consumed) || consumed != reader.ValueBytes.Length) && state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert number (disable {nameof(ReadState.ErrorOnTypeMismatch)} to prevent this exception)");
                    }
                    else
                    {
#if NETSTANDARD2_0
                        if (!Decimal.TryParse(reader.ValueChars.ToString(), out number) && state.ErrorOnTypeMismatch)
#else
                        if (!Decimal.TryParse(reader.ValueChars, out number) && state.ErrorOnTypeMismatch)
#endif
                            throw reader.CreateException($"Cannot convert number (disable {nameof(ReadState.ErrorOnTypeMismatch)} to prevent this exception)");
                    }
                    value = new JsonObject(number);
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadJsonObjectFromParent(ref JsonReader reader, ref ReadState state, in Action<JsonObject> addMethod)
        {
            if (state.Current.ChildJsonToken == JsonToken.NotDetermined)
            {
                state.Current.ChildJsonToken = reader.Token;
            }

            JsonObject? value;

            switch (state.Current.ChildJsonToken)
            {
                case JsonToken.ObjectStart:
                    state.PushFrame(null);
                    if (!ReadJsonObjectObject(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    addMethod(value!);
                    state.EndFrame();
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.ArrayStart:
                    state.PushFrame(null);
                    if (!ReadJsonObjectArray(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    addMethod(value!);
                    state.EndFrame();
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.String:
                    if (reader.UseBytes)
                        addMethod(new JsonObject(reader.UnescapeStringBytes()));
                    else
                        addMethod(new JsonObject(reader.PositionOfFirstEscape == -1 ? reader.ValueChars.ToString() : reader.UnescapeStringChars()));
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.Null:
                    addMethod(new JsonObject());
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.False:
                    addMethod(new JsonObject(false));
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.True:
                    addMethod(new JsonObject(true));
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.Number:
                    decimal number;
                    if (reader.UseBytes)
                    {
                        if ((!Utf8Parser.TryParse(reader.ValueBytes, out number, out var consumed) || consumed != reader.ValueBytes.Length) && state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert number (disable {nameof(ReadState.ErrorOnTypeMismatch)} to prevent this exception)");
                    }
                    else
                    {
#if NETSTANDARD2_0
                        if (!Decimal.TryParse(reader.ValueChars.ToString(), out number) && state.ErrorOnTypeMismatch)
#else
                        if (!Decimal.TryParse(reader.ValueChars, out number) && state.ErrorOnTypeMismatch)
#endif
                            throw reader.CreateException($"Cannot convert number (disable {nameof(ReadState.ErrorOnTypeMismatch)} to prevent this exception)");
                    }
                    addMethod(new JsonObject(number));
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadJsonObjectFromParentMember(ref JsonReader reader, ref ReadState state, in JsonObject parent, string property)
        {
            if (state.Current.ChildJsonToken == JsonToken.NotDetermined)
            {
                if (!reader.TryReadToken(out state.SizeNeeded))
                    return false;
                state.Current.ChildJsonToken = reader.Token;
            }

            JsonObject? value;

            switch (state.Current.ChildJsonToken)
            {
                case JsonToken.ObjectStart:
                    state.PushFrame(null);
                    if (!ReadJsonObjectObject(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    parent[property] = value!;
                    state.EndFrame();
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.ArrayStart:
                    state.PushFrame(null);
                    if (!ReadJsonObjectArray(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    parent[property] = value!;
                    state.EndFrame();
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.String:
                    if (reader.UseBytes)
                        parent[property] = new JsonObject(reader.UnescapeStringBytes());
                    else
                        parent[property] = new JsonObject(reader.UnescapeStringChars());
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.Null:
                    parent[property] = new JsonObject();
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.False:
                    parent[property] = new JsonObject(false);
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.True:
                    parent[property] = new JsonObject(true);
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                case JsonToken.Number:
                    decimal number;
                    if (reader.UseBytes)
                    {
                        if ((!Utf8Parser.TryParse(reader.ValueBytes, out number, out var consumed) || consumed != reader.ValueBytes.Length) && state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert number (disable {nameof(ReadState.ErrorOnTypeMismatch)} to prevent this exception)");
                    }
                    else
                    {
#if NETSTANDARD2_0
                        if (!Decimal.TryParse(reader.ValueChars.ToString(), out number) && state.ErrorOnTypeMismatch)
#else
                        if (!Decimal.TryParse(reader.ValueChars, out number) && state.ErrorOnTypeMismatch)
#endif
                            throw reader.CreateException($"Cannot convert number (disable {nameof(ReadState.ErrorOnTypeMismatch)} to prevent this exception)");
                    }
                    parent[property] = new JsonObject(number!);
                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadJsonObject(ref JsonReader reader, ref ReadState state, JsonToken token, out JsonObject? value)
        {
            switch (token)
            {
                case JsonToken.ObjectStart:
                    return ReadJsonObjectObject(ref reader, ref state, out value);
                case JsonToken.ArrayStart:
                    return ReadJsonObjectArray(ref reader, ref state, out value);
                case JsonToken.String:
                    if (reader.UseBytes)
                        value = new JsonObject(reader.UnescapeStringBytes());
                    else
                        value = new JsonObject(reader.PositionOfFirstEscape == -1 ? reader.ValueChars.ToString() : reader.UnescapeStringChars());
                    return true;
                case JsonToken.Null:
                    value = new JsonObject();
                    return true;
                case JsonToken.False:
                    value = new JsonObject(false);
                    return true;
                case JsonToken.True:
                    value = new JsonObject(true);
                    return true;
                case JsonToken.Number:
                    decimal number;
                    if (reader.UseBytes)
                    {
                        if ((!Utf8Parser.TryParse(reader.ValueBytes, out number, out var consumed) || consumed != reader.ValueBytes.Length) && state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert number (disable {nameof(ReadState.ErrorOnTypeMismatch)} to prevent this exception)");
                    }
                    else
                    {
#if NETSTANDARD2_0
                        if (!Decimal.TryParse(reader.ValueChars.ToString(), out number) && state.ErrorOnTypeMismatch)
#else
                        if (!Decimal.TryParse(reader.ValueChars, out number) && state.ErrorOnTypeMismatch)
#endif
                            throw reader.CreateException($"Cannot convert number (disable {nameof(ReadState.ErrorOnTypeMismatch)} to prevent this exception)");
                    }
                    value = new JsonObject(number);
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadJsonObjectObject(ref JsonReader reader, ref ReadState state, out JsonObject? value)
        {
            if (!state.Current.HasCreated)
            {
                if (!reader.TryReadToken(out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }
                state.Current.HasReadFirstToken = true;

                value = new JsonObject(new Dictionary<string, JsonObject>());

                if (reader.Token == JsonToken.ObjectEnd)
                    return true;

                state.Current.HasCreated = true;
            }
            else
            {
                value = (JsonObject)state.Current.Object!;
            }

            for (; ; )
            {
                string? property;
                if (!state.Current.HasReadProperty)
                {
                    if (!state.Current.HasReadFirstToken)
                    {
                        if (!reader.TryReadToken(out state.SizeNeeded))
                        {
                            state.Current.Object = value;
                            return false;
                        }
                    }

                    if (reader.UseBytes)
                        property = reader.UnescapeStringBytes();
                    else
                        property = reader.PositionOfFirstEscape == -1 ? reader.ValueChars.ToString() : reader.UnescapeStringChars(); ;

                    if (String.IsNullOrWhiteSpace(property))
                        throw reader.CreateException();
                }
                else
                {
                    property = (string)state.Current.Property!;
                }

                if (!state.Current.HasReadSeperator)
                {
                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        state.Current.HasReadFirstToken = true;
                        state.Current.Object = value;
                        state.Current.Property = property;
                        state.Current.HasReadProperty = true;
                        state.SizeNeeded = 1;
                        return false;
                    }
                    if (reader.Token != JsonToken.PropertySeperator)
                        throw reader.CreateException();
                }

                if (!state.Current.HasReadValue)
                {
                    if (!ReadJsonObjectFromParentMember(ref reader, ref state, value, property))
                    {
                        state.Current.HasReadFirstToken = true;
                        state.Current.Object = value;
                        state.Current.Property = property;
                        state.Current.HasReadProperty = true;
                        state.Current.HasReadSeperator = true;
                        return false;
                    }
                }

                if (!reader.TryReadToken(out state.SizeNeeded))
                {
                    state.Current.HasReadFirstToken = true;
                    state.Current.Object = value;
                    state.Current.Property = property;
                    state.Current.HasReadProperty = true;
                    state.Current.HasReadSeperator = true;
                    state.Current.HasReadValue = true;
                    return false;
                }

                if (reader.Token == JsonToken.ObjectEnd)
                    break;

                if (reader.Token != JsonToken.NextItem)
                    throw reader.CreateException();

                state.Current.HasReadFirstToken = false;
                state.Current.HasReadProperty = false;
                state.Current.HasReadSeperator = false;
                state.Current.HasReadValue = false;
            }

            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadJsonObjectArray(ref JsonReader reader, ref ReadState state, out JsonObject? value)
        {
            ArrayOrListAccessor<JsonObject> accessor;
            if (!state.Current.HasCreated)
            {
                if (!reader.TryReadToken(out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }
                state.Current.HasReadFirstToken = true;

                if (reader.Token == JsonToken.ArrayEnd)
                {
                    value = new JsonObject(new List<JsonObject>(0));
                    return true;
                }

                if (reader.TryPeakArrayLength(out var length))
                    accessor = new ArrayOrListAccessor<JsonObject>(new JsonObject[length]);
                else
                    accessor = new ArrayOrListAccessor<JsonObject>();
            }
            else
            {
                accessor = (ArrayOrListAccessor<JsonObject>)state.Current.Object!;
            }

            for (; ; )
            {
                if (!state.Current.HasReadFirstToken)
                {
                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        state.Current.Object = accessor;
                        state.Current.HasCreated = true;
                        value = default;
                        return false;
                    }
                }

                if (!state.Current.HasReadValue)
                {
                    if (!ReadJsonObjectFromParent(ref reader, ref state, accessor.Add))
                    {
                        state.Current.Object = accessor;
                        state.Current.HasCreated = true;
                        state.Current.HasReadFirstToken = true;
                        value = default;
                        return false;
                    }
                }

                if (!reader.TryReadToken(out state.SizeNeeded))
                {
                    state.Current.Object = accessor;
                    state.Current.HasCreated = true;
                    state.Current.HasReadFirstToken = true;
                    state.Current.HasReadValue = true;
                    value = default;
                    return false;
                }

                if (reader.Token == JsonToken.ArrayEnd)
                {
                    value = new JsonObject(accessor.ToList());
                    return true;
                }

                if (reader.Token != JsonToken.NextItem)
                    throw reader.CreateException();

                state.Current.HasReadFirstToken = false;
                state.Current.HasReadValue = false;
            }
        }
    }
}

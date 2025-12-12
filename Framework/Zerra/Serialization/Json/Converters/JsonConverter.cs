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
    /// <summary>
    /// Abstract base class for JSON serialization and deserialization converters.
    /// </summary>
    /// <remarks>
    /// This class provides the foundation for converting objects to and from JSON format.
    /// Derived classes implement specific conversion logic for different types.
    /// The converter manages state during reading and writing operations and supports
    /// both boxed and unboxed value operations, as well as draining JSON values without processing.
    /// </remarks>
    public abstract partial class JsonConverter
    {
        //The max converter stack before we unwind
        protected const int MaxStackDepth = 31;

        /// <summary>
        /// Initializes the converter with member-specific information.
        /// </summary>
        /// <param name="memberKey">A unique key identifying the member being converted.</param>
        /// <param name="getterDelegate">An optional delegate for retrieving values from parent objects.</param>
        /// <param name="setterDelegate">An optional delegate for setting values on parent objects.</param>
        public abstract void Setup(string memberKey, Delegate? getterDelegate, Delegate? setterDelegate);

        /// <summary>
        /// Attempts to read a boxed value from the JSON reader.
        /// </summary>
        /// <param name="reader">The JSON reader to read from.</param>
        /// <param name="state">The current read state.</param>
        /// <param name="value">The deserialized boxed value if successful; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the read operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        public abstract bool TryReadBoxed(ref JsonReader reader, ref ReadState state, out object? value);

        /// <summary>
        /// Attempts to write a boxed value to the JSON writer.
        /// </summary>
        /// <param name="writer">The JSON writer to write to.</param>
        /// <param name="state">The current write state.</param>
        /// <param name="value">The boxed value to serialize.</param>
        /// <returns><c>true</c> if the write operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        public abstract bool TryWriteBoxed(ref JsonWriter writer, ref WriteState state, in object? value);

        /// <summary>
        /// Attempts to read a value from a parent object in JSON format.
        /// </summary>
        /// <param name="reader">The JSON reader to read from.</param>
        /// <param name="state">The current read state.</param>
        /// <param name="parent">The parent object to populate.</param>
        /// <returns><c>true</c> if the read operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        public abstract bool TryReadFromParent(ref JsonReader reader, ref ReadState state, object? parent);

        /// <summary>
        /// Attempts to write a value from a parent object to JSON format.
        /// </summary>
        /// <param name="writer">The JSON writer to write to.</param>
        /// <param name="state">The current write state.</param>
        /// <param name="parent">The parent object to serialize.</param>
        /// <returns><c>true</c> if the write operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        public abstract bool TryWriteFromParent(ref JsonWriter writer, ref WriteState state, object parent);

        /// <summary>
        /// Attempts to read a value from a parent object in JSON format.
        /// </summary>
        /// <param name="reader">The JSON reader to read from.</param>
        /// <param name="state">The current read state.</param>
        /// <param name="parent">The parent object to populate.</param>
        /// <param name="propertyName">The optional property name being deserialized.</param>
        /// <returns><c>true</c> if the read operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        public abstract bool TryReadFromParentMember(ref JsonReader reader, ref ReadState state, object? parent, string? propertyName = null);

        /// <summary>
        /// Attempts to write a value from a parent object to JSON format.
        /// </summary>
        /// <param name="writer">The JSON writer to write to.</param>
        /// <param name="state">The current write state.</param>
        /// <param name="parent">The parent object to serialize.</param>
        /// <param name="propertyName">The optional property name for the value.</param>
        /// <param name="jsonNameSegmentChars">An optional property name as a character span.</param>
        /// <param name="jsonNameSegmentBytes">An optional property name as a byte span.</param>
        /// <param name="ignoreCondition">The condition determining whether to ignore this property.</param>
        /// <param name="ignoreDoNotWriteNullProperties">Whether to skip writing properties with null values.</param>
        /// <returns><c>true</c> if the write operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        public abstract bool TryWriteFromParentMember(ref JsonWriter writer, ref WriteState state, object parent, string? propertyName = null, ReadOnlySpan<char> jsonNameSegmentChars = default, ReadOnlySpan<byte> jsonNameSegmentBytes = default, JsonIgnoreCondition ignoreCondition = JsonIgnoreCondition.Never, bool ignoreDoNotWriteNullProperties = false);

        /// <summary>
        /// Attempts to read a boxed value from the JSON reader with the specified value type information.
        /// </summary>
        /// <param name="reader">The JSON reader to read from.</param>
        /// <param name="state">The current read state.</param>
        /// <param name="valueType">The determined JSON value type.</param>
        /// <param name="value">The deserialized boxed value if successful; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the read operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryReadValueBoxed(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out object? value);

        /// <summary>
        /// Attempts to write a boxed value to the JSON writer.
        /// </summary>
        /// <param name="writer">The JSON writer to write to.</param>
        /// <param name="state">The current write state.</param>
        /// <param name="value">The boxed value to serialize.</param>
        /// <returns><c>true</c> if the write operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryWriteValueBoxed(ref JsonWriter writer, ref WriteState state, in object value);

        /// <summary>
        /// Sets a collected value on a parent object.
        /// </summary>
        /// <param name="parent">The parent object to set the value on.</param>
        /// <param name="value">The value to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract void CollectedValuesSetter(object? parent, in object? value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainFromParent(ref JsonReader reader, ref ReadState state)
        {
            if (state.Current.ChildValueType == JsonValueType.NotDetermined)
            {
                if (!reader.TryReadValueType(out state.Current.ChildValueType, out state.SizeNeeded))
                    return false;
            }

            switch (state.Current.ChildValueType)
            {
                case JsonValueType.Object:
                    state.PushFrame(null);
                    if (!DrainObject(ref reader, ref state))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Array:
                    state.PushFrame(null);
                    if (!DrainArray(ref reader, ref state))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.String:
                    state.PushFrame(null);
                    if (!DrainString(ref reader, ref state))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Null_Completed:
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.False_Completed:
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.True_Completed:
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Number:
                    state.PushFrame(null);
                    if (!DrainNumber(ref reader, ref state))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool Drain(ref JsonReader reader, ref ReadState state, JsonValueType valueType)
        {
            switch (valueType)
            {
                case JsonValueType.Object:
                    return DrainObject(ref reader, ref state);
                case JsonValueType.Array:
                    return DrainArray(ref reader, ref state);
                case JsonValueType.String:
                    return DrainString(ref reader, ref state);
                case JsonValueType.Null_Completed:
                    return true;
                case JsonValueType.False_Completed:
                    return true;
                case JsonValueType.True_Completed:
                    return true;
                case JsonValueType.Number:
                    return DrainNumber(ref reader, ref state);
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainObject(ref JsonReader reader, ref ReadState state)
        {
            char c;

            if (!state.Current.HasCreated)
            {
                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.SizeNeeded = 1;
                    return false;
                }

                if (c == '}')
                    return true;

                reader.BackOne();

                state.Current.HasCreated = true;
            }

            for (; ; )
            {
                if (!state.Current.HasReadProperty)
                {
                    if (!reader.TryReadStringEscapedQuoted(false, out var name, out state.SizeNeeded))
                        return false;

                    if (String.IsNullOrWhiteSpace(name))
                        throw reader.CreateException("Unexpected character");
                }

                if (!state.Current.HasReadSeperator)
                {
                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.Current.HasReadProperty = true;
                        state.SizeNeeded = 1;
                        return false;
                    }
                    if (c != ':')
                        throw reader.CreateException("Unexpected character");
                }

                if (!state.Current.HasReadValue)
                {
                    if (!DrainFromParent(ref reader, ref state))
                    {
                        state.Current.HasReadProperty = true;
                        state.Current.HasReadSeperator = true;
                        return false;
                    }
                }

                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.SizeNeeded = 1;
                    state.Current.HasReadProperty = true;
                    state.Current.HasReadSeperator = true;
                    state.Current.HasReadValue = true;
                    return false;
                }

                if (c == '}')
                    break;

                if (c != ',')
                    throw reader.CreateException("Unexpected character");

                state.Current.HasReadProperty = false;
                state.Current.HasReadSeperator = false;
                state.Current.HasReadValue = false;
            }

            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainArray(ref JsonReader reader, ref ReadState state)
        {
            char c;

            if (!state.Current.HasCreated)
            {
                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.SizeNeeded = 1;
                    return false;
                }

                if (c == ']')
                {
                    return true;
                }

                reader.BackOne();
            }

            for (; ; )
            {
                if (!state.Current.HasReadValue)
                {
                    if (!DrainFromParent(ref reader, ref state))
                    {
                        state.Current.HasCreated = true;
                        return false;
                    }
                }

                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.SizeNeeded = 1;
                    state.Current.HasCreated = true;
                    state.Current.HasReadValue = true;
                    return false;
                }

                if (c == ']')
                    return true;

                if (c != ',')
                    throw reader.CreateException("Unexpected character");

                state.Current.HasReadValue = false;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainString(ref JsonReader reader, ref ReadState state)
        {
            if (reader.UseBytes)
                return reader.TryReadStringQuotedBytes(true, out _, out state.SizeNeeded);
            else
                return reader.TryReadStringQuotedChars(true, out _, out state.SizeNeeded);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainNumber(ref JsonReader reader, ref ReadState state)
        {
            if (reader.UseBytes)
                return reader.TryReadNumberBytes(out _, out state.SizeNeeded);
            else
                return reader.TryReadNumberChars(out _, out state.SizeNeeded);
        }

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
            if (state.EntryValueType == JsonValueType.NotDetermined)
            {
                if (!reader.TryReadValueType(out state.EntryValueType, out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }
            }

            switch (state.EntryValueType)
            {
                case JsonValueType.Object:
                    state.PushFrame(null);
                    if (!ReadJsonObjectObject(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        value = default;
                        return false;
                    }
                    state.EndFrame();
                    return true;
                case JsonValueType.Array:
                    state.PushFrame(null);
                    if (!ReadJsonObjectArray(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        value = default;
                        return false;
                    }
                    state.EndFrame();
                    return true;
                case JsonValueType.String:
                    if (!reader.TryReadStringEscapedQuoted(true, out var str, out state.SizeNeeded))
                    {
                        value = default;
                        return false;
                    }
                    value = new JsonObject(str);
                    return true;
                case JsonValueType.Null_Completed:
                    value = new JsonObject();
                    return true;
                case JsonValueType.False_Completed:
                    value = new JsonObject(false);
                    return true;
                case JsonValueType.True_Completed:
                    value = new JsonObject(true);
                    return true;
                case JsonValueType.Number:
                    decimal number;
                    if (reader.UseBytes)
                    {
                        if (!reader.TryReadNumberBytes(out var bytes, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if ((!Utf8Parser.TryParse(bytes, out number, out var consumed) || consumed != bytes.Length) && state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert number (disable {nameof(ReadState.ErrorOnTypeMismatch)} to prevent this exception)");
                    }
                    else
                    {
                        if (!reader.TryReadNumberChars(out var chars, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
#if NETSTANDARD2_0
                        if (!Decimal.TryParse(chars.ToString(), out number) && state.ErrorOnTypeMismatch)
#else
                        if (!Decimal.TryParse(chars, out number) && state.ErrorOnTypeMismatch)
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
        protected static bool ReadJsonObjectFromParent(ref JsonReader reader, ref ReadState state, in JsonObject parent, string property)
        {
            if (state.Current.ChildValueType == JsonValueType.NotDetermined)
            {
                if (!reader.TryReadValueType(out state.Current.ChildValueType, out state.SizeNeeded))
                    return false;
            }

            JsonObject? value;

            switch (state.Current.ChildValueType)
            {
                case JsonValueType.Object:
                    state.PushFrame(null);
                    if (!ReadJsonObjectObject(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    parent[property] = value!;
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Array:
                    state.PushFrame(null);
                    if (!ReadJsonObjectArray(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    parent[property] = value!;
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.String:
                    if (!reader.TryReadStringEscapedQuoted(true, out var str, out state.SizeNeeded))
                    {
                        value = default;
                        return false;
                    }
                    parent[property] = new JsonObject(str!);
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Null_Completed:
                    parent[property] = new JsonObject();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.False_Completed:
                    parent[property] = new JsonObject(false);
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.True_Completed:
                    parent[property] = new JsonObject(true);
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Number:
                    decimal number;
                    if (reader.UseBytes)
                    {
                        if (!reader.TryReadNumberBytes(out var bytes, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if ((!Utf8Parser.TryParse(bytes, out number, out var consumed) || consumed != bytes.Length) && state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert number (disable {nameof(ReadState.ErrorOnTypeMismatch)} to prevent this exception)");
                    }
                    else
                    {
                        if (!reader.TryReadNumberChars(out var chars, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
#if NETSTANDARD2_0
                        if (!Decimal.TryParse(chars.ToString(), out number) && state.ErrorOnTypeMismatch)
#else
                        if (!Decimal.TryParse(chars, out number) && state.ErrorOnTypeMismatch)
#endif
                            throw reader.CreateException($"Cannot convert number (disable {nameof(ReadState.ErrorOnTypeMismatch)} to prevent this exception)");
                    }
                    parent[property] = new JsonObject(number!);
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadJsonObjectFromParent(ref JsonReader reader, ref ReadState state, in Action<JsonObject> addMethod)
        {
            if (state.Current.ChildValueType == JsonValueType.NotDetermined)
            {
                if (!reader.TryReadValueType(out state.Current.ChildValueType, out state.SizeNeeded))
                    return false;
            }

            JsonObject? value;

            switch (state.Current.ChildValueType)
            {
                case JsonValueType.Object:
                    state.PushFrame(null);
                    if (!ReadJsonObjectObject(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    addMethod(value!);
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Array:
                    state.PushFrame(null);
                    if (!ReadJsonObjectArray(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    addMethod(value!);
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.String:
                    if (!reader.TryReadStringEscapedQuoted(true, out var str, out state.SizeNeeded))
                    {
                        value = default;
                        return false;
                    }
                    addMethod(new JsonObject(str!));
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Null_Completed:
                    addMethod(new JsonObject());
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.False_Completed:
                    addMethod(new JsonObject(false));
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.True_Completed:
                    addMethod(new JsonObject(true));
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Number:
                    decimal number;
                    if (reader.UseBytes)
                    {
                        if (!reader.TryReadNumberBytes(out var bytes, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if ((!Utf8Parser.TryParse(bytes, out number, out var consumed) || consumed != bytes.Length) && state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert number (disable {nameof(ReadState.ErrorOnTypeMismatch)} to prevent this exception)");
                    }
                    else
                    {
                        if (!reader.TryReadNumberChars(out var chars, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
#if NETSTANDARD2_0
                        if (!Decimal.TryParse(chars.ToString(), out number) && state.ErrorOnTypeMismatch)
#else
                        if (!Decimal.TryParse(chars, out number) && state.ErrorOnTypeMismatch)
#endif
                            throw reader.CreateException($"Cannot convert number (disable {nameof(ReadState.ErrorOnTypeMismatch)} to prevent this exception)");
                    }
                    addMethod(new JsonObject(number));
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadJsonObject(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out JsonObject? value)
        {
            switch (valueType)
            {
                case JsonValueType.Object:
                    return ReadJsonObjectObject(ref reader, ref state, out value);
                case JsonValueType.Array:
                    return ReadJsonObjectArray(ref reader, ref state, out value);
                case JsonValueType.String:
                    if (!reader.TryReadStringEscapedQuoted(true, out var str, out state.SizeNeeded))
                    {
                        value = default;
                        return false;
                    }
                    value = new JsonObject(str);
                    return true;
                case JsonValueType.Null_Completed:
                    value = new JsonObject();
                    return true;
                case JsonValueType.False_Completed:
                    value = new JsonObject(false);
                    return true;
                case JsonValueType.True_Completed:
                    value = new JsonObject(true);
                    return true;
                case JsonValueType.Number:
                    decimal number;
                    if (reader.UseBytes)
                    {
                        if (!reader.TryReadNumberBytes(out var bytes, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
                        if ((!Utf8Parser.TryParse(bytes, out number, out var consumed) || consumed != bytes.Length) && state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert number (disable {nameof(ReadState.ErrorOnTypeMismatch)} to prevent this exception)");
                    }
                    else
                    {
                        if (!reader.TryReadNumberChars(out var chars, out state.SizeNeeded))
                        {
                            value = default;
                            return false;
                        }
#if NETSTANDARD2_0
                        if (!Decimal.TryParse(chars.ToString(), out number) && state.ErrorOnTypeMismatch)
#else
                        if (!Decimal.TryParse(chars, out number) && state.ErrorOnTypeMismatch)
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
            char c;

            if (!state.Current.HasCreated)
            {
                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.SizeNeeded = 1;
                    value = default;
                    return false;
                }

                value = new JsonObject(new Dictionary<string, JsonObject>());

                if (c == '}')
                    return true;

                reader.BackOne();

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
                    if (!reader.TryReadStringEscapedQuoted(false, out property, out state.SizeNeeded))
                    {
                        state.Current.Object = value;
                        return false;
                    }

                    if (String.IsNullOrWhiteSpace(property))
                        throw reader.CreateException("Unexpected character");
                }
                else
                {
                    property = (string)state.Current.Property!;
                }

                if (!state.Current.HasReadSeperator)
                {
                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.Current.Object = value;
                        state.Current.Property = property;
                        state.Current.HasReadProperty = true;
                        state.SizeNeeded = 1;
                        return false;
                    }
                    if (c != ':')
                        throw reader.CreateException("Unexpected character");
                }

                if (!state.Current.HasReadValue)
                {
                    if (!ReadJsonObjectFromParent(ref reader, ref state, value, property))
                    {
                        state.Current.Object = value;
                        state.Current.Property = property;
                        state.Current.HasReadProperty = true;
                        state.Current.HasReadSeperator = true;
                        return false;
                    }
                }

                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.SizeNeeded = 1;
                    state.Current.Object = value;
                    state.Current.Property = property;
                    state.Current.HasReadProperty = true;
                    state.Current.HasReadSeperator = true;
                    state.Current.HasReadValue = true;
                    return false;
                }

                if (c == '}')
                    break;

                if (c != ',')
                    throw reader.CreateException("Unexpected character");

                state.Current.HasReadProperty = false;
                state.Current.HasReadSeperator = false;
                state.Current.HasReadValue = false;
            }

            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadJsonObjectArray(ref JsonReader reader, ref ReadState state, out JsonObject? value)
        {
            char c;

            ArrayOrListAccessor<JsonObject> accessor;
            if (!state.Current.HasCreated)
            {
                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.SizeNeeded = 1;
                    value = default;
                    return false;
                }

                if (c == ']')
                {
                    value = new JsonObject(new List<JsonObject>(0));
                    return true;
                }

                reader.BackOne();

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
                if (!state.Current.HasReadValue)
                {
                    if (!ReadJsonObjectFromParent(ref reader, ref state, accessor.Add))
                    {
                        state.Current.Object = accessor;
                        state.Current.HasCreated = true;
                        value = default;
                        return false;
                    }
                }

                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.SizeNeeded = 1;
                    state.Current.Object = accessor;
                    state.Current.HasCreated = true;
                    state.Current.HasReadValue = true;
                    value = default;
                    return false;
                }

                if (c == ']')
                {
                    value = new JsonObject(accessor.ToList());
                    return true;
                }

                if (c != ',')
                    throw reader.CreateException("Unexpected character");

                state.Current.HasReadValue = false;
            }
        }
    }
}

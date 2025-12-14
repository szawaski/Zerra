// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;
using System.Runtime.CompilerServices;

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
        public abstract bool TryReadFromParentMember(ref JsonReader reader, ref ReadState state, object? parent, string? propertyName, bool readToken);

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
        public abstract bool TryWriteFromParentMember(ref JsonWriter writer, ref WriteState state, object parent, string? propertyName, ReadOnlySpan<char> jsonNameSegmentChars, ReadOnlySpan<byte> jsonNameSegmentBytes, JsonIgnoreCondition ignoreCondition, bool ignoreDoNotWriteNullProperties);

        /// <summary>
        /// Attempts to read a boxed value from the JSON reader with the specified value type information.
        /// </summary>
        /// <param name="reader">The JSON reader to read from.</param>
        /// <param name="state">The current read state.</param>
        /// <param name="valueType">The determined JSON value type.</param>
        /// <param name="value">The deserialized boxed value if successful; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the read operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryReadValueBoxed(ref JsonReader reader, ref ReadState state, JsonToken token, out object? value);

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
    }
}

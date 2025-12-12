// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.State;
using Zerra.Serialization.Bytes.IO;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization.Bytes.Converters
{
    /// <summary>
    /// Abstract base class for byte serialization and deserialization converters.
    /// </summary>
    /// <remarks>
    /// This class provides the foundation for converting objects to and from byte format.
    /// Derived classes implement specific conversion logic for different types.
    /// The converter manages state during reading and writing operations and supports
    /// both boxed and unboxed value operations.
    /// </remarks>
    public abstract class ByteConverter
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
        /// Attempts to read a boxed value from the byte reader.
        /// </summary>
        /// <param name="reader">The byte reader to read from.</param>
        /// <param name="state">The current read state.</param>
        /// <param name="value">The deserialized boxed value if successful; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the read operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        public abstract bool TryReadBoxed(ref ByteReader reader, ref ReadState state, out object? value);

        /// <summary>
        /// Attempts to write a boxed value to the byte writer.
        /// </summary>
        /// <param name="writer">The byte writer to write to.</param>
        /// <param name="state">The current write state.</param>
        /// <param name="value">The boxed value to serialize.</param>
        /// <returns><c>true</c> if the write operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        public abstract bool TryWriteBoxed(ref ByteWriter writer, ref WriteState state, in object? value);

        /// <summary>
        /// Attempts to read a value from a parent object.
        /// </summary>
        /// <param name="reader">The byte reader to read from.</param>
        /// <param name="state">The current read state.</param>
        /// <param name="parent">The parent object to read from; may be <c>null</c>.</param>
        /// <returns><c>true</c> if the read operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        public abstract bool TryReadFromParent(ref ByteReader reader, ref ReadState state, object? parent);

        /// <summary>
        /// Attempts to write a value from a parent object.
        /// </summary>
        /// <param name="writer">The byte writer to write to.</param>
        /// <param name="state">The current write state.</param>
        /// <param name="parent">The parent object to write from.</param>
        /// <returns><c>true</c> if the write operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        public abstract bool TryWriteFromParent(ref ByteWriter writer, ref WriteState state, object parent);

        /// <summary>
        /// Attempts to read a member value from a parent object.
        /// </summary>
        /// <param name="reader">The byte reader to read from.</param>
        /// <param name="state">The current read state.</param>
        /// <param name="parent">The parent object to read from; may be <c>null</c>.</param>
        /// <param name="drainBytes">If <c>true</c>, any remaining bytes should be consumed; otherwise, <c>false</c>.</param>
        /// <returns><c>true</c> if the read operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        public abstract bool TryReadFromParentMember(ref ByteReader reader, ref ReadState state, object? parent, bool drainBytes);

        /// <summary>
        /// Attempts to write a member value from a parent object.
        /// </summary>
        /// <param name="writer">The byte writer to write to.</param>
        /// <param name="state">The current write state.</param>
        /// <param name="parent">The parent object to write from.</param>
        /// <param name="indexProperty">An optional index property identifier; defaults to zero if not specified.</param>
        /// <param name="indexPropertyName">An optional index property name as a byte span; defaults to an empty span if not specified.</param>
        /// <returns><c>true</c> if the write operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        public abstract bool TryWriteFromParentMember(ref ByteWriter writer, ref WriteState state, object parent, ushort indexProperty, ReadOnlySpan<byte> indexPropertyName);


        /// <summary>
        /// Attempts to read a boxed value from the byte reader.
        /// </summary>
        /// <param name="reader">The byte reader to read from.</param>
        /// <param name="state">The current read state.</param>
        /// <param name="value">The deserialized boxed value if successful; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the read operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryReadValueBoxed(ref ByteReader reader, ref ReadState state, out object? value);

        /// <summary>
        /// Attempts to write a boxed value to the byte writer.
        /// </summary>
        /// <param name="writer">The byte writer to write to.</param>
        /// <param name="state">The current write state.</param>
        /// <param name="value">The boxed value to serialize.</param>
        /// <returns><c>true</c> if the write operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryWriteValueBoxed(ref ByteWriter writer, ref WriteState state, in object value);

        /// <summary>
        /// Sets a collected value on a parent object.
        /// </summary>
        /// <param name="parent">The parent object to set the value on.</param>
        /// <param name="value">The value to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract void CollectedValuesSetter(object? parent, in object? value);
    }
}
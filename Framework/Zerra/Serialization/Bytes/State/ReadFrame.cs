// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.InteropServices;

namespace Zerra.Serialization.Bytes.State
{
    /// <summary>
    /// Represents the state of reading a frame during bytes deserialization.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct ReadFrame
    {
        /// <summary>
        /// Gets or sets the type of the child element being read.
        /// </summary>
        public Type? ChildReadType;

        /// <summary>
        /// Gets or sets a value indicating whether a null check has been performed on the child element.
        /// </summary>
        public bool ChildHasNullChecked;

        /// <summary>
        /// Gets or sets a value indicating whether the object has been created.
        /// </summary>
        public bool HasCreated;

        /// <summary>
        /// Gets or sets the object being deserialized.
        /// </summary>
        public object? Object;

        /// <summary>
        /// Gets or sets the current index in the enumerator.
        /// </summary>
        public int EnumeratorIndex;

        /// <summary>
        /// Gets or sets the length of the enumerable, if applicable.
        /// </summary>
        public int? EnumerableLength;

        /// <summary>
        /// Gets or sets a value indicating whether a property has been read.
        /// </summary>
        public bool HasReadProperty;

        /// <summary>
        /// Gets or sets the property value being processed.
        /// </summary>
        public object? Property;

        /// <summary>
        /// Gets or sets a value indicating whether remaining bytes should be drained.
        /// </summary>
        public bool DrainBytes;
    }
}
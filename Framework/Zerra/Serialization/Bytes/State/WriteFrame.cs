// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.InteropServices;

namespace Zerra.Serialization.Bytes.State
{
    /// <summary>
    /// Represents the state of writing a frame during bytes serialization.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct WriteFrame
    {
        /// <summary>
        /// Gets or sets the type of the child element being written.
        /// </summary>
        public Type? ChildWriteType;

        /// <summary>
        /// Gets or sets the object being serialized.
        /// </summary>
        public object? Object;

        /// <summary>
        /// Gets or sets a value indicating whether a null indicator has been written for the child element.
        /// </summary>
        public bool ChildHasWrittenIsNull;

        /// <summary>
        /// Gets or sets the current index in the enumerator.
        /// </summary>
        public int EnumeratorIndex;

        /// <summary>
        /// Gets or sets a value indicating whether enumeration is currently in progress.
        /// </summary>
        public bool EnumeratorInProgress;

        /// <summary>
        /// Gets or sets a value indicating whether a property index has been written.
        /// </summary>
        public bool HasWrittenPropertyIndex;
    }
}
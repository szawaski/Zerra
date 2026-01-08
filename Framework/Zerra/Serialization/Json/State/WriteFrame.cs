// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.InteropServices;

namespace Zerra.Serialization.Json.State
{
    /// <summary>
    /// Represents the state of writing a frame during JSON serialization.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct WriteFrame
    {
        /// <summary>
        /// Gets or sets a value indicating whether a property name has been written.
        /// </summary>
        public bool HasWrittenPropertyName;

        /// <summary>
        /// Gets or sets the object being serialized.
        /// </summary>
        public object? Object;

        /// <summary>
        /// Gets or sets a value indicating whether the opening bracket or brace has been written.
        /// </summary>
        public bool HasWrittenStart;

        /// <summary>
        /// Gets or sets a value indicating whether the first item has been written.
        /// </summary>
        public bool HasWrittenFirst;

        /// <summary>
        /// Gets or sets a value indicating whether a separator has been written.
        /// </summary>
        public bool HasWrittenSeperator;

        /// <summary>
        /// Gets or sets the current index in the enumerator.
        /// </summary>
        public int EnumeratorIndex;

        /// <summary>
        /// Gets or sets a value indicating whether enumeration is currently in progress.
        /// </summary>
        public bool EnumeratorInProgress;

        /// <summary>
        /// Gets or sets the graph that specifies which members should be included or excluded from processing.
        /// </summary>
        public Graph? Graph;
    }
}
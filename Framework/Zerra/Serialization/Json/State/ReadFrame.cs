// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.InteropServices;

namespace Zerra.Serialization.Json.State
{
    /// <summary>
    /// Represents the state of reading a frame during JSON deserialization.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct ReadFrame
    {
        /// <summary>
        /// Gets or sets the JSON token type of the child element being read.
        /// </summary>
        public JsonToken ChildJsonToken;

        /// <summary>
        /// Gets or sets the current state of the frame during deserialization.
        /// </summary>
        public byte State;

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
        /// Gets or sets a value indicating whether the first token has been read.
        /// </summary>
        public bool HasReadFirstToken;

        /// <summary>
        /// Gets or sets a value indicating whether a property has been read.
        /// </summary>
        public bool HasReadProperty;

        /// <summary>
        /// Gets or sets a value indicating whether a separator has been read.
        /// </summary>
        public bool HasReadSeperator;

        /// <summary>
        /// Gets or sets a value indicating whether a value has been read.
        /// </summary>
        public bool HasReadValue;

        /// <summary>
        /// Gets or sets the property value being processed.
        /// </summary>
        public object? Property;

        /// <summary>
        /// Gets or sets the graph that specifies which members should be included or excluded from processing.
        /// </summary>
        public Graph? Graph;

        /// <summary>
        /// Gets or sets the graph containing the members that were present in the JSON data.
        /// </summary>
        public Graph? ReturnGraph;
    }
}
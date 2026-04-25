// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json.State
{
    /// <summary>
    /// Represents the different types of tokens in JSON data.
    /// </summary>
    public enum JsonToken : byte
    {
        /// <summary>
        /// The token type has not been determined yet.
        /// </summary>
        NotDetermined,

        /// <summary>
        /// The start of a JSON object (opening brace).
        /// </summary>
        ObjectStart,

        /// <summary>
        /// The end of a JSON object (closing brace).
        /// </summary>
        ObjectEnd,

        /// <summary>
        /// The start of a JSON array (opening bracket).
        /// </summary>
        ArrayStart,

        /// <summary>
        /// The end of a JSON array (closing bracket).
        /// </summary>
        ArrayEnd,

        /// <summary>
        /// A separator between items in an array or object.
        /// </summary>
        NextItem,

        /// <summary>
        /// A separator between a property name and its value.
        /// </summary>
        PropertySeperator,

        /// <summary>
        /// A JSON null value.
        /// </summary>
        Null,

        /// <summary>
        /// A JSON false boolean value.
        /// </summary>
        False,

        /// <summary>
        /// A JSON true boolean value.
        /// </summary>
        True,

        /// <summary>
        /// A JSON string value.
        /// </summary>
        String,

        /// <summary>
        /// A JSON number value.
        /// </summary>
        Number
    }
}
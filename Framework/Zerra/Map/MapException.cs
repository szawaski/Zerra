// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Map
{
    /// <summary>
    /// Describes a problem that occured during a Map or Copy.
    /// </summary>
    public sealed class MapException : Exception
    {
        /// <summary>
        /// Creates a new map exception.
        /// </summary>
        /// <param name="message">Describes what went wrong with the mapping.</param>
        public MapException(string message) : base(message) { }
        /// <summary>
        /// Creates a new map exception.
        /// </summary>
        /// <param name="message">Describes what went wrong with the mapping.</param>
        /// <param name="innerException">An inner exception with what went wrong with the mapping.</param>
        public MapException(string message, Exception innerException) : base(message, innerException) { }
    }
}

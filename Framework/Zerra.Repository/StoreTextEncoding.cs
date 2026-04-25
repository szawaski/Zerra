// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Specifies the text encoding to use when storing string values.
    /// </summary>
    public enum StoreTextEncoding
    {
        /// <summary>
        /// Stores text using Unicode encoding, supporting the full range of characters.
        /// </summary>
        Unicode,
        /// <summary>
        /// Stores text using non-Unicode encoding, typically limited to a specific character set.
        /// </summary>
        NonUnicode
    }
}

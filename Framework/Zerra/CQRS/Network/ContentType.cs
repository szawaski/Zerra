// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// The data format between CQRS services
    /// </summary>
    public enum ContentType : byte
    {
        /// <summary>
        /// The fastest and smallest data format using <see cref="Zerra.Serialization.Bytes.ByteSerializer"/>.
        /// </summary>
        Bytes = 0,
        /// <summary>
        /// Traditional JSON data format using <see cref="Zerra.Serialization.Json.JsonSerializer"/>.
        /// Mostly used for networks that require this format.
        /// </summary>
        Json = 1,
        /// <summary>
        /// Custom JSON by removing object names and deserializing needs a matching model. <see cref="Zerra.Serialization.Json.JsonSerializer"/>.
        /// Mostly used for JavaScript front ends.
        /// </summary>
        JsonNameless = 2
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Defines the high-level provider contract for reading and writing byte streams.
    /// </summary>
    public interface IByteStoreProvider
    {
        /// <summary>
        /// Retrieves a stream for the entry with the specified name.
        /// </summary>
        /// <param name="name">The name of the entry to retrieve.</param>
        /// <returns>A <see cref="Stream"/> containing the stored data.</returns>
        Stream Get(string name);
        /// <summary>
        /// Persists the given stream under the specified name.
        /// </summary>
        /// <param name="name">The name to store the entry under.</param>
        /// <param name="stream">The stream containing the data to store.</param>
        void Save(string name, Stream stream);
        /// <summary>
        /// Determines whether an entry with the specified name exists.
        /// </summary>
        /// <param name="name">The name of the entry to check.</param>
        /// <returns><see langword="true"/> if the entry exists; otherwise, <see langword="false"/>.</returns>
        bool Exists(string name);

        /// <summary>
        /// Asynchronously retrieves a stream for the entry with the specified name.
        /// </summary>
        /// <param name="name">The name of the entry to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation, containing a <see cref="Stream"/> with the stored data.</returns>
        Task<Stream> GetAsync(string name);
        /// <summary>
        /// Asynchronously persists the given stream under the specified name.
        /// </summary>
        /// <param name="name">The name to store the entry under.</param>
        /// <param name="stream">The stream containing the data to store.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SaveAsync(string name, Stream stream);
        /// <summary>
        /// Asynchronously determines whether an entry with the specified name exists.
        /// </summary>
        /// <param name="name">The name of the entry to check.</param>
        /// <returns>A task that represents the asynchronous operation, containing <see langword="true"/> if the entry exists; otherwise, <see langword="false"/>.</returns>
        Task<bool> ExistsAsync(string name);
    }
}

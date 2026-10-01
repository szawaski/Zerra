// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// An <see cref="IByteStoreProvider"/> implementation that delegates storage operations to an <see cref="IByteStoreEngine"/>.
    /// </summary>
    public sealed class ByteStoreProvider : IByteStoreProvider
    {
        private readonly IByteStoreEngine Engine;

        /// <summary>
        /// Initializes a new instance of <see cref="ByteStoreProvider"/> on <paramref name="engine"/>.
        /// </summary>
        /// <param name="engine">The engine to store the bytes in.</param>
        public ByteStoreProvider(IByteStoreEngine engine)
        {
            if (engine is null)
                throw new ArgumentNullException(nameof(engine));
            this.Engine = engine;
        }

        /// <inheritdoc/>
        public bool Exists(string name)
        {
            return Engine.Exists(name);
        }
        /// <inheritdoc/>
        public Stream Get(string name)
        {
            return Engine.Get(name);
        }
        /// <inheritdoc/>
        public void Save(string name, Stream stream)
        {
            Engine.Save(name, stream);
        }

        /// <inheritdoc/>
        public Task<bool> ExistsAsync(string name)
        {
            return Engine.ExistsAsync(name);
        }
        /// <inheritdoc/>
        public Task<Stream> GetAsync(string name)
        {
            return Engine.GetAsync(name);
        }
        /// <inheritdoc/>
        public Task SaveAsync(string name, Stream stream)
        {
            return Engine.SaveAsync(name, stream);
        }
    }
}
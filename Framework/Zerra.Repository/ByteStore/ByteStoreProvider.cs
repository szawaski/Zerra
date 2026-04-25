// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// An <see cref="IByteStoreProvider"/> implementation that delegates storage operations to an <see cref="IByteStoreEngine"/> obtained from a <typeparamref name="TContext"/>.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DataContext"/> type used to resolve the underlying <see cref="IByteStoreEngine"/>.</typeparam>
    public sealed class ByteStoreProvider<TContext> : IByteStoreProvider
        where TContext : DataContext, new()
    {
        private readonly IByteStoreEngine Engine;

        /// <summary>
        /// Initializes a new instance of <see cref="ByteStoreProvider{TContext}"/>, resolving an <see cref="IByteStoreEngine"/> from the specified context.
        /// </summary>
        /// <exception cref="Exception">Thrown if <typeparamref name="TContext"/> cannot produce an <see cref="IByteStoreEngine"/>.</exception>
        public ByteStoreProvider()
        {
            var context = new TContext();
            if (!context.TryGetEngine(out var engine))
                throw new Exception($"{typeof(TContext).Name} could not produce an engine of {typeof(IByteStoreEngine).Name}");
            if (engine is not IByteStoreEngine byteStoreEngine)
                throw new Exception($"{typeof(TContext).Name} produced an engine of {engine.GetType().Name} which is not a {typeof(IByteStoreEngine).Name}");
            this.Engine = byteStoreEngine;
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
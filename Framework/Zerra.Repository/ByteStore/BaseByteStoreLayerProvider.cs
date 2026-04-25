// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// An abstract base class for layered byte store providers that wrap another <see cref="IByteStoreProvider"/> and can intercept or transform streams on get and save operations.
    /// </summary>
    /// <typeparam name="TNextProviderInterface">The type of the next <see cref="IByteStoreProvider"/> in the layer chain.</typeparam>
    public abstract class BaseByteStoreLayerProvider<TNextProviderInterface> : LayerProvider<TNextProviderInterface>, IByteStoreProvider
        where TNextProviderInterface : IByteStoreProvider
    {
        /// <summary>
        /// Initializes a new instance of <see cref="BaseByteStoreLayerProvider{TNextProviderInterface}"/> with the next provider in the chain.
        /// </summary>
        /// <param name="nextProvider">The next <see cref="IByteStoreProvider"/> to delegate operations to.</param>
        public BaseByteStoreLayerProvider(TNextProviderInterface nextProvider)
            : base(nextProvider)
        {
        }

        /// <inheritdoc/>
        public bool Exists(string name)
        {
            return NextProvider.Exists(name);
        }
        /// <inheritdoc/>
        public Stream Get(string name)
        {
            var stream = NextProvider.Get(name);
            return OnGet(stream);
        }
        /// <inheritdoc/>
        public void Save(string name, Stream stream)
        {
            var resultStream = OnSave(stream);
            NextProvider.Save(name, resultStream);
        }

        /// <inheritdoc/>
        public Task<bool> ExistsAsync(string name)
        {
            return NextProvider.ExistsAsync(name);
        }
        /// <inheritdoc/>
        public async Task<Stream> GetAsync(string name)
        {
            var stream = await NextProvider.GetAsync(name);
            return await OnGetAsync(stream);
        }
        /// <inheritdoc/>
        public async Task SaveAsync(string name, Stream stream)
        {
            var resultStream = await OnSaveAsync(stream);
            await NextProvider.SaveAsync(name, resultStream);
        }

        /// <summary>
        /// Override to transform or inspect a stream after it has been retrieved from the next provider.
        /// </summary>
        /// <param name="stream">The stream returned by the next provider.</param>
        /// <returns>The stream to return to the caller, optionally transformed.</returns>
        protected virtual Stream OnGet(Stream stream) { return stream; }
        /// <summary>
        /// Override to transform or inspect a stream before it is passed to the next provider for saving.
        /// </summary>
        /// <param name="stream">The stream provided by the caller.</param>
        /// <returns>The stream to forward to the next provider, optionally transformed.</returns>
        protected virtual Stream OnSave(Stream stream) { return stream; }

        /// <summary>
        /// Override to asynchronously transform or inspect a stream after it has been retrieved from the next provider.
        /// </summary>
        /// <param name="stream">The stream returned by the next provider.</param>
        /// <returns>A task that represents the asynchronous operation, containing the stream to return to the caller, optionally transformed.</returns>
        protected virtual Task<Stream> OnGetAsync(Stream stream) { return Task.FromResult(stream); }
        /// <summary>
        /// Override to asynchronously transform or inspect a stream before it is passed to the next provider for saving.
        /// </summary>
        /// <param name="stream">The stream provided by the caller.</param>
        /// <returns>A task that represents the asynchronous operation, containing the stream to forward to the next provider, optionally transformed.</returns>
        protected virtual Task<Stream> OnSaveAsync(Stream stream) { return Task.FromResult(stream); }
    }
}

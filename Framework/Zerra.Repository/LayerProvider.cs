// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// An abstract base class for implementing layered providers that wrap another provider of type <typeparamref name="TProvider"/>.
    /// </summary>
    /// <typeparam name="TProvider">The interface type of the next provider in the chain.</typeparam>
    public abstract class LayerProvider<TProvider> : BaseStore
    {
        private static readonly Type InterfaceType;
        static LayerProvider()
        {
            InterfaceType = typeof(TProvider);
            if (!InterfaceType.IsInterface)
                throw new Exception($"{nameof(LayerProvider<TProvider>)} must have a generic argument that is an interface, {InterfaceType.Name} is not an interface");
        }

        private readonly TProvider nextProvider;
        /// <summary>
        /// Initializes a new instance of <see cref="LayerProvider{TProvider}"/> with the specified next provider.
        /// </summary>
        /// <param name="nextProvider">The next provider in the chain to delegate calls to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="nextProvider"/> is <see langword="null"/>.</exception>
        public LayerProvider(TProvider nextProvider)
        {
            if (nextProvider == null)
                throw new ArgumentNullException(nameof(nextProvider));
            this.nextProvider = nextProvider;
        }

        /// <summary>
        /// Gets the next provider in the chain.
        /// </summary>
        protected TProvider NextProvider => nextProvider;

        /// <inheritdoc/>
        public override void OnInitialize(RepoContext context)
        {
            if (nextProvider is BaseStore baseStore)
                baseStore.Initialize(context);
        }
    }
}

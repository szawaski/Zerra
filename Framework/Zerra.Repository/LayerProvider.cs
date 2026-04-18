// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
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
        public LayerProvider(TProvider nextProvider)
        {
            if (nextProvider == null)
                throw new ArgumentNullException(nameof(nextProvider));
            this.nextProvider = nextProvider;
        }

        protected TProvider NextProvider => nextProvider;

        public override void OnInitialize(RepoContext context)
        {
            if (nextProvider is BaseStore baseStore)
                baseStore.Initialize(context);
        }
    }
}

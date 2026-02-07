// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Reflection.Dynamic;

namespace Zerra.Providers
{
    public abstract class LayerProvider<TProvider>
    {
        private static readonly Type InterfaceType;
        static LayerProvider()
        {
            InterfaceType = typeof(TProvider);
            if (!InterfaceType.IsInterface)
                throw new Exception($"{nameof(LayerProvider<TProvider>)} must have a generic argument that is an interface, {InterfaceType.Name} is not an interface");
        }

        private readonly object locker = new();
        private TProvider? nextProvider;
        protected TProvider NextProvider
        {
            get
            {
                if (this.nextProvider is null)
                {
                    lock (locker)
                    {
                        if (this.nextProvider is null)
                        {
                            nextProvider = ProviderResolver.GetNext<TProvider>(this.GetType());
                            var type = EmptyImplementations.GetType(typeof(TProvider));
                            nextProvider ??= (TProvider)Instantiator.Create(type);
                        }
                    }
                }
                return nextProvider;
            }
        }

        internal void SetNextProvider(TProvider provider)
        {
            lock (locker)
            {
                if (this.nextProvider is not null)
                    throw new InvalidOperationException("Next provider already set.");
                this.nextProvider = provider;
            }
        }

#pragma warning disable CA1822 // Mark members as static
        internal Type GetProviderInterfaceType() { return InterfaceType; }
#pragma warning restore CA1822 // Mark members as static
    }
}

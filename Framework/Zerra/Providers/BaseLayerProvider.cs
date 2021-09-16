// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;

namespace Zerra.Providers
{
    public abstract class BaseLayerProvider<TNextProviderInterface> : IBaseProvider
        where TNextProviderInterface : IBaseProvider
    {
        private static readonly Type InterfaceType = typeof(TNextProviderInterface);

        private TNextProviderInterface nextProvider;
        protected TNextProviderInterface NextProvider
        {
            get
            {
                if (this.nextProvider == null)
                {
                    lock (this)
                    {
                        if (this.nextProvider == null)
                        {
                            var highestStackInterface = ProviderLayers.GetHighestProviderInterface(this.GetType());
                            var nextInterfaceType = ProviderLayers.GetProviderInterfaceLayerAfter(highestStackInterface);
                            if (!Resolver.TryGet(nextInterfaceType, out nextProvider))
                            {
                                this.nextProvider = EmptyImplementations.GetEmptyImplementation<TNextProviderInterface>();
                            }
                        }
                    }
                }
                return nextProvider;
            }
        }

        public void SetNextProvider(TNextProviderInterface provider)
        {
            lock (this)
            {
                if (this.nextProvider != null)
                    throw new InvalidOperationException("Next provider already set.");
                this.nextProvider = provider;
            }
        }

        public Type GetProviderInterfaceType() { return InterfaceType; }

        public override sealed string ToString() { return base.ToString(); }
        public override sealed bool Equals(object obj) { return base.Equals(obj); }
        public override sealed int GetHashCode() { return base.GetHashCode(); }
    }
}

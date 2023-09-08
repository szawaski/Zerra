// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;

namespace Zerra.Providers
{
    public abstract class BaseLayerProvider<TProvider>
    {
        private static readonly Type InterfaceType;
        static BaseLayerProvider()
        {
            InterfaceType = typeof(TProvider);
            if (!InterfaceType.IsInterface)
                throw new Exception($"{nameof(BaseLayerProvider<TProvider>)} must have a generic argument that is an interface, {InterfaceType.Name} is not an interface");
        }

        private readonly object locker = new();
        private TProvider nextProvider;
        protected TProvider NextProvider
        {
            get
            {
                if (this.nextProvider == null)
                {
                    lock (locker)
                    {
                        if (this.nextProvider == null)
                        {
                            nextProvider = ProviderResolver.GetNext<TProvider>(this.GetType());
                            if (nextProvider == null)
                                this.nextProvider = EmptyImplementations.GetEmptyImplementation<TProvider>();
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
                if (this.nextProvider != null)
                    throw new InvalidOperationException("Next provider already set.");
                this.nextProvider = provider;
            }
        }

        internal Type GetProviderInterfaceType() { return InterfaceType; }

        public override sealed string ToString() { return base.ToString(); }
        public override sealed bool Equals(object obj) { return base.Equals(obj); }
        public override sealed int GetHashCode() { return base.GetHashCode(); }
    }
}

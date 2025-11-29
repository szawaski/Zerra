// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    public sealed class BusScopes
    {
        internal Dictionary<Type, object> Dependencies { get; init; }

        public BusScopes()
        {
            this.Dependencies = new();
        }

        public void AddScope<TInterface>(TInterface instance) where TInterface : notnull
        {
            if (instance is null)
                throw new ArgumentNullException(nameof(instance));
            var type = typeof(TInterface);
            if (!type.IsInterface)
                throw new ArgumentException("TInterface must be an interface type");
            Dependencies[type] = instance;
        }
    }
}

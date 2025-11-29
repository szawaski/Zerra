// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Logging;

namespace Zerra.CQRS
{
    public sealed class BusContext
    {
        public IBus Bus { get; init; }
        public string Service { get; init; }
        public ILogger? Log { get; init; }

        private readonly Dictionary<Type, object>? dependencies;

        internal BusContext(IBus bus, string service, ILogger? log, BusScopes busScopes)
        {
            this.Bus = bus;
            this.Service = service;
            this.Log = log;
            this.dependencies = busScopes?.Dependencies;
        }

        public TInterface Get<TInterface>() where TInterface : notnull
        {
            if (dependencies == null || !dependencies.TryGetValue(typeof(TInterface), out var instance))
                throw new ArgumentException($"No dependency registered for type {typeof(TInterface).FullName}");
            return (TInterface)instance;
        }
    }
}

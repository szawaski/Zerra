// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public class Persist<TModel> : Persist where TModel : class, new()
    {
        public Persist(PersistOperation operation)
            : base(operation)
        {
        }

        public Persist(PersistOperation operation, string? eventName, object? source)
            : base(operation, eventName, source)
        {
        }

        public Persist(PersistOperation operation, PersistEvent @event)
            : base(operation, @event)
        {
        }

        public Persist(Persist<TModel> persist)
            : base(persist)
        {
        }
    }
}
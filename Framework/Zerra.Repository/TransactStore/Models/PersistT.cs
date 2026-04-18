// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public class Persist<TModel> : Persist where TModel : class, new()
    {
        public Persist(PersistOperation operation, string? eventName, object? source, object[]? models, object[]? ids, Graph? graph)
            : base(operation, eventName, source, typeof(TModel), models, ids, graph)
        {
        }

        public Persist(PersistOperation operation, PersistEvent @event, object[]? models, object[]? ids, Graph? graph)
            : base(operation, @event, typeof(TModel), models, ids, graph)
        {
        }

        public Persist(Persist<TModel> persist)
            : base(persist)
        {
        }
    }
}
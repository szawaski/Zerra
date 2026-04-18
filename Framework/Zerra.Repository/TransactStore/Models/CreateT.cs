// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public sealed class Create<TModel> : Persist<TModel> where TModel : class, new()
    {
        public Create(TModel model) : this(null, null, model, null) { }
        public Create(string? eventName, TModel model) : this(eventName, null, model, null) { }
        public Create(string? eventName, object? source, TModel model) : this(eventName, source, model, null) { }
        public Create(TModel model, Graph<TModel>? graph) : this(null, null, model, graph) { }
        public Create(string? eventName, TModel model, Graph<TModel>? graph) : this(eventName, null, model, graph) { }
        public Create(string? eventName, object? source, TModel model, Graph<TModel>? graph)
            : base(PersistOperation.Create, eventName, source, [model], null, graph)
        {
        }

        public Create(PersistEvent @event, TModel model) : this(@event, model, null) { }
        public Create(PersistEvent @event, TModel model, Graph<TModel>? graph)
            : base(PersistOperation.Create, @event, [model], null, graph)
        {
        }

        public Create(IEnumerable<TModel> models) : this(null, null, models, null) { }
        public Create(string? eventName, IEnumerable<TModel> models) : this(eventName, null, models, null) { }
        public Create(string? eventName, object? source, IEnumerable<TModel> models) : this(eventName, source, models, null) { }
        public Create(IEnumerable<TModel> models, Graph<TModel>? graph) : this(null, null, models, graph) { }
        public Create(string? eventName, IEnumerable<TModel> models, Graph<TModel>? graph) : this(eventName, null, models, graph) { }
        public Create(string? eventName, object? source, IEnumerable<TModel> models, Graph<TModel>? graph)
            : base(PersistOperation.Create, eventName, source, models.Cast<object>().ToArray(), null, graph)
        {
        }

        public Create(PersistEvent @event, IEnumerable<TModel> models) : this(@event, models, null) { }
        public Create(PersistEvent @event, IEnumerable<TModel> models, Graph<TModel>? graph)
            : base(PersistOperation.Create, @event, models.Cast<object>().ToArray(), null, graph)
        {
        }
    }
}

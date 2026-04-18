// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;

namespace Zerra.Repository
{
    public sealed class Create : Persist
    {
        public Create(object model) : this(null, null, model, null) { }
        public Create(string? eventName, object model) : this(eventName, null, model, null) { }
        public Create(string? eventName, object? source, object model) : this(eventName, source, model, null) { }
        public Create(object model, Graph? graph) : this(null, null, model, graph) { }
        public Create(string? eventName, object model, Graph? graph) : this(eventName, null, model, graph) { }
        public Create(string? eventName, object? source, object model, Graph? graph)
            : base(PersistOperation.Create, eventName, source, model.GetType(), [model], null, graph)
        {
        }

        public Create(PersistEvent @event, object model) : this(@event, model, null) { }
        public Create(PersistEvent @event, object model, Graph? graph)
            : base(PersistOperation.Create, @event, model.GetType(), [model], null, graph)
        {
        }

        public Create(IEnumerable models) : this(null, null, models, null) { }
        public Create(string? eventName, IEnumerable models) : this(eventName, null, models, null) { }
        public Create(string? eventName, object? source, IEnumerable models) : this(eventName, source, models, null) { }
        public Create(IEnumerable models, Graph? graph) : this(null, null, models, graph) { }
        public Create(string? eventName, IEnumerable models, Graph? graph) : this(eventName, null, models, graph) { }
        public Create(string? eventName, object? source, IEnumerable models, Graph? graph)
            : base(PersistOperation.Create, eventName, source, models.Cast<object>().First().GetType(), models.Cast<object>().ToArray(), null, graph)
        {
        }

        public Create(PersistEvent @event, IEnumerable models) : this(@event, models, null) { }
        public Create(PersistEvent @event, IEnumerable models, Graph? graph)
            : base(PersistOperation.Create, @event, models.Cast<object>().First().GetType(), models.Cast<object>().ToArray(), null, graph)
        {
        }
    }
}

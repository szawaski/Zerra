// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;

namespace Zerra.Repository
{
    public sealed class Delete : Persist
    {
        public Delete(object model) : this(null, null, model, null) { }
        public Delete(string? eventName, object model) : this(eventName, null, model, null) { }
        public Delete(string? eventName, object? source, object model) : this(eventName, source, model, null) { }
        public Delete(object model, Graph? graph) : this(null, null, model, graph) { }
        public Delete(string? eventName, object model, Graph? graph) : this(eventName, null, model, graph) { }
        public Delete(string? eventName, object? source, object model, Graph? graph)
            : base(PersistOperation.Delete, eventName, source, model.GetType(), [model], null, graph)
        {
        }

        public Delete(PersistEvent @event, object model) : this(@event, model, null) { }
        public Delete(PersistEvent @event, object model, Graph? graph)
            : base(PersistOperation.Delete, @event, model.GetType(), [model], null, graph)
        {
        }

        public Delete(IEnumerable models) : this(null, null, models, null) { }
        public Delete(string? eventName, IEnumerable models) : this(eventName, null, models, null) { }
        public Delete(string? eventName, object? source, IEnumerable models) : this(eventName, source, models, null) { }
        public Delete(IEnumerable models, Graph? graph) : this(null, null, models, graph) { }
        public Delete(string? eventName, IEnumerable models, Graph? graph) : this(eventName, null, models, graph) { }
        public Delete(string? eventName, object? source, IEnumerable models, Graph? graph)
            : base(PersistOperation.Delete, eventName, source, models.Cast<object>().First().GetType(), models.Cast<object>().ToArray(), null, graph)
        {
        }

        public Delete(PersistEvent @event, IEnumerable models) : this(@event, models, null) { }
        public Delete(PersistEvent @event, IEnumerable models, Graph? graph)
            : base(PersistOperation.Delete, @event, models.Cast<object>().First().GetType(), models.Cast<object>().ToArray(), null, graph)
        {
        }
    }
}

// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;

namespace Zerra.Repository
{
    public sealed class Update : Persist
    {
        public Update(object model) : this(null, null, model, null) { }
        public Update(string? eventName, object model) : this(eventName, null, model, null) { }
        public Update(string? eventName, object? source, object model) : this(eventName, source, model, null) { }
        public Update(object model, Graph? graph) : this(null, null, model, graph) { }
        public Update(string? eventName, object model, Graph? graph) : this(eventName, null, model, graph) { }
        public Update(string? eventName, object? source, object model, Graph? graph)
            : base(PersistOperation.Update, eventName, source, model.GetType(), [model], null, graph)
        {

        }
        public Update(PersistEvent @event, object model) : this(@event, model, null) { }
        public Update(PersistEvent @event, object model, Graph? graph)
            : base(PersistOperation.Update, @event, model.GetType(), [model], null, graph)
        {

        }
        public Update(IEnumerable models) : this(null, null, models, null) { }
        public Update(string? eventName, IEnumerable models) : this(eventName, null, models, null) { }
        public Update(string? eventName, object? source, IEnumerable models) : this(eventName, source, models, null) { }
        public Update(IEnumerable models, Graph? graph) : this(null, null, models, graph) { }
        public Update(string? eventName, IEnumerable models, Graph? graph) : this(eventName, null, models, graph) { }
        public Update(string? eventName, object? source, IEnumerable models, Graph? graph)
            : base(PersistOperation.Update, eventName, source, models.Cast<object>().First().GetType(), models.Cast<object>().ToArray(), null, graph)
        {

        }
        public Update(PersistEvent @event, IEnumerable models) : this(@event, models, null) { }
        public Update(PersistEvent @event, IEnumerable models, Graph? graph)
            : base(PersistOperation.Update, @event, models.Cast<object>().First().GetType(), models.Cast<object>().ToArray(), null, graph)
        {
         
        }
    }
}

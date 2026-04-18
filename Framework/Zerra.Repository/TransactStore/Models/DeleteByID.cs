// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;

namespace Zerra.Repository
{
    public sealed class DeleteByID : Persist
    {
        public DeleteByID(Type modelType, object id) : this(null, null, modelType, id, null) { }
        public DeleteByID(string? eventName, Type modelType, object id) : this(eventName, null, modelType, id, null) { }
        public DeleteByID(string? eventName, object? source, Type modelType, object id) : this(eventName, source, modelType, id, null) { }
        public DeleteByID(Type modelType, object id, Graph? graph) : this(null, null, modelType, id, graph) { }
        public DeleteByID(string? eventName, Type modelType, object id, Graph? graph) : this(eventName, null, modelType, id, graph) { }
        public DeleteByID(string? eventName, object? source, Type modelType, object id, Graph? graph)
            : base(PersistOperation.Delete, eventName, source, modelType, null, [id], graph)
        {
        }

        public DeleteByID(PersistEvent @event, Type modelType, object id) : this(@event, modelType, id, null) { }
        public DeleteByID(PersistEvent @event, Type modelType, object id, Graph? graph)
            : base(PersistOperation.Delete, @event, modelType, null, [id], graph)
        {
        }

        public DeleteByID(Type modelType, ICollection ids) : this(null, null, modelType, ids, null) { }
        public DeleteByID(string? eventName, Type modelType, ICollection ids) : this(eventName, null, modelType, ids, null) { }
        public DeleteByID(string? eventName, object? source, Type modelType, ICollection ids) : this(eventName, source, modelType, ids, null) { }
        public DeleteByID(Type modelType, ICollection ids, Graph? graph) : this((string?)null, modelType, ids, graph) { }
        public DeleteByID(string? eventName, Type modelType, ICollection ids, Graph? graph) : this(eventName, null, modelType, ids, graph) { }
        public DeleteByID(string? eventName, object? source, Type modelType, ICollection ids, Graph? graph)
            : base(PersistOperation.Delete, eventName, source, modelType, null, ids.Cast<object>().ToArray(), graph)
        {
        }

        public DeleteByID(PersistEvent @event, Type modelType, ICollection ids) : this(@event, modelType, ids, null) { }
        public DeleteByID(PersistEvent @event, Type modelType, ICollection ids, Graph? graph)
            : base(PersistOperation.Delete, @event, modelType, null, ids.Cast<object>().ToArray(), graph)
        {
        }
    }
}

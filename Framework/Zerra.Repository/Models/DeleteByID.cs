// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Zerra.Repository
{
    public class DeleteByID<TModel> : Persist<TModel> where TModel : class, new()
    {
        public DeleteByID(object id) : this(null, null, id, null) { }
        public DeleteByID(string eventName, object id) : this(eventName, null, id, null) { }
        public DeleteByID(string eventName,object source, object id) : this(eventName, source, id, null) { }
        public DeleteByID(object id, Graph<TModel> graph) : this(null, null, id, graph) { }
        public DeleteByID(string eventName, object id, Graph<TModel> graph) : this(eventName, null, id, graph) { }
        public DeleteByID(string eventName, object source, object id, Graph<TModel> graph)
            : base(PersistOperation.Delete, eventName, source)
        {
            this.IDs = new object[] { id };
            this.Graph = graph;
        }
        public DeleteByID(PersistEvent @event, object id) : this(@event, id, null) { }
        public DeleteByID(PersistEvent @event, object id, Graph<TModel> graph)
            : base(PersistOperation.Delete, @event)
        {
            this.IDs = new object[] { id };
            this.Graph = graph;
        }
        public DeleteByID(ICollection ids) : this(null, null, ids, null) { }
        public DeleteByID(string eventName, ICollection ids) : this(eventName, null, ids, null) { }
        public DeleteByID(string eventName, object source, ICollection ids) : this(eventName, source, ids, null) { }
        public DeleteByID(ICollection ids, Graph<TModel> graph) : this((string)null, ids, graph) { }
        public DeleteByID(string eventName, ICollection ids, Graph<TModel> graph) : this(eventName, null, ids, graph) { }
        public DeleteByID(string eventName, object source, ICollection ids, Graph<TModel> graph)
            : base(PersistOperation.Delete, eventName, source)
        {
            this.IDs = ids;
            this.Graph = graph;
        }
        public DeleteByID(PersistEvent @event, ICollection ids) : this(@event, ids, null) { }
        public DeleteByID(PersistEvent @event, ICollection ids, Graph<TModel> graph)
            : base(PersistOperation.Delete, @event)
        {
            this.IDs = ids;
            this.Graph = graph;
        }
    }
}

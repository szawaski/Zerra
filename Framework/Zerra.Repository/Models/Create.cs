// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using System.Linq;

namespace Zerra.Repository
{
    public class Create<TModel> : Persist<TModel> where TModel : class, new()
    {
        public Create(TModel model) : this(null, null, model, null) { }
        public Create(string eventName, TModel model) : this(eventName, null, model, null) { }
        public Create(string eventName, object source, TModel model) : this(eventName, source, model, null) { }
        public Create(TModel model, Graph<TModel> graph) : this(null, null, model, graph) { }
        public Create(string eventName, TModel model, Graph<TModel> graph) : this(eventName, null, model, graph) { }
        public Create(string eventName, object source, TModel model, Graph<TModel> graph)
            : base(PersistOperation.Create, eventName, source)
        {
            this.Models = new TModel[] { model };
            this.Graph = graph;
        }
        public Create(PersistEvent @event, TModel model) : this(@event, model, null) { }
        public Create(PersistEvent @event, TModel model, Graph<TModel> graph)
            : base(PersistOperation.Create, @event)
        {
            this.Models = new TModel[] { model };
            this.Graph = graph;
        }
        public Create(ICollection<TModel> models) : this(null, null, models, null) { }
        public Create(string eventName, ICollection<TModel> models) : this(eventName, null, models, null) { }
        public Create(string eventName, object source, ICollection<TModel> models) : this(eventName, source, models, null) { }
        public Create(ICollection<TModel> models, Graph<TModel> graph) : this(null, null, models, graph) { }
        public Create(string eventName, ICollection<TModel> models, Graph<TModel> graph) : this(eventName, null, models, graph) { }
        public Create(string eventName, object source, ICollection<TModel> models, Graph<TModel> graph)
            : base(PersistOperation.Create, eventName, source)
        {
            this.Models = models.ToArray();
            this.Graph = graph;
        }
        public Create(PersistEvent @event, ICollection<TModel> models) : this(@event, models, null) { }
        public Create(PersistEvent @event, ICollection<TModel> models, Graph<TModel> graph)
            : base(PersistOperation.Create, @event)
        {
            this.Models = models.ToArray();
            this.Graph = graph;
        }
    }
}

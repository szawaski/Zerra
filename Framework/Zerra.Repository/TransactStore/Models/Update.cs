// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using System.Linq;

namespace Zerra.Repository
{
    public class Update<TModel> : Persist<TModel> where TModel : class, new()
    {
        public Update(TModel model) : this(null, null, model, null) { }
        public Update(string eventName, TModel model) : this(eventName, null, model, null) { }
        public Update(string eventName, object source, TModel model) : this(eventName, source, model, null) { }
        public Update(TModel model, Graph<TModel> graph) : this(null, null, model, graph) { }
        public Update(string eventName, TModel model, Graph<TModel> graph) : this(eventName, null, model, graph) { }
        public Update(string eventName, object source, TModel model, Graph<TModel> graph)
            : base(PersistOperation.Update, eventName, source)
        {
            this.Models = new TModel[] { model };
            this.Graph = graph;
        }
        public Update(PersistEvent @event, TModel model) : this(@event, model, null) { }
        public Update(PersistEvent @event, TModel model, Graph<TModel> graph)
            : base(PersistOperation.Update, @event)
        {
            this.Models = new TModel[] { model };
            this.Graph = graph;
        }
        public Update(ICollection<TModel> models) : this(null, null, models, null) { }
        public Update(string eventName, ICollection<TModel> models) : this(eventName, null, models, null) { }
        public Update(string eventName, object source, ICollection<TModel> models) : this(eventName, source, models, null) { }
        public Update(ICollection<TModel> models, Graph<TModel> graph) : this(null, null, models, graph) { }
        public Update(string eventName, ICollection<TModel> models, Graph<TModel> graph) : this(eventName, null, models, graph) { }
        public Update(string eventName, object source, ICollection<TModel> models, Graph<TModel> graph)
            : base(PersistOperation.Update, eventName, source)
        {
            this.Models = models.ToArray();
            this.Graph = graph;
        }
        public Update(PersistEvent @event, ICollection<TModel> models) : this(@event, models, null) { }
        public Update(PersistEvent @event, ICollection<TModel> models, Graph<TModel> graph)
            : base(PersistOperation.Update, @event)
        {
            this.Models = models.ToArray();
            this.Graph = graph;
        }
    }
}

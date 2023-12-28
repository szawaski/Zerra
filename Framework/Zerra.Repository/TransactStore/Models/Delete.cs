// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using System.Linq;

namespace Zerra.Repository
{
    public sealed class Delete<TModel> : Persist<TModel> where TModel : class, new()
    {
        public Delete(TModel model) : this(null, null, model, null) { }
        public Delete(string? eventName, TModel model) : this(eventName, null, model, null) { }
        public Delete(string? eventName, object? source, TModel model) : this(eventName, source, model, null) { }
        public Delete(TModel model, Graph<TModel>? graph) : this(null, null, model, graph) { }
        public Delete(string? eventName, TModel model, Graph<TModel>? graph) : this(eventName, null, model, graph) { }
        public Delete(string? eventName, object? source, TModel model, Graph<TModel>? graph)
            : base(PersistOperation.Delete, eventName, source)
        {
            this.Models = new TModel[] { model };
            this.Graph = graph;
        }
        public Delete(PersistEvent @event, TModel model) : this(@event, model, null) { }
        public Delete(PersistEvent @event, TModel model, Graph<TModel>? graph)
            : base(PersistOperation.Delete, @event)
        {
            this.Models = new TModel[] { model };
            this.Graph = graph;
        }
        public Delete(ICollection<TModel> models) : this(null, null, models, null) { }
        public Delete(string? eventName, ICollection<TModel> models) : this(eventName, null, models, null) { }
        public Delete(string? eventName, object? source, ICollection<TModel> models) : this(eventName, source, models, null) { }
        public Delete(ICollection<TModel> models, Graph<TModel>? graph) : this(null, null, models, graph) { }
        public Delete(string? eventName, ICollection<TModel> models, Graph<TModel>? graph) : this(eventName, null, models, graph) { }
        public Delete(string? eventName, object? source, ICollection<TModel> models, Graph<TModel>? graph)
            : base(PersistOperation.Delete, eventName, source)
        {
            this.Models = models.ToArray();
            this.Graph = graph;
        }
        public Delete(PersistEvent @event, ICollection<TModel> models) : this(@event, models, null) { }
        public Delete(PersistEvent @event, ICollection<TModel> models, Graph<TModel>? graph)
            : base(PersistOperation.Delete, @event)
        {
            this.Models = models.ToArray();
            this.Graph = graph;
        }
    }
}

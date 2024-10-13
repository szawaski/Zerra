// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;

namespace Zerra.Repository
{
    public class Persist<TModel> where TModel : class, new()
    {
        public PersistOperation Operation { get; }
        public PersistEvent Event { get; }

        public Graph<TModel>? Graph { get; set; }

        public TModel[]? Models { get; set; }
        public ICollection? IDs { get; set; }

        private static string GetEventName(PersistOperation operation)
        {
            return $"{operation} {typeof(TModel).Name}";
        }

        public Persist(PersistOperation operation)
        {
            this.Operation = operation;
            this.Event = new PersistEvent(Guid.NewGuid(), Persist<TModel>.GetEventName(operation), null);
        }
        public Persist(PersistOperation operation, string? eventName, object? source)
        {
            this.Operation = operation;
            this.Event = new PersistEvent(Guid.NewGuid(), String.IsNullOrWhiteSpace(eventName) ? Persist<TModel>.GetEventName(operation) : eventName, source);
        }
        public Persist(PersistOperation operation, PersistEvent @event)
        {
            this.Operation = operation;
            this.Event = @event;
        }
        public Persist(Persist<TModel> persist)
        {
            this.Operation = persist.Operation;
            this.Event = persist.Event;
            this.Graph = persist.Graph is null ? null : new Graph<TModel>(persist.Graph);
            this.Models = persist.Models;
            this.IDs = persist.IDs;
        }
    }
}
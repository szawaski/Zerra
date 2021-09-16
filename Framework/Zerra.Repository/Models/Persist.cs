// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;

namespace Zerra.Repository
{
    public class Persist<TModel> where TModel : class, new()
    {
        public PersistOperation Operation { get; private set; }
        public PersistEvent Event { get; private set; }

        public Graph<TModel> Graph { get; set; }

        public TModel[] Models { get; set; }
        public ICollection IDs { get; set; }

        private string GetEventName(PersistOperation operation)
        {
            return $"{operation} {typeof(TModel).Name}";
        }

        public Persist(PersistOperation operation)
        {
            this.Operation = operation;
            this.Event = new PersistEvent()
            {
                ID = Guid.NewGuid(),
                Name = GetEventName(operation)
            };
        }
        public Persist(PersistOperation operation, string eventName, object source)
        {
            this.Operation = operation;
            this.Event = new PersistEvent()
            {
                ID = Guid.NewGuid(),
                Name = String.IsNullOrWhiteSpace(eventName) ? GetEventName(operation) : eventName,
                Source = source
            };
        }
        public Persist(PersistOperation operation, PersistEvent @event)
        {
            this.Operation = operation;
            this.Event = @event;
            if (this.Event == null)
            {
                this.Event = new PersistEvent()
                {
                    ID = Guid.NewGuid(),
                    Name = GetEventName(operation),
                    Source = null
                };
            }
            else if (String.IsNullOrWhiteSpace(this.Event.Name))
            {
                this.Event.Name = GetEventName(operation);
            }
        }
        public Persist(Persist<TModel> persist)
        {
            this.Operation = persist.Operation;
            this.Event = persist.Event;
            this.Graph = new Graph<TModel>(persist.Graph);
            this.Models = persist.Models;
            this.IDs = persist.IDs;
        }
    }
}
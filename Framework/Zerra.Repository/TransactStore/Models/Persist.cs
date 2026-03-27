// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;

namespace Zerra.Repository
{
    public class Persist
    {
        public PersistOperation Operation { get; }
        public PersistEvent Event { get; init; }

        public Graph? Graph { get; init; }

        public object[]? Models { get; init; }
        public object[]? IDs { get; init; }

        protected Type? modelType = null;
        public Type ModelType
        {
            get
            {
                if (modelType is null)
                {
                    if (Models is null || Models.Length == 0)
                        throw new InvalidOperationException("Models must be set to get model type");
                    modelType = Models[0].GetType();
                }
                return modelType;
            }
        }

        private string GetEventName(PersistOperation operation)
        {
            return $"{operation} {ModelType.FullName}";
        }

        public Persist(PersistOperation operation)
        {
            this.Operation = operation;
            this.Event = new PersistEvent(Guid.NewGuid(), GetEventName(operation), null);
        }
        public Persist(PersistOperation operation, string? eventName, object? source)
        {
            this.Operation = operation;
            this.Event = new PersistEvent(Guid.NewGuid(), String.IsNullOrWhiteSpace(eventName) ? GetEventName(operation) : eventName, source);
        }
        public Persist(PersistOperation operation, PersistEvent @event)
        {
            this.Operation = operation;
            this.Event = @event;
        }
        public Persist(Persist persist)
        {
            this.Operation = persist.Operation;
            this.Event = persist.Event;
            this.Graph = persist.Graph is null ? null : new Graph(persist.Graph);
            this.Models = persist.Models;
            this.IDs = persist.IDs;
        }
    }
}
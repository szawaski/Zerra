// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public class Persist
    {
        public PersistOperation Operation { get; }
        public PersistEvent Event { get; init; }
        public object[]? Models { get; init; }
        public object[]? IDs { get; init; }
        public Graph? Graph { get; init; }
        public Type ModelType { get; init; }

        private string GetEventName(PersistOperation operation) => $"{operation} {ModelType.FullName}";

        public Persist(PersistOperation operation, string? eventName, object? source, Type modelType, object[]? models, object[]? ids, Graph? graph)
        {
            this.Operation = operation;
            this.ModelType = modelType;
            this.Models = models;
            this.IDs = ids;
            this.Graph = graph;
            this.Event = new PersistEvent(Guid.NewGuid(), String.IsNullOrWhiteSpace(eventName) ? GetEventName(operation) : eventName, source);
        }
        public Persist(PersistOperation operation, PersistEvent @event, Type modelType, object[]? models, object[]? ids, Graph? graph)
        {
            this.Operation = operation;            
            this.ModelType = modelType;
            this.Models = models;
            this.IDs = ids;
            this.Graph = graph;
            this.Event = @event;
        }

        public Persist(Persist persist)
        {
            this.Operation = persist.Operation;
            this.ModelType = persist.ModelType;
            this.Models = persist.Models;
            this.IDs = persist.IDs;
            this.Graph = persist.Graph is null ? null : new Graph(persist.Graph);
            this.Event = persist.Event;
        }
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Represents the full set of parameters for a write (mutation) operation against a data store,
    /// including the operation type, event metadata, model instances or IDs, and an optional property graph.
    /// </summary>
    public class Persist
    {
        /// <summary>Gets the persist operation type.</summary>
        public PersistOperation Operation { get; }
        /// <summary>Gets the event metadata associated with this persist operation.</summary>
        public PersistEvent Event { get; init; }
        /// <summary>Gets the model instances to persist; <see langword="null"/> when deleting by ID.</summary>
        public object[]? Models { get; init; }
        /// <summary>Gets the IDs to delete; <see langword="null"/> when persisting full model instances.</summary>
        public object[]? IDs { get; init; }
        /// <summary>Gets the property graph specifying which members to persist.</summary>
        public Graph? Graph { get; init; }
        /// <summary>Gets the CLR type of the model being persisted.</summary>
        public Type ModelType { get; init; }
        /// <summary>Gets the condition selecting the records to update or delete, or <see langword="null"/> when <see cref="Models"/> or <see cref="IDs"/> name them.</summary>
        public System.Linq.Expressions.LambdaExpression? Where { get; set; }
        /// <summary>Gets or sets the values of an update by <see cref="Where"/> computed from each record, as a projection of the model to itself such as <c>x => new Model { Price = x.Price * 1.1m }</c>; <see langword="null"/> when <see cref="Models"/> and <see cref="Graph"/> hold the values.</summary>
        public System.Linq.Expressions.LambdaExpression? Set { get; set; }
        /// <summary>Gets or sets how many records an update or delete by <see cref="Where"/> changed. The provider sets it.</summary>
        public int RowsAffected { get; set; }

        private string GetEventName(PersistOperation operation) => $"{operation} {ModelType.FullName}";

        /// <summary>
        /// Initializes a new persist operation, generating a new event ID and using <paramref name="eventName"/>
        /// (or a default derived from the operation and model type) as the event name.
        /// </summary>
        /// <param name="operation">The persist operation type.</param>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="modelType">The CLR type of the model being persisted.</param>
        /// <param name="models">The model instances to persist; <see langword="null"/> when deleting by ID.</param>
        /// <param name="ids">The IDs to delete; <see langword="null"/> when persisting full model instances.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        public Persist(PersistOperation operation, string? eventName, object? source, Type modelType, object[]? models, object[]? ids, Graph? graph)
        {
            this.Operation = operation;
            this.ModelType = modelType;
            this.Models = models;
            this.IDs = ids;
            this.Graph = graph;
            this.Event = new PersistEvent(Guid.NewGuid(), String.IsNullOrWhiteSpace(eventName) ? GetEventName(operation) : eventName, source);
        }

        /// <summary>
        /// Initializes a new persist operation with a pre-built event.
        /// </summary>
        /// <param name="operation">The persist operation type.</param>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="modelType">The CLR type of the model being persisted.</param>
        /// <param name="models">The model instances to persist; <see langword="null"/> when deleting by ID.</param>
        /// <param name="ids">The IDs to delete; <see langword="null"/> when persisting full model instances.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        public Persist(PersistOperation operation, PersistEvent @event, Type modelType, object[]? models, object[]? ids, Graph? graph)
        {
            this.Operation = operation;            
            this.ModelType = modelType;
            this.Models = models;
            this.IDs = ids;
            this.Graph = graph;
            this.Event = @event;
        }

        /// <summary>
        /// Initializes a new instance as a deep copy of an existing persist operation.
        /// </summary>
        /// <param name="persist">The persist operation to copy.</param>
        public Persist(Persist persist)
        {
            this.Operation = persist.Operation;
            this.ModelType = persist.ModelType;
            this.Models = persist.Models;
            this.IDs = persist.IDs;
            this.Graph = persist.Graph is null ? null : new Graph(persist.Graph);
            this.Event = persist.Event;
            this.Where = persist.Where;
            this.Set = persist.Set;
        }

        /// <summary>
        /// Initializes an update or delete of every record matching a condition.
        /// </summary>
        /// <param name="operation"><see cref="PersistOperation.Update"/> or <see cref="PersistOperation.Delete"/>.</param>
        /// <param name="eventName">The name of the persist event, or <see langword="null"/> for a default.</param>
        /// <param name="source">The source object that triggered the persist, or <see langword="null"/>.</param>
        /// <param name="modelType">The CLR type of the model.</param>
        /// <param name="where">The condition selecting the records.</param>
        /// <param name="model">For an update, the model holding the values to set; <see langword="null"/> for a delete.</param>
        /// <param name="graph">For an update, the members to set.</param>
        public Persist(PersistOperation operation, string? eventName, object? source, Type modelType, System.Linq.Expressions.LambdaExpression where, object? model, Graph? graph)
        {
            this.Operation = operation;
            this.ModelType = modelType;
            this.Where = where;
            this.Models = model is null ? null : [model];
            this.Graph = graph;
            this.Event = new PersistEvent(Guid.NewGuid(), String.IsNullOrWhiteSpace(eventName) ? GetEventName(operation) : eventName, source);
        }

        /// <summary>
        /// Initializes an update or delete of every record matching a condition, with an existing persist event.
        /// </summary>
        /// <param name="operation"><see cref="PersistOperation.Update"/> or <see cref="PersistOperation.Delete"/>.</param>
        /// <param name="event">The persist event.</param>
        /// <param name="modelType">The CLR type of the model.</param>
        /// <param name="where">The condition selecting the records.</param>
        /// <param name="model">For an update, the model holding the values to set; <see langword="null"/> for a delete.</param>
        /// <param name="graph">For an update, the members to set.</param>
        public Persist(PersistOperation operation, PersistEvent @event, Type modelType, System.Linq.Expressions.LambdaExpression where, object? model, Graph? graph)
        {
            this.Operation = operation;
            this.ModelType = modelType;
            this.Where = where;
            this.Models = model is null ? null : [model];
            this.Graph = graph;
            this.Event = @event;
        }

        /// <summary>
        /// Initializes an update of every record matching a condition to values computed from each record.
        /// </summary>
        /// <param name="operation"><see cref="PersistOperation.Update"/>.</param>
        /// <param name="eventName">The name of the persist event, or <see langword="null"/> for a default.</param>
        /// <param name="source">The source object that triggered the persist, or <see langword="null"/>.</param>
        /// <param name="modelType">The CLR type of the model.</param>
        /// <param name="where">The condition selecting the records.</param>
        /// <param name="set">The members to set and their values in terms of the current record, as <c>x => new Model { Member = ..., ... }</c>.</param>
        public Persist(PersistOperation operation, string? eventName, object? source, Type modelType, System.Linq.Expressions.LambdaExpression where, System.Linq.Expressions.LambdaExpression set)
        {
            this.Operation = operation;
            this.ModelType = modelType;
            this.Where = where;
            this.Set = set;
            this.Event = new PersistEvent(Guid.NewGuid(), String.IsNullOrWhiteSpace(eventName) ? GetEventName(operation) : eventName, source);
        }

        /// <summary>
        /// Initializes an update of every record matching a condition to values computed from each record, with an existing persist event.
        /// </summary>
        /// <param name="operation"><see cref="PersistOperation.Update"/>.</param>
        /// <param name="event">The persist event.</param>
        /// <param name="modelType">The CLR type of the model.</param>
        /// <param name="where">The condition selecting the records.</param>
        /// <param name="set">The members to set and their values in terms of the current record, as <c>x => new Model { Member = ..., ... }</c>.</param>
        public Persist(PersistOperation operation, PersistEvent @event, Type modelType, System.Linq.Expressions.LambdaExpression where, System.Linq.Expressions.LambdaExpression set)
        {
            this.Operation = operation;
            this.ModelType = modelType;
            this.Where = where;
            this.Set = set;
            this.Event = @event;
        }
    }
}
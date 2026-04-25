// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>
        /// Deletes a single record from the data store by its ID.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="id">The ID of the record to delete.</param>
        void DeleteByID<TModel>(object id) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, null, null, null, [id], null));

        /// <summary>
        /// Deletes a single record from the data store by its ID with an event name.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="id">The ID of the record to delete.</param>
        void DeleteByID<TModel>(string? eventName, object id) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, eventName, null, null, [id], null));

        /// <summary>
        /// Deletes a single record from the data store by its ID with an event name and source.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="id">The ID of the record to delete.</param>
        void DeleteByID<TModel>(string? eventName, object? source, object id) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, eventName, source, null, [id], null));

        /// <summary>
        /// Deletes a single record from the data store by its ID with a property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="id">The ID of the record to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        void DeleteByID<TModel>(object id, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, null, null, null, [id], graph));

        /// <summary>
        /// Deletes a single record from the data store by its ID with an event name and property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="id">The ID of the record to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        void DeleteByID<TModel>(string? eventName, object id, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, eventName, null, null, [id], graph));

        /// <summary>
        /// Deletes a single record from the data store by its ID with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="id">The ID of the record to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        void DeleteByID<TModel>(string? eventName, object? source, object id, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, eventName, source, null, [id], graph));

        /// <summary>
        /// Deletes a single record from the data store by its ID using a pre-built event.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="id">The ID of the record to delete.</param>
        void DeleteByID<TModel>(PersistEvent @event, object id) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, @event, null, [id], null));

        /// <summary>
        /// Deletes a single record from the data store by its ID using a pre-built event with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="id">The ID of the record to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        void DeleteByID<TModel>(PersistEvent @event, object id, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, @event, null, [id], graph));

        /// <summary>
        /// Deletes multiple records from the data store by their IDs.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="ids">The IDs of the records to delete.</param>
        void DeleteByID<TModel>(ICollection ids) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, null, null, null, ids.Cast<object>().ToArray(), null));

        /// <summary>
        /// Deletes multiple records from the data store by their IDs with an event name.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="ids">The IDs of the records to delete.</param>
        void DeleteByID<TModel>(string? eventName, ICollection ids) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, eventName, null, null, ids.Cast<object>().ToArray(), null));

        /// <summary>
        /// Deletes multiple records from the data store by their IDs with an event name and source.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="ids">The IDs of the records to delete.</param>
        void DeleteByID<TModel>(string? eventName, object? source, ICollection ids) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, eventName, source, null, ids.Cast<object>().ToArray(), null));

        /// <summary>
        /// Deletes multiple records from the data store by their IDs with a property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="ids">The IDs of the records to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        void DeleteByID<TModel>(ICollection ids, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, null, null, null, ids.Cast<object>().ToArray(), graph));

        /// <summary>
        /// Deletes multiple records from the data store by their IDs with an event name and property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="ids">The IDs of the records to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        void DeleteByID<TModel>(string? eventName, ICollection ids, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, eventName, null, null, ids.Cast<object>().ToArray(), graph));

        /// <summary>
        /// Deletes multiple records from the data store by their IDs with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="ids">The IDs of the records to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        void DeleteByID<TModel>(string? eventName, object? source, ICollection ids, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, eventName, source, null, ids.Cast<object>().ToArray(), graph));

        /// <summary>
        /// Deletes multiple records from the data store by their IDs using a pre-built event.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="ids">The IDs of the records to delete.</param>
        void DeleteByID<TModel>(PersistEvent @event, ICollection ids) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, @event, null, ids.Cast<object>().ToArray(), null));

        /// <summary>
        /// Deletes multiple records from the data store by their IDs using a pre-built event with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="ids">The IDs of the records to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        void DeleteByID<TModel>(PersistEvent @event, ICollection ids, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, @event, null, ids.Cast<object>().ToArray(), graph));

        /// <summary>
        /// Asynchronously deletes a single record from the data store by its ID.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="id">The ID of the record to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteByIDAsync<TModel>(object id) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, null, null, null, [id], null));

        /// <summary>
        /// Asynchronously deletes a single record from the data store by its ID with an event name.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="id">The ID of the record to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteByIDAsync<TModel>(string? eventName, object id) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, eventName, null, null, [id], null));

        /// <summary>
        /// Asynchronously deletes a single record from the data store by its ID with an event name and source.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="id">The ID of the record to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteByIDAsync<TModel>(string? eventName, object? source, object id) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, eventName, source, null, [id], null));

        /// <summary>
        /// Asynchronously deletes a single record from the data store by its ID with a property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="id">The ID of the record to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteByIDAsync<TModel>(object id, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, null, null, null, [id], graph));

        /// <summary>
        /// Asynchronously deletes a single record from the data store by its ID with an event name and property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="id">The ID of the record to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteByIDAsync<TModel>(string? eventName, object id, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, eventName, null, null, [id], graph));

        /// <summary>
        /// Asynchronously deletes a single record from the data store by its ID with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="id">The ID of the record to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteByIDAsync<TModel>(string? eventName, object? source, object id, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, eventName, source, null, [id], graph));

        /// <summary>
        /// Asynchronously deletes a single record from the data store by its ID using a pre-built event.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="id">The ID of the record to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteByIDAsync<TModel>(PersistEvent @event, object id) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, @event, null, [id], null));

        /// <summary>
        /// Asynchronously deletes a single record from the data store by its ID using a pre-built event with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="id">The ID of the record to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteByIDAsync<TModel>(PersistEvent @event, object id, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, @event, null, [id], graph));

        /// <summary>
        /// Asynchronously deletes multiple records from the data store by their IDs.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="ids">The IDs of the records to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteByIDAsync<TModel>(ICollection ids) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, null, null, null, ids.Cast<object>().ToArray(), null));

        /// <summary>
        /// Asynchronously deletes multiple records from the data store by their IDs with an event name.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="ids">The IDs of the records to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteByIDAsync<TModel>(string? eventName, ICollection ids) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, eventName, null, null, ids.Cast<object>().ToArray(), null));

        /// <summary>
        /// Asynchronously deletes multiple records from the data store by their IDs with an event name and source.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="ids">The IDs of the records to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteByIDAsync<TModel>(string? eventName, object? source, ICollection ids) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, eventName, source, null, ids.Cast<object>().ToArray(), null));

        /// <summary>
        /// Asynchronously deletes multiple records from the data store by their IDs with a property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="ids">The IDs of the records to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteByIDAsync<TModel>(ICollection ids, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, null, null, null, ids.Cast<object>().ToArray(), graph));

        /// <summary>
        /// Asynchronously deletes multiple records from the data store by their IDs with an event name and property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="ids">The IDs of the records to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteByIDAsync<TModel>(string? eventName, ICollection ids, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, eventName, null, null, ids.Cast<object>().ToArray(), graph));

        /// <summary>
        /// Asynchronously deletes multiple records from the data store by their IDs with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="ids">The IDs of the records to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteByIDAsync<TModel>(string? eventName, object? source, ICollection ids, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, eventName, source, null, ids.Cast<object>().ToArray(), graph));

        /// <summary>
        /// Asynchronously deletes multiple records from the data store by their IDs using a pre-built event.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="ids">The IDs of the records to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteByIDAsync<TModel>(PersistEvent @event, ICollection ids) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, @event, null, ids.Cast<object>().ToArray(), null));

        /// <summary>
        /// Asynchronously deletes multiple records from the data store by their IDs using a pre-built event with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="ids">The IDs of the records to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteByIDAsync<TModel>(PersistEvent @event, ICollection ids, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, @event, null, ids.Cast<object>().ToArray(), graph));
    }
}

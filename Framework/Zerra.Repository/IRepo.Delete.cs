// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>
        /// Deletes a single model from the data store.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="model">The model instance to delete.</param>
        void Delete<TModel>(TModel model) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, null, null, [model], null, null));

        /// <summary>
        /// Deletes a single model from the data store with an event name.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="model">The model instance to delete.</param>
        void Delete<TModel>(string? eventName, TModel model) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, eventName, null, [model], null, null));

        /// <summary>
        /// Deletes a single model from the data store with an event name and source.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="model">The model instance to delete.</param>
        void Delete<TModel>(string? eventName, object? source, TModel model) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, eventName, source, [model], null, null));

        /// <summary>
        /// Deletes a single model from the data store with a property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="model">The model instance to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        void Delete<TModel>(TModel model, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, null, null, [model], null, graph));

        /// <summary>
        /// Deletes a single model from the data store with an event name and property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="model">The model instance to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        void Delete<TModel>(string? eventName, TModel model, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, eventName, null, [model], null, graph));

        /// <summary>
        /// Deletes a single model from the data store with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="model">The model instance to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        void Delete<TModel>(string? eventName, object? source, TModel model, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, eventName, source, [model], null, graph));

        /// <summary>
        /// Deletes a single model from the data store using a pre-built event.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="model">The model instance to delete.</param>
        void Delete<TModel>(PersistEvent @event, TModel model) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, @event, [model], null, null));

        /// <summary>
        /// Deletes a single model from the data store using a pre-built event with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="model">The model instance to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        void Delete<TModel>(PersistEvent @event, TModel model, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, @event, [model], null, graph));

        /// <summary>
        /// Deletes multiple models from the data store.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="models">The model instances to delete.</param>
        void Delete<TModel>(IEnumerable<TModel> models) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, null, null, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Deletes multiple models from the data store with an event name.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="models">The model instances to delete.</param>
        void Delete<TModel>(string? eventName, IEnumerable<TModel> models) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, eventName, null, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Deletes multiple models from the data store with an event name and source.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="models">The model instances to delete.</param>
        void Delete<TModel>(string? eventName, object? source, IEnumerable<TModel> models) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, eventName, source, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Deletes multiple models from the data store with a property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="models">The model instances to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        void Delete<TModel>(IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, null, null, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Deletes multiple models from the data store with an event name and property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="models">The model instances to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        void Delete<TModel>(string? eventName, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, eventName, null, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Deletes multiple models from the data store with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="models">The model instances to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        void Delete<TModel>(string? eventName, object? source, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, eventName, source, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Deletes multiple models from the data store using a pre-built event.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="models">The model instances to delete.</param>
        void Delete<TModel>(PersistEvent @event, IEnumerable<TModel> models) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, @event, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Deletes multiple models from the data store using a pre-built event with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="models">The model instances to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        void Delete<TModel>(PersistEvent @event, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Delete, @event, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Asynchronously deletes a single model from the data store.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="model">The model instance to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync<TModel>(TModel model) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, null, null, [model], null, null));

        /// <summary>
        /// Asynchronously deletes a single model from the data store with an event name.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="model">The model instance to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync<TModel>(string? eventName, TModel model) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, eventName, null, [model], null, null));

        /// <summary>
        /// Asynchronously deletes a single model from the data store with an event name and source.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="model">The model instance to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync<TModel>(string? eventName, object? source, TModel model) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, eventName, source, [model], null, null));

        /// <summary>
        /// Asynchronously deletes a single model from the data store with a property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="model">The model instance to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync<TModel>(TModel model, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, null, null, [model], null, graph));

        /// <summary>
        /// Asynchronously deletes a single model from the data store with an event name and property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="model">The model instance to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync<TModel>(string? eventName, TModel model, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, eventName, null, [model], null, graph));

        /// <summary>
        /// Asynchronously deletes a single model from the data store with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="model">The model instance to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync<TModel>(string? eventName, object? source, TModel model, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, eventName, source, [model], null, graph));

        /// <summary>
        /// Asynchronously deletes a single model from the data store using a pre-built event.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="model">The model instance to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync<TModel>(PersistEvent @event, TModel model) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, @event, [model], null, null));

        /// <summary>
        /// Asynchronously deletes a single model from the data store using a pre-built event with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="model">The model instance to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync<TModel>(PersistEvent @event, TModel model, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, @event, [model], null, graph));

        /// <summary>
        /// Asynchronously deletes multiple models from the data store.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="models">The model instances to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync<TModel>(IEnumerable<TModel> models) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, null, null, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Asynchronously deletes multiple models from the data store with an event name.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="models">The model instances to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync<TModel>(string? eventName, IEnumerable<TModel> models) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, eventName, null, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Asynchronously deletes multiple models from the data store with an event name and source.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="models">The model instances to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync<TModel>(string? eventName, object? source, IEnumerable<TModel> models) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, eventName, source, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Asynchronously deletes multiple models from the data store with a property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="models">The model instances to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync<TModel>(IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, null, null, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Asynchronously deletes multiple models from the data store with an event name and property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="models">The model instances to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync<TModel>(string? eventName, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, eventName, null, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Asynchronously deletes multiple models from the data store with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="models">The model instances to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync<TModel>(string? eventName, object? source, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, eventName, source, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Asynchronously deletes multiple models from the data store using a pre-built event.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="models">The model instances to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync<TModel>(PersistEvent @event, IEnumerable<TModel> models) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, @event, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Asynchronously deletes multiple models from the data store using a pre-built event with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being deleted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="models">The model instances to delete.</param>
        /// <param name="graph">The property graph specifying which members to consider during deletion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync<TModel>(PersistEvent @event, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Delete, @event, models.Cast<object>().ToArray(), null, graph));
    }
}

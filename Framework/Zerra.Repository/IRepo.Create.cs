// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>
        /// Inserts a single model into the data store.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="model">The model instance to insert.</param>
        void Create<TModel>(TModel model) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Create, null, null, [model], null, null));

        /// <summary>
        /// Inserts a single model into the data store with an event name.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="model">The model instance to insert.</param>
        void Create<TModel>(string? eventName, TModel model) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Create, eventName, null, [model], null, null));

        /// <summary>
        /// Inserts a single model into the data store with an event name and source.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="model">The model instance to insert.</param>
        void Create<TModel>(string? eventName, object? source, TModel model) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Create, eventName, source, [model], null, null));

        /// <summary>
        /// Inserts a single model into the data store with a property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="model">The model instance to insert.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        void Create<TModel>(TModel model, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Create, null, null, [model], null, graph));

        /// <summary>
        /// Inserts a single model into the data store with an event name and property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="model">The model instance to insert.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        void Create<TModel>(string? eventName, TModel model, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Create, eventName, null, [model], null, graph));

        /// <summary>
        /// Inserts a single model into the data store with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="model">The model instance to insert.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        void Create<TModel>(string? eventName, object? source, TModel model, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Create, eventName, source, [model], null, graph));

        /// <summary>
        /// Inserts a single model into the data store using a pre-built event.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="model">The model instance to insert.</param>
        void Create<TModel>(PersistEvent @event, TModel model) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Create, @event, [model], null, null));

        /// <summary>
        /// Inserts a single model into the data store using a pre-built event with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="model">The model instance to insert.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        void Create<TModel>(PersistEvent @event, TModel model, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Create, @event, [model], null, graph));

        /// <summary>
        /// Inserts multiple models into the data store.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="models">The model instances to insert.</param>
        void Create<TModel>(IEnumerable<TModel> models) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Create, null, null, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Inserts multiple models into the data store with an event name.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="models">The model instances to insert.</param>
        void Create<TModel>(string? eventName, IEnumerable<TModel> models) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Create, eventName, null, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Inserts multiple models into the data store with an event name and source.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="models">The model instances to insert.</param>
        void Create<TModel>(string? eventName, object? source, IEnumerable<TModel> models) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Create, eventName, source, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Inserts multiple models into the data store with a property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="models">The model instances to insert.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        void Create<TModel>(IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Create, null, null, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Inserts multiple models into the data store with an event name and property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="models">The model instances to insert.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        void Create<TModel>(string? eventName, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Create, eventName, null, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Inserts multiple models into the data store with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="models">The model instances to insert.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        void Create<TModel>(string? eventName, object? source, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Create, eventName, source, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Inserts multiple models into the data store using a pre-built event.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="models">The model instances to insert.</param>
        void Create<TModel>(PersistEvent @event, IEnumerable<TModel> models) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Create, @event, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Inserts multiple models into the data store using a pre-built event with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="models">The model instances to insert.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        void Create<TModel>(PersistEvent @event, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Create, @event, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Asynchronously inserts a single model into the data store.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="model">The model instance to insert.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateAsync<TModel>(TModel model) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Create, null, null, [model], null, null));

        /// <summary>
        /// Asynchronously inserts a single model into the data store with an event name.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="model">The model instance to insert.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateAsync<TModel>(string? eventName, TModel model) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Create, eventName, null, [model], null, null));

        /// <summary>
        /// Asynchronously inserts a single model into the data store with an event name and source.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="model">The model instance to insert.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateAsync<TModel>(string? eventName, object? source, TModel model) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Create, eventName, source, [model], null, null));

        /// <summary>
        /// Asynchronously inserts a single model into the data store with a property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="model">The model instance to insert.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateAsync<TModel>(TModel model, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Create, null, null, [model], null, graph));

        /// <summary>
        /// Asynchronously inserts a single model into the data store with an event name and property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="model">The model instance to insert.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateAsync<TModel>(string? eventName, TModel model, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Create, eventName, null, [model], null, graph));

        /// <summary>
        /// Asynchronously inserts a single model into the data store with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="model">The model instance to insert.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateAsync<TModel>(string? eventName, object? source, TModel model, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Create, eventName, source, [model], null, graph));

        /// <summary>
        /// Asynchronously inserts a single model into the data store using a pre-built event.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="model">The model instance to insert.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateAsync<TModel>(PersistEvent @event, TModel model) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Create, @event, [model], null, null));

        /// <summary>
        /// Asynchronously inserts a single model into the data store using a pre-built event with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="model">The model instance to insert.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateAsync<TModel>(PersistEvent @event, TModel model, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Create, @event, [model], null, graph));

        /// <summary>
        /// Asynchronously inserts multiple models into the data store.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="models">The model instances to insert.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateAsync<TModel>(IEnumerable<TModel> models) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Create, null, null, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Asynchronously inserts multiple models into the data store with an event name.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="models">The model instances to insert.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateAsync<TModel>(string? eventName, IEnumerable<TModel> models) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Create, eventName, null, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Asynchronously inserts multiple models into the data store with an event name and source.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="models">The model instances to insert.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateAsync<TModel>(string? eventName, object? source, IEnumerable<TModel> models) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Create, eventName, source, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Asynchronously inserts multiple models into the data store with a property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="models">The model instances to insert.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateAsync<TModel>(IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Create, null, null, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Asynchronously inserts multiple models into the data store with an event name and property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="models">The model instances to insert.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateAsync<TModel>(string? eventName, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Create, eventName, null, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Asynchronously inserts multiple models into the data store with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="models">The model instances to insert.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateAsync<TModel>(string? eventName, object? source, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Create, eventName, source, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Asynchronously inserts multiple models into the data store using a pre-built event.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="models">The model instances to insert.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateAsync<TModel>(PersistEvent @event, IEnumerable<TModel> models) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Create, @event, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Asynchronously inserts multiple models into the data store using a pre-built event with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being inserted.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="models">The model instances to insert.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateAsync<TModel>(PersistEvent @event, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Create, @event, models.Cast<object>().ToArray(), null, graph));
    }
}

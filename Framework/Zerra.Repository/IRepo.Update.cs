// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>
        /// Updates a single model in the data store.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="model">The model instance to update.</param>
        void Update<TModel>(TModel model) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Update, null, null, [model], null, null));

        /// <summary>
        /// Updates a single model in the data store with an event name.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="model">The model instance to update.</param>
        void Update<TModel>(string? eventName, TModel model) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Update, eventName, null, [model], null, null));

        /// <summary>
        /// Updates a single model in the data store with an event name and source.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="model">The model instance to update.</param>
        void Update<TModel>(string? eventName, object? source, TModel model) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Update, eventName, source, [model], null, null));

        /// <summary>
        /// Updates a single model in the data store with a property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="model">The model instance to update.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        void Update<TModel>(TModel model, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Update, null, null, [model], null, graph));

        /// <summary>
        /// Updates a single model in the data store with an event name and property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="model">The model instance to update.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        void Update<TModel>(string? eventName, TModel model, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Update, eventName, null, [model], null, graph));

        /// <summary>
        /// Updates a single model in the data store with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="model">The model instance to update.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        void Update<TModel>(string? eventName, object? source, TModel model, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Update, eventName, source, [model], null, graph));

        /// <summary>
        /// Updates a single model in the data store using a pre-built event.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="model">The model instance to update.</param>
        void Update<TModel>(PersistEvent @event, TModel model) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Update, @event, [model], null, null));

        /// <summary>
        /// Updates a single model in the data store using a pre-built event with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="model">The model instance to update.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        void Update<TModel>(PersistEvent @event, TModel model, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Update, @event, [model], null, graph));

        /// <summary>
        /// Updates multiple models in the data store.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="models">The model instances to update.</param>
        void Update<TModel>(IEnumerable<TModel> models) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Update, null, null, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Updates multiple models in the data store with an event name.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="models">The model instances to update.</param>
        void Update<TModel>(string? eventName, IEnumerable<TModel> models) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Update, eventName, null, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Updates multiple models in the data store with an event name and source.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="models">The model instances to update.</param>
        void Update<TModel>(string? eventName, object? source, IEnumerable<TModel> models) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Update, eventName, source, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Updates multiple models in the data store with a property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="models">The model instances to update.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        void Update<TModel>(IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Update, null, null, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Updates multiple models in the data store with an event name and property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="models">The model instances to update.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        void Update<TModel>(string? eventName, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Update, eventName, null, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Updates multiple models in the data store with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="models">The model instances to update.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        void Update<TModel>(string? eventName, object? source, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Update, eventName, source, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Updates multiple models in the data store using a pre-built event.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="models">The model instances to update.</param>
        void Update<TModel>(PersistEvent @event, IEnumerable<TModel> models) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Update, @event, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Updates multiple models in the data store using a pre-built event with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="models">The model instances to update.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        void Update<TModel>(PersistEvent @event, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => Persist(new Persist<TModel>(PersistOperation.Update, @event, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Asynchronously updates a single model in the data store.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="model">The model instance to update.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateAsync<TModel>(TModel model) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Update, null, null, [model], null, null));

        /// <summary>
        /// Asynchronously updates a single model in the data store with an event name.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="model">The model instance to update.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateAsync<TModel>(string? eventName, TModel model) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Update, eventName, null, [model], null, null));

        /// <summary>
        /// Asynchronously updates a single model in the data store with an event name and source.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="model">The model instance to update.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateAsync<TModel>(string? eventName, object? source, TModel model) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Update, eventName, source, [model], null, null));

        /// <summary>
        /// Asynchronously updates a single model in the data store with a property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="model">The model instance to update.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateAsync<TModel>(TModel model, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Update, null, null, [model], null, graph));

        /// <summary>
        /// Asynchronously updates a single model in the data store with an event name and property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="model">The model instance to update.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateAsync<TModel>(string? eventName, TModel model, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Update, eventName, null, [model], null, graph));

        /// <summary>
        /// Asynchronously updates a single model in the data store with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="model">The model instance to update.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateAsync<TModel>(string? eventName, object? source, TModel model, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Update, eventName, source, [model], null, graph));

        /// <summary>
        /// Asynchronously updates a single model in the data store using a pre-built event.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="model">The model instance to update.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateAsync<TModel>(PersistEvent @event, TModel model) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Update, @event, [model], null, null));

        /// <summary>
        /// Asynchronously updates a single model in the data store using a pre-built event with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="model">The model instance to update.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateAsync<TModel>(PersistEvent @event, TModel model, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Update, @event, [model], null, graph));

        /// <summary>
        /// Asynchronously updates multiple models in the data store.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="models">The model instances to update.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateAsync<TModel>(IEnumerable<TModel> models) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Update, null, null, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Asynchronously updates multiple models in the data store with an event name.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="models">The model instances to update.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateAsync<TModel>(string? eventName, IEnumerable<TModel> models) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Update, eventName, null, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Asynchronously updates multiple models in the data store with an event name and source.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="models">The model instances to update.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateAsync<TModel>(string? eventName, object? source, IEnumerable<TModel> models) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Update, eventName, source, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Asynchronously updates multiple models in the data store with a property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="models">The model instances to update.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateAsync<TModel>(IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Update, null, null, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Asynchronously updates multiple models in the data store with an event name and property graph.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="models">The model instances to update.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateAsync<TModel>(string? eventName, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Update, eventName, null, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Asynchronously updates multiple models in the data store with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="eventName">An optional human-readable event name; defaults to a generated name when <see langword="null"/> or whitespace.</param>
        /// <param name="source">An optional object that initiated this operation.</param>
        /// <param name="models">The model instances to update.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateAsync<TModel>(string? eventName, object? source, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Update, eventName, source, models.Cast<object>().ToArray(), null, graph));

        /// <summary>
        /// Asynchronously updates multiple models in the data store using a pre-built event.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="models">The model instances to update.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateAsync<TModel>(PersistEvent @event, IEnumerable<TModel> models) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Update, @event, models.Cast<object>().ToArray(), null, null));

        /// <summary>
        /// Asynchronously updates multiple models in the data store using a pre-built event with full parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of model being updated.</typeparam>
        /// <param name="event">The pre-built event metadata to associate with this operation.</param>
        /// <param name="models">The model instances to update.</param>
        /// <param name="graph">The property graph specifying which members to persist.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateAsync<TModel>(PersistEvent @event, IEnumerable<TModel> models, Graph? graph) where TModel : class, new()
            => PersistAsync(new Persist<TModel>(PersistOperation.Update, @event, models.Cast<object>().ToArray(), null, graph));
    }
}

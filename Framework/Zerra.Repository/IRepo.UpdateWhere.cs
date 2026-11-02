// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>Sets the members named by <paramref name="graph"/> to the values in <paramref name="model"/> on every model matching <paramref name="where"/>, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="model">A model holding the values to set.</param>
        /// <param name="graph">The members to set. Identity and relation members can't be set.</param>
        /// <returns>How many models were updated.</returns>
        int Update<TModel>(Expression<Func<TModel, bool>> where, TModel model, Graph<TModel> graph) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, null, null, where, model, graph);
            ((IRepoInternal)this).Persist(persist);
            return persist.RowsAffected;
        }

        /// <summary>Sets the members named by <paramref name="graph"/> to the values in <paramref name="model"/> on every model matching <paramref name="where"/>, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="eventName">The name of the persist event, or <see langword="null"/> for a default.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="model">A model holding the values to set.</param>
        /// <param name="graph">The members to set. Identity and relation members can't be set.</param>
        /// <returns>How many models were updated.</returns>
        int Update<TModel>(string? eventName, Expression<Func<TModel, bool>> where, TModel model, Graph<TModel> graph) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, eventName, null, where, model, graph);
            ((IRepoInternal)this).Persist(persist);
            return persist.RowsAffected;
        }

        /// <summary>Sets the members named by <paramref name="graph"/> to the values in <paramref name="model"/> on every model matching <paramref name="where"/>, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="eventName">The name of the persist event, or <see langword="null"/> for a default.</param>
        /// <param name="source">The source object that triggered the update, or <see langword="null"/>.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="model">A model holding the values to set.</param>
        /// <param name="graph">The members to set. Identity and relation members can't be set.</param>
        /// <returns>How many models were updated.</returns>
        int Update<TModel>(string? eventName, object? source, Expression<Func<TModel, bool>> where, TModel model, Graph<TModel> graph) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, eventName, source, where, model, graph);
            ((IRepoInternal)this).Persist(persist);
            return persist.RowsAffected;
        }

        /// <summary>Sets the members named by <paramref name="graph"/> to the values in <paramref name="model"/> on every model matching <paramref name="where"/>, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="event">The persist event.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="model">A model holding the values to set.</param>
        /// <param name="graph">The members to set. Identity and relation members can't be set.</param>
        /// <returns>How many models were updated.</returns>
        int Update<TModel>(PersistEvent @event, Expression<Func<TModel, bool>> where, TModel model, Graph<TModel> graph) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, @event, where, model, graph);
            ((IRepoInternal)this).Persist(persist);
            return persist.RowsAffected;
        }

        /// <summary>Asynchronously sets the members named by <paramref name="graph"/> to the values in <paramref name="model"/> on every model matching <paramref name="where"/>, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="model">A model holding the values to set.</param>
        /// <param name="graph">The members to set. Identity and relation members can't be set.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task that represents the asynchronous operation. How many models were updated.</returns>
        async Task<int> UpdateAsync<TModel>(Expression<Func<TModel, bool>> where, TModel model, Graph<TModel> graph, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, null, null, where, model, graph);
            await ((IRepoInternal)this).PersistAsync(persist, cancellationToken);
            return persist.RowsAffected;
        }

        /// <summary>Asynchronously sets the members named by <paramref name="graph"/> to the values in <paramref name="model"/> on every model matching <paramref name="where"/>, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="eventName">The name of the persist event, or <see langword="null"/> for a default.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="model">A model holding the values to set.</param>
        /// <param name="graph">The members to set. Identity and relation members can't be set.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task that represents the asynchronous operation. How many models were updated.</returns>
        async Task<int> UpdateAsync<TModel>(string? eventName, Expression<Func<TModel, bool>> where, TModel model, Graph<TModel> graph, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, eventName, null, where, model, graph);
            await ((IRepoInternal)this).PersistAsync(persist, cancellationToken);
            return persist.RowsAffected;
        }

        /// <summary>Asynchronously sets the members named by <paramref name="graph"/> to the values in <paramref name="model"/> on every model matching <paramref name="where"/>, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="eventName">The name of the persist event, or <see langword="null"/> for a default.</param>
        /// <param name="source">The source object that triggered the update, or <see langword="null"/>.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="model">A model holding the values to set.</param>
        /// <param name="graph">The members to set. Identity and relation members can't be set.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task that represents the asynchronous operation. How many models were updated.</returns>
        async Task<int> UpdateAsync<TModel>(string? eventName, object? source, Expression<Func<TModel, bool>> where, TModel model, Graph<TModel> graph, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, eventName, source, where, model, graph);
            await ((IRepoInternal)this).PersistAsync(persist, cancellationToken);
            return persist.RowsAffected;
        }

        /// <summary>Asynchronously sets the members named by <paramref name="graph"/> to the values in <paramref name="model"/> on every model matching <paramref name="where"/>, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="event">The persist event.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="model">A model holding the values to set.</param>
        /// <param name="graph">The members to set. Identity and relation members can't be set.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task that represents the asynchronous operation. How many models were updated.</returns>
        async Task<int> UpdateAsync<TModel>(PersistEvent @event, Expression<Func<TModel, bool>> where, TModel model, Graph<TModel> graph, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, @event, where, model, graph);
            await ((IRepoInternal)this).PersistAsync(persist, cancellationToken);
            return persist.RowsAffected;
        }

        /// <summary>Sets members on every model matching <paramref name="where"/> to values computed from each one, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="set">The members to set and their values, as <c>x => new TModel { Price = x.Price * 1.1m, Status = "Active" }</c>. Identity and relation members can't be set, and a value reads only the model's own members.</param>
        /// <returns>How many models were updated.</returns>
        int Update<TModel>(Expression<Func<TModel, bool>> where, Expression<Func<TModel, TModel>> set) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, null, null, where, set);
            ((IRepoInternal)this).Persist(persist);
            return persist.RowsAffected;
        }

        /// <summary>Asynchronously sets members on every model matching <paramref name="where"/> to values computed from each one, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="set">The members to set and their values, as <c>x => new TModel { Price = x.Price * 1.1m, Status = "Active" }</c>. Identity and relation members can't be set, and a value reads only the model's own members.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task that represents the asynchronous operation. How many models were updated.</returns>
        async Task<int> UpdateAsync<TModel>(Expression<Func<TModel, bool>> where, Expression<Func<TModel, TModel>> set, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, null, null, where, set);
            await ((IRepoInternal)this).PersistAsync(persist, cancellationToken);
            return persist.RowsAffected;
        }

        /// <summary>Sets members on every model matching <paramref name="where"/> to values computed from each one, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="eventName">The name of the persist event, or <see langword="null"/> for a default.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="set">The members to set and their values, as <c>x => new TModel { Price = x.Price * 1.1m, Status = "Active" }</c>. Identity and relation members can't be set, and a value reads only the model's own members.</param>
        /// <returns>How many models were updated.</returns>
        int Update<TModel>(string? eventName, Expression<Func<TModel, bool>> where, Expression<Func<TModel, TModel>> set) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, eventName, null, where, set);
            ((IRepoInternal)this).Persist(persist);
            return persist.RowsAffected;
        }

        /// <summary>Asynchronously sets members on every model matching <paramref name="where"/> to values computed from each one, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="eventName">The name of the persist event, or <see langword="null"/> for a default.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="set">The members to set and their values, as <c>x => new TModel { Price = x.Price * 1.1m, Status = "Active" }</c>. Identity and relation members can't be set, and a value reads only the model's own members.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task that represents the asynchronous operation. How many models were updated.</returns>
        async Task<int> UpdateAsync<TModel>(string? eventName, Expression<Func<TModel, bool>> where, Expression<Func<TModel, TModel>> set, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, eventName, null, where, set);
            await ((IRepoInternal)this).PersistAsync(persist, cancellationToken);
            return persist.RowsAffected;
        }

        /// <summary>Sets members on every model matching <paramref name="where"/> to values computed from each one, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="eventName">The name of the persist event, or <see langword="null"/> for a default.</param>
        /// <param name="source">The source object that triggered the update, or <see langword="null"/>.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="set">The members to set and their values, as <c>x => new TModel { Price = x.Price * 1.1m, Status = "Active" }</c>. Identity and relation members can't be set, and a value reads only the model's own members.</param>
        /// <returns>How many models were updated.</returns>
        int Update<TModel>(string? eventName, object? source, Expression<Func<TModel, bool>> where, Expression<Func<TModel, TModel>> set) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, eventName, source, where, set);
            ((IRepoInternal)this).Persist(persist);
            return persist.RowsAffected;
        }

        /// <summary>Asynchronously sets members on every model matching <paramref name="where"/> to values computed from each one, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="eventName">The name of the persist event, or <see langword="null"/> for a default.</param>
        /// <param name="source">The source object that triggered the update, or <see langword="null"/>.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="set">The members to set and their values, as <c>x => new TModel { Price = x.Price * 1.1m, Status = "Active" }</c>. Identity and relation members can't be set, and a value reads only the model's own members.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task that represents the asynchronous operation. How many models were updated.</returns>
        async Task<int> UpdateAsync<TModel>(string? eventName, object? source, Expression<Func<TModel, bool>> where, Expression<Func<TModel, TModel>> set, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, eventName, source, where, set);
            await ((IRepoInternal)this).PersistAsync(persist, cancellationToken);
            return persist.RowsAffected;
        }

        /// <summary>Sets members on every model matching <paramref name="where"/> to values computed from each one, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="event">The persist event.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="set">The members to set and their values, as <c>x => new TModel { Price = x.Price * 1.1m, Status = "Active" }</c>. Identity and relation members can't be set, and a value reads only the model's own members.</param>
        /// <returns>How many models were updated.</returns>
        int Update<TModel>(PersistEvent @event, Expression<Func<TModel, bool>> where, Expression<Func<TModel, TModel>> set) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, @event, where, set);
            ((IRepoInternal)this).Persist(persist);
            return persist.RowsAffected;
        }

        /// <summary>Asynchronously sets members on every model matching <paramref name="where"/> to values computed from each one, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="event">The persist event.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="set">The members to set and their values, as <c>x => new TModel { Price = x.Price * 1.1m, Status = "Active" }</c>. Identity and relation members can't be set, and a value reads only the model's own members.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task that represents the asynchronous operation. How many models were updated.</returns>
        async Task<int> UpdateAsync<TModel>(PersistEvent @event, Expression<Func<TModel, bool>> where, Expression<Func<TModel, TModel>> set, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, @event, where, set);
            await ((IRepoInternal)this).PersistAsync(persist, cancellationToken);
            return persist.RowsAffected;
        }
    }
}

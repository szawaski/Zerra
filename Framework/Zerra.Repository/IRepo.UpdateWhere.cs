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
            Persist(persist);
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
            Persist(persist);
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
            Persist(persist);
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
            Persist(persist);
            return persist.RowsAffected;
        }

        /// <summary>Asynchronously sets the members named by <paramref name="graph"/> to the values in <paramref name="model"/> on every model matching <paramref name="where"/>, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="model">A model holding the values to set.</param>
        /// <param name="graph">The members to set. Identity and relation members can't be set.</param>
        /// <returns>A task that represents the asynchronous operation. How many models were updated.</returns>
        async Task<int> UpdateAsync<TModel>(Expression<Func<TModel, bool>> where, TModel model, Graph<TModel> graph) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, null, null, where, model, graph);
            await PersistAsync(persist);
            return persist.RowsAffected;
        }

        /// <summary>Asynchronously sets the members named by <paramref name="graph"/> to the values in <paramref name="model"/> on every model matching <paramref name="where"/>, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="eventName">The name of the persist event, or <see langword="null"/> for a default.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="model">A model holding the values to set.</param>
        /// <param name="graph">The members to set. Identity and relation members can't be set.</param>
        /// <returns>A task that represents the asynchronous operation. How many models were updated.</returns>
        async Task<int> UpdateAsync<TModel>(string? eventName, Expression<Func<TModel, bool>> where, TModel model, Graph<TModel> graph) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, eventName, null, where, model, graph);
            await PersistAsync(persist);
            return persist.RowsAffected;
        }

        /// <summary>Asynchronously sets the members named by <paramref name="graph"/> to the values in <paramref name="model"/> on every model matching <paramref name="where"/>, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="eventName">The name of the persist event, or <see langword="null"/> for a default.</param>
        /// <param name="source">The source object that triggered the update, or <see langword="null"/>.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="model">A model holding the values to set.</param>
        /// <param name="graph">The members to set. Identity and relation members can't be set.</param>
        /// <returns>A task that represents the asynchronous operation. How many models were updated.</returns>
        async Task<int> UpdateAsync<TModel>(string? eventName, object? source, Expression<Func<TModel, bool>> where, TModel model, Graph<TModel> graph) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, eventName, source, where, model, graph);
            await PersistAsync(persist);
            return persist.RowsAffected;
        }

        /// <summary>Asynchronously sets the members named by <paramref name="graph"/> to the values in <paramref name="model"/> on every model matching <paramref name="where"/>, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to update.</typeparam>
        /// <param name="event">The persist event.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="model">A model holding the values to set.</param>
        /// <param name="graph">The members to set. Identity and relation members can't be set.</param>
        /// <returns>A task that represents the asynchronous operation. How many models were updated.</returns>
        async Task<int> UpdateAsync<TModel>(PersistEvent @event, Expression<Func<TModel, bool>> where, TModel model, Graph<TModel> graph) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Update, @event, where, model, graph);
            await PersistAsync(persist);
            return persist.RowsAffected;
        }
    }
}

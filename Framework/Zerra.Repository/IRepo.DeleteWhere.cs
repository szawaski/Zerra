// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>Deletes every model matching <paramref name="where"/> in the store, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to delete.</typeparam>
        /// <param name="where">The condition selecting the models.</param>
        /// <returns>How many models were deleted.</returns>
        int Delete<TModel>(Expression<Func<TModel, bool>> where) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Delete, null, null, where, null, null);
            Persist(persist);
            return persist.RowsAffected;
        }

        /// <summary>Deletes every model matching <paramref name="where"/> in the store, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to delete.</typeparam>
        /// <param name="eventName">The name of the persist event, or <see langword="null"/> for a default.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <returns>How many models were deleted.</returns>
        int Delete<TModel>(string? eventName, Expression<Func<TModel, bool>> where) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Delete, eventName, null, where, null, null);
            Persist(persist);
            return persist.RowsAffected;
        }

        /// <summary>Deletes every model matching <paramref name="where"/> in the store, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to delete.</typeparam>
        /// <param name="eventName">The name of the persist event, or <see langword="null"/> for a default.</param>
        /// <param name="source">The source object that triggered the delete, or <see langword="null"/>.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <returns>How many models were deleted.</returns>
        int Delete<TModel>(string? eventName, object? source, Expression<Func<TModel, bool>> where) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Delete, eventName, source, where, null, null);
            Persist(persist);
            return persist.RowsAffected;
        }

        /// <summary>Deletes every model matching <paramref name="where"/> in the store, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to delete.</typeparam>
        /// <param name="event">The persist event.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <returns>How many models were deleted.</returns>
        int Delete<TModel>(PersistEvent @event, Expression<Func<TModel, bool>> where) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Delete, @event, where, null, null);
            Persist(persist);
            return persist.RowsAffected;
        }

        /// <summary>Asynchronously deletes every model matching <paramref name="where"/> in the store, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to delete.</typeparam>
        /// <param name="where">The condition selecting the models.</param>
        /// <returns>A task that represents the asynchronous operation. How many models were deleted.</returns>
        async Task<int> DeleteAsync<TModel>(Expression<Func<TModel, bool>> where) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Delete, null, null, where, null, null);
            await PersistAsync(persist);
            return persist.RowsAffected;
        }

        /// <summary>Asynchronously deletes every model matching <paramref name="where"/> in the store, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to delete.</typeparam>
        /// <param name="eventName">The name of the persist event, or <see langword="null"/> for a default.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <returns>A task that represents the asynchronous operation. How many models were deleted.</returns>
        async Task<int> DeleteAsync<TModel>(string? eventName, Expression<Func<TModel, bool>> where) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Delete, eventName, null, where, null, null);
            await PersistAsync(persist);
            return persist.RowsAffected;
        }

        /// <summary>Asynchronously deletes every model matching <paramref name="where"/> in the store, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to delete.</typeparam>
        /// <param name="eventName">The name of the persist event, or <see langword="null"/> for a default.</param>
        /// <param name="source">The source object that triggered the delete, or <see langword="null"/>.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <returns>A task that represents the asynchronous operation. How many models were deleted.</returns>
        async Task<int> DeleteAsync<TModel>(string? eventName, object? source, Expression<Func<TModel, bool>> where) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Delete, eventName, source, where, null, null);
            await PersistAsync(persist);
            return persist.RowsAffected;
        }

        /// <summary>Asynchronously deletes every model matching <paramref name="where"/> in the store, without loading them.</summary>
        /// <typeparam name="TModel">The type of the model to delete.</typeparam>
        /// <param name="event">The persist event.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <returns>A task that represents the asynchronous operation. How many models were deleted.</returns>
        async Task<int> DeleteAsync<TModel>(PersistEvent @event, Expression<Func<TModel, bool>> where) where TModel : class, new()
        {
            var persist = new Persist<TModel>(PersistOperation.Delete, @event, where, null, null);
            await PersistAsync(persist);
            return persist.RowsAffected;
        }
    }
}

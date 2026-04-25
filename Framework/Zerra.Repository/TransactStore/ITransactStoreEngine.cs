// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// Defines the contract for a transact store engine that executes queries and persist operations against a data store.
    /// </summary>
    public interface ITransactStoreEngine : IDataStoreEngine
    {
        /// <summary>Executes a query and returns all matching models.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph specifying which members to populate.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A collection of matching models.</returns>
        IReadOnlyCollection<TModel> ExecuteMany<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Executes a query and returns the first matching model, or <see langword="null"/>.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph specifying which members to populate.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The first matching model, or <see langword="null"/>.</returns>
        TModel? ExecuteFirst<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Executes a query and returns the single matching model, or <see langword="null"/>.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph specifying which members to populate.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The single matching model, or <see langword="null"/>.</returns>
        TModel? ExecuteSingle<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Executes a query and returns the count of matching models.</summary>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The count of matching models.</returns>
        long ExecuteCount(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail);
        /// <summary>Executes a query and returns whether any matching models exist.</summary>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns><see langword="true"/> if any matching model exists; otherwise <see langword="false"/>.</returns>
        bool ExecuteAny(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail);

        /// <summary>Asynchronously executes a query and returns all matching models.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph specifying which members to populate.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing a collection of matching models.</returns>
        Task<IReadOnlyCollection<TModel>> ExecuteManyAsync<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously executes a query and returns the first matching model, or <see langword="null"/>.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph specifying which members to populate.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the first matching model, or <see langword="null"/>.</returns>
        Task<TModel?> ExecuteFirstAsync<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously executes a query and returns the single matching model, or <see langword="null"/>.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph specifying which members to populate.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the single matching model, or <see langword="null"/>.</returns>
        Task<TModel?> ExecuteSingleAsync<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously executes a query and returns the count of matching models.</summary>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the count of matching models.</returns>
        Task<long> ExecuteCountAsync(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail);
        /// <summary>Asynchronously executes a query and returns whether any matching models exist.</summary>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing <see langword="true"/> if any matching model exists; otherwise <see langword="false"/>.</returns>
        Task<bool> ExecuteAnyAsync(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail);

        /// <summary>Executes an insert and returns the auto-generated identities.</summary>
        /// <typeparam name="TModel">The model type to insert.</typeparam>
        /// <param name="model">The model to insert.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A collection of auto-generated identity values.</returns>
        IReadOnlyCollection<object> ExecuteInsertGetIdentities<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Executes an insert and returns the number of rows affected.</summary>
        /// <typeparam name="TModel">The model type to insert.</typeparam>
        /// <param name="model">The model to insert.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The number of rows affected.</returns>
        int ExecuteInsert<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Executes an update and returns the number of rows affected.</summary>
        /// <typeparam name="TModel">The model type to update.</typeparam>
        /// <param name="model">The model to update.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The number of rows affected.</returns>
        int ExecuteUpdate<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Executes a delete for the given identities and returns the number of rows affected.</summary>
        /// <param name="ids">The identities of the models to delete.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The number of rows affected.</returns>
        int ExecuteDelete(ICollection ids, ModelDetail modelDetail);

        /// <summary>Asynchronously executes an insert and returns the auto-generated identities.</summary>
        /// <typeparam name="TModel">The model type to insert.</typeparam>
        /// <param name="model">The model to insert.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing a collection of auto-generated identity values.</returns>
        Task<IReadOnlyCollection<object>> ExecuteInsertGetIdentitiesAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously executes an insert and returns the number of rows affected.</summary>
        /// <typeparam name="TModel">The model type to insert.</typeparam>
        /// <param name="model">The model to insert.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the number of rows affected.</returns>
        Task<int> ExecuteInsertAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously executes an update and returns the number of rows affected.</summary>
        /// <typeparam name="TModel">The model type to update.</typeparam>
        /// <param name="model">The model to update.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the number of rows affected.</returns>
        Task<int> ExecuteUpdateAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously executes a delete for the given identities and returns the number of rows affected.</summary>
        /// <param name="ids">The identities of the models to delete.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the number of rows affected.</returns>
        Task<int> ExecuteDeleteAsync(ICollection ids, ModelDetail modelDetail);
    }
}

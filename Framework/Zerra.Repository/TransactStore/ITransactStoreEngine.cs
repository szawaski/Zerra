// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Zerra.Repository.Linq;
using Zerra.Repository.Reflection;
using Zerra.Repository.Transactions;

namespace Zerra.Repository
{
    /// <summary>
    /// Defines the contract for a transact store engine that executes queries and persist operations against a data store.
    /// </summary>
    public interface ITransactStoreEngine
    {
        /// <summary>
        /// Whether the engine's operations join <see cref="RepoTransaction.Current"/>. A write to an engine that doesn't is refused inside a transaction.
        /// </summary>
        bool SupportsTransactions => false;

        /// <summary>
        /// Runs a LINQ query, converted in one pass to a statement over the model with the relations it reads as statements of their own after the rows, and builds the elements as the plan says.
        /// </summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <typeparam name="TResult">The type of the elements, or of the value the query ends in.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="modelDetail">Metadata about the model being queried.</param>
        /// <returns>The elements as a list of <typeparamref name="TResult"/>, or the value as <typeparamref name="TResult"/> boxed: a count, whether there is any, or an aggregate, <see langword="null"/> when there is none.</returns>
        object? ExecuteLinq<TModel, TResult>(LinqQuery query, ModelDetail modelDetail) where TModel : class, new();

        /// <summary>
        /// Asynchronously runs a LINQ query, converted in one pass to a statement over the model with the relations it reads as statements of their own after the rows, and builds the elements as the plan says.
        /// </summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <typeparam name="TResult">The type of the elements, or of the value the query ends in.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="modelDetail">Metadata about the model being queried.</param>
        /// <param name="cancellationToken">A token that cancels the query.</param>
        /// <returns>A task containing the elements as a list of <typeparamref name="TResult"/>, or the value as <typeparamref name="TResult"/> boxed, <see langword="null"/> when there is none.</returns>
        Task<object?> ExecuteLinqAsync<TModel, TResult>(LinqQuery query, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new();

        /// <summary>
        /// Runs a LINQ query ending in its elements and gives each one as it's read, with the relations the statement reads after the rows loaded first. The connection stays open until the elements are read or the reading stops.
        /// </summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <typeparam name="TResult">The type of the elements.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="modelDetail">Metadata about the model being queried.</param>
        /// <returns>The elements.</returns>
        IEnumerable<TResult> ExecuteLinqStream<TModel, TResult>(LinqQuery query, ModelDetail modelDetail) where TModel : class, new();

        /// <summary>
        /// Runs a LINQ query ending in its elements and gives each one as it's read, with the relations the statement reads after the rows loaded first. The connection stays open until the elements are read or the reading stops.
        /// </summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <typeparam name="TResult">The type of the elements.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="modelDetail">Metadata about the model being queried.</param>
        /// <param name="cancellationToken">A token that cancels the query.</param>
        /// <returns>The elements.</returns>
        IAsyncEnumerable<TResult> ExecuteLinqStreamAsync<TModel, TResult>(LinqQuery query, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new();

        /// <summary>Executes an insert and returns the auto-generated identities.</summary>
        /// <typeparam name="TModel">The model type to insert.</typeparam>
        /// <param name="model">The model to insert.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The auto-generated identity value.</returns>
        object ExecuteInsertGetIdentities<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Executes an insert and returns if it was successful.</summary>
        /// <typeparam name="TModel">The model type to insert.</typeparam>
        /// <param name="model">The model to insert.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>If the insert was successful</returns>
        bool ExecuteInsert<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Executes an update and returns if it was successful.</summary>
        /// <typeparam name="TModel">The model type to update.</typeparam>
        /// <param name="model">The model to update.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>If the update was successful</returns>
        bool ExecuteUpdate<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Executes an insert of many models in as few round trips as the store takes, in one transaction when there isn't one, and returns the auto-generated identities of each model in order, as <see cref="ExecuteInsertGetIdentities"/> returns them.</summary>
        /// <typeparam name="TModel">The model type to insert.</typeparam>
        /// <param name="models">The models to insert.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The auto-generated identities of each model.</returns>
        object[] ExecuteInsertManyGetIdentities<TModel>(object[] models, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var identities = new object[models.Length];
            for (var i = 0; i < models.Length; i++)
                identities[i] = ExecuteInsertGetIdentities((TModel)models[i], graph, modelDetail);
            return identities;
        }
        /// <summary>Executes an insert of many models in as few round trips as the store takes, in one transaction when there isn't one, and returns the number of rows inserted.</summary>
        /// <typeparam name="TModel">The model type to insert.</typeparam>
        /// <param name="models">The models to insert.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The number of rows inserted.</returns>
        int ExecuteInsertMany<TModel>(object[] models, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var count = 0;
            foreach (var model in models)
            {
                if (ExecuteInsert((TModel)model, graph, modelDetail))
                    count++;
            }
            return count;
        }
        /// <summary>Executes an update of many models in as few round trips as the store takes, in one transaction when there isn't one, and returns the number of rows updated.</summary>
        /// <typeparam name="TModel">The model type to update.</typeparam>
        /// <param name="models">The models to update.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The number of rows updated.</returns>
        int ExecuteUpdateMany<TModel>(object[] models, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var count = 0;
            foreach (var model in models)
            {
                if (ExecuteUpdate((TModel)model, graph, modelDetail))
                    count++;
            }
            return count;
        }
        /// <summary>Executes a delete for the given identities and returns the number of rows affected.</summary>
        /// <typeparam name="TModel">The model type to delete.</typeparam>
        /// <param name="ids">The identities of the models to delete.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The number of rows affected.</returns>
        int ExecuteDelete<TModel>(ICollection ids, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Sets the graph's members to the values on every row matching the condition and returns the number of rows affected.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="model">The model holding the values to set.</param>
        /// <param name="graph">The members to set; never an identity or a relation.</param>
        /// <param name="where">The condition selecting the rows.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The number of rows affected.</returns>
        int ExecuteUpdateWhere<TModel>(TModel model, Graph graph, LambdaExpression where, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Sets members on every row matching the condition to values computed from the row and returns the number of rows affected.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="set">The members to set and their values in terms of the row, as a projection of the model to itself for <see cref="ProjectionMember.FromSet"/>; never an identity or a relation, and reading only the model's own members.</param>
        /// <param name="where">The condition selecting the rows.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The number of rows affected.</returns>
        int ExecuteUpdateWhere<TModel>(LambdaExpression set, LambdaExpression where, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Deletes every row matching the condition and returns the number of rows affected.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="where">The condition selecting the rows.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The number of rows affected.</returns>
        int ExecuteDeleteWhere<TModel>(LambdaExpression where, ModelDetail modelDetail) where TModel : class, new();

        /// <summary>Asynchronously executes an insert and returns the auto-generated identities.</summary>
        /// <typeparam name="TModel">The model type to insert.</typeparam>
        /// <param name="model">The model to insert.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task containing the auto-generated identity value.</returns>
        Task<object> ExecuteInsertGetIdentitiesAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new();
        /// <summary>Asynchronously executes an insert and returns if it was successful.</summary>
        /// <typeparam name="TModel">The model type to insert.</typeparam>
        /// <param name="model">The model to insert.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task containing if the insert was successful.</returns>
        Task<bool> ExecuteInsertAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new();
        /// <summary>Asynchronously executes an update and returns if it was successful.</summary>
        /// <typeparam name="TModel">The model type to update.</typeparam>
        /// <param name="model">The model to update.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task containing if the update was successful.</returns>
        Task<bool> ExecuteUpdateAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new();
        /// <summary>Asynchronously executes an insert of many models in as few round trips as the store takes, in one transaction when there isn't one, and returns the auto-generated identities of each model in order, as <see cref="ExecuteInsertGetIdentitiesAsync"/> returns them.</summary>
        /// <typeparam name="TModel">The model type to insert.</typeparam>
        /// <param name="models">The models to insert.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task containing the auto-generated identities of each model.</returns>
        async Task<object[]> ExecuteInsertManyGetIdentitiesAsync<TModel>(object[] models, Graph? graph, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var identities = new object[models.Length];
            for (var i = 0; i < models.Length; i++)
                identities[i] = await ExecuteInsertGetIdentitiesAsync((TModel)models[i], graph, modelDetail, cancellationToken);
            return identities;
        }
        /// <summary>Asynchronously executes an insert of many models in as few round trips as the store takes, in one transaction when there isn't one, and returns the number of rows inserted.</summary>
        /// <typeparam name="TModel">The model type to insert.</typeparam>
        /// <param name="models">The models to insert.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task containing the number of rows inserted.</returns>
        async Task<int> ExecuteInsertManyAsync<TModel>(object[] models, Graph? graph, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var count = 0;
            foreach (var model in models)
            {
                if (await ExecuteInsertAsync((TModel)model, graph, modelDetail, cancellationToken))
                    count++;
            }
            return count;
        }
        /// <summary>Asynchronously executes an update of many models in as few round trips as the store takes, in one transaction when there isn't one, and returns the number of rows updated.</summary>
        /// <typeparam name="TModel">The model type to update.</typeparam>
        /// <param name="models">The models to update.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task containing the number of rows updated.</returns>
        async Task<int> ExecuteUpdateManyAsync<TModel>(object[] models, Graph? graph, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var count = 0;
            foreach (var model in models)
            {
                if (await ExecuteUpdateAsync((TModel)model, graph, modelDetail, cancellationToken))
                    count++;
            }
            return count;
        }
        /// <summary>Asynchronously executes a delete for the given identities and returns the number of rows affected.</summary>
        /// <typeparam name="TModel">The model type to delete.</typeparam>
        /// <param name="ids">The identities of the models to delete.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task containing the number of rows affected.</returns>
        Task<int> ExecuteDeleteAsync<TModel>(ICollection ids, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new();
        /// <summary>Asynchronously sets the graph's members to the values on every row matching the condition and returns the number of rows affected.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="model">The model holding the values to set.</param>
        /// <param name="graph">The members to set; never an identity or a relation.</param>
        /// <param name="where">The condition selecting the rows.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task containing the number of rows affected.</returns>
        Task<int> ExecuteUpdateWhereAsync<TModel>(TModel model, Graph graph, LambdaExpression where, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new();
        /// <summary>Asynchronously sets members on every row matching the condition to values computed from the row and returns the number of rows affected.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="set">The members to set and their values in terms of the row, as a projection of the model to itself for <see cref="ProjectionMember.FromSet"/>; never an identity or a relation, and reading only the model's own members.</param>
        /// <param name="where">The condition selecting the rows.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task containing the number of rows affected.</returns>
        Task<int> ExecuteUpdateWhereAsync<TModel>(LambdaExpression set, LambdaExpression where, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new();
        /// <summary>Asynchronously deletes every row matching the condition and returns the number of rows affected.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="where">The condition selecting the rows.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task containing the number of rows affected.</returns>
        Task<int> ExecuteDeleteWhereAsync<TModel>(LambdaExpression where, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new();

        /// <summary>
        /// Builds a plan for generating or modifying the data store structure based on the provided model details.
        /// </summary>
        /// <param name="create">Indicates whether to include creation of new store structures.</param>
        /// <param name="update">Indicates whether to include updates to existing store structures.</param>
        /// <param name="delete">Indicates whether to include deletion of obsolete store structures.</param>
        /// <param name="modelDetail">The collection of model details used to build the generation plan.</param>
        /// <returns>An <see cref="IDataStoreGenerationPlan"/> representing the planned store changes.</returns>
        IDataStoreGenerationPlan BuildStoreGenerationPlan(bool create, bool update, bool delete, ICollection<ModelDetail> modelDetail);

        /// <summary>
        /// Reads every table in the data store, described the way a model would declare it, for <see cref="DatabaseFirstGeneration"/>.
        /// </summary>
        /// <returns>The tables.</returns>
        /// <exception cref="NotSupportedException">The engine has no schema to read.</exception>
        IReadOnlyList<DataStoreSchemaTable> ReadDataStoreSchema() => throw new NotSupportedException($"{GetType().Name} has no schema to read");
    }
}

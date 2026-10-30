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
    public interface ITransactStoreEngine
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
        IReadOnlyCollection<TModel> ExecuteMany<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Executes a query and returns the first matching model, or <see langword="null"/>.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph specifying which members to populate.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The first matching model, or <see langword="null"/>.</returns>
        TModel? ExecuteFirst<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Executes a query and returns the single matching model, or <see langword="null"/>.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph specifying which members to populate.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The single matching model, or <see langword="null"/>.</returns>
        TModel? ExecuteSingle<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Executes a query and returns the count of matching models.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The count of matching models.</returns>
        long ExecuteCount<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Executes a query and returns whether any matching models exist.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns><see langword="true"/> if any matching model exists; otherwise <see langword="false"/>.</returns>
        bool ExecuteAny<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Executes a calculation (a sum, average, minimum, maximum, or count of different values) over all the matching models.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <param name="operation">The calculation.</param>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="value">The value expression.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The boxed result of the type from <see cref="CalculationCommon.GetResultType"/>, or <see langword="null"/> when nothing matched.</returns>
        object? ExecuteCalculation<TModel>(QueryOperation operation, LambdaExpression? where, LambdaExpression? value, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Executes a calculation (a count, sum, average, minimum, maximum, or count of different values) per group of the matching models.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <typeparam name="TKey">The group key type, built with <see cref="CalculationCommon.GetKeyBuilder{TKey}"/>.</typeparam>
        /// <typeparam name="TValue">The calculated value type.</typeparam>
        /// <param name="operation">The calculation.</param>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="groupBy">The group key expression.</param>
        /// <param name="value">The value expression, or <see langword="null"/> for a count.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The key and value of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, TValue Value)> ExecuteCalculationGroups<TModel, TKey, TValue>(QueryOperation operation, LambdaExpression? where, LambdaExpression groupBy, LambdaExpression? value, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Returns the different values of an expression over the matching models.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <typeparam name="TValue">The value type, built with <see cref="CalculationCommon.GetKeyBuilder{TKey}"/>.</typeparam>
        /// <param name="value">The value expression, a member or an anonymous object of several.</param>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The different values, in no particular order.</returns>
        IReadOnlyCollection<TValue> ExecuteDistinct<TModel, TValue>(LambdaExpression value, LambdaExpression? where, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Builds a result from each matching model with a projection, reading only what it sets.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <typeparam name="TResult">The projection's result type.</typeparam>
        /// <param name="select">The projection, <c>x => new TResult { Member = ..., ... }</c>, as <see cref="LinqExtensions.GetProjectionMembers"/> reads it.</param>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering of the models.</param>
        /// <param name="skip">An optional number of models to skip.</param>
        /// <param name="take">An optional maximum number of models to return.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The results, one per row in the order the store returns them, each member set from its column.</returns>
        IReadOnlyCollection<TResult> ExecuteSelect<TModel, TResult>(LambdaExpression select, LambdaExpression? where, QueryOrder? order, int? skip, int? take, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Builds a result from each group of matching models with a projection of the key and aggregates, then filters, orders, and pages the results.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <typeparam name="TResult">The projection's result type.</typeparam>
        /// <param name="groupBy">The group key expression.</param>
        /// <param name="select">The projection of a group, as <see cref="LinqExtensions.GetProjectionMembers"/> reads it.</param>
        /// <param name="where">An optional filter expression on the models.</param>
        /// <param name="having">An optional filter on the results.</param>
        /// <param name="order">An optional ordering of the results.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>The results, one per row in the order the store returns them, each member set from its column.</returns>
        IReadOnlyCollection<TResult> ExecuteGroup<TModel, TResult>(LambdaExpression groupBy, LambdaExpression select, LambdaExpression? where, LambdaExpression? having, QueryOrder? order, int? skip, int? take, ModelDetail modelDetail) where TModel : class, new();

        /// <summary>Asynchronously executes a query and returns all matching models.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph specifying which members to populate.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing a collection of matching models.</returns>
        Task<IReadOnlyCollection<TModel>> ExecuteManyAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously executes a query and returns the first matching model, or <see langword="null"/>.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph specifying which members to populate.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the first matching model, or <see langword="null"/>.</returns>
        Task<TModel?> ExecuteFirstAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously executes a query and returns the single matching model, or <see langword="null"/>.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph specifying which members to populate.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the single matching model, or <see langword="null"/>.</returns>
        Task<TModel?> ExecuteSingleAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously executes a query and returns the count of matching models.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the count of matching models.</returns>
        Task<long> ExecuteCountAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously executes a query and returns whether any matching models exist.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="graph">An optional graph.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing <see langword="true"/> if any matching model exists; otherwise <see langword="false"/>.</returns>
        Task<bool> ExecuteAnyAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously executes a calculation (a sum, average, minimum, maximum, or count of different values) over all the matching models.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <param name="operation">The calculation.</param>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="value">The value expression.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the boxed result of the type from <see cref="CalculationCommon.GetResultType"/>, or <see langword="null"/> when nothing matched.</returns>
        Task<object?> ExecuteCalculationAsync<TModel>(QueryOperation operation, LambdaExpression? where, LambdaExpression? value, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously executes a calculation (a count, sum, average, minimum, maximum, or count of different values) per group of the matching models.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <typeparam name="TKey">The group key type, built with <see cref="CalculationCommon.GetKeyBuilder{TKey}"/>.</typeparam>
        /// <typeparam name="TValue">The calculated value type.</typeparam>
        /// <param name="operation">The calculation.</param>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="groupBy">The group key expression.</param>
        /// <param name="value">The value expression, or <see langword="null"/> for a count.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the key and value of each group, in no particular order.</returns>
        Task<IReadOnlyCollection<(TKey Key, TValue Value)>> ExecuteCalculationGroupsAsync<TModel, TKey, TValue>(QueryOperation operation, LambdaExpression? where, LambdaExpression groupBy, LambdaExpression? value, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously returns the different values of an expression over the matching models.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <typeparam name="TValue">The value type, built with <see cref="CalculationCommon.GetKeyBuilder{TKey}"/>.</typeparam>
        /// <param name="value">The value expression, a member or an anonymous object of several.</param>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the different values, in no particular order.</returns>
        Task<IReadOnlyCollection<TValue>> ExecuteDistinctAsync<TModel, TValue>(LambdaExpression value, LambdaExpression? where, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously builds a result from each matching model with a projection, reading only what it sets.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <typeparam name="TResult">The projection's result type.</typeparam>
        /// <param name="select">The projection, <c>x => new TResult { Member = ..., ... }</c>, as <see cref="LinqExtensions.GetProjectionMembers"/> reads it.</param>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering of the models.</param>
        /// <param name="skip">An optional number of models to skip.</param>
        /// <param name="take">An optional maximum number of models to return.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the results, one per row in the order the store returns them, each member set from its column.</returns>
        Task<IReadOnlyCollection<TResult>> ExecuteSelectAsync<TModel, TResult>(LambdaExpression select, LambdaExpression? where, QueryOrder? order, int? skip, int? take, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously builds a result from each group of matching models with a projection of the key and aggregates, then filters, orders, and pages the results.</summary>
        /// <typeparam name="TModel">The model type to query.</typeparam>
        /// <typeparam name="TResult">The projection's result type.</typeparam>
        /// <param name="groupBy">The group key expression.</param>
        /// <param name="select">The projection of a group, as <see cref="LinqExtensions.GetProjectionMembers"/> reads it.</param>
        /// <param name="where">An optional filter expression on the models.</param>
        /// <param name="having">An optional filter on the results.</param>
        /// <param name="order">An optional ordering of the results.</param>
        /// <param name="skip">An optional number of results to skip.</param>
        /// <param name="take">An optional maximum number of results to return.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the results, one per row in the order the store returns them, each member set from its column.</returns>
        Task<IReadOnlyCollection<TResult>> ExecuteGroupAsync<TModel, TResult>(LambdaExpression groupBy, LambdaExpression select, LambdaExpression? where, LambdaExpression? having, QueryOrder? order, int? skip, int? take, ModelDetail modelDetail) where TModel : class, new();

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
        /// <param name="set">The members to set and their values in terms of the row, as a projection of the model to itself for <see cref="LinqExtensions.GetProjectionMembers"/>; never an identity or a relation, and reading only the model's own members.</param>
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
        /// <returns>A task containing the auto-generated identity value.</returns>
        Task<object> ExecuteInsertGetIdentitiesAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously executes an insert and returns if it was successful.</summary>
        /// <typeparam name="TModel">The model type to insert.</typeparam>
        /// <param name="model">The model to insert.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing if the insert was successful.</returns>
        Task<bool> ExecuteInsertAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously executes an update and returns if it was successful.</summary>
        /// <typeparam name="TModel">The model type to update.</typeparam>
        /// <param name="model">The model to update.</param>
        /// <param name="graph">An optional graph specifying which members to persist.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing if the update was successful.</returns>
        Task<bool> ExecuteUpdateAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously executes a delete for the given identities and returns the number of rows affected.</summary>
        /// <typeparam name="TModel">The model type to delete.</typeparam>
        /// <param name="ids">The identities of the models to delete.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the number of rows affected.</returns>
        Task<int> ExecuteDeleteAsync<TModel>(ICollection ids, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously sets the graph's members to the values on every row matching the condition and returns the number of rows affected.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="model">The model holding the values to set.</param>
        /// <param name="graph">The members to set; never an identity or a relation.</param>
        /// <param name="where">The condition selecting the rows.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the number of rows affected.</returns>
        Task<int> ExecuteUpdateWhereAsync<TModel>(TModel model, Graph graph, LambdaExpression where, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously sets members on every row matching the condition to values computed from the row and returns the number of rows affected.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="set">The members to set and their values in terms of the row, as a projection of the model to itself for <see cref="LinqExtensions.GetProjectionMembers"/>; never an identity or a relation, and reading only the model's own members.</param>
        /// <param name="where">The condition selecting the rows.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the number of rows affected.</returns>
        Task<int> ExecuteUpdateWhereAsync<TModel>(LambdaExpression set, LambdaExpression where, ModelDetail modelDetail) where TModel : class, new();
        /// <summary>Asynchronously deletes every row matching the condition and returns the number of rows affected.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="where">The condition selecting the rows.</param>
        /// <param name="modelDetail">The model metadata.</param>
        /// <returns>A task containing the number of rows affected.</returns>
        Task<int> ExecuteDeleteWhereAsync<TModel>(LambdaExpression where, ModelDetail modelDetail) where TModel : class, new();

        /// <summary>
        /// Builds a plan for generating or modifying the data store structure based on the provided model details.
        /// </summary>
        /// <param name="create">Indicates whether to include creation of new store structures.</param>
        /// <param name="update">Indicates whether to include updates to existing store structures.</param>
        /// <param name="delete">Indicates whether to include deletion of obsolete store structures.</param>
        /// <param name="modelDetail">The collection of model details used to build the generation plan.</param>
        /// <returns>An <see cref="IDataStoreGenerationPlan"/> representing the planned store changes.</returns>
        IDataStoreGenerationPlan BuildStoreGenerationPlan(bool create, bool update, bool delete, ICollection<ModelDetail> modelDetail);
    }
}

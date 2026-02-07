// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public interface ITransactStoreEngine : IDataStoreEngine
    {
        IReadOnlyCollection<TModel> ExecuteQueryToModelMany<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        TModel? ExecuteQueryToModelFirst<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        TModel? ExecuteQueryToModelSingle<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        long ExecuteQueryCount(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail);
        bool ExecuteQueryAny(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail);

        Task<IReadOnlyCollection<TModel>> ExecuteQueryToModelManyAsync<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        Task<TModel?> ExecuteQueryToModelFirstAsync<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        Task<TModel?> ExecuteQueryToModelSingleAsync<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new();
        Task<long> ExecuteQueryCountAsync(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail);
        Task<bool> ExecuteQueryAnyAsync(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail);

        IReadOnlyCollection<object> ExecuteInsertGetIdentities<TModel>(TModel model, Graph<TModel>? graph, ModelDetail modelDetail) where TModel : class, new();
        int ExecuteInsert<TModel>(TModel model, Graph<TModel>? graph, ModelDetail modelDetail) where TModel : class, new();
        int ExecuteUpdate<TModel>(TModel model, Graph<TModel>? graph, ModelDetail modelDetail) where TModel : class, new();
        int ExecuteDelete(ICollection ids, ModelDetail modelDetail);

        Task<IReadOnlyCollection<object>> ExecuteInsertGetIdentitiesAsync<TModel>(TModel model, Graph<TModel>? graph, ModelDetail modelDetail) where TModel : class, new();
        Task<int> ExecuteInsertAsync<TModel>(TModel model, Graph<TModel>? graph, ModelDetail modelDetail) where TModel : class, new();
        Task<int> ExecuteUpdateAsync<TModel>(TModel model, Graph<TModel>? graph, ModelDetail modelDetail) where TModel : class, new();
        Task<int> ExecuteDeleteAsync(ICollection ids, ModelDetail modelDetail);
    }
}

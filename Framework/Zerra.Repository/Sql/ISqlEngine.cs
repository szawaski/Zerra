// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Sql
{
    public interface ISqlEngine
    {
        string ConvertToSql(QueryOperation select, Expression where, QueryOrder order, int? skip, int? take, Graph graph, ModelDetail modelDetail);

        ICollection<TModel> ExecuteSqlQueryToModelMany<TModel>(string sql, ModelDetail modelDetail) where TModel : class, new();
        TModel ExecuteSqlQueryToModelFirst<TModel>(string sql, ModelDetail modelDetail) where TModel : class, new();
        TModel ExecuteSqlQueryToModelSingle<TModel>(string sql, ModelDetail modelDetail) where TModel : class, new();
        long ExecuteSqlQueryCount(string sql);
        bool ExecuteSqlQueryAny(string sql);
        ICollection<object> ExecuteSqlQuery(string sql);
        int ExecuteSql(string sql);

        Task<ICollection<TModel>> ExecuteSqlQueryToModelManyAsync<TModel>(string sql, ModelDetail modelDetail) where TModel : class, new();
        Task<TModel> ExecuteSqlQueryToModelFirstAsync<TModel>(string sql, ModelDetail modelDetail) where TModel : class, new();
        Task<TModel> ExecuteSqlQueryToModelSingleAsync<TModel>(string sql, ModelDetail modelDetail) where TModel : class, new();
        Task<long> ExecuteSqlQueryCountAsync(string sql);
        Task<bool> ExecuteSqlQueryAnyAsync(string sql);
        Task<ICollection<object>> ExecuteSqlQueryAsync(string sql);
        Task<int> ExecuteSqlAsync(string sql);

        void AssureDataStore(ICollection<ModelDetail> modelDetail);
    }
}

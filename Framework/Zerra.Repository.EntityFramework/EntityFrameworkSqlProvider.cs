// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zerra.Reflection;

namespace Zerra.Repository.EntityFramework
{
    public class EntityFrameworkSqlProvider<TContext, TModel, TSource> : BaseDataProvider<TModel>, IDataProvider<TModel>
        where TContext : DbContext
        where TModel : class, new()
        where TSource : class
    {
        protected override bool DisableQueryLinking => true;

        protected TContext GetContext()
        {
            var context = Instantiator.CreateInstance<TContext>();
            return context;
        }

        protected virtual Expression<Func<TSource, TModel>> GetModelSelector(Graph<TModel> graph)
        {
            graph.MergePropertiesForIdenticalModelTypes();
            return graph.GenerateSelect<TSource>();
        }

        protected override sealed ICollection<TModel> QueryMany(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var selector = GetModelSelector(query.Graph);

            using (var context = GetContext())
            {
                IQueryable<TSource> set = context.Set<TSource>();

                IQueryable<TSource> queriedSet = set.QueryChangeTypes(query);

                IQueryable<TModel> select = queriedSet.Select(selector);

                TModel[] selectedArray = select.ToArray();

                return selectedArray;
            }
        }
        protected override sealed TModel QueryFirst(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var selector = GetModelSelector(query.Graph);

            using (var context = GetContext())
            {
                IQueryable<TSource> set = context.Set<TSource>();

                IQueryable<TSource> queriedSet = set.QueryChangeTypes(query);

                IQueryable<TModel> select = queriedSet.Select(selector);

                TModel selected = select.FirstOrDefault();

                return selected;
            }
        }
        protected override sealed TModel QuerySingle(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var selector = GetModelSelector(query.Graph);

            using (var context = GetContext())
            {
                IQueryable<TSource> set = context.Set<TSource>();

                IQueryable<TSource> queriedSet = set.QueryChangeTypes(query);

                IQueryable<TModel> select = queriedSet.Select(selector);

                TModel selected = select.SingleOrDefault();

                return selected;
            }
        }
        protected override sealed long QueryCount(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            using (var context = GetContext())
            {
                IQueryable<TSource> set = context.Set<TSource>();

                IQueryable<TSource> queriedSet = set.QueryChangeTypes(query);

                long count = queriedSet.Count();

                return count;
            }
        }
        protected override sealed bool QueryAny(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            using (var context = GetContext())
            {
                IQueryable<TSource> set = context.Set<TSource>();

                IQueryable<TSource> queriedSet = set.QueryChangeTypes(query);

                bool any = queriedSet.Any();

                return any;
            }
        }
        protected override sealed ICollection<EventModel<TModel>> QueryEventMany(Query<TModel> query) => throw new NotSupportedException("Temporal queries not supported with this provider");
        protected override sealed EventModel<TModel> QueryEventFirst(Query<TModel> query) => throw new NotSupportedException("Temporal queries not supported with this provider");
        protected override sealed EventModel<TModel> QueryEventSingle(Query<TModel> query) => throw new NotSupportedException("Temporal queries not supported with this provider");
        protected override sealed long QueryEventCount(Query<TModel> query) => throw new NotSupportedException("Temporal queries not supported with this provider");
        protected override sealed bool QueryEventAny(Query<TModel> query) => throw new NotSupportedException("Temporal queries not supported with this provider");

        protected override sealed async Task<ICollection<TModel>> QueryManyAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var selector = GetModelSelector(query.Graph);

            using (var context = GetContext())
            {
                IQueryable<TSource> set = context.Set<TSource>();

                IQueryable<TSource> queriedSet = set.QueryChangeTypes(query);

                IQueryable<TModel> select = queriedSet.Select(selector);

                TModel[] selectedArray = await select.ToArrayAsync();

                return selectedArray;
            }
        }
        protected override sealed async Task<TModel> QueryFirstAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var selector = GetModelSelector(query.Graph);

            using (var context = GetContext())
            {
                IQueryable<TSource> set = context.Set<TSource>();

                IQueryable<TSource> queriedSet = set.QueryChangeTypes(query);

                IQueryable<TModel> select = queriedSet.Select(selector);

                TModel selected = await select.FirstOrDefaultAsync();

                return selected;
            }
        }
        protected override sealed async Task<TModel> QuerySingleAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var selector = GetModelSelector(query.Graph);

            using (var context = GetContext())
            {
                IQueryable<TSource> set = context.Set<TSource>();

                IQueryable<TSource> queriedSet = set.QueryChangeTypes(query);

                IQueryable<TModel> select = queriedSet.Select(selector);

                TModel selected = await select.SingleOrDefaultAsync();

                return selected;
            }
        }
        protected override sealed async Task<long> QueryCountAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            using (var context = GetContext())
            {
                IQueryable<TSource> set = context.Set<TSource>();

                IQueryable<TSource> queriedSet = set.QueryChangeTypes(query);

                long count = await queriedSet.CountAsync();

                return count;
            }
        }
        protected override sealed async Task<bool> QueryAnyAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            using (var context = GetContext())
            {
                IQueryable<TSource> set = context.Set<TSource>();

                IQueryable<TSource> queriedSet = set.QueryChangeTypes(query);

                bool any = await queriedSet.AnyAsync();

                return any;
            }
        }
        protected override sealed Task<ICollection<EventModel<TModel>>> QueryEventManyAsync(Query<TModel> query) => throw new NotSupportedException("Temporal queries not supported with this provider");
        protected override sealed Task<EventModel<TModel>> QueryEventFirstAsync(Query<TModel> query) => throw new NotSupportedException("Temporal queries not supported with this provider");
        protected override sealed Task<EventModel<TModel>> QueryEventSingleAsync(Query<TModel> query) => throw new NotSupportedException("Temporal queries not supported with this provider");
        protected override sealed Task<long> QueryEventCountAsync(Query<TModel> query) => throw new NotSupportedException("Temporal queries not supported with this provider");
        protected override sealed Task<bool> QueryEventAnyAsync(Query<TModel> query) => throw new NotSupportedException("Temporal queries not supported with this provider");

        private const int deleteBatchSize = 250;
        private string GenerateSqlDelete(ICollection ids)
        {
            var sbWhere = new StringBuilder();
            foreach (object id in ids)
            {
                if (sbWhere.Length > 1)
                    sbWhere.Append(" OR ");
                sbWhere.Append('(');

                if (ModelInfo.IdentityProperties.Count == 1)
                {
                    var modelPropertyInfo = ModelInfo.IdentityProperties[0];
                    object value = id;
                    string property = modelPropertyInfo.PropertySourceName;
                    string sqlValue = FormatSqlValue(modelPropertyInfo.Type, value);
                    sbWhere.Append('[').Append(property).Append(']').Append('=');
                    sbWhere.Append(sqlValue);
                }
                else
                {
                    object[] idArray = ((ICollection)id).Cast<object>().ToArray();

                    int i = 0;
                    foreach (var modelPropertyInfo in ModelInfo.IdentityProperties)
                    {
                        object value = idArray[i];
                        string property = modelPropertyInfo.PropertySourceName;
                        string sqlValue = FormatSqlValue(modelPropertyInfo.Type, value);

                        if (i > 0)
                            sbWhere.Append(" AND ");
                        sbWhere.Append('[').Append(property).Append(']').Append('=');
                        sbWhere.Append(sqlValue);
                        i++;
                    }
                }

                sbWhere.Append(')');
            }

            string sql = null;
            if (sbWhere.Length > 0)
                sql = String.Format("DELETE FROM [{0}] WHERE {1}", ModelInfo.DataSourceEntityName, sbWhere.ToString());
            return sql;
        }
        private string GenerateSqlInsert(TModel model, Graph<TModel> graph)
        {
            StringBuilder sbColumns = new StringBuilder();
            StringBuilder sbValues = new StringBuilder();

            foreach (var modelPropertyInfo in ModelInfo.NonAutoGeneratedNonRelationProperties)
            {
                bool isProperty = graph.HasLocalProperty(modelPropertyInfo.Name);
                if (!modelPropertyInfo.IsNullable || isProperty)
                {
                    object value = null;
                    if (isProperty)
                        value = modelPropertyInfo.Getter(model);
                    else
                        value = Instantiator.CreateInstance(modelPropertyInfo.Type);

                    if (value != null)
                    {
                        string property = modelPropertyInfo.PropertySourceName;
                        string sqlValue = FormatSqlValue(modelPropertyInfo.Type, value);

                        if (sbColumns.Length > 1)
                            sbColumns.Append(',');
                        sbColumns.Append('[').Append(property).Append(']');

                        if (sbValues.Length > 1)
                            sbValues.Append(',');
                        sbValues.Append(sqlValue);
                    }
                }
            }

            string sql = null;
            if (sbColumns.Length > 0 && sbValues.Length > 0)
                sql = String.Format("INSERT INTO [{0}] ({1}) VALUES ({2}) \r\n SELECT SCOPE_IDENTITY() AS [SCOPE_IDENTITY]", ModelInfo.DataSourceEntityName, sbColumns.ToString(), sbValues.ToString());
            else
                sql = String.Format("INSERT INTO [{0}] DEFAULT VALUES \r\n SELECT SCOPE_IDENTITY() AS [SCOPE_IDENTITY]", ModelInfo.DataSourceEntityName);
            return sql;
        }
        private string GenerateSqlUpdate(TModel model, Graph<TModel> graph)
        {
            StringBuilder sbSet = new StringBuilder();
            foreach (var modelPropertyInfo in ModelInfo.NonAutoGeneratedNonRelationProperties)
            {
                if (graph.HasLocalProperty(modelPropertyInfo.Name))
                {
                    object value = modelPropertyInfo.Getter(model);
                    string property = modelPropertyInfo.PropertySourceName;
                    string sqlValue = FormatSqlValue(modelPropertyInfo.Type, value);

                    if (sbSet.Length > 0)
                        sbSet.Append(',');
                    sbSet.Append('[').Append(property).Append(']').Append('=');
                    sbSet.Append(sqlValue);
                }
            }

            if (sbSet.Length == 0)
                return null;

            StringBuilder sbWhere = new StringBuilder();
            foreach (var modelPropertyInfo in ModelInfo.IdentityProperties)
            {
                object value = modelPropertyInfo.Getter(model);
                string property = modelPropertyInfo.PropertySourceName;
                string sqlValue = FormatSqlValue(modelPropertyInfo.Type, value);

                if (sbWhere.Length > 0)
                    sbWhere.Append(" AND ");
                sbWhere.Append('[').Append(property).Append(']').Append('=');
                sbWhere.Append(sqlValue);
            }
            string sql = null;
            if (sbSet.Length > 0 && sbWhere.Length > 0)
                sql = String.Format("UPDATE [{0}] SET {1} WHERE {2}", ModelInfo.DataSourceEntityName, sbSet.ToString(), sbWhere.ToString());
            return sql;
        }
        private string FormatSqlValue(Type type, object value)
        {
            if (value == null)
                return "null";

            if (type == typeof(byte[]))
            {
                string hex = BitConverter.ToString((byte[])value).Replace("-", "");
                return String.Format("CONVERT(varbinary(MAX), '{0}', 2)", hex);
            }

            string stringValue = value.ToString();
            return '\'' + stringValue.Replace("\'", "\'\'") + '\'';
        }

        private const string defaultEventName = "EntityFramework SQL";

        protected override sealed void PersistModel(PersistEvent @event, TModel model, Graph<TModel> graph, bool create)
        {
            if (create)
            {
                string sql = GenerateSqlInsert(model, graph);

                int autoGeneratedCount = ModelInfo.IdentityAutoGeneratedProperties.Count;

                if (autoGeneratedCount == 1)
                {
                    var modelPropertyInfo = ModelInfo.IdentityAutoGeneratedProperties[0];
                    object identity = ExecuteSqlQuery(sql);
                    if (identity == null)
                        throw new Exception(String.Format("Insert failed: {0}", String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name));
                    identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                    modelPropertyInfo.Setter(model, identity);
                }
                else if (autoGeneratedCount > 1)
                {
                    object[] identities = (object[])(ExecuteSqlQuery(sql));
                    int i = 0;
                    foreach (var modelPropertyInfo in ModelInfo.IdentityAutoGeneratedProperties)
                    {
                        object identity = modelPropertyInfo.Getter(identities[i]);
                        if (identity == null)
                            throw new Exception(string.Format("Insert failed: {0}", string.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name));
                        identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                        modelPropertyInfo.Setter(model, identity);
                        i++;
                    }
                }
                else
                {
                    int rowsAffected = ExecuteSql(sql);
                    if (rowsAffected == 0)
                        throw new Exception(String.Format("No rows affected: {0}", String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name));
                }
            }
            else
            {
                string sql = GenerateSqlUpdate(model, graph);
                int rowsAffected = ExecuteSql(sql);
                if (rowsAffected == 0)
                    throw new Exception(String.Format("No rows affected: {0}", String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name));
            }
        }
        protected override sealed void DeleteModel(PersistEvent @event, object[] ids)
        {
            for (int i = 0; i <= ids.Length; i += deleteBatchSize)
            {
                object[] deleteIds = ids.Skip(i).Take(deleteBatchSize).ToArray();
                string sql = GenerateSqlDelete(deleteIds);
                int rowsAffected = ExecuteSql(sql);
                if (rowsAffected == 0)
                    throw new Exception(String.Format("No rows affected: {0}", String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name));
            }
        }

        protected override sealed async Task PersistModelAsync(PersistEvent @event, TModel model, Graph<TModel> graph, bool create)
        {
            if (create)
            {
                string sql = GenerateSqlInsert(model, graph);

                int autoGeneratedCount = ModelInfo.IdentityAutoGeneratedProperties.Count;

                if (autoGeneratedCount == 1)
                {
                    var modelPropertyInfo = ModelInfo.IdentityAutoGeneratedProperties[0];
                    object identity = ExecuteSqlQueryAsync(sql);
                    if (identity == null)
                        throw new Exception(String.Format("Insert failed: {0}", String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name));
                    identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                    modelPropertyInfo.Setter(model, identity);
                }
                else if (autoGeneratedCount > 1)
                {
                    object[] identities = (object[])(await ExecuteSqlQueryAsync(sql));
                    int i = 0;
                    foreach (var modelPropertyInfo in ModelInfo.IdentityAutoGeneratedProperties)
                    {
                        object identity = modelPropertyInfo.Getter(identities[i]);
                        if (identity == null)
                            throw new Exception(string.Format("Insert failed: {0}", string.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name));
                        identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                        modelPropertyInfo.Setter(model, identity);
                        i++;
                    }
                }
                else
                {
                    int rowsAffected = await ExecuteSqlAsync(sql);
                    if (rowsAffected == 0)
                        throw new Exception(String.Format("No rows affected: {0}", String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name));
                }
            }
            else
            {
                string sql = GenerateSqlUpdate(model, graph);
                int rowsAffected = await ExecuteSqlAsync(sql);
                if (rowsAffected == 0)
                    throw new Exception(String.Format("No rows affected: {0}", String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name));
            }
        }
        protected override sealed async Task DeleteModelAsync(PersistEvent @event, object[] ids)
        {
            for (int i = 0; i <= ids.Length; i += deleteBatchSize)
            {
                object[] deleteIds = ids.Skip(i).Take(deleteBatchSize).ToArray();
                string sql = GenerateSqlDelete(deleteIds);
                int rowsAffected = await ExecuteSqlAsync(sql);
                if (rowsAffected == 0)
                    throw new Exception(String.Format("No rows affected: {0}", String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name));
            }
        }

        private object ExecuteSqlQuery(string sql)
        {
            using (var context = GetContext())
            {
                List<object> allValues = new List<object>();
                using (var connection = new SqlConnection(context.Database.GetDbConnection().ConnectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    List<object> values = new List<object>();
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        var value = reader[i];
                                        values.Add(value);
                                    }
                                    if (values.Count == 1)
                                        allValues.Add(values[0]);
                                    else if (values.Count > 1)
                                        allValues.Add(values.ToArray());
                                }
                            }
                        }
                    }
                }

                if (allValues.Count == 0)
                    return null;
                if (allValues.Count == 1)
                    return allValues[0];
                else
                    return allValues;
            }
        }
        private int ExecuteSql(string sql)
        {
            using (var context = GetContext())
            {
                int rowsAffected = 0;
                using (var connection = new SqlConnection(context.Database.GetDbConnection().ConnectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        rowsAffected = command.ExecuteNonQuery();
                    }
                }
                return rowsAffected;
            }
        }

        private async Task<object> ExecuteSqlQueryAsync(string sql)
        {
            using (var context = GetContext())
            {
                List<object> allValues = new List<object>();
                using (var connection = new SqlConnection(context.Database.GetDbConnection().ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                while (await reader.ReadAsync())
                                {
                                    List<object> values = new List<object>();
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        var value = reader[i];
                                        values.Add(value);
                                    }
                                    if (values.Count == 1)
                                        allValues.Add(values[0]);
                                    else if (values.Count > 1)
                                        allValues.Add(values.ToArray());
                                }
                            }
                        }
                    }
                }

                if (allValues.Count == 0)
                    return null;
                if (allValues.Count == 1)
                    return allValues[0];
                else
                    return allValues;
            }
        }
        private async Task<int> ExecuteSqlAsync(string sql)
        {
            using (var context = GetContext())
            {
                int rowsAffected = 0;
                using (var connection = new SqlConnection(context.Database.GetDbConnection().ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        rowsAffected = await command.ExecuteNonQueryAsync();
                    }
                }
                return rowsAffected;
            }
        }
    }
}

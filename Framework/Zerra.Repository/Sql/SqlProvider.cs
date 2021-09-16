// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zerra.IO;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Sql
{
    public class SqlProvider<TContext, TModel> : BaseDataProvider<TModel>
        where TContext : SqlDataContext
        where TModel : class, new()
    {
        protected readonly ISqlEngine Engine;

        public SqlProvider()
        {
            var context = Instantiator.GetSingleInstance<TContext>();
            this.Engine = context.GetEngine();
        }

        protected override sealed ICollection<TModel> QueryMany(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.Many, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelInfo);

            var models = Engine.ExecuteSqlQueryToModelMany<TModel>(sql, ModelInfo);
            return models;
        }
        protected override sealed TModel QueryFirst(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.First, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelInfo);

            var model = Engine.ExecuteSqlQueryToModelFirst<TModel>(sql, ModelInfo);
            return model;
        }
        protected override sealed TModel QuerySingle(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.Single, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelInfo);

            var model = Engine.ExecuteSqlQueryToModelSingle<TModel>(sql, ModelInfo);
            return model;
        }
        protected override sealed long QueryCount(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.Count, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelInfo);

            var count = Engine.ExecuteSqlQueryCount(sql);
            return count;
        }
        protected override sealed bool QueryAny(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.Any, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelInfo);

            var any = Engine.ExecuteSqlQueryAny(sql);
            return any;
        }
        protected override sealed ICollection<EventModel<TModel>> QueryEventMany(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");
        protected override sealed EventModel<TModel> QueryEventFirst(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");
        protected override sealed EventModel<TModel> QueryEventSingle(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");
        protected override sealed long QueryEventCount(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");
        protected override sealed bool QueryEventAny(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");

        protected override sealed Task<ICollection<TModel>> QueryManyAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.Many, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelInfo);

            var models = Engine.ExecuteSqlQueryToModelManyAsync<TModel>(sql, ModelInfo);
            return models;
        }
        protected override sealed Task<TModel> QueryFirstAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.First, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelInfo);

            var model = Engine.ExecuteSqlQueryToModelFirstAsync<TModel>(sql, ModelInfo);
            return model;
        }
        protected override sealed Task<TModel> QuerySingleAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.Single, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelInfo);

            var model = Engine.ExecuteSqlQueryToModelSingleAsync<TModel>(sql, ModelInfo);
            return model;
        }
        protected override sealed Task<long> QueryCountAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.Count, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelInfo);

            var count = Engine.ExecuteSqlQueryCountAsync(sql);
            return count;
        }
        protected override sealed Task<bool> QueryAnyAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.Any, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelInfo);

            var any = Engine.ExecuteSqlQueryAnyAsync(sql);
            return any;
        }
        protected override sealed Task<ICollection<EventModel<TModel>>> QueryEventManyAsync(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");
        protected override sealed Task<EventModel<TModel>> QueryEventFirstAsync(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");
        protected override sealed Task<EventModel<TModel>> QueryEventSingleAsync(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");
        protected override sealed Task<long> QueryEventCountAsync(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");
        protected override sealed Task<bool> QueryEventAnyAsync(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");

        private const int deleteBatchSize = 250;
        private static string GenerateSqlDelete(ICollection ids)
        {
            var sbWhere = new CharWriteBuffer();
            try
            {
                sbWhere.Write($"DELETE FROM [");
                sbWhere.Write(ModelInfo.DataSourceEntityName);
                sbWhere.Write("] WHERE ");
                var first = true;
                foreach (object id in ids)
                {
                    if (first) first = false;
                    else sbWhere.Write(" OR ");
                    sbWhere.Write('(');

                    if (ModelInfo.IdentityProperties.Count == 1)
                    {
                        var modelPropertyInfo = ModelInfo.IdentityProperties[0];
                        object value = id;
                        string property = modelPropertyInfo.PropertySourceName;
                        sbWhere.Write('[');
                        sbWhere.Write(property);
                        sbWhere.Write(']');
                        AppendSqlValue(ref sbWhere, modelPropertyInfo, value, false, true);
                    }
                    else
                    {
                        object[] idArray = ((ICollection)id).Cast<object>().ToArray();

                        int i = 0;
                        foreach (var modelPropertyInfo in ModelInfo.IdentityProperties)
                        {
                            object value = idArray[i];
                            string property = modelPropertyInfo.PropertySourceName;

                            if (i > 0)
                                sbWhere.Write(" AND ");
                            sbWhere.Write('[');
                            sbWhere.Write(property);
                            sbWhere.Write(']');
                            AppendSqlValue(ref sbWhere, modelPropertyInfo, value, false, true);
                            i++;
                        }
                    }

                    sbWhere.Write(')');
                }

                return sbWhere.ToString();
            }
            finally
            {
                sbWhere.Dispose();
            }
        }
        private static string GenerateSqlInsert(TModel model, Graph<TModel> graph)
        {
            var sbColumns = new CharWriteBuffer();
            var sbValues = new CharWriteBuffer();
            try 
            {
                foreach (var modelPropertyDetail in ModelInfo.NonAutoGeneratedNonRelationProperties)
                {
                    bool isProperty = graph.HasLocalProperty(modelPropertyDetail.Name);
                    if (!modelPropertyDetail.IsNullable || isProperty)
                    {
                        object value;
                        if (isProperty)
                            value = modelPropertyDetail.Getter(model);
                        else
                            value = Instantiator.CreateInstance(modelPropertyDetail.Type);

                        if (value != null)
                        {
                            string property = modelPropertyDetail.PropertySourceName;

                            if (sbColumns.Length > 0)
                                sbColumns.Write(',');
                            sbColumns.Write('[');
                            sbColumns.Write(property);
                            sbColumns.Write(']');

                            if (sbValues.Length > 0)
                                sbValues.Write(',');
                            AppendSqlValue(ref sbValues, modelPropertyDetail, value, false, false);
                        }
                    }
                }

                string sql;
                if (sbColumns.Length > 0 && sbValues.Length > 0)
                    sql = $"INSERT INTO [{ModelInfo.DataSourceEntityName}] ({sbColumns.ToString()}) VALUES ({sbValues.ToString()}) \r\n SELECT SCOPE_IDENTITY() AS [SCOPE_IDENTITY]";
                else
                    sql = $"INSERT INTO [{ModelInfo.DataSourceEntityName}] DEFAULT VALUES \r\n SELECT SCOPE_IDENTITY() AS [SCOPE_IDENTITY]";
                return sql;
            }
            finally
            {
                sbColumns.Dispose();
                sbValues.Dispose();
            }
        }
        private static string GenerateSqlUpdate(TModel model, Graph<TModel> graph)
        {
            if (ModelInfo.IdentityProperties.Count == 0)
                return null;

            var writer = new CharWriteBuffer();
            try
            {
                writer.Write("UPDATE [");
                writer.Write(ModelInfo.DataSourceEntityName);
                writer.Write("] SET ");
                bool first = true;
                foreach (var modelPropertyInfo in ModelInfo.NonAutoGeneratedNonRelationProperties)
                {
                    if (graph.HasLocalProperty(modelPropertyInfo.Name))
                    {
                        object value = modelPropertyInfo.Getter(model);
                        string property = modelPropertyInfo.PropertySourceName;

                        if (first) first = false;
                        else writer.Write(',');
                        writer.Write('[');
                        writer.Write(property);
                        writer.Write(']');
                        AppendSqlValue(ref writer, modelPropertyInfo, value, true, false);
                    }
                }

                if (writer.Length == 0)
                    return null;

                writer.Write(" WHERE ");

                first = true;
                foreach (var modelPropertyInfo in ModelInfo.IdentityProperties)
                {
                    object value = modelPropertyInfo.Getter(model);
                    string property = modelPropertyInfo.PropertySourceName;

                    if (first) first = false;
                    else writer.Write(" AND ");
                    writer.Write('[');
                    writer.Write(property);
                    writer.Write(']');
                    AppendSqlValue(ref writer, modelPropertyInfo, value, false, true);
                }

                return writer.ToString();
            }
            finally
            {
                writer.Dispose();
            }
        }
        private static void AppendSqlValue(ref CharWriteBuffer writer, ModelPropertyDetail modelPropertyDetail, object value, bool assigningValue, bool comparingValue)
        {
            if (value == null)
            {
                if (assigningValue)
                    writer.Write("=NULL");
                else if (comparingValue)
                    writer.Write("IS NULL");
                else
                    writer.Write("NULL");
                return;
            }

            if (modelPropertyDetail.CoreType.HasValue)
            {
                if (assigningValue || comparingValue)
                    writer.Write('=');
                switch (modelPropertyDetail.CoreType.Value)
                {
                    case CoreType.String:
                        writer.Write('\'');
                        writer.Write(value.ToString().Replace("'", "''"));
                        writer.Write('\'');
                        return;

                    case CoreType.Boolean:
                    case CoreType.BooleanNullable:
                        writer.Write((bool)value == false ? '0' : '1');
                        return;
                    case CoreType.Byte:
                    case CoreType.ByteNullable:
                        writer.Write((byte)value);
                        return;
                    case CoreType.Int16:
                    case CoreType.Int16Nullable:
                        writer.Write((short)value);
                        return;
                    case CoreType.Int32:
                    case CoreType.Int32Nullable:
                        writer.Write((int)value);
                        return;
                    case CoreType.Int64:
                    case CoreType.Int64Nullable:
                        writer.Write((long)value);
                        return;
                    case CoreType.Single:
                    case CoreType.SingleNullable:
                        writer.Write((float)value);
                        return;
                    case CoreType.Double:
                    case CoreType.DoubleNullable:
                        writer.Write((double)value);
                        return;
                    case CoreType.Decimal:
                    case CoreType.DecimalNullable:
                        writer.Write((decimal)value);
                        return;
                    case CoreType.Char:
                    case CoreType.CharNullable:
                        writer.Write('\'');
                        var castedChar = (char)value;
                        if (castedChar == '\'')
                            writer.Write("''");
                        else
                            writer.Write(castedChar);
                        writer.Write('\'');
                        return;
                    case CoreType.DateTime:
                    case CoreType.DateTimeNullable:
                        writer.Write('\'');
                        writer.Write((DateTime)value, DateTimeFormat.MsSql);
                        writer.Write('\'');
                        return;
                    case CoreType.DateTimeOffset:
                    case CoreType.DateTimeOffsetNullable:
                        writer.Write('\'');
                        writer.Write((DateTimeOffset)value, DateTimeFormat.MsSqlOffset);
                        writer.Write('\'');
                        return;
                    case CoreType.TimeSpan:
                    case CoreType.TimeSpanNullable:
                        writer.Write('\'');
                        writer.Write((TimeSpan)value, TimeFormat.MsSql);
                        writer.Write('\'');
                        return;
                    case CoreType.Guid:
                    case CoreType.GuidNullable:
                        writer.Write('\'');
                        writer.Write((Guid)value);
                        writer.Write('\'');
                        return;

                    default:
                        throw new NotImplementedException();
                }
            }

            if (modelPropertyDetail.Type == typeof(byte[]))
            {
                string hex = BitConverter.ToString((byte[])value).Replace("-", "");
                if (assigningValue || comparingValue)
                    writer.Write($"=0x{hex}");
                else
                    writer.Write($"0x{hex}");
                return;
            }

            throw new Exception($"Cannot convert type to SQL value {modelPropertyDetail.Type.GetNiceName()}");
        }

        private const string defaultEventName = "Transact SQL";

        protected override sealed void PersistModel(PersistEvent @event, TModel model, Graph<TModel> graph, bool create)
        {
            if (create)
            {
                string sql = GenerateSqlInsert(model, graph);

                int autoGeneratedCount = ModelInfo.IdentityAutoGeneratedProperties.Count;

                if (autoGeneratedCount == 1)
                {
                    var rows = Engine.ExecuteSqlQuery(sql);
                    if (rows.Count != 1)
                        throw new Exception($"Insert failed: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                    var identity = rows.First();
                    var modelPropertyInfo = ModelInfo.IdentityAutoGeneratedProperties[0];
                    identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                    modelPropertyInfo.Setter(model, identity);

                }
                else if (autoGeneratedCount > 1)
                {
                    var rows = Engine.ExecuteSqlQuery(sql);
                    if (rows.Count != 1)
                        throw new Exception($"Insert failed: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                    var identities = (IList<object>)rows.First();
                    int i = 0;
                    foreach (var modelPropertyInfo in ModelInfo.IdentityAutoGeneratedProperties)
                    {
                        object identity = modelPropertyInfo.Getter(identities[i]);
                        identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                        modelPropertyInfo.Setter(model, identity);
                        i++;
                    }
                }
                else
                {
                    int rowsAffected = Engine.ExecuteSql(sql);
                    if (rowsAffected == 0)
                        throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                }
            }
            else
            {
                string sql = GenerateSqlUpdate(model, graph);
                int rowsAffected = Engine.ExecuteSql(sql);
                if (rowsAffected == 0)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }

        protected override sealed void DeleteModel(PersistEvent @event, object[] ids)
        {
            for (int i = 0; i <= ids.Length; i += deleteBatchSize)
            {
                object[] deleteIds = ids.Skip(i).Take(deleteBatchSize).ToArray();
                string sql = GenerateSqlDelete(deleteIds);
                int rowsAffected = Engine.ExecuteSql(sql);
                if (rowsAffected == 0)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
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
                    var rows = await Engine.ExecuteSqlQueryAsync(sql);
                    if (rows.Count != 1)
                        throw new Exception($"Insert failed: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                    var identity = rows.First();
                    var modelPropertyInfo = ModelInfo.IdentityAutoGeneratedProperties[0];
                    identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                    modelPropertyInfo.Setter(model, identity);

                }
                else if (autoGeneratedCount > 1)
                {
                    var rows = await Engine.ExecuteSqlQueryAsync(sql);
                    if (rows.Count != 1)
                        throw new Exception($"Insert failed: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                    var identities = (IList<object>)rows.First();
                    int i = 0;
                    foreach (var modelPropertyInfo in ModelInfo.IdentityAutoGeneratedProperties)
                    {
                        object identity = modelPropertyInfo.Getter(identities[i]);
                        identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                        modelPropertyInfo.Setter(model, identity);
                        i++;
                    }
                }
                else
                {
                    int rowsAffected = await Engine.ExecuteSqlAsync(sql);
                    if (rowsAffected == 0)
                        throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                }
            }
            else
            {
                string sql = GenerateSqlUpdate(model, graph);
                int rowsAffected = await Engine.ExecuteSqlAsync(sql);
                if (rowsAffected == 0)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }

        protected override sealed async Task DeleteModelAsync(PersistEvent @event, object[] ids)
        {
            for (int i = 0; i <= ids.Length; i += deleteBatchSize)
            {
                object[] deleteIds = ids.Skip(i).Take(deleteBatchSize).ToArray();
                string sql = GenerateSqlDelete(deleteIds);
                int rowsAffected = await Engine.ExecuteSqlAsync(sql);
                if (rowsAffected == 0)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }
    }
}
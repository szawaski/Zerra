﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Zerra.Repository.Reflection;
using Zerra.Reflection;
using System.Text;
using System.Linq;
using Zerra.Logging;
using System.Data;
using System.Runtime.CompilerServices;
using Npgsql;
using Zerra.IO;
using System.Collections;
using Zerra.Logging;

namespace Zerra.Repository.PostgreSql
{
    public sealed partial class PostgreSqlEngine : ITransactStoreEngine
    {
        private readonly string connectionString;
        public PostgreSqlEngine(string connectionString)
        {
            this.connectionString = connectionString;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ICoreTypeSetter<TModel>[] ReadColumns<TModel>(NpgsqlDataReader reader, ModelDetail modelDetail)
        {
            var columnProperties = new ICoreTypeSetter<TModel>[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var property = reader.GetName(i);
                if (modelDetail.TryGetPropertyLower(property, out var propertyInfo))
                    columnProperties[i] = (ICoreTypeSetter<TModel>)propertyInfo.CoreTypeSetter;
            }
            return columnProperties;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TModel ReadRow<TModel>(NpgsqlDataReader reader, ModelDetail modelDetail, ICoreTypeSetter<TModel>[] columnProperties)
        {
            var model = (TModel)modelDetail.Creator();

            for (var i = 0; i < reader.FieldCount; i++)
            {
                var columnProperty = columnProperties[i];
                if (columnProperty != null)
                {
                    if (columnProperty.CoreType.HasValue)
                    {
                        switch (columnProperty.CoreType.Value)
                        {
                            case CoreType.Boolean:
                                {
                                    var value = reader.GetBoolean(i);
                                    columnProperty.Setter(model, value);
                                }
                                break;
                            case CoreType.Byte:
                                {
                                    var value = reader.GetByte(i);
                                    columnProperty.Setter(model, value);
                                }
                                break;
                            case CoreType.Int16:
                                {
                                    var value = reader.GetInt16(i);
                                    columnProperty.Setter(model, value);
                                }
                                break;
                            case CoreType.Int32:
                                {
                                    var value = reader.GetInt32(i);
                                    columnProperty.Setter(model, value);
                                }
                                break;
                            case CoreType.Int64:
                                {
                                    var value = reader.GetInt64(i);
                                    columnProperty.Setter(model, value);
                                }
                                break;
                            case CoreType.Single:
                                {
                                    var value = reader.GetFloat(i);
                                    columnProperty.Setter(model, value);
                                }
                                break;
                            case CoreType.Double:
                                {
                                    var value = reader.GetDouble(i);
                                    columnProperty.Setter(model, value);
                                }
                                break;
                            case CoreType.Decimal:
                                {
                                    var value = reader.GetDecimal(i);
                                    columnProperty.Setter(model, value);
                                }
                                break;
                            case CoreType.Char:
                                {
                                    var value = reader.GetString(i)[0];
                                    columnProperty.Setter(model, value);
                                }
                                break;
                            case CoreType.DateTime:
                                {
                                    var value = new DateTime(reader.GetDateTime(i).Ticks, DateTimeKind.Utc);
                                    columnProperty.Setter(model, value);
                                }
                                break;
                            case CoreType.DateTimeOffset:
                                {
                                    var value = new DateTimeOffset(new DateTime(reader.GetDateTime(i).Ticks, DateTimeKind.Utc));
                                    columnProperty.Setter(model, value);
                                }
                                break;
                            case CoreType.TimeSpan:
                                {
                                    var value = reader.GetTimeSpan(i);
                                    columnProperty.Setter(model, value);
                                }
                                break;
                            case CoreType.Guid:
                                {
                                    var value = reader.GetGuid(i);
                                    columnProperty.Setter(model, value);
                                }
                                break;
                            case CoreType.String:
                                {
                                    var value = reader.GetValue(i);
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (string)value);
                                }
                                break;
                            case CoreType.BooleanNullable:
                                {
                                    var value = reader.GetValue(i);
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (bool?)value);
                                }
                                break;
                            case CoreType.ByteNullable:
                                {
                                    var value = reader.GetValue(i);
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (byte?)(short?)value);
                                }
                                break;
                            case CoreType.Int16Nullable:
                                {
                                    var value = reader.GetValue(i);
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (short?)value);
                                }
                                break;
                            case CoreType.Int32Nullable:
                                {
                                    var value = reader.GetValue(i);
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (int?)value);
                                }
                                break;
                            case CoreType.Int64Nullable:
                                {
                                    var value = reader.GetValue(i);
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (long?)value);
                                }
                                break;
                            case CoreType.SingleNullable:
                                {
                                    var value = reader.GetValue(i);
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (float?)value);
                                }
                                break;
                            case CoreType.DoubleNullable:
                                {
                                    var value = reader.GetValue(i);  //GetSqlDouble doesn't handle SqlMoney
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (double?)value);
                                }
                                break;
                            case CoreType.DecimalNullable:
                                {
                                    var value = reader.GetValue(i);  //GetSqlDecimal doesn't handle SqlMoney
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (decimal?)value);
                                }
                                break;
                            case CoreType.CharNullable:
                                {
                                    var value = reader.GetValue(i);
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (char?)((string)value)[0]);
                                }
                                break;
                            case CoreType.DateTimeNullable:
                                {
                                    var value = reader.GetValue(i);
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (DateTime?)new DateTime(((DateTime)value).Ticks, DateTimeKind.Utc));
                                }
                                break;
                            case CoreType.DateTimeOffsetNullable:
                                {
                                    var value = reader.GetValue(i);
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (DateTimeOffset?)new DateTimeOffset(new DateTime(((DateTime)value).Ticks, DateTimeKind.Utc)));
                                }
                                break;
                            case CoreType.TimeSpanNullable:
                                {
                                    var value = reader.GetValue(i);
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (TimeSpan?)value);
                                }
                                break;
                            case CoreType.GuidNullable:
                                {
                                    var value = reader.GetValue(i);
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (Guid?)value);
                                }
                                break;

                            default:
                                throw new NotSupportedException($"{nameof(PostgreSqlEngine)} Cannot map to type {columnProperty.CoreType.Value} in {modelDetail.Type.GetNiceName()}");
                        }

                    }
                    else if (columnProperty.IsByteArray)
                    {
                        if (!reader.IsDBNull(i))
                        {
                            var value = reader.GetValue(i);
                            if (value != DBNull.Value)
                                columnProperty.Setter(model, (byte[])value);
                        }
                    }
                    else
                    {
                        throw new NotSupportedException($"{nameof(PostgreSqlEngine)} cannot map to type in {modelDetail.Type.GetNiceName()}");
                    }
                }
            }

            return model;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GenerateSqlInsert<TModel>(TModel model, Graph<TModel> graph, ModelDetail modelDetail, bool getIdentities)
        {
            var sbColumns = new CharWriter();
            var sbValues = new CharWriter();
            var sbReturns = new CharWriter();
            try
            {
                foreach (var modelPropertyDetail in modelDetail.NonAutoGeneratedNonRelationProperties)
                {
                    var isProperty = graph.HasLocalProperty(modelPropertyDetail.Name);
                    if (!modelPropertyDetail.IsNullable || isProperty)
                    {
                        object value;
                        if (isProperty)
                            value = modelPropertyDetail.Getter(model);
                        else
                            value = Instantiator.Create(modelPropertyDetail.Type);

                        if (value != null)
                        {
                            var property = modelPropertyDetail.PropertySourceName;

                            if (sbColumns.Length > 0)
                                sbColumns.Write(',');
                            sbColumns.Write(property);

                            if (sbValues.Length > 0)
                                sbValues.Write(',');
                            AppendSqlValue(ref sbValues, modelPropertyDetail, value, false, false);
                        }
                    }
                }

                if (getIdentities)
                {
                    foreach (var modelPropertyDetail in modelDetail.IdentityAutoGeneratedProperties)
                    {
                        var isProperty = graph.HasLocalProperty(modelPropertyDetail.Name);
                        if (!modelPropertyDetail.IsNullable || isProperty)
                        {
                            object value;
                            if (isProperty)
                                value = modelPropertyDetail.Getter(model);
                            else
                                value = Instantiator.Create(modelPropertyDetail.Type);

                            if (value != null)
                            {
                                var property = modelPropertyDetail.PropertySourceName;

                                if (sbReturns.Length > 0)
                                    sbReturns.Write(',');
                                sbReturns.Write(property);
                            }
                        }
                    }
                }

                string sql;
                if (getIdentities)
                {
                    if (sbColumns.Length > 0 && sbValues.Length > 0)
                        sql = $"INSERT INTO {modelDetail.DataSourceEntityName} ({sbColumns.ToString()}) VALUES ({sbValues.ToString()}) RETURNING {sbReturns.ToString()}";
                    else
                        sql = $"INSERT INTO {modelDetail.DataSourceEntityName} DEFAULT VALUES RETURNING {sbReturns.ToString()}";
                }
                else
                {
                    if (sbColumns.Length > 0 && sbValues.Length > 0)
                        sql = $"INSERT INTO {modelDetail.DataSourceEntityName} ({sbColumns.ToString()}) VALUES ({sbValues.ToString()})";
                    else
                        sql = $"INSERT INTO {modelDetail.DataSourceEntityName} DEFAULT VALUES";
                }
                return sql;
            }
            finally
            {
                sbColumns.Dispose();
                sbValues.Dispose();
                sbReturns.Dispose();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GenerateSqlUpdate<TModel>(TModel model, Graph<TModel> graph, ModelDetail modelDetail)
        {
            if (modelDetail.IdentityProperties.Count == 0)
                return null;

            var writer = new CharWriter();
            try
            {
                writer.Write("UPDATE ");
                writer.Write(modelDetail.DataSourceEntityName);
                writer.Write(" SET ");
                var first = true;
                foreach (var modelPropertyInfo in modelDetail.NonAutoGeneratedNonRelationProperties)
                {
                    if (graph.HasLocalProperty(modelPropertyInfo.Name))
                    {
                        var value = modelPropertyInfo.Getter(model);
                        var property = modelPropertyInfo.PropertySourceName;

                        if (first)
                            first = false;
                        else
                            writer.Write(',');
                        writer.Write(property);
                        AppendSqlValue(ref writer, modelPropertyInfo, value, true, false);
                    }
                }

                if (writer.Length == 0)
                    return null;

                writer.Write(" WHERE ");

                first = true;
                foreach (var modelPropertyInfo in modelDetail.IdentityProperties)
                {
                    var value = modelPropertyInfo.Getter(model);
                    var property = modelPropertyInfo.PropertySourceName;

                    if (first)
                        first = false;
                    else
                        writer.Write(" AND ");
                    writer.Write(property);
                    AppendSqlValue(ref writer, modelPropertyInfo, value, false, true);
                }

                return writer.ToString();
            }
            finally
            {
                writer.Dispose();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GenerateSqlDelete(ICollection ids, ModelDetail modelDetail)
        {
            var sbWhere = new CharWriter();
            try
            {
                sbWhere.Write($"DELETE FROM ");
                sbWhere.Write(modelDetail.DataSourceEntityName);
                sbWhere.Write(" WHERE ");

                if (modelDetail.IdentityProperties.Count == 1)
                {
                    var modelPropertyInfo = modelDetail.IdentityProperties[0];
                    sbWhere.Write(modelPropertyInfo.PropertySourceName);
                    sbWhere.Write(" IN (");

                    var first = true;
                    foreach (var id in ids)
                    {
                        if (first)
                            first = false;
                        else
                            sbWhere.Write(',');
                        AppendSqlValue(ref sbWhere, modelPropertyInfo, id, false, false);
                    }

                    sbWhere.Write(')');
                }
                else
                {
                    var first = true;
                    foreach (var id in ids)
                    {
                        if (first)
                            first = false;
                        else
                            sbWhere.Write(" OR ");
                        sbWhere.Write('(');

                        var idArray = ((ICollection)id).Cast<object>().ToArray();

                        var i = 0;
                        foreach (var modelPropertyInfo in modelDetail.IdentityProperties)
                        {
                            var value = idArray[i];

                            if (i > 0)
                                sbWhere.Write(" AND ");
                            sbWhere.Write(modelPropertyInfo.PropertySourceName);
                            AppendSqlValue(ref sbWhere, modelPropertyInfo, value, false, true);
                            i++;
                        }
                        sbWhere.Write(')');
                    }
                }

                return sbWhere.ToString();
            }
            finally
            {
                sbWhere.Dispose();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AppendSqlValue(ref CharWriter writer, ModelPropertyDetail modelPropertyDetail, object value, bool assigningValue, bool comparingValue)
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
                        writer.Write(((DateTime)value).ToUniversalTime(), DateTimeFormat.PostgreSql);
                        writer.Write('\'');
                        return;
                    case CoreType.DateTimeOffset:
                    case CoreType.DateTimeOffsetNullable:
                        writer.Write('\'');
                        writer.Write(((DateTimeOffset)value).ToUniversalTime(), DateTimeFormat.PostgreSql);
                        writer.Write('\'');
                        return;
                    case CoreType.TimeSpan:
                    case CoreType.TimeSpanNullable:
                        writer.Write('\'');
                        writer.Write((TimeSpan)value, TimeFormat.PostgreSql);
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
                if (assigningValue || comparingValue)
                    writer.Write('=');
                writer.Write("decode('");
                writer.Write((byte[])value, ByteFormat.Hex);
                writer.Write("','hex')");
                return;
            }

            throw new Exception($"Cannot convert type to SQL value {modelPropertyDetail.Type.GetNiceName()}");
        }

        public ICollection<TModel> ExecuteQueryToModelMany<TModel>(Expression where, QueryOrder order, int? skip, int? take, Graph graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var sql = LinqPostgreSqlConverter.Convert(QueryOperation.Many, where, order, skip, take, graph, modelDetail);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    using (var reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            var models = new List<TModel>();

                            var columnProperties = ReadColumns<TModel>(reader, modelDetail);

                            while (reader.Read())
                            {
                                var model = ReadRow<TModel>(reader, modelDetail, columnProperties);
                                models.Add(model);
                            }

                            return models;
                        }
                        else
                        {
                            return Array.Empty<TModel>();
                        }
                    }
                }
            }
        }
        public TModel ExecuteQueryToModelFirst<TModel>(Expression where, QueryOrder order, int? skip, int? take, Graph graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var sql = LinqPostgreSqlConverter.Convert(QueryOperation.First, where, order, skip, take, graph, modelDetail);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    using (var reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            var columnProperties = ReadColumns<TModel>(reader, modelDetail);

                            _ = reader.Read();

                            var model = ReadRow<TModel>(reader, modelDetail, columnProperties);
                            return model;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }
        public TModel ExecuteQueryToModelSingle<TModel>(Expression where, QueryOrder order, int? skip, int? take, Graph graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var sql = LinqPostgreSqlConverter.Convert(QueryOperation.Single, where, order, skip, take, graph, modelDetail);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    using (var reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            var columnProperties = ReadColumns<TModel>(reader, modelDetail);

                            _ = reader.Read();

                            var model = ReadRow<TModel>(reader, modelDetail, columnProperties);

                            if (reader.Read())
                                throw new Exception("More than a single entity found");

                            return model;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }
        public long ExecuteQueryCount(Expression where, QueryOrder order, int? skip, int? take, Graph graph, ModelDetail modelDetail)
        {
            var sql = LinqPostgreSqlConverter.Convert(QueryOperation.Count, where, order, skip, take, graph, modelDetail);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    using (var reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            _ = reader.Read();
                            var count = reader.GetInt32(0);
                            return count;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            }
        }
        public bool ExecuteQueryAny(Expression where, QueryOrder order, int? skip, int? take, Graph graph, ModelDetail modelDetail)
        {
            var sql = LinqPostgreSqlConverter.Convert(QueryOperation.Any, where, order, skip, take, graph, modelDetail);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    using (var reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        var any = reader.HasRows;
                        return any;
                    }
                }
            }
        }

        public async Task<ICollection<TModel>> ExecuteQueryToModelManyAsync<TModel>(Expression where, QueryOrder order, int? skip, int? take, Graph graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var sql = LinqPostgreSqlConverter.Convert(QueryOperation.Many, where, order, skip, take, graph, modelDetail);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            var models = new List<TModel>();

                            var columnProperties = ReadColumns<TModel>(reader, modelDetail);

                            while (await reader.ReadAsync())
                            {
                                var model = ReadRow<TModel>(reader, modelDetail, columnProperties);
                                models.Add(model);
                            }

                            return models;
                        }
                        else
                        {
                            return Array.Empty<TModel>();
                        }
                    }
                }
            }
        }
        public async Task<TModel> ExecuteQueryToModelFirstAsync<TModel>(Expression where, QueryOrder order, int? skip, int? take, Graph graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var sql = LinqPostgreSqlConverter.Convert(QueryOperation.First, where, order, skip, take, graph, modelDetail);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            var columnProperties = ReadColumns<TModel>(reader, modelDetail);

                            _ = await reader.ReadAsync();

                            var model = ReadRow<TModel>(reader, modelDetail, columnProperties);
                            return model;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }
        public async Task<TModel> ExecuteQueryToModelSingleAsync<TModel>(Expression where, QueryOrder order, int? skip, int? take, Graph graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var sql = LinqPostgreSqlConverter.Convert(QueryOperation.Single, where, order, skip, take, graph, modelDetail);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            var columnProperties = ReadColumns<TModel>(reader, modelDetail);

                            _ = await reader.ReadAsync();

                            var model = ReadRow<TModel>(reader, modelDetail, columnProperties);

                            if (await reader.ReadAsync())
                                throw new Exception("More than a single entity found");

                            return model;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }
        public async Task<long> ExecuteQueryCountAsync(Expression where, QueryOrder order, int? skip, int? take, Graph graph, ModelDetail modelDetail)
        {
            var sql = LinqPostgreSqlConverter.Convert(QueryOperation.Count, where, order, skip, take, graph, modelDetail);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            _ = await reader.ReadAsync();
                            var count = reader.GetInt32(0);
                            return count;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            }
        }
        public async Task<bool> ExecuteQueryAnyAsync(Expression where, QueryOrder order, int? skip, int? take, Graph graph, ModelDetail modelDetail)
        {
            var sql = LinqPostgreSqlConverter.Convert(QueryOperation.Any, where, order, skip, take, graph, modelDetail);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                    {
                        var any = reader.HasRows;
                        return any;
                    }
                }
            }
        }

        public ICollection<object> ExecuteInsertGetIdentities<TModel>(TModel model, Graph<TModel> graph, ModelDetail modelDetail)
        {
            var sql = GenerateSqlInsert(model, graph, modelDetail, true);

            var allValues = new List<object>();
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    using (var reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                if (reader.FieldCount == 1)
                                {
                                    var value = reader[0];
                                    allValues.Add(value);
                                }
                                else
                                {
                                    var values = new List<object>();
                                    for (var i = 0; i < reader.FieldCount; i++)
                                    {
                                        var value = reader[i];
                                        values.Add(value);
                                    }
                                    allValues.Add(values.ToArray());
                                }
                            }
                        }
                    }
                }
            }

            return allValues;
        }
        public int ExecuteInsert<TModel>(TModel model, Graph<TModel> graph, ModelDetail modelDetail)
        {
            var sql = GenerateSqlInsert(model, graph, modelDetail, false);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    return command.ExecuteNonQuery();
                }
            }
        }
        public int ExecuteUpdate<TModel>(TModel model, Graph<TModel> graph, ModelDetail modelDetail)
        {
            var sql = GenerateSqlUpdate(model, graph, modelDetail);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    return command.ExecuteNonQuery();
                }
            }
        }
        public int ExecuteDelete(ICollection ids, ModelDetail modelDetail)
        {
            var sql = GenerateSqlDelete(ids, modelDetail);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    return command.ExecuteNonQuery();
                }
            }
        }

        public async Task<ICollection<object>> ExecuteInsertGetIdentitiesAsync<TModel>(TModel model, Graph<TModel> graph, ModelDetail modelDetail)
        {
            var sql = GenerateSqlInsert(model, graph, modelDetail, true);

            var allValues = new List<object>();
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                if (reader.FieldCount == 1)
                                {
                                    var value = reader[0];
                                    allValues.Add(value);
                                }
                                else
                                {
                                    var values = new List<object>();
                                    for (var i = 0; i < reader.FieldCount; i++)
                                    {
                                        var value = reader[i];
                                        values.Add(value);
                                    }
                                    allValues.Add(values.ToArray());
                                }
                            }
                        }
                    }
                }
            }

            return allValues;
        }
        public async Task<int> ExecuteInsertAsync<TModel>(TModel model, Graph<TModel> graph, ModelDetail modelDetail)
        {
            var sql = GenerateSqlInsert(model, graph, modelDetail, false);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }
        public async Task<int> ExecuteUpdateAsync<TModel>(TModel model, Graph<TModel> graph, ModelDetail modelDetail)
        {
            var sql = GenerateSqlUpdate(model, graph, modelDetail);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }
        public async Task<int> ExecuteDeleteAsync(ICollection ids, ModelDetail modelDetail)
        {
            var sql = GenerateSqlDelete(ids, modelDetail);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ExecuteSql(string sql)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    _ = command.ExecuteNonQuery();
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T ExecuteSqlScalar<T>(string sql)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    return (T)command.ExecuteScalar();
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ICollection<object> ExecuteSqlQuery(string sql)
        {
            var allValues = new List<object>();
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    using (var reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                if (reader.FieldCount == 1)
                                {
                                    var value = reader[0];
                                    allValues.Add(value);
                                }
                                else
                                {
                                    var values = new List<object>();
                                    for (var i = 0; i < reader.FieldCount; i++)
                                    {
                                        var value = reader[i];
                                        values.Add(value);
                                    }
                                    allValues.Add(values.ToArray());
                                }
                            }
                        }
                    }
                }
            }

            return allValues;
        }

        public IDataStoreGenerationPlan BuildStoreGenerationPlan(bool create, bool update, bool delete, ICollection<ModelDetail> modelDetails)
        {
            var connectionForParsing = new NpgsqlConnection(connectionString);
            var databaseName = connectionForParsing.Database;
            connectionForParsing.Dispose();

            bool needCreateDatabase;
            var sql = new List<string>();
            try
            {
                needCreateDatabase = NeedCreateDatabase(databaseName);

                AssureExtensions(sql, needCreateDatabase);

                var columnsToCheck = new List<ModelDetail>();
                foreach (var model in modelDetails)
                {
                    var needCreateTable = AssureTable(create, sql, needCreateDatabase, model);
                    if (!needCreateTable)
                        columnsToCheck.Add(model);
                }

                foreach (var model in columnsToCheck)
                {
                    var sqlColumns = GetSqlColumns(model);
                    var sqlConstraints = GetSqlConstraints(model);
                    AssureColumns(create, update, delete, sql, model, sqlColumns, sqlConstraints);
                }

                foreach (var model in modelDetails)
                {
                    var sqlConstraints = needCreateDatabase ? Array.Empty<SqlConstraint>() : GetSqlConstraints(model);
                    AssureConstraints(create, update, delete, sql, model, sqlConstraints);
                }
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync($"{nameof(PostgreSqlEngine)} error while reading datastore.", ex);
                throw;
            }

            var plan = new PostgreSqlDataStoreGenerationPlan(this, needCreateDatabase ? databaseName : null, sql);
            return plan;
        }



        private bool NeedCreateDatabase(string databaseName)
        {
            var sql = $"SELECT COUNT(1) FROM pg_database WHERE datname = '{databaseName.ToLower()}'";

            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            builder.Database = "postgres";
            var connectionStringForMaster = builder.ToString();

            bool needCreate;
            using (var connection = new NpgsqlConnection(connectionStringForMaster))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    needCreate = (long)command.ExecuteScalar() == 0;
                }
            }

            return needCreate;
        }

        internal void CreateDatabase(string databaseName)
        {
            var sql = $"CREATE DATABASE {databaseName}";

            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            builder.Database = "postgres";
            var connectionStringForMaster = builder.ToString();
            using (var connection = new NpgsqlConnection(connectionStringForMaster))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    _ = command.ExecuteNonQuery();
                }
            }
        }

        private void AssureExtensions(List<string> sql, bool needCreateDatabase)
        {
            if (!needCreateDatabase)
            {
                var sqlQuery = "SELECT COUNT(1) FROM pg_extension WHERE extname = 'uuid-ossp';";
                var exists = ExecuteSqlScalar<long>(sqlQuery) > 0;
                if (exists)
                    return;
            }

            sql.Add("CREATE EXTENSION \"uuid-ossp\";");
        }

        private bool AssureTable(bool create, List<string> sql, bool needCreateDatabase, ModelDetail model)
        {
            var nonIdentityColumns = model.Properties.Where(x => !x.IsIdentity && !x.IsDataSourceEntity && x.CoreType.HasValue || x.Type == typeof(byte[])).ToArray();
            var identityColumns = model.Properties.Where(x => x.IsIdentity).ToArray();

            if (nonIdentityColumns.Length + identityColumns.Length == 0)
                return false; //Cannot create table with no columns

            if (!needCreateDatabase)
            {
                var sqlQuery = $"SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME = '{model.DataSourceEntityName.ToLower()}'";
                var exists = ExecuteSqlScalar<long>(sqlQuery) > 0;
                if (exists)
                    return false;
            }

            if (create)
            {
                var sb = new StringBuilder();
                _ = sb.Append("CREATE TABLE ").Append(model.DataSourceEntityName).Append("(\r\n");

                for (var i = 0; i < identityColumns.Length; i++)
                {
                    var property = identityColumns[i];
                    if (i > 0)
                        _ = sb.Append(",\r\n");
                    _ = sb.Append("\t").Append(property.PropertySourceName).Append(" ");
                    WriteSqlTypeFromModel(sb, property);
                    WriteTypeEndingFromModel(sb, property);
                }
                for (var i = 0; i < nonIdentityColumns.Length; i++)
                {
                    var property = nonIdentityColumns[i];
                    if (i > 0 || identityColumns.Length > 0)
                        _ = sb.Append(",\r\n");
                    _ = sb.Append("\t").Append(property.PropertySourceName).Append(" ");
                    WriteSqlTypeFromModel(sb, property);
                    WriteTypeEndingFromModel(sb, property);
                }
                if (identityColumns.Length > 0)
                {
                    if (identityColumns.Length > 0 || nonIdentityColumns.Length > 0)
                        _ = sb.Append(',');
                    _ = sb.Append("\r\n\tCONSTRAINT PK_").Append(model.DataSourceEntityName).Append(" PRIMARY KEY(\r\n");

                    for (var i = 0; i < identityColumns.Length; i++)
                    {
                        var property = identityColumns[i];
                        if (i > 0)
                            _ = sb.Append(",\r\n");
                        _ = sb.Append("\t\t").Append(property.PropertySourceName);
                    }
                    _ = sb.Append("\r\n\t)\r\n");
                }
                _ = sb.Append("\r\n)");

                sql.Add(sb.ToString());
            }
            return true;
        }

        private void AssureColumns(bool create, bool update, bool delete, List<string> sql, ModelDetail model, SqlColumnType[] sqlColumns, SqlConstraint[] sqlConstraints)
        {
            var columns = model.Properties.Where(x => !x.IsDataSourceEntity && x.CoreType.HasValue || x.Type == typeof(byte[])).ToArray();

            var sb = new StringBuilder();

            foreach (var column in columns)
            {
                var sqlColumn = sqlColumns.FirstOrDefault(x => x.Table == model.DataSourceEntityName.ToLower() && x.Column == column.PropertySourceName.ToLower());
                if (sqlColumn == null)
                {
                    if (create)
                    {
                        _ = sb.Append("ALTER TABLE ").Append(model.DataSourceEntityName.ToLower()).Append(" ADD ").Append(column.Name).Append(" ");
                        WriteSqlTypeFromModel(sb, column);
                        WriteTypeEndingFromModel(sb, column);
                        _ = sb.Append(";");
                        sql.Add(sb.ToString());
                        _ = sb.Clear();
                    }
                }
                else
                {
                    if (update)
                    {
                        var sameType = ModelMatchesSqlType(column, sqlColumn);
                        if (!sameType)
                        {
                            if (sqlColumn.IsPrimaryKey || sqlColumn.IsIdentity || sqlColumn.IsPrimaryKey != column.IsIdentity || sqlColumn.IsIdentity != column.IsIdentityAutoGenerated)
                                throw new Exception($"{nameof(ITransactStoreEngine.BuildStoreGenerationPlan)} {nameof(PostgreSqlEngine)} cannot automatically change column with a Primary Key or Identity {model.Type.GetNiceName()}.{column.Name}");

                            var theseSqlConstraints = sqlConstraints.Where(x => (x.PK_Table == model.DataSourceEntityName.ToLower() && x.PK_Column == column.Name) || (x.FK_Table == model.DataSourceEntityName && x.FK_Column == column.Name)).ToArray();
                            if (theseSqlConstraints.Length > 0)
                            {
                                foreach (var sqlConstraint in theseSqlConstraints)
                                {
                                    _ = sb.Append("ALTER TABLE ").Append(sqlConstraint.FK_Table.ToLower()).Append(" DROP CONSTRAINT ").Append(sqlConstraint.FK_Name.ToLower()).Append(";");
                                    sql.Add(sb.ToString());
                                    _ = sb.Clear();
                                }
                            }

                            _ = sb.Append("ALTER TABLE ").Append(model.DataSourceEntityName.ToLower()).Append(" ALTER COLUMN ").Append(column.Name.ToLower()).Append(" TYPE ");
                            WriteSqlTypeFromModel(sb, column);
                            _ = sb.Append(";");
                            sql.Add(sb.ToString());
                            _ = sb.Clear();

                            _ = sb.Append("ALTER TABLE ").Append(model.DataSourceEntityName.ToLower()).Append(" ALTER COLUMN ").Append(column.Name.ToLower());
                            if (column.IsDataSourceNotNull) //model checks for identity not null
                            {
                                _ = sb.Append(" SET NOT NULL");
                                if (column.IsIdentity && !column.IsIdentityAutoGenerated)
                                {
                                    _ = sb.Append(" DEFAULT ");
                                    WriteDefaultValue(sb, column);
                                }
                            }
                            else
                            {
                                _ = sb.Append(" DROP NOT NULL");
                            }
                            _ = sb.Append(";");
                            sql.Add(sb.ToString());
                            _ = sb.Clear();
                        }
                    }
                }
            }

            if (delete)
            {
                //columns not in model
                foreach (var sqlColumn in sqlColumns.Where(x => x.Table == model.DataSourceEntityName.ToLower()))
                {
                    var column = columns.FirstOrDefault(x => x.PropertySourceName.ToLower() == sqlColumn.Column);
                    if (column == null)
                    {
                        var theseSqlConstraints = sqlConstraints.Where(x => (x.PK_Table == sqlColumn.Table && x.PK_Column == sqlColumn.Column) || (x.FK_Table == sqlColumn.Table && x.FK_Column == sqlColumn.Column)).ToArray();
                        if (theseSqlConstraints.Length > 0)
                        {
                            foreach (var sqlConstraint in theseSqlConstraints)
                            {
                                _ = sb.Append("ALTER TABLE ").Append(sqlConstraint.FK_Table.ToLower()).Append(" DROP CONSTRAINT ").Append(sqlConstraint.FK_Name.ToLower()).Append(";");
                                sql.Add(sb.ToString());
                                _ = sb.Clear();
                            }
                        }

                        if (!sqlColumn.IsNullable)
                        {
                            if (sqlColumn.IsPrimaryKey || sqlColumn.IsIdentity)
                                throw new Exception($"{nameof(ITransactStoreEngine.BuildStoreGenerationPlan)} {nameof(PostgreSqlEngine)} needs to make {sqlColumn} nullable but cannot automatically change column with a Primary Key or Identity");

                            _ = sb.Append("ALTER TABLE ").Append(model.DataSourceEntityName.ToLower()).Append(" ALTER COLUMN ").Append(sqlColumn.Column.ToLower()).Append(" DROP NOT NULL;");
                            sql.Add(sb.ToString());
                            _ = sb.Clear();
                        }
                    }
                }
            }
        }

        private void AssureConstraints(bool create, bool update, bool delete, List<string> sql, ModelDetail model, SqlConstraint[] sqlConstraints)
        {
            var constraintNameDictionary = sqlConstraints.Select(x => x.FK_Name).ToDictionary(x => x, x => 0);

            var sb = new StringBuilder();

            var fkTable = model.DataSourceEntityName;

            var usedSqlConstraints = new List<SqlConstraint>();
            foreach (var columnForRelation in model.RelatedNonEnumerableProperties)
            {
                var relatedModelDetails = ModelAnalyzer.GetModel(columnForRelation.InnerType);
                if (relatedModelDetails.IdentityProperties.Count != 1)
                    throw new Exception($"{nameof(ITransactStoreEngine.BuildStoreGenerationPlan)} {nameof(PostgreSqlEngine)} does not support automatic constraints with multiple identities {relatedModelDetails.Type.GetNiceName()}");

                var fkColumn = columnForRelation.ForeignIdentity;
                var pkTable = relatedModelDetails.DataSourceEntityName;
                var pkColumn = relatedModelDetails.IdentityProperties[0].Name;

                var sqlConstraint = sqlConstraints.FirstOrDefault(x => x.FK_Table == fkTable.ToLower() && x.FK_Column == fkColumn.ToLower() && x.PK_Table == pkTable.ToLower() && x.PK_Column == pkColumn.ToLower());
                if (sqlConstraint == null)
                {
                    if (create)
                    {
                        var baseConstraintName = $"FK_{fkTable}_{pkTable}";
                        string constraintName;
                        if (constraintNameDictionary.TryGetValue(baseConstraintName, out var constraintNameIndex))
                        {
                            constraintNameIndex++;
                            constraintName = baseConstraintName + constraintNameIndex;
                            while (constraintNameDictionary.Keys.Contains(constraintName))
                            {
                                constraintNameIndex++;
                                constraintName = baseConstraintName + constraintNameIndex;
                            }
                            constraintNameDictionary[constraintName] = constraintNameIndex;
                        }
                        else
                        {
                            constraintNameDictionary.Add(baseConstraintName, 0);
                            constraintName = baseConstraintName;
                        }
                        _ = sb.Append("ALTER TABLE ").Append(fkTable.ToLower()).Append(" ADD CONSTRAINT ").Append(constraintName).Append(" FOREIGN KEY (").Append(fkColumn.ToLower()).Append(") REFERENCES ").Append(pkTable.ToLower()).Append("(").Append(pkColumn.ToLower()).Append(");");
                        sql.Add(sb.ToString());
                        _ = sb.Clear();
                    }
                }
                else
                {
                    usedSqlConstraints.Add(sqlConstraint);
                }
            }

            if (delete)
            {
                //foreign keys not in model
                foreach (var sqlConstraint in sqlConstraints.Where(x => x.FK_Table == fkTable))
                {
                    if (!usedSqlConstraints.Contains(sqlConstraint))
                    {
                        _ = sb.Append("ALTER TABLE ").Append(sqlConstraint.FK_Table.ToLower()).Append(" DROP CONSTRAINT ").Append(sqlConstraint.FK_Name.ToLower()).Append(";");
                        sql.Add(sb.ToString());
                        _ = sb.Clear();
                    }
                }
            }
        }

        public static void WriteSqlTypeFromModel(StringBuilder sb, ModelPropertyDetail property)
        {
            var canBeIdentity = false;
            if (property.CoreType.HasValue)
            {
                switch (property.CoreType.Value)
                {
                    case CoreType.Boolean:
                    case CoreType.BooleanNullable:
                        _ = sb.Append("boolean");
                        break;
                    case CoreType.Byte:
                    case CoreType.ByteNullable:
                        _ = sb.Append("smallint");
                        canBeIdentity = true;
                        break;
                    case CoreType.Int16:
                    case CoreType.Int16Nullable:
                        _ = sb.Append("smallint");
                        canBeIdentity = true;
                        break;
                    case CoreType.Int32:
                    case CoreType.Int32Nullable:
                        _ = sb.Append("int");
                        canBeIdentity = true;
                        break;
                    case CoreType.Int64:
                    case CoreType.Int64Nullable:
                        _ = sb.Append("bigint");
                        canBeIdentity = true;
                        break;
                    case CoreType.Single:
                    case CoreType.SingleNullable:
                        _ = sb.Append("real");
                        break;
                    case CoreType.Double:
                    case CoreType.DoubleNullable:
                        _ = sb.Append("double precision");
                        break;
                    case CoreType.Decimal:
                    case CoreType.DecimalNullable:
                        _ = sb.Append("decimal(").Append(property.DataSourcePrecisionLength ?? 19).Append(", ").Append(property.DataSourceScale ?? 5).Append(')');
                        canBeIdentity = property.DataSourceScale == 0;
                        break;
                    case CoreType.Char:
                    case CoreType.CharNullable:
                        _ = sb.Append("varchar(").Append(property.DataSourcePrecisionLength ?? 1).Append(')');
                        break;
                    case CoreType.DateTime:
                    case CoreType.DateTimeNullable:
                        _ = sb.Append("timestamp(").Append(property.DataSourcePrecisionLength ?? 0).Append(')');
                        break;
                    case CoreType.DateTimeOffset:
                    case CoreType.DateTimeOffsetNullable:
                        _ = sb.Append("timestamp(").Append(property.DataSourcePrecisionLength ?? 0).Append(')');
                        break;
                    case CoreType.TimeSpan:
                    case CoreType.TimeSpanNullable:
                        _ = sb.Append("time(").Append(property.DataSourcePrecisionLength ?? 0).Append(')');
                        break;
                    case CoreType.Guid:
                    case CoreType.GuidNullable:
                        _ = sb.Append("uuid");
                        break;
                    case CoreType.String:
                        if (property.DataSourcePrecisionLength.HasValue)
                            _ = sb.Append("varchar(").Append(property.DataSourcePrecisionLength.Value).Append(')');
                        else
                            _ = sb.Append("text");
                        break;
                    default:
                        throw new Exception($"Cannot match type {property.Type.GetNiceName()} to an {nameof(PostgreSqlEngine)} type.");
                }
            }
            else if (property.Type == typeof(byte[]))
            {
                if (property.DataSourcePrecisionLength.HasValue)
                    _ = sb.Append("bit(").Append(property.DataSourcePrecisionLength.Value).Append(')');
                else
                    _ = sb.Append("bytea");
            }
            else
            {
                throw new Exception($"Cannot match type {property.Type.GetNiceName()} to an {nameof(PostgreSqlEngine)} type.");
            }
        }
        public static void WriteTypeEndingFromModel(StringBuilder sb, ModelPropertyDetail property)
        {
            var canBeIdentity = false;
            if (property.CoreType.HasValue)
            {
                switch (property.CoreType.Value)
                {
                    case CoreType.Byte:
                    case CoreType.ByteNullable:
                    case CoreType.Int16:
                    case CoreType.Int16Nullable:
                    case CoreType.Int32:
                    case CoreType.Int32Nullable:
                    case CoreType.Int64:
                    case CoreType.Int64Nullable:
                        canBeIdentity = true;
                        break;
                }
            }

            if (property.IsDataSourceNotNull) //model checks for identity not null
            {
                _ = sb.Append(" NOT NULL");
                if (property.IsIdentity && !property.IsIdentityAutoGenerated)
                {
                    _ = sb.Append(" DEFAULT ");
                    WriteDefaultValue(sb, property);
                }
            }
            else
            {
                _ = sb.Append(" NULL");
            }

            if (property.IsIdentity && property.IsIdentityAutoGenerated)
            {
                if (!canBeIdentity)
                    throw new Exception($"{nameof(ITransactStoreEngine.BuildStoreGenerationPlan)} {nameof(PostgreSqlEngine)} {property.Type.GetNiceName()} {property.Name} cannot be an auto generated identity");
                _ = sb.Append(" GENERATED ALWAYS AS IDENTITY");
            }
        }

        private static void WriteDefaultValue(StringBuilder sb, ModelPropertyDetail property)
        {
            if (property.CoreType.HasValue)
            {
                switch (property.CoreType.Value)
                {
                    case CoreType.Boolean:
                    case CoreType.BooleanNullable:
                    case CoreType.Byte:
                    case CoreType.ByteNullable:
                    case CoreType.Int16:
                    case CoreType.Int16Nullable:
                    case CoreType.Int32:
                    case CoreType.Int32Nullable:
                    case CoreType.Int64:
                    case CoreType.Int64Nullable:
                    case CoreType.Single:
                    case CoreType.SingleNullable:
                    case CoreType.Double:
                    case CoreType.DoubleNullable:
                    case CoreType.Decimal:
                    case CoreType.DecimalNullable:
                        _ = sb.Append('0');
                        break;
                    case CoreType.Char:
                    case CoreType.CharNullable:
                        _ = sb.Append("''");
                        break;
                    case CoreType.DateTime:
                    case CoreType.DateTimeNullable:
                    case CoreType.DateTimeOffset:
                    case CoreType.DateTimeOffsetNullable:
                    case CoreType.TimeSpan:
                    case CoreType.TimeSpanNullable:
                        _ = sb.Append("CAST(0 AS timestamp)");
                        break;
                    case CoreType.Guid:
                    case CoreType.GuidNullable:
                        _ = sb.Append("uuid_nil()");
                        break;
                    case CoreType.String:
                        _ = sb.Append("''");
                        break;
                    default:
                        throw new Exception($"Cannot match type {property.Type.GetNiceName()} to an {nameof(PostgreSqlEngine)} type.");
                }
            }
            else if (property.Type == typeof(byte[]))
            {
                _ = sb.Append("E'\\x0'");
            }
            else
            {
                throw new Exception($"Cannot match type {property.Type.GetNiceName()} to an {nameof(PostgreSqlEngine)} type.");
            }
        }

        private static bool ModelMatchesSqlType(ModelPropertyDetail property, SqlColumnType sqlColumn)
        {
            if (sqlColumn.IsPrimaryKey != property.IsIdentity || sqlColumn.IsIdentity != property.IsIdentityAutoGenerated)
                return false;

            if (property.CoreType.HasValue)
            {
                switch (property.CoreType.Value)
                {
                    case CoreType.Boolean: return sqlColumn.DataType == "boolean" && sqlColumn.IsNullable == false;
                    case CoreType.Byte: return sqlColumn.DataType == "smallint" && sqlColumn.IsNullable == false;
                    case CoreType.Int16: return sqlColumn.DataType == "smallint" && sqlColumn.IsNullable == false;
                    case CoreType.Int32: return sqlColumn.DataType == "integer" && sqlColumn.IsNullable == false;
                    case CoreType.Int64: return sqlColumn.DataType == "bigint" && sqlColumn.IsNullable == false;
                    case CoreType.Single: return sqlColumn.DataType == "real" && sqlColumn.IsNullable == false;
                    case CoreType.Double: return sqlColumn.DataType == "double precision" && sqlColumn.IsNullable == false;
                    case CoreType.Decimal: return sqlColumn.DataType == "numeric" && sqlColumn.IsNullable == false && sqlColumn.NumericPrecision == (property.DataSourcePrecisionLength ?? 19) && sqlColumn.NumericScale == (property.DataSourceScale ?? 5);
                    case CoreType.Char: return sqlColumn.DataType == "character varying" && sqlColumn.IsNullable == false && sqlColumn.CharacterMaximumLength == (property.DataSourcePrecisionLength ?? 1);
                    case CoreType.DateTime: return sqlColumn.DataType == "timestamp without time zone" && sqlColumn.IsNullable == false && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.DateTimeOffset: return sqlColumn.DataType == "timestamp without time zone" && sqlColumn.IsNullable == false && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.TimeSpan: return sqlColumn.DataType == "time without time zone" && sqlColumn.IsNullable == false && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.Guid: return sqlColumn.DataType == "uuid" && sqlColumn.IsNullable == false;

                    case CoreType.BooleanNullable: return sqlColumn.DataType == "boolean" && sqlColumn.IsNullable == true;
                    case CoreType.ByteNullable: return sqlColumn.DataType == "smallint" && sqlColumn.IsNullable == true;
                    case CoreType.Int16Nullable: return sqlColumn.DataType == "smallint" && sqlColumn.IsNullable == true;
                    case CoreType.Int32Nullable: return sqlColumn.DataType == "integer" && sqlColumn.IsNullable == true;
                    case CoreType.Int64Nullable: return sqlColumn.DataType == "bigint" && sqlColumn.IsNullable == true;
                    case CoreType.SingleNullable: return sqlColumn.DataType == "real" && sqlColumn.IsNullable == true;
                    case CoreType.DoubleNullable: return sqlColumn.DataType == "double precision" && sqlColumn.IsNullable == true;
                    case CoreType.DecimalNullable: return sqlColumn.DataType == "numeric" && sqlColumn.IsNullable == true && sqlColumn.NumericPrecision == (property.DataSourcePrecisionLength ?? 19) && sqlColumn.NumericScale == (property.DataSourceScale ?? 5);
                    case CoreType.CharNullable: return sqlColumn.DataType == "character varying" && sqlColumn.IsNullable == true && sqlColumn.CharacterMaximumLength == (property.DataSourcePrecisionLength ?? 1);
                    case CoreType.DateTimeNullable: return sqlColumn.DataType == "timestamp without time zone" && sqlColumn.IsNullable == true && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.DateTimeOffsetNullable: return sqlColumn.DataType == "timestamp without time zone" && sqlColumn.IsNullable == true && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.TimeSpanNullable: return sqlColumn.DataType == "time without time zone" && sqlColumn.IsNullable == true && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.GuidNullable: return sqlColumn.DataType == "uuid" && sqlColumn.IsNullable == true;

                    case CoreType.String:
                        if (sqlColumn.NumericPrecision.HasValue)
                            return sqlColumn.DataType == "varchar" && sqlColumn.IsNullable == !property.IsDataSourceNotNull;
                        else
                            return sqlColumn.DataType == "text" && sqlColumn.IsNullable == !property.IsDataSourceNotNull;
                }
            }

            if (property.Type == typeof(byte[]))
            {
                if (sqlColumn.NumericPrecision.HasValue)
                    return sqlColumn.DataType == "bit" && sqlColumn.IsNullable == !property.IsDataSourceNotNull;
                else
                    return sqlColumn.DataType == "bytea" && sqlColumn.IsNullable == !property.IsDataSourceNotNull;
            }

            throw new Exception($"{nameof(ITransactStoreEngine.BuildStoreGenerationPlan)} cannot match type {property.Type.GetNiceName()} to an {nameof(PostgreSqlEngine)} type.");
        }
        private SqlColumnType[] GetSqlColumns(ModelDetail model)
        {
            var query = $@"SELECT C.TABLE_NAME, C.COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, DATETIME_PRECISION
	,C.IDENTITY_GENERATION = 'ALWAYS' OR C.IS_GENERATED = 'ALWAYS' AS IS_IDENTITY
	,RC.CONSTRAINT_TYPE = 'PRIMARY KEY' AS IS_PRIMARYKEY
FROM INFORMATION_SCHEMA.COLUMNS C
LEFT OUTER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KF ON KF.COLUMN_NAME = C.COLUMN_NAME AND C.TABLE_NAME = KF.TABLE_NAME AND C.TABLE_SCHEMA = KF.TABLE_SCHEMA
LEFT OUTER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS RC ON RC.CONSTRAINT_NAME = KF.CONSTRAINT_NAME AND RC.TABLE_NAME = KF.TABLE_NAME AND RC.TABLE_SCHEMA = KF.TABLE_SCHEMA
WHERE C.TABLE_NAME = '{model.DataSourceEntityName.ToLower()}'";

            var sqlColumns = ExecuteSqlQuery(query).Select(x => (IList<object>)x).Select(x => new SqlColumnType()
            {
                Table = (string)x[0],
                Column = (string)x[1],
                DataType = (string)x[2],
                IsNullable = (string)x[3] == "YES",
                CharacterMaximumLength = x[4] != DBNull.Value ? (int)x[4] : (int?)null,
                NumericPrecision = x[5] != DBNull.Value ? (int)x[5] : (int?)null,
                NumericScale = x[6] != DBNull.Value ? (int)x[6] : (int?)null,
                DatetimePrecision = x[7] != DBNull.Value ? (int)x[7] : (int?)null,
                IsIdentity = x[8] != DBNull.Value ? (bool)x[8] : false,
                IsPrimaryKey = x[9] != DBNull.Value ? (bool)x[9] : false,
            }).ToArray();

            return sqlColumns;
        }
        private SqlConstraint[] GetSqlConstraints(ModelDetail model)
        {
            var query = $@"SELECT RC.CONSTRAINT_NAME FK_Name, KF.TABLE_SCHEMA FK_Schema, KF.TABLE_NAME FK_Table, KF.COLUMN_NAME FK_Column, RC.UNIQUE_CONSTRAINT_NAME PK_Name, KP.TABLE_SCHEMA PK_Schema, KP.TABLE_NAME PK_Table, KP.COLUMN_NAME PK_Column
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC
LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KF ON RC.CONSTRAINT_NAME = KF.CONSTRAINT_NAME AND RC.CONSTRAINT_SCHEMA = KF.CONSTRAINT_SCHEMA
LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KP ON RC.UNIQUE_CONSTRAINT_NAME = KP.CONSTRAINT_NAME AND RC.CONSTRAINT_SCHEMA = KP.CONSTRAINT_SCHEMA
LEFT JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS TCKF ON KF.CONSTRAINT_NAME = TCKF.CONSTRAINT_NAME AND KF.CONSTRAINT_SCHEMA = TCKF.CONSTRAINT_SCHEMA
WHERE TCKF.CONSTRAINT_TYPE = 'FOREIGN KEY'
AND KF.TABLE_NAME = '{model.DataSourceEntityName.ToLower()}'";

            var sqlConstrains = ExecuteSqlQuery(query).Select(x => (IList<object>)x).Select(x => new SqlConstraint()
            {
                FK_Name = (string)x[0],
                FK_Schema = (string)x[1],
                FK_Table = (string)x[2],
                FK_Column = (string)x[3],
                PK_Name = (string)x[4],
                PK_Schema = (string)x[5],
                PK_Table = (string)x[6],
                PK_Column = (string)x[7],
            }).ToArray();

            return sqlConstrains;
        }

        public bool ValidateDataSource()
        {
            if (String.IsNullOrWhiteSpace(connectionString))
                return false;

            const string sql = "SELECT version()";

            try
            {
                var builder = new NpgsqlConnectionStringBuilder(connectionString);
                builder.Database = "postgres";
                var connectionStringForMaster = builder.ToString();

                using (var connection = new NpgsqlConnection(connectionStringForMaster))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandTimeout = 0;
                        command.CommandText = sql;
                        var version = (string)command.ExecuteScalar();
                        if (version.Contains("PostgreSQL"))
                            return true;

                        _ = Log.ErrorAsync($"{nameof(PostgreSqlEngine)} failed to validate: Invalid version {version}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync($"{nameof(PostgreSqlEngine)} failed to validate", ex);
            }
            return false;
        }
    }
}
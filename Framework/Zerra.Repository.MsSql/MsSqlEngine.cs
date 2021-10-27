﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Zerra.Repository.Reflection;
using Zerra.Reflection;
using System.Text;
using System.Linq;
using Zerra.Logging;
using System.Data;
using System.Runtime.CompilerServices;
using System.Collections;
using Zerra.IO;

namespace Zerra.Repository.MsSql
{
    public sealed class MsSqlEngine : ITransactStoreEngine
    {
        private readonly string connectionString;
        public MsSqlEngine(string connectionString)
        {
            this.connectionString = connectionString;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static CoreTypeSetter<TModel>[] ReadColumns<TModel>(SqlDataReader reader, ModelDetail modelDetail)
        {
            var columnProperties = new CoreTypeSetter<TModel>[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var property = reader.GetName(i);
                if (modelDetail.TryGetProperty(property, out ModelPropertyDetail propertyInfo))
                    columnProperties[i] = (CoreTypeSetter<TModel>)propertyInfo.CoreTypeSetter;
            }
            return columnProperties;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TModel ReadRow<TModel>(SqlDataReader reader, ModelDetail modelDetail, CoreTypeSetter<TModel>[] columnProperties)
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
                                    var sqlValue = reader.GetSqlString(i);
                                    if (!sqlValue.IsNull)
                                        columnProperty.Setter(model, sqlValue.Value[0]);
                                }
                                break;
                            case CoreType.DateTime:
                                {
                                    var value = reader.GetDateTime(i);
                                    columnProperty.Setter(model, value);
                                }
                                break;
                            case CoreType.DateTimeOffset:
                                {
                                    var value = reader.GetDateTimeOffset(i);
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
                                    var sqlValue = reader.GetSqlString(i);
                                    if (!sqlValue.IsNull)
                                        columnProperty.Setter(model, sqlValue.Value);
                                }
                                break;
                            case CoreType.BooleanNullable:
                                {
                                    var sqlValue = reader.GetSqlBoolean(i);
                                    if (!sqlValue.IsNull)
                                        columnProperty.Setter(model, (bool?)sqlValue.Value);
                                }
                                break;
                            case CoreType.ByteNullable:
                                {
                                    var sqlValue = reader.GetSqlByte(i);
                                    if (!sqlValue.IsNull)
                                        columnProperty.Setter(model, (byte?)sqlValue.Value);
                                }
                                break;
                            case CoreType.Int16Nullable:
                                {
                                    var sqlValue = reader.GetSqlInt16(i);
                                    if (!sqlValue.IsNull)
                                        columnProperty.Setter(model, (short?)sqlValue.Value);
                                }
                                break;
                            case CoreType.Int32Nullable:
                                {
                                    var sqlValue = reader.GetSqlInt32(i);
                                    if (!sqlValue.IsNull)
                                        columnProperty.Setter(model, (int?)sqlValue.Value);
                                }
                                break;
                            case CoreType.Int64Nullable:
                                {
                                    var sqlValue = reader.GetSqlInt64(i);
                                    if (!sqlValue.IsNull)
                                        columnProperty.Setter(model, (long?)sqlValue.Value);
                                }
                                break;
                            case CoreType.SingleNullable:
                                {
                                    var sqlValue = reader.GetSqlSingle(i);
                                    if (!sqlValue.IsNull)
                                        columnProperty.Setter(model, (float?)sqlValue.Value);
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
                                    var sqlValue = reader.GetSqlChars(i);
                                    if (!sqlValue.IsNull && sqlValue.Value.Length > 0)
                                        columnProperty.Setter(model, (char?)sqlValue.Value[0]);
                                }
                                break;
                            case CoreType.DateTimeNullable:
                                {
                                    var value = reader.GetValue(i); //GetSqlDateTime doesn't handle null
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (DateTime?)value);
                                }
                                break;
                            case CoreType.DateTimeOffsetNullable:
                                {
                                    var value = reader.GetValue(i); //GetSqlDateTime doesn't handle null
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (DateTimeOffset?)value);
                                }
                                break;
                            case CoreType.TimeSpanNullable:
                                {
                                    var value = reader.GetValue(i); //GetSqlDateTime doesn't handle null
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (TimeSpan?)value);
                                }
                                break;
                            case CoreType.GuidNullable:
                                {
                                    var sqlValue = reader.GetSqlGuid(i);
                                    if (!sqlValue.IsNull)
                                        columnProperty.Setter(model, (Guid?)sqlValue.Value);
                                }
                                break;

                            default:
                                throw new NotSupportedException($"{nameof(MsSqlEngine)} Cannot map to type {columnProperty.CoreType.Value} in {modelDetail.Type.GetNiceName()}");
                        }

                    }
                    else if (columnProperty.IsByteArray)
                    {
                        if (!reader.IsDBNull(i))
                        {
                            var sqlValue = reader.GetSqlBytes(i);
                            if (!sqlValue.IsNull)
                                columnProperty.Setter(model, sqlValue.Value);
                        }
                    }
                    else
                    {
                        throw new NotSupportedException($"{nameof(MsSqlEngine)} cannot map to type in {modelDetail.Type.GetNiceName()}");
                    }
                }
            }

            return model;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GenerateSqlInsert<TModel>(TModel model, Graph<TModel> graph, ModelDetail modelDetail, bool getIdentities)
        {
            var sbColumns = new CharWriteBuffer();
            var sbValues = new CharWriteBuffer();
            try
            {
                foreach (var modelPropertyDetail in modelDetail.NonAutoGeneratedNonRelationProperties)
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
                if (getIdentities)
                {
                    if (sbColumns.Length > 0 && sbValues.Length > 0)
                        sql = $"INSERT INTO [{modelDetail.DataSourceEntityName}] ({sbColumns.ToString()}) VALUES ({sbValues.ToString()}) \r\n SELECT SCOPE_IDENTITY() AS SCOPE_IDENTITY";
                    else
                        sql = $"INSERT INTO [{modelDetail.DataSourceEntityName}] DEFAULT VALUES \r\n SELECT SCOPE_IDENTITY() AS SCOPE_IDENTITY";
                }
                else
                {
                    if (sbColumns.Length > 0 && sbValues.Length > 0)
                        sql = $"INSERT INTO [{modelDetail.DataSourceEntityName}] ({sbColumns.ToString()}) VALUES ({sbValues.ToString()})";
                    else
                        sql = $"INSERT INTO [{modelDetail.DataSourceEntityName}] DEFAULT VALUES";
                }
                return sql;
            }
            finally
            {
                sbColumns.Dispose();
                sbValues.Dispose();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GenerateSqlUpdate<TModel>(TModel model, Graph<TModel> graph, ModelDetail modelDetail)
        {
            if (modelDetail.IdentityProperties.Count == 0)
                return null;

            var writer = new CharWriteBuffer();
            try
            {
                writer.Write("UPDATE [");
                writer.Write(modelDetail.DataSourceEntityName);
                writer.Write("] SET ");
                bool first = true;
                foreach (var modelPropertyInfo in modelDetail.NonAutoGeneratedNonRelationProperties)
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
                foreach (var modelPropertyInfo in modelDetail.IdentityProperties)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GenerateSqlDelete(ICollection ids, ModelDetail modelDetail)
        {
            var sbWhere = new CharWriteBuffer();
            try
            {
                sbWhere.Write($"DELETE FROM [");
                sbWhere.Write(modelDetail.DataSourceEntityName);
                sbWhere.Write("] WHERE ");
                var first = true;
                foreach (object id in ids)
                {
                    if (first) first = false;
                    else sbWhere.Write(" OR ");
                    sbWhere.Write('(');

                    if (modelDetail.IdentityProperties.Count == 1)
                    {
                        var modelPropertyInfo = modelDetail.IdentityProperties[0];
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
                        foreach (var modelPropertyInfo in modelDetail.IdentityProperties)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        public ICollection<TModel> ExecuteQueryToModelMany<TModel>(Expression where, QueryOrder order, int? skip, int? take, Graph graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var sql = LinqMsSqlConverter.Convert(QueryOperation.Many, where, order, skip, take, graph, modelDetail);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
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
            var sql = LinqMsSqlConverter.Convert(QueryOperation.First, where, order, skip, take, graph, modelDetail);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    using (var reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            var columnProperties = ReadColumns<TModel>(reader, modelDetail);

                            reader.Read();

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
            var sql = LinqMsSqlConverter.Convert(QueryOperation.Single, where, order, skip, take, graph, modelDetail);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    using (var reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            var columnProperties = ReadColumns<TModel>(reader, modelDetail);

                            reader.Read();

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
            var sql = LinqMsSqlConverter.Convert(QueryOperation.Count, where, order, skip, take, graph, modelDetail);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    using (var reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
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
            var sql = LinqMsSqlConverter.Convert(QueryOperation.Any, where, order, skip, take, graph, modelDetail);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
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
            var sql = LinqMsSqlConverter.Convert(QueryOperation.Many, where, order, skip, take, graph, modelDetail);

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
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
            var sql = LinqMsSqlConverter.Convert(QueryOperation.First, where, order, skip, take, graph, modelDetail);

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            var columnProperties = ReadColumns<TModel>(reader, modelDetail);

                            await reader.ReadAsync();

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
            var sql = LinqMsSqlConverter.Convert(QueryOperation.Single, where, order, skip, take, graph, modelDetail);

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            var columnProperties = ReadColumns<TModel>(reader, modelDetail);

                            await reader.ReadAsync();

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
            var sql = LinqMsSqlConverter.Convert(QueryOperation.Count, where, order, skip, take, graph, modelDetail);

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
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
            var sql = LinqMsSqlConverter.Convert(QueryOperation.Any, where, order, skip, take, graph, modelDetail);

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
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

            List<object> allValues = new List<object>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
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
                                    for (int i = 0; i < reader.FieldCount; i++)
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

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    return command.ExecuteNonQuery();
                }
            }
        }
        public int ExecuteUpdate<TModel>(TModel model, Graph<TModel> graph, ModelDetail modelDetail)
        {
            var sql = GenerateSqlUpdate(model, graph, modelDetail);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    return command.ExecuteNonQuery();
                }
            }
        }
        public int ExecuteDelete(ICollection ids, ModelDetail modelDetail)
        {
            var sql = GenerateSqlDelete(ids, modelDetail);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    return command.ExecuteNonQuery();
                }
            }
        }

        public async Task<ICollection<object>> ExecuteInsertGetIdentitiesAsync<TModel>(TModel model, Graph<TModel> graph, ModelDetail modelDetail)
        {
            var sql = GenerateSqlInsert(model, graph, modelDetail, true);

            var allValues = new List<object>();
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
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
                                    for (int i = 0; i < reader.FieldCount; i++)
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

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }
        public async Task<int> ExecuteUpdateAsync<TModel>(TModel model, Graph<TModel> graph, ModelDetail modelDetail)
        {
            var sql = GenerateSqlUpdate(model, graph, modelDetail);

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }
        public async Task<int> ExecuteDeleteAsync(ICollection ids, ModelDetail modelDetail)
        {
            var sql = GenerateSqlDelete(ids, modelDetail);

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ExecuteSql(string sql)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    return command.ExecuteNonQuery();
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ICollection<object> ExecuteSqlQuery(string sql)
        {
            var allValues = new List<object>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
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
                                    for (int i = 0; i < reader.FieldCount; i++)
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

        public void BuildStoreFromModels(ICollection<ModelDetail> modelDetails)
        {
            var connectionForParsing = new SqlConnection(connectionString);
            var databaseName = connectionForParsing.Database;
            connectionForParsing.Dispose();

            try
            {
                AssureDatabase(databaseName);

                foreach (var model in modelDetails)
                {
                    AssureTable(model);
                }

                var sqlColumns = GetSqlColumns();
                var sqlConstraints = GetSqlConstraints();
                foreach (var model in modelDetails)
                {
                    AssureColumns(model, sqlColumns, sqlConstraints);
                }

                foreach (var model in modelDetails)
                {
                    AssureConstraints(model, sqlConstraints);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorAsync($"{nameof(ITransactStoreEngine.BuildStoreFromModels)} {nameof(MsSqlEngine)} error while assuring datastore.", ex);
                throw;
            }
        }

        private void AssureDatabase(string databaseName)
        {
            var sb = new StringBuilder();
            sb.Append("IF NOT EXISTS (SELECT [dbid] FROM master.dbo.sysdatabases WHERE [name] = '").Append(databaseName).Append("')\r\n");
            sb.Append("BEGIN\r\n");
            sb.Append("\tCREATE DATABASE [").Append(databaseName).Append("]\r\n");
            sb.Append("END\r\n");

            var sql = sb.ToString();

            var builder = new SqlConnectionStringBuilder(connectionString);
            builder.InitialCatalog = "master";
            var connectionStringForMaster = builder.ToString();
            using (var connection = new SqlConnection(connectionStringForMaster))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
        }

        private void AssureTable(ModelDetail model)
        {
            var nonIdentityColumns = model.Properties.Where(x => !x.IsIdentity && !x.IsDataSourceEntity && x.CoreType.HasValue || x.Type == typeof(byte[])).ToArray();
            var identityColumns = model.Properties.Where(x => x.IsIdentity).ToArray();

            if (nonIdentityColumns.Length + identityColumns.Length == 0)
                return; //Cannot create table with no columns

            var sb = new StringBuilder();
            sb.Append("IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' AND TABLE_NAME='").Append(model.DataSourceEntityName).Append("')\r\n");
            sb.Append("BEGIN\r\n");
            sb.Append("CREATE TABLE [").Append(model.DataSourceEntityName).Append("](\r\n");

            for (var i = 0; i < identityColumns.Length; i++)
            {
                var property = identityColumns[i];
                if (i > 0)
                    sb.Append(",\r\n");
                sb.Append("\t[").Append(property.PropertySourceName).Append("] ");
                WriteSqlTypeFromModel(sb, property);
            }
            for (var i = 0; i < nonIdentityColumns.Length; i++)
            {
                var property = nonIdentityColumns[i];
                if (i > 0 || identityColumns.Length > 0)
                    sb.Append(",\r\n");
                sb.Append("\t[").Append(property.PropertySourceName).Append("] ");
                WriteSqlTypeFromModel(sb, property);
            }
            if (identityColumns.Length > 0)
            {
                sb.Append("\r\n\tCONSTRAINT [PK_").Append(model.DataSourceEntityName).Append("] PRIMARY KEY CLUSTERED(\r\n");

                for (var i = 0; i < identityColumns.Length; i++)
                {
                    var property = identityColumns[i];
                    if (i > 0)
                        sb.Append(",\r\n");
                    sb.Append("\t\t[").Append(property.PropertySourceName).Append("] ASC");
                }
                sb.Append("\r\n\t)\r\n");
            }
            sb.Append("\tON [PRIMARY]");
            sb.Append("\r\n)");
            sb.Append("\r\nON [PRIMARY]");
            sb.Append("\r\n");
            sb.Append("END");

            var sql = sb.ToString();
            ExecuteSql(sql);
        }

        private void AssureColumns(ModelDetail model, SqlColumnType[] sqlColumns, SqlConstraint[] sqlConstraints)
        {
            var columns = model.Properties.Where(x => !x.IsDataSourceEntity && x.CoreType.HasValue || x.Type == typeof(byte[])).ToArray();

            var sqlStatements = 0;
            var sb = new StringBuilder();

            foreach (var column in columns)
            {
                var sqlColumn = sqlColumns.FirstOrDefault(x => x.Table == model.DataSourceEntityName && x.Column == column.PropertySourceName);
                if (sqlColumn == null)
                {
                    sb.Append("ALTER TABLE [").Append(model.DataSourceEntityName).Append("] ADD [").Append(column.Name).Append("] ");
                    WriteSqlTypeFromModel(sb, column);
                    sb.Append("\r\n");
                    sqlStatements++;
                }
                else
                {
                    var sameType = ModelMatchesSqlType(column, sqlColumn);
                    if (!sameType)
                    {
                        if (sqlColumn.IsPrimaryKey || sqlColumn.IsIdentity || sqlColumn.IsPrimaryKey != column.IsIdentity || sqlColumn.IsIdentity != column.IsIdentityAutoGenerated)
                            throw new Exception($"{nameof(ITransactStoreEngine.BuildStoreFromModels)} {nameof(MsSqlEngine)} cannot automatically change column with a Primary Key or Identity {model.Type.GetNiceName()}.{column.Name}");

                        var theseSqlConstraints = sqlConstraints.Where(x => (x.PK_Table == model.DataSourceEntityName && x.PK_Column == column.Name) || (x.FK_Table == model.DataSourceEntityName && x.FK_Column == column.Name)).ToArray();
                        if (theseSqlConstraints.Length > 0)
                        {
                            foreach (var sqlConstraint in theseSqlConstraints)
                            {
                                sb.Append("ALTER TABLE [").Append(sqlConstraint.FK_Table).Append("] DROP CONSTRAINT [").Append(sqlConstraint.FK_Name).Append("]\r\n");
                                sb.Append("\r\n");
                                sqlStatements++;
                            }
                        }

                        sb.Append("ALTER TABLE [").Append(model.DataSourceEntityName).Append("] ALTER COLUMN [").Append(column.Name).Append("] ");
                        WriteSqlTypeFromModel(sb, column);
                        sb.Append("\r\n");
                        sqlStatements++;
                    }
                }
            }

            //columns not in model
            foreach (var sqlColumn in sqlColumns.Where(x => x.Table == model.DataSourceEntityName))
            {
                var column = columns.FirstOrDefault(x => x.PropertySourceName == sqlColumn.Column);
                if (column == null)
                {
                    var theseSqlConstraints = sqlConstraints.Where(x => (x.PK_Table == sqlColumn.Table && x.PK_Column == sqlColumn.Column) || (x.FK_Table == sqlColumn.Table && x.FK_Column == sqlColumn.Column)).ToArray();
                    if (theseSqlConstraints.Length > 0)
                    {
                        foreach (var sqlConstraint in theseSqlConstraints)
                        {
                            sb.Append("ALTER TABLE [").Append(sqlConstraint.FK_Table).Append("] DROP CONSTRAINT [").Append(sqlConstraint.FK_Name).Append("]\r\n");
                            sb.Append("\r\n");
                            sqlStatements++;
                        }
                    }

                    if (!sqlColumn.IsNullable)
                    {
                        if (sqlColumn.IsPrimaryKey || sqlColumn.IsIdentity)
                            throw new Exception($"{nameof(ITransactStoreEngine.BuildStoreFromModels)} {nameof(MsSqlEngine)} needs to make {sqlColumn} nullable but cannot automatically change column with a Primary Key or Identity");

                        sb.Append("ALTER TABLE [").Append(model.DataSourceEntityName).Append("] ALTER COLUMN [").Append(sqlColumn.Column).Append("] ");
                        WriteSqlTypeFromColumnAsNullable(sb, sqlColumn);
                        sb.Append("\r\n");
                        sqlStatements++;
                    }
                }
            }

            var sql = sb.ToString();
            if (sql.Length > 0)
            {
                var results = ExecuteSql(sql);
                if (results != sqlStatements)
                    Log.WarnAsync($"DataSource {model.DataSourceEntityName} completed {results} of {sqlStatements} column changes");
            }
        }

        private static void AssureConstraints(ModelDetail model, SqlConstraint[] sqlConstraints)
        {
            var constraintNameDictionary = sqlConstraints.Select(x => x.FK_Name).ToDictionary(x => x, x => 0);

            var sqlStatements = 0;
            var sb = new StringBuilder();

            var fkTable = model.DataSourceEntityName;

            var usedSqlConstraints = new List<SqlConstraint>();
            foreach (var columnForRelation in model.RelatedNonEnumerableProperties)
            {
                var relatedModelDetails = ModelAnalyzer.GetModel(columnForRelation.InnerType);
                if (relatedModelDetails.IdentityProperties.Count != 1)
                    throw new Exception($"{nameof(ITransactStoreEngine.BuildStoreFromModels)} {nameof(MsSqlEngine)} does not support automatic constraints with multiple identities {relatedModelDetails.Type.GetNiceName()}");

                var fkColumn = columnForRelation.ForeignIdentity;
                var pkTable = relatedModelDetails.DataSourceEntityName;
                var pkColumn = relatedModelDetails.IdentityProperties[0].Name;

                var sqlConstraint = sqlConstraints.FirstOrDefault(x => x.FK_Table == fkTable && x.FK_Column == fkColumn && x.PK_Table == pkTable && x.PK_Column == pkColumn);
                if (sqlConstraint == null)
                {
                    var baseConstraintName = $"FK_{fkTable}_{pkTable}";
                    string constraintName;
                    if (constraintNameDictionary.TryGetValue(baseConstraintName, out int constraintNameIndex))
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
                    sb.Append("ALTER TABLE [").Append(fkTable).Append("] WITH CHECK ADD CONSTRAINT [").Append(constraintName).Append("] FOREIGN KEY ([").Append(fkColumn).Append("]) REFERENCES [").Append(pkTable).Append("]([").Append(pkColumn).Append("])\r\n");
                    sb.Append("\r\n");
                    sqlStatements++;
                }
                else
                {
                    usedSqlConstraints.Add(sqlConstraint);
                }
            }

            ////foreign keys not in model
            //foreach (var sqlConstraint in sqlConstraints.Where(x => x.FK_Table == fkTable))
            //{
            //    if (!usedSqlConstraints.Contains(sqlConstraint))
            //    {
            //        sb.Append("ALTER TABLE [").Append(sqlConstraint.FK_Table).Append("] DROP CONSTRAINT [").Append(sqlConstraint.FK_Name).Append("]\r\n");
            //        sb.Append("\r\n");
            //        sqlStatements++;
            //    }
            //}

            //var sql = sb.ToString();
            //if (sql.Length > 0)
            //{
            //    //    var results = ExecuteSql(sql);
            //    //    if (results != sqlStatements)
            //    //        Log.WarnAsync($"DataSource {model.DataSourceEntityName} completed {results} of {sqlStatements} constraint changes");
            //}
        }

        private static void WriteSqlTypeFromModel(StringBuilder sb, ModelPropertyDetail property)
        {
            bool canBeIdentity = false;
            if (property.CoreType.HasValue)
            {
                switch (property.CoreType.Value)
                {
                    case CoreType.Boolean:
                    case CoreType.BooleanNullable:
                        sb.Append("[bit]");
                        break;
                    case CoreType.Byte:
                    case CoreType.ByteNullable:
                        sb.Append("[tinyint]");
                        canBeIdentity = true;
                        break;
                    case CoreType.Int16:
                    case CoreType.Int16Nullable:
                        sb.Append("[smallint]");
                        canBeIdentity = true;
                        break;
                    case CoreType.Int32:
                    case CoreType.Int32Nullable:
                        sb.Append("[int]");
                        canBeIdentity = true;
                        break;
                    case CoreType.Int64:
                    case CoreType.Int64Nullable:
                        sb.Append("[bigint]");
                        canBeIdentity = true;
                        break;
                    case CoreType.Single:
                    case CoreType.SingleNullable:
                        sb.Append("[real]");
                        break;
                    case CoreType.Double:
                    case CoreType.DoubleNullable:
                        sb.Append("[float]");
                        break;
                    case CoreType.Decimal:
                    case CoreType.DecimalNullable:
                        sb.Append("[decimal](").Append(property.DataSourcePrecisionLength ?? 19).Append(", ").Append(property.DataSourceScale ?? 5).Append(')');
                        canBeIdentity = property.DataSourceScale == 0;
                        break;
                    case CoreType.Char:
                    case CoreType.CharNullable:
                        sb.Append("[nvarchar](").Append(property.DataSourcePrecisionLength ?? 1).Append(')');
                        break;
                    case CoreType.DateTime:
                    case CoreType.DateTimeNullable:
                        sb.Append("[datetime]");
                        break;
                    case CoreType.DateTimeOffset:
                    case CoreType.DateTimeOffsetNullable:
                        sb.Append("[datetimeoffset](").Append(property.DataSourcePrecisionLength ?? 0).Append(')');
                        break;
                    case CoreType.TimeSpan:
                    case CoreType.TimeSpanNullable:
                        sb.Append("[time](").Append(property.DataSourcePrecisionLength ?? 0).Append(')');
                        break;
                    case CoreType.Guid:
                    case CoreType.GuidNullable:
                        sb.Append("[uniqueidentifier]");
                        break;
                    case CoreType.String:
                        sb.Append("[nvarchar](").Append(property.DataSourcePrecisionLength?.ToString() ?? "max").Append(')');
                        break;
                    default:
                        throw new Exception($"Cannot match type {property.Type.GetNiceName()} to an {nameof(MsSqlEngine)} type.");
                }
            }
            else if (property.Type == typeof(byte[]))
            {
                sb.Append("[varbinary](").Append(property.DataSourcePrecisionLength?.ToString() ?? "max").Append(')');
            }
            else
            {
                throw new Exception($"Cannot match type {property.Type.GetNiceName()} to an {nameof(MsSqlEngine)} type.");
            }

            if (property.IsIdentity && property.IsIdentityAutoGenerated)
            {
                if (!canBeIdentity)
                    throw new Exception($"{nameof(ITransactStoreEngine.BuildStoreFromModels)} {nameof(MsSqlEngine)} {property.Type.GetNiceName()} {property.Name} cannot be an auto generated identity");
                sb.Append(" Identity(1,1)");
            }

            if (property.IsDataSourceNotNull) //model checks for identity not null
            {
                sb.Append(" NOT NULL");
                if (property.IsIdentity)
                {
                    sb.Append(" DEFAULT ");
                    WriteDefaultValue(sb, property);
                }
            }
            else
            {
                sb.Append(" NULL");
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
                        sb.Append('0');
                        break;
                    case CoreType.Char:
                    case CoreType.CharNullable:
                        sb.Append("''");
                        break;
                    case CoreType.DateTime:
                    case CoreType.DateTimeNullable:
                    case CoreType.DateTimeOffset:
                    case CoreType.DateTimeOffsetNullable:
                    case CoreType.TimeSpan:
                    case CoreType.TimeSpanNullable:
                        sb.Append("CONVERT(datetime, 0)");
                        break;
                    case CoreType.Guid:
                    case CoreType.GuidNullable:
                        sb.Append("CAST(0x AS uniqueidentifier)");
                        break;
                    case CoreType.String:
                        sb.Append("''");
                        break;
                    default:
                        throw new Exception($"Cannot match type {property.Type.GetNiceName()} to an {nameof(MsSqlEngine)} type.");
                }
            }
            else if (property.Type == typeof(byte[]))
            {
                sb.Append("0x");
            }
            else
            {
                throw new Exception($"Cannot match type {property.Type.GetNiceName()} to an {nameof(MsSqlEngine)} type.");
            }
        }

        private static void WriteSqlTypeFromColumnAsNullable(StringBuilder sb, SqlColumnType sqlColumn)
        {
            switch (sqlColumn.DataType)
            {
                case "bit":
                case "tinyint":
                case "smallint":
                case "int":
                case "bigint":
                case "real":
                case "float":
                case "smallmoney":
                case "money":
                case "date":
                case "smalldatetime":
                case "datetime":
                case "timestamp":
                case "rowversion":
                case "text":
                case "ntext":
                case "image":
                case "uniqueidentifier":
                case "xml":
                case "sql_variant":
                    sb.Append(sqlColumn.DataType);
                    break;
                case "numeric":
                case "decimal":
                    sb.Append(sqlColumn.DataType).Append('(').Append(sqlColumn.NumericPrecision ?? 0).Append(", ").Append(sqlColumn.NumericScale ?? 0).Append(')');
                    break;
                case "datetime2":
                    sb.Append(sqlColumn.DataType).Append('(').Append(sqlColumn.DatetimePrecision ?? 0).Append(')');
                    break;
                case "datetimeoffset":
                    sb.Append(sqlColumn.DataType).Append('(').Append(sqlColumn.DatetimePrecision ?? 0).Append(')');
                    break;
                case "time":
                    sb.Append(sqlColumn.DataType).Append('(').Append(sqlColumn.DatetimePrecision ?? 0).Append(')');
                    break;
                case "varchar":
                case "nvarchar":
                case "char":
                case "nchar":
                case "binary":
                case "varbinary":
                    sb.Append(sqlColumn.DataType).Append('(').Append(!sqlColumn.CharacterMaximumLength.HasValue || sqlColumn.CharacterMaximumLength.Value == -1 ? "max" : sqlColumn.CharacterMaximumLength.Value.ToString()).Append(')');
                    break;
                default:
                    throw new NotImplementedException($"{nameof(ITransactStoreEngine.BuildStoreFromModels)} {nameof(MsSqlEngine)} type {sqlColumn.DataType} not implemented.");
            }

            sb.Append(" NULL");
        }

        private static bool ModelMatchesSqlType(ModelPropertyDetail property, SqlColumnType sqlColumn)
        {
            if (sqlColumn.IsPrimaryKey != property.IsIdentity || sqlColumn.IsIdentity != property.IsIdentityAutoGenerated)
                return false;

            if (property.CoreType.HasValue)
            {
                switch (property.CoreType.Value)
                {
                    case CoreType.Boolean: return sqlColumn.DataType == "bit" && sqlColumn.IsNullable == false;
                    case CoreType.Byte: return sqlColumn.DataType == "tinyint" && sqlColumn.IsNullable == false;
                    case CoreType.Int16: return sqlColumn.DataType == "smallint" && sqlColumn.IsNullable == false;
                    case CoreType.Int32: return sqlColumn.DataType == "int" && sqlColumn.IsNullable == false;
                    case CoreType.Int64: return sqlColumn.DataType == "bigint" && sqlColumn.IsNullable == false;
                    case CoreType.Single: return sqlColumn.DataType == "real" && sqlColumn.IsNullable == false;
                    case CoreType.Double: return sqlColumn.DataType == "float" && sqlColumn.IsNullable == false;
                    case CoreType.Decimal: return sqlColumn.DataType == "decimal" && sqlColumn.IsNullable == false && sqlColumn.NumericPrecision == (property.DataSourcePrecisionLength ?? 19) && sqlColumn.NumericScale == (property.DataSourceScale ?? 5);
                    case CoreType.Char: return sqlColumn.DataType == "nvarchar" && sqlColumn.IsNullable == false && sqlColumn.CharacterMaximumLength == (property.DataSourcePrecisionLength ?? 1);
                    case CoreType.DateTime: return sqlColumn.DataType == "datetime" && sqlColumn.IsNullable == false;
                    case CoreType.DateTimeOffset: return sqlColumn.DataType == "datetimeoffset" && sqlColumn.IsNullable == false && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.TimeSpan: return sqlColumn.DataType == "time" && sqlColumn.IsNullable == false && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.Guid: return sqlColumn.DataType == "uniqueidentifier" && sqlColumn.IsNullable == false;

                    case CoreType.BooleanNullable: return sqlColumn.DataType == "bit" && sqlColumn.IsNullable == true;
                    case CoreType.ByteNullable: return sqlColumn.DataType == "tinyint" && sqlColumn.IsNullable == true;
                    case CoreType.Int16Nullable: return sqlColumn.DataType == "smallint" && sqlColumn.IsNullable == true;
                    case CoreType.Int32Nullable: return sqlColumn.DataType == "int" && sqlColumn.IsNullable == true;
                    case CoreType.Int64Nullable: return sqlColumn.DataType == "bigint" && sqlColumn.IsNullable == true;
                    case CoreType.SingleNullable: return sqlColumn.DataType == "real" && sqlColumn.IsNullable == true;
                    case CoreType.DoubleNullable: return sqlColumn.DataType == "float" && sqlColumn.IsNullable == true;
                    case CoreType.DecimalNullable: return sqlColumn.DataType == "decimal" && sqlColumn.IsNullable == true && sqlColumn.NumericPrecision == (property.DataSourcePrecisionLength ?? 19) && sqlColumn.NumericScale == (property.DataSourceScale ?? 5);
                    case CoreType.CharNullable: return sqlColumn.DataType == "nvarchar" && sqlColumn.IsNullable == true && sqlColumn.CharacterMaximumLength == (property.DataSourcePrecisionLength ?? 1);
                    case CoreType.DateTimeNullable: return sqlColumn.DataType == "datetime" && sqlColumn.IsNullable == true;
                    case CoreType.DateTimeOffsetNullable: return sqlColumn.DataType == "datetimeoffset" && sqlColumn.IsNullable == true && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.TimeSpanNullable: return sqlColumn.DataType == "time" && sqlColumn.IsNullable == true && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.GuidNullable: return sqlColumn.DataType == "uniqueidentifier" && sqlColumn.IsNullable == true;

                    case CoreType.String:
                        return sqlColumn.DataType == "nvarchar" && sqlColumn.IsNullable == !property.IsDataSourceNotNull;
                }
            }

            if (property.Type == typeof(byte[]))
                return sqlColumn.DataType == "varbinary" && sqlColumn.IsNullable == !property.IsDataSourceNotNull;

            throw new Exception($"{nameof(ITransactStoreEngine.BuildStoreFromModels)} cannot match type {property.Type.GetNiceName()} to an {nameof(MsSqlEngine)} type.");
        }

        private class SqlColumnType
        {
            public string Table { get; set; }
            public string Column { get; set; }
            public string DataType { get; set; }
            public bool IsNullable { get; set; }
            public int? CharacterMaximumLength { get; set; }
            public byte? NumericPrecision { get; set; }
            public int? NumericScale { get; set; }
            public short? DatetimePrecision { get; set; }
            public bool IsIdentity { get; set; }
            public bool IsPrimaryKey { get; set; }
        }
        private SqlColumnType[] GetSqlColumns()
        {
            const string query = @"SELECT C.TABLE_NAME, C.COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, DATETIME_PRECISION
	,CONVERT(bit, COLUMNPROPERTY(OBJECT_ID(C.TABLE_SCHEMA + '.' + C.TABLE_NAME), C.COLUMN_NAME, 'IsIdentity')) AS IS_IDENTITY
	,CAST(CASE WHEN RC.CONSTRAINT_TYPE = 'PRIMARY KEY' AND C.TABLE_NAME = KF.TABLE_NAME THEN 1 ELSE 0 END AS bit) AS IS_PRIMARYKEY
FROM INFORMATION_SCHEMA.COLUMNS C
LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KF ON KF.COLUMN_NAME = C.COLUMN_NAME
LEFT JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS RC ON RC.CONSTRAINT_NAME = KF.CONSTRAINT_NAME";

            var sqlColumns = ExecuteSqlQuery(query).Select(x => (IList<object>)x).Select(x => new SqlColumnType()
            {
                Table = (string)x[0],
                Column = (string)x[1],
                DataType = (string)x[2],
                IsNullable = (string)x[3] == "YES",
                CharacterMaximumLength = x[4] != DBNull.Value ? (int?)x[4] : null,
                NumericPrecision = x[5] != DBNull.Value ? (byte?)x[5] : null,
                NumericScale = x[6] != DBNull.Value ? (int?)x[6] : null,
                DatetimePrecision = x[7] != DBNull.Value ? (short?)x[7] : null,
                IsIdentity = (bool)x[8],
                IsPrimaryKey = (bool)x[9],
            }).ToArray();

            return sqlColumns;
        }

        private class SqlConstraint
        {
            public string FK_Name { get; set; }
            public string FK_Schema { get; set; }
            public string FK_Table { get; set; }
            public string FK_Column { get; set; }
            public string PK_Name { get; set; }
            public string PK_Schema { get; set; }
            public string PK_Table { get; set; }
            public string PK_Column { get; set; }
        }
        private SqlConstraint[] GetSqlConstraints()
        {
            const string query = @"SELECT RC.CONSTRAINT_NAME FK_Name, KF.TABLE_SCHEMA FK_Schema, KF.TABLE_NAME FK_Table, KF.COLUMN_NAME FK_Column, RC.UNIQUE_CONSTRAINT_NAME PK_Name, KP.TABLE_SCHEMA PK_Schema, KP.TABLE_NAME PK_Table, KP.COLUMN_NAME PK_Column
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KF ON RC.CONSTRAINT_NAME = KF.CONSTRAINT_NAME
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KP ON RC.UNIQUE_CONSTRAINT_NAME = KP.CONSTRAINT_NAME
JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS TCKP ON KP.CONSTRAINT_NAME = TCKP.CONSTRAINT_NAME
WHERE
(TCKP.CONSTRAINT_TYPE = 'PRIMARY KEY' OR TCKP.CONSTRAINT_TYPE = 'FOREIGN KEY')";

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
            const string sql = "SELECT @@version";

            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                builder.InitialCatalog = "master";
                var connectionStringForMaster = builder.ToString();

                using (var connection = new SqlConnection(connectionStringForMaster))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        var version = (string)command.ExecuteScalar();
                        if (version.Contains("Microsoft SQL Server"))
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }
    }
}
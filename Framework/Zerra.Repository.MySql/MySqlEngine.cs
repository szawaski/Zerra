// Copyright © KaKush LLC
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
using Zerra.IO;
using System.Collections;
using MySql.Data.MySqlClient;
using System.Data.Common;

namespace Zerra.Repository.MySql
{
    public sealed partial class MySqlEngine : ITransactStoreEngine
    {
        private readonly string connectionString;
        public MySqlEngine(string connectionString)
        {
            this.connectionString = connectionString;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ICoreTypeSetter<TModel>[] ReadColumns<TModel>(DbDataReader reader, ModelDetail modelDetail)
        {
            var columnProperties = new ICoreTypeSetter<TModel>[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var property = reader.GetName(i);
                if (modelDetail.TryGetProperty(property, out var propertyInfo))
                    columnProperties[i] = (ICoreTypeSetter<TModel>)propertyInfo.CoreTypeSetter;
            }
            return columnProperties;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TModel ReadRow<TModel>(DbDataReader reader, ModelDetail modelDetail, ICoreTypeSetter<TModel>[] columnProperties)
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
                                    var value = reader.GetDateTime(i);
                                    columnProperty.Setter(model, value);
                                }
                                break;
                            case CoreType.DateTimeOffset:
                                {
                                    var value = new DateTimeOffset(reader.GetDateTime(i));
                                    columnProperty.Setter(model, value);
                                }
                                break;
                            case CoreType.TimeSpan:
                                {
                                    var value = ((MySqlDataReader)reader).GetTimeSpan(i);
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
                                        columnProperty.Setter(model, (byte?)(sbyte)value);
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
                                        columnProperty.Setter(model, (DateTime?)value);
                                }
                                break;
                            case CoreType.DateTimeOffsetNullable:
                                {
                                    var value = reader.GetValue(i);
                                    if (value != DBNull.Value)
                                        columnProperty.Setter(model, (DateTimeOffset?)new DateTimeOffset((DateTime)value));
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
                                        columnProperty.Setter(model, (Guid?)Guid.Parse((string)value));
                                }
                                break;

                            default:
                                throw new NotSupportedException($"{nameof(MySqlEngine)} Cannot map to type {columnProperty.CoreType.Value} in {modelDetail.Type.GetNiceName()}");
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
                        throw new NotSupportedException($"{nameof(MySqlEngine)} cannot map to type in {modelDetail.Type.GetNiceName()}");
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
                    var isProperty = graph.HasLocalProperty(modelPropertyDetail.Name);
                    if (!modelPropertyDetail.IsNullable || isProperty)
                    {
                        object value;
                        if (isProperty)
                            value = modelPropertyDetail.Getter(model);
                        else
                            value = Instantiator.CreateInstance(modelPropertyDetail.Type);

                        if (value != null)
                        {
                            var property = modelPropertyDetail.PropertySourceName;

                            if (sbColumns.Length > 0)
                                sbColumns.Write(',');
                            sbColumns.Write('`');
                            sbColumns.Write(property);
                            sbColumns.Write('`');

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
                        sql = $"INSERT INTO `{modelDetail.DataSourceEntityName}` ({sbColumns.ToString()}) VALUES ({sbValues.ToString()});SELECT LAST_INSERT_ID();";
                    else
                        sql = $"INSERT INTO `{modelDetail.DataSourceEntityName}` DEFAULT VALUES;SELECT LAST_INSERT_ID();";
                }
                else
                {
                    if (sbColumns.Length > 0 && sbValues.Length > 0)
                        sql = $"INSERT INTO `{modelDetail.DataSourceEntityName}` ({sbColumns.ToString()}) VALUES ({sbValues.ToString()})";
                    else
                        sql = $"INSERT INTO `{modelDetail.DataSourceEntityName}` DEFAULT VALUES";
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
                writer.Write("UPDATE `");
                writer.Write(modelDetail.DataSourceEntityName);
                writer.Write("` SET ");
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
                        writer.Write('`');
                        writer.Write(property);
                        writer.Write('`');
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
                    writer.Write('`');
                    writer.Write(property);
                    writer.Write('`');
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

                if (modelDetail.IdentityProperties.Count == 1)
                {
                    var modelPropertyInfo = modelDetail.IdentityProperties[0];
                    sbWhere.Write('`');
                    sbWhere.Write(modelPropertyInfo.PropertySourceName);
                    sbWhere.Write("` IN (");

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
                            sbWhere.Write('`');
                            sbWhere.Write(modelPropertyInfo.PropertySourceName);
                            sbWhere.Write('`');
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
                        writer.Write((DateTimeOffset)value, DateTimeFormat.MsSql);
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
                        writer.Write(((Guid)value).ToString("N"));
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
                writer.Write("0x");
                writer.Write((byte[])value, ByteFormat.Hex);
                return;
            }

            throw new Exception($"Cannot convert type to SQL value {modelPropertyDetail.Type.GetNiceName()}");
        }

        public ICollection<TModel> ExecuteQueryToModelMany<TModel>(Expression where, QueryOrder order, int? skip, int? take, Graph graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var sql = LinqMySqlConverter.Convert(QueryOperation.Many, where, order, skip, take, graph, modelDetail);

            using (var connection = new MySqlConnection(connectionString))
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
            var sql = LinqMySqlConverter.Convert(QueryOperation.First, where, order, skip, take, graph, modelDetail);

            using (var connection = new MySqlConnection(connectionString))
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
            var sql = LinqMySqlConverter.Convert(QueryOperation.Single, where, order, skip, take, graph, modelDetail);

            using (var connection = new MySqlConnection(connectionString))
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
            var sql = LinqMySqlConverter.Convert(QueryOperation.Count, where, order, skip, take, graph, modelDetail);

            using (var connection = new MySqlConnection(connectionString))
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
            var sql = LinqMySqlConverter.Convert(QueryOperation.Any, where, order, skip, take, graph, modelDetail);

            using (var connection = new MySqlConnection(connectionString))
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
            var sql = LinqMySqlConverter.Convert(QueryOperation.Many, where, order, skip, take, graph, modelDetail);

            using (var connection = new MySqlConnection(connectionString))
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
            var sql = LinqMySqlConverter.Convert(QueryOperation.First, where, order, skip, take, graph, modelDetail);

            using (var connection = new MySqlConnection(connectionString))
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
            var sql = LinqMySqlConverter.Convert(QueryOperation.Single, where, order, skip, take, graph, modelDetail);

            using (var connection = new MySqlConnection(connectionString))
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
            var sql = LinqMySqlConverter.Convert(QueryOperation.Count, where, order, skip, take, graph, modelDetail);

            using (var connection = new MySqlConnection(connectionString))
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
            var sql = LinqMySqlConverter.Convert(QueryOperation.Any, where, order, skip, take, graph, modelDetail);

            using (var connection = new MySqlConnection(connectionString))
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
            using (var connection = new MySqlConnection(connectionString))
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

            using (var connection = new MySqlConnection(connectionString))
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

            using (var connection = new MySqlConnection(connectionString))
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

            using (var connection = new MySqlConnection(connectionString))
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
            using (var connection = new MySqlConnection(connectionString))
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

            using (var connection = new MySqlConnection(connectionString))
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

            using (var connection = new MySqlConnection(connectionString))
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

            using (var connection = new MySqlConnection(connectionString))
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
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T ExecuteSqlScalar<T>(string sql)
        {
            using (var connection = new MySqlConnection(connectionString))
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
            using (var connection = new MySqlConnection(connectionString))
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
            var connectionForParsing = new MySqlConnection(connectionString);
            var databaseName = connectionForParsing.Database;
            connectionForParsing.Dispose();

            bool needCreateDatabase;
            var sql = new List<string>();
            try
            {
                needCreateDatabase = NeedCreateDatabase(databaseName);

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
                Log.ErrorAsync($"{nameof(MySqlEngine)} error while reading datastore.", ex);
                throw;
            }

            var plan = new MySqlDataStoreGenerationPlan(this, needCreateDatabase ? databaseName : null, sql);
            return plan;
        }

        private bool NeedCreateDatabase(string databaseName)
        {
            var sql = $"SHOW DATABASES WHERE `Database`= '{databaseName}'";

            var builder = new MySqlConnectionStringBuilder(connectionString);
            builder.Database = "sys";
            var connectionStringForMaster = builder.ToString();

            bool needCreate;
            using (var connection = new MySqlConnection(connectionStringForMaster))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    needCreate = String.IsNullOrWhiteSpace((string)command.ExecuteScalar());
                }
            }

            return needCreate;
        }

        internal void CreateDatabase(string databaseName)
        {
            var sql = $"CREATE DATABASE {databaseName}";

            var builder = new MySqlConnectionStringBuilder(connectionString);
            builder.Database = "sys";
            var connectionStringForMaster = builder.ToString();
            using (var connection = new MySqlConnection(connectionStringForMaster))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandTimeout = 0;
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
        }

        private bool AssureTable(bool create, List<string> sql, bool needCreateDatabase, ModelDetail model)
        {
            var nonIdentityColumns = model.Properties.Where(x => !x.IsIdentity && !x.IsDataSourceEntity && x.CoreType.HasValue || x.Type == typeof(byte[])).ToArray();
            var identityColumns = model.Properties.Where(x => x.IsIdentity).ToArray();

            if (nonIdentityColumns.Length + identityColumns.Length == 0)
                return false; //Cannot create table with no columns

            if (!needCreateDatabase)
            {
                var sqlQuery = $"SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME = '{model.DataSourceEntityName}'";
                var exists = ExecuteSqlScalar<long>(sqlQuery) > 0;
                if (exists)
                    return false;
            }

            if (create)
            {
                var sb = new StringBuilder();
                sb.Append("CREATE TABLE ").Append(model.DataSourceEntityName).Append("(\r\n");

                for (var i = 0; i < identityColumns.Length; i++)
                {
                    var property = identityColumns[i];
                    if (i > 0)
                        sb.Append(",\r\n");
                    sb.Append("\t`").Append(property.PropertySourceName).Append("` ");
                    WriteSqlTypeFromModel(sb, property);
                }
                for (var i = 0; i < nonIdentityColumns.Length; i++)
                {
                    var property = nonIdentityColumns[i];
                    if (i > 0 || identityColumns.Length > 0)
                        sb.Append(",\r\n");
                    sb.Append("\t`").Append(property.PropertySourceName).Append("` ");
                    WriteSqlTypeFromModel(sb, property);
                }


                if (identityColumns.Length > 0)
                {
                    if (identityColumns.Length > 0 || nonIdentityColumns.Length > 0)
                        sb.Append(',');
                    sb.Append("\r\n\tCONSTRAINT PK_").Append(model.DataSourceEntityName).Append(" PRIMARY KEY(\r\n");

                    //MySql must have AutoGenerated first
                    identityColumns = identityColumns.OrderByDescending(x => x.IsIdentityAutoGenerated).ToArray();
                    for (var i = 0; i < identityColumns.Length; i++)
                    {
                        var property = identityColumns[i];
                        if (i > 0)
                            sb.Append(",\r\n");
                        sb.Append("\t\t`").Append(property.PropertySourceName).Append('`');
                    }
                    sb.Append("\r\n\t)\r\n");
                }
                sb.Append("\r\n)");

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
                var sqlColumn = sqlColumns.FirstOrDefault(x => x.Table == model.DataSourceEntityName.ToLower() && x.Column == column.PropertySourceName);
                if (sqlColumn == null)
                {
                    if (create)
                    {
                        sb.Append("ALTER TABLE `").Append(model.DataSourceEntityName).Append("` ADD `").Append(column.Name).Append("` ");
                        WriteSqlTypeFromModel(sb, column);
                        sb.Append(";");
                        sql.Add(sb.ToString());
                        sb.Clear();
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
                                throw new Exception($"{nameof(ITransactStoreEngine.BuildStoreGenerationPlan)} {nameof(MySqlEngine)} cannot automatically change column with a Primary Key or Identity {model.Type.GetNiceName()}.{column.Name}");

                            var theseSqlConstraints = sqlConstraints.Where(x => (x.PK_Table == model.DataSourceEntityName.ToLower() && x.PK_Column == column.Name) || (x.FK_Table == model.DataSourceEntityName && x.FK_Column == column.Name)).ToArray();
                            if (theseSqlConstraints.Length > 0)
                            {
                                foreach (var sqlConstraint in theseSqlConstraints)
                                {
                                    sb.Append("ALTER TABLE `").Append(sqlConstraint.FK_Table).Append("` DROP CONSTRAINT `").Append(sqlConstraint.FK_Name).Append("`;");
                                    sql.Add(sb.ToString());
                                    sb.Clear();
                                }
                            }

                            sb.Append("ALTER TABLE `").Append(model.DataSourceEntityName).Append("` MODIFY `").Append(column.Name).Append("` ");
                            WriteSqlTypeFromModel(sb, column);
                            sb.Append(";");
                            sql.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                }
            }

            if (delete)
            {
                //columns not in model
                foreach (var sqlColumn in sqlColumns.Where(x => x.Table == model.DataSourceEntityName.ToLower()))
                {
                    var column = columns.FirstOrDefault(x => x.PropertySourceName == sqlColumn.Column);
                    if (column == null)
                    {
                        var theseSqlConstraints = sqlConstraints.Where(x => (x.PK_Table == sqlColumn.Table && x.PK_Column == sqlColumn.Column) || (x.FK_Table == sqlColumn.Table && x.FK_Column == sqlColumn.Column)).ToArray();
                        if (theseSqlConstraints.Length > 0)
                        {
                            foreach (var sqlConstraint in theseSqlConstraints)
                            {
                                sb.Append("ALTER TABLE `").Append(sqlConstraint.FK_Table).Append("` DROP CONSTRAINT `").Append(sqlConstraint.FK_Name).Append("`;");
                                sql.Add(sb.ToString());
                                sb.Clear();
                            }
                        }

                        if (!sqlColumn.IsNullable)
                        {
                            if (sqlColumn.IsPrimaryKey || sqlColumn.IsIdentity)
                                throw new Exception($"{nameof(ITransactStoreEngine.BuildStoreGenerationPlan)} {nameof(MySqlEngine)} needs to make {sqlColumn} nullable but cannot automatically change column with a Primary Key or Identity");

                            sb.Append("ALTER TABLE `").Append(model.DataSourceEntityName).Append("` MODIFY `").Append(sqlColumn.Column).Append("` ");
                            WriteSqlTypeFromColumnAsNullable(sb, sqlColumn);
                            sb.Append(";");
                            sql.Add(sb.ToString());
                            sb.Clear();
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
                    throw new Exception($"{nameof(ITransactStoreEngine.BuildStoreGenerationPlan)} {nameof(MySqlEngine)} does not support automatic constraints with multiple identities {relatedModelDetails.Type.GetNiceName()}");

                var fkColumn = columnForRelation.ForeignIdentity;
                var pkTable = relatedModelDetails.DataSourceEntityName;
                var pkColumn = relatedModelDetails.IdentityProperties[0].Name;

                var sqlConstraint = sqlConstraints.FirstOrDefault(x => x.FK_Table.ToLower() == fkTable.ToLower() && x.FK_Column.ToLower() == fkColumn.ToLower() && x.PK_Table.ToLower() == pkTable.ToLower() && x.PK_Column.ToLower() == pkColumn.ToLower());
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
                        sb.Append("ALTER TABLE `").Append(fkTable).Append("` ADD CONSTRAINT ").Append(constraintName).Append(" FOREIGN KEY (").Append(fkColumn).Append(") REFERENCES `").Append(pkTable).Append("`(").Append(pkColumn).Append(");");
                        sql.Add(sb.ToString());
                        sb.Clear();
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
                        sb.Append("ALTER TABLE `").Append(sqlConstraint.FK_Table).Append("` DROP CONSTRAINT ").Append(sqlConstraint.FK_Name).Append(";");
                        sql.Add(sb.ToString());
                        sb.Clear();
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
                        sb.Append("bit");
                        break;
                    case CoreType.Byte:
                    case CoreType.ByteNullable:
                        sb.Append("tinyint");
                        canBeIdentity = true;
                        break;
                    case CoreType.Int16:
                    case CoreType.Int16Nullable:
                        sb.Append("smallint");
                        canBeIdentity = true;
                        break;
                    case CoreType.Int32:
                    case CoreType.Int32Nullable:
                        sb.Append("int");
                        canBeIdentity = true;
                        break;
                    case CoreType.Int64:
                    case CoreType.Int64Nullable:
                        sb.Append("bigint");
                        canBeIdentity = true;
                        break;
                    case CoreType.Single:
                    case CoreType.SingleNullable:
                        sb.Append("float");
                        break;
                    case CoreType.Double:
                    case CoreType.DoubleNullable:
                        sb.Append("real");
                        break;
                    case CoreType.Decimal:
                    case CoreType.DecimalNullable:
                        sb.Append("decimal(").Append(property.DataSourcePrecisionLength ?? 19).Append(", ").Append(property.DataSourceScale ?? 5).Append(')');
                        canBeIdentity = property.DataSourceScale == 0;
                        break;
                    case CoreType.Char:
                    case CoreType.CharNullable:
                        sb.Append("varchar(").Append(property.DataSourcePrecisionLength ?? 1).Append(')');
                        break;
                    case CoreType.DateTime:
                    case CoreType.DateTimeNullable:
                        if (property.DatePart == StoreDatePart.Date)
                            sb.Append("date");
                        else
                            sb.Append("datetime(").Append(property.DataSourcePrecisionLength ?? 0).Append(')');
                        break;
                    case CoreType.DateTimeOffset:
                    case CoreType.DateTimeOffsetNullable:
                        sb.Append("timestamp(").Append(property.DataSourcePrecisionLength ?? 0).Append(')');
                        break;
                    case CoreType.TimeSpan:
                    case CoreType.TimeSpanNullable:
                        sb.Append("time(").Append(property.DataSourcePrecisionLength ?? 0).Append(')');
                        break;
                    case CoreType.Guid:
                    case CoreType.GuidNullable:
                        sb.Append("char(32)");
                        break;
                    case CoreType.String:
                        if (property.DataSourcePrecisionLength.HasValue)
                            sb.Append("varchar(").Append(property.DataSourcePrecisionLength.Value).Append(')');
                        else
                            sb.Append("text");
                        break;
                    default:
                        throw new Exception($"Cannot match type {property.Type.GetNiceName()} to an {nameof(MySqlEngine)} type.");
                }
            }
            else if (property.Type == typeof(byte[]))
            {
                if (property.DataSourcePrecisionLength.HasValue)
                    sb.Append("varbinary(").Append(property.DataSourcePrecisionLength.Value).Append(')');
                else
                    sb.Append("blob");
            }
            else
            {
                throw new Exception($"Cannot match type {property.Type.GetNiceName()} to an {nameof(MySqlEngine)} type.");
            }

            if (property.IsDataSourceNotNull) //model checks for identity not null
            {
                sb.Append(" NOT NULL");
                if (property.IsIdentity && !property.IsIdentityAutoGenerated)
                {
                    sb.Append(" DEFAULT (");
                    WriteDefaultValue(sb, property);
                    sb.Append(')');
                }
            }
            else
            {
                sb.Append(" NULL");
            }

            if (property.IsIdentity && property.IsIdentityAutoGenerated)
            {
                if (!canBeIdentity)
                    throw new Exception($"{nameof(ITransactStoreEngine.BuildStoreGenerationPlan)} {nameof(MySqlEngine)} {property.Type.GetNiceName()} {property.Name} cannot be an auto generated identity");
                sb.Append(" AUTO_INCREMENT");
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
                        sb.Append("CONVERT(timestamp, 0)");
                        break;
                    case CoreType.Guid:
                    case CoreType.GuidNullable:
                        sb.Append("UUID()");
                        break;
                    case CoreType.String:
                        sb.Append("''");
                        break;
                    default:
                        throw new Exception($"Cannot match type {property.Type.GetNiceName()} to an {nameof(MySqlEngine)} type.");
                }
            }
            else if (property.Type == typeof(byte[]))
            {
                sb.Append("0x");
            }
            else
            {
                throw new Exception($"Cannot match type {property.Type.GetNiceName()} to an {nameof(MySqlEngine)} type.");
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
                case "double":

                case "text":
                    sb.Append(sqlColumn.DataType);
                    break;
                case "numeric":
                case "decimal":
                    sb.Append(sqlColumn.DataType).Append('(').Append(sqlColumn.NumericPrecision ?? 0).Append(", ").Append(sqlColumn.NumericScale ?? 0).Append(')');
                    break;
                case "datetime":
                case "timestamp":
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
                    throw new NotImplementedException($"{nameof(ITransactStoreEngine.BuildStoreGenerationPlan)} {nameof(MySqlEngine)} type {sqlColumn.DataType} not implemented.");
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
                    case CoreType.Single: return sqlColumn.DataType == "float" && sqlColumn.IsNullable == false;
                    case CoreType.Double: return sqlColumn.DataType == "double" && sqlColumn.IsNullable == false;
                    case CoreType.Decimal: return sqlColumn.DataType == "decimal" && sqlColumn.IsNullable == false && sqlColumn.NumericPrecision == (property.DataSourcePrecisionLength ?? 19) && sqlColumn.NumericScale == (property.DataSourceScale ?? 5);
                    case CoreType.Char: return sqlColumn.DataType == "varchar" && sqlColumn.IsNullable == false && sqlColumn.CharacterMaximumLength == (property.DataSourcePrecisionLength ?? 1);
                    case CoreType.DateTime: return ((sqlColumn.DataType == "datetime" && property.DatePart == StoreDatePart.DateTime && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0)) || (sqlColumn.DataType == "date" && property.DatePart == StoreDatePart.Date)) && sqlColumn.IsNullable == false;
                    case CoreType.DateTimeOffset: return sqlColumn.DataType == "timestamp" && sqlColumn.IsNullable == false && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.TimeSpan: return sqlColumn.DataType == "time" && sqlColumn.IsNullable == false && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.Guid: return sqlColumn.DataType == "char" && sqlColumn.IsNullable == false && sqlColumn.CharacterMaximumLength == 32;

                    case CoreType.BooleanNullable: return sqlColumn.DataType == "bit" && sqlColumn.IsNullable == true;
                    case CoreType.ByteNullable: return sqlColumn.DataType == "tinyint" && sqlColumn.IsNullable == true;
                    case CoreType.Int16Nullable: return sqlColumn.DataType == "smallint" && sqlColumn.IsNullable == true;
                    case CoreType.Int32Nullable: return sqlColumn.DataType == "int" && sqlColumn.IsNullable == true;
                    case CoreType.Int64Nullable: return sqlColumn.DataType == "bigint" && sqlColumn.IsNullable == true;
                    case CoreType.SingleNullable: return sqlColumn.DataType == "float" && sqlColumn.IsNullable == true;
                    case CoreType.DoubleNullable: return sqlColumn.DataType == "double" && sqlColumn.IsNullable == true;
                    case CoreType.DecimalNullable: return sqlColumn.DataType == "decimal" && sqlColumn.IsNullable == true && sqlColumn.NumericPrecision == (property.DataSourcePrecisionLength ?? 19) && sqlColumn.NumericScale == (property.DataSourceScale ?? 5);
                    case CoreType.CharNullable: return sqlColumn.DataType == "varchar" && sqlColumn.IsNullable == true && sqlColumn.CharacterMaximumLength == (property.DataSourcePrecisionLength ?? 1);
                    case CoreType.DateTimeNullable: return ((sqlColumn.DataType == "datetime" && property.DatePart == StoreDatePart.DateTime && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0)) || (sqlColumn.DataType == "date" && property.DatePart == StoreDatePart.Date)) && sqlColumn.IsNullable == true;
                    case CoreType.DateTimeOffsetNullable: return sqlColumn.DataType == "timestamp" && sqlColumn.IsNullable == true && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.TimeSpanNullable: return sqlColumn.DataType == "time" && sqlColumn.IsNullable == true && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.GuidNullable: return sqlColumn.DataType == "char" && sqlColumn.IsNullable == true && sqlColumn.CharacterMaximumLength == 32;

                    case CoreType.String:
                        if (sqlColumn.NumericPrecision.HasValue)
                            return sqlColumn.DataType == "varchar" && sqlColumn.IsNullable == !property.IsDataSourceNotNull;
                        else
                            return sqlColumn.DataType == "text" && sqlColumn.IsNullable == !property.IsDataSourceNotNull;
                }
            }

            if (property.Type == typeof(byte[]))
                if (sqlColumn.NumericPrecision.HasValue)
                    return sqlColumn.DataType == "varbinary" && sqlColumn.IsNullable == !property.IsDataSourceNotNull;
                else
                    return sqlColumn.DataType == "blob" && sqlColumn.IsNullable == !property.IsDataSourceNotNull;

            throw new Exception($"{nameof(ITransactStoreEngine.BuildStoreGenerationPlan)} cannot match type {property.Type.GetNiceName()} to an {nameof(MySqlEngine)} type.");
        }
        private SqlColumnType[] GetSqlColumns(ModelDetail model)
        {
            var query = $@"SELECT C.TABLE_NAME, C.COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, DATETIME_PRECISION
	,C.EXTRA LIKE '%auto_increment%' AS IS_IDENTITY
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
                CharacterMaximumLength = x[4] != DBNull.Value ? (long)x[4] : (long?)null,
                NumericPrecision = x[5] != DBNull.Value ? (uint)x[5] : (uint?)null,
                NumericScale = x[6] != DBNull.Value ? (uint)x[6] : (uint?)null,
                DatetimePrecision = x[7] != DBNull.Value ? (uint)x[7] : (uint?)null,
                IsIdentity = x[8] != DBNull.Value ? (ulong)x[8] == 1 : false,
                IsPrimaryKey = x[9] != DBNull.Value ? (ulong)x[9] == 1 : false,
            }).ToArray();

            return sqlColumns;
        }
        private SqlConstraint[] GetSqlConstraints(ModelDetail model)
        {
            var query = $@"SELECT RC.CONSTRAINT_NAME FK_Name, KF.TABLE_SCHEMA FK_Schema, KF.TABLE_NAME FK_Table, KF.COLUMN_NAME FK_Column, RC.UNIQUE_CONSTRAINT_NAME PK_Name, KF.REFERENCED_TABLE_SCHEMA PK_Schema, KF.REFERENCED_TABLE_NAME PK_Table, KF.REFERENCED_COLUMN_NAME PK_Column
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC
LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KF ON RC.CONSTRAINT_NAME = KF.CONSTRAINT_NAME AND RC.CONSTRAINT_SCHEMA = KF.CONSTRAINT_SCHEMA
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
                var builder = new MySqlConnectionStringBuilder(connectionString);
                builder.Database = "sys";
                var connectionStringForMaster = builder.ToString();

                using (var connection = new MySqlConnection(connectionStringForMaster))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandTimeout = 0;
                        command.CommandText = sql;
                        var version = (string)command.ExecuteScalar();
                        if (version.Length > 0 && Char.IsNumber(version[0]))
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }
    }
}
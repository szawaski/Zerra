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
using Npgsql;
using Zerra.IO;
using System.Collections;

namespace Zerra.Repository.Sql.PostgreSql
{
    public sealed class PostgreSqlEngine : ISqlEngine
    {
        private readonly string connectionString;
        public PostgreSqlEngine(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public string ConvertToSql(QueryOperation select, Expression where, QueryOrder order, int? skip, int? take, Graph graph, ModelDetail modelDetail)
        {
            return LinqPostgreSqlConverter.Convert(select, where, order, skip, take, graph, modelDetail);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static CoreTypeSetter<TModel>[] ReadColumns<TModel>(NpgsqlDataReader reader, ModelDetail modelDetail)
        {
            var columnProperties = new CoreTypeSetter<TModel>[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var property = reader.GetName(i);
                if (modelDetail.TryGetPropertyLower(property, out ModelPropertyDetail propertyInfo))
                    columnProperties[i] = (CoreTypeSetter<TModel>)propertyInfo.CoreTypeSetter;
            }
            return columnProperties;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TModel ReadRow<TModel>(NpgsqlDataReader reader, ModelDetail modelDetail, CoreTypeSetter<TModel>[] columnProperties)
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
                                        columnProperty.Setter(model, (byte?)value);
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
                                        columnProperty.Setter(model, ((string)value)[0]);
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
                                        columnProperty.Setter(model, (DateTimeOffset?)value);
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

        public ICollection<TModel> ExecuteSqlQueryToModelMany<TModel>(string sql, ModelDetail modelDetail) where TModel : class, new()
        {
            using (var connection = new NpgsqlConnection(connectionString))
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
        public TModel ExecuteSqlQueryToModelFirst<TModel>(string sql, ModelDetail modelDetail) where TModel : class, new()
        {
            using (var connection = new NpgsqlConnection(connectionString))
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
        public TModel ExecuteSqlQueryToModelSingle<TModel>(string sql, ModelDetail modelDetail) where TModel : class, new()
        {
            using (var connection = new NpgsqlConnection(connectionString))
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
        public long ExecuteSqlQueryCount(string sql)
        {
            using (var connection = new NpgsqlConnection(connectionString))
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
        public bool ExecuteSqlQueryAny(string sql)
        {
            using (var connection = new NpgsqlConnection(connectionString))
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
        public ICollection<object> ExecuteSqlQuery(string sql)
        {
            List<object> allValues = new List<object>();
            using (var connection = new NpgsqlConnection(connectionString))
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
        public int ExecuteSql(string sql)
        {
            int rowsAffected = 0;
            using (var connection = new NpgsqlConnection(connectionString))
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

        public async Task<ICollection<TModel>> ExecuteSqlQueryToModelManyAsync<TModel>(string sql, ModelDetail modelDetail) where TModel : class, new()
        {
            using (var connection = new NpgsqlConnection(connectionString))
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
        public async Task<TModel> ExecuteSqlQueryToModelFirstAsync<TModel>(string sql, ModelDetail modelDetail) where TModel : class, new()
        {
            using (var connection = new NpgsqlConnection(connectionString))
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
        public async Task<TModel> ExecuteSqlQueryToModelSingleAsync<TModel>(string sql, ModelDetail modelDetail) where TModel : class, new()
        {
            using (var connection = new NpgsqlConnection(connectionString))
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
        public async Task<long> ExecuteSqlQueryCountAsync(string sql)
        {
            using (var connection = new NpgsqlConnection(connectionString))
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
        public async Task<bool> ExecuteSqlQueryAnyAsync(string sql)
        {
            using (var connection = new NpgsqlConnection(connectionString))
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
        public async Task<ICollection<object>> ExecuteSqlQueryAsync(string sql)
        {
            var allValues = new List<object>();
            using (var connection = new NpgsqlConnection(connectionString))
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
        public async Task<int> ExecuteSqlAsync(string sql)
        {
            int rowsAffected = 0;
            using (var connection = new NpgsqlConnection(connectionString))
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

        public void AssureDataStore(ICollection<ModelDetail> modelDetails)
        {
            var connectionForParsing = new NpgsqlConnection(connectionString);
            var databaseName = connectionForParsing.Database;
            connectionForParsing.Dispose();

            try
            {
                AssureDatabase(databaseName);

                AssureExtensions();

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
                Log.ErrorAsync($"{nameof(ISqlEngine.AssureDataStore)} {nameof(PostgreSqlEngine)} error while assuring datastore.", ex);
                throw;
            }
        }

        private void AssureDatabase(string databaseName)
        {
            var sql = $"SELECT COUNT(*) FROM pg_database WHERE datname = '{databaseName.ToLower()}'";

            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            builder.Database = "postgres";
            var connectionStringForMaster = builder.ToString();

            bool exists;
            using (var connection = new NpgsqlConnection(connectionStringForMaster))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    exists = (long)command.ExecuteScalar() > 0;
                }
            }

            if (exists)
                return;

            sql = $"CREATE DATABASE {databaseName}";
            using (var connection = new NpgsqlConnection(connectionStringForMaster))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
        }

        private void AssureExtensions()
        {
            var sql = "CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\"";
            ExecuteSql(sql);
        }

        private void AssureTable(ModelDetail model)
        {
            var nonIdentityColumns = model.Properties.Where(x => !x.IsIdentity && !x.IsDataSourceEntity && x.CoreType.HasValue || x.Type == typeof(byte[])).ToArray();
            var identityColumns = model.Properties.Where(x => x.IsIdentity).ToArray();

            if (nonIdentityColumns.Length + identityColumns.Length == 0)
                return; //Cannot create table with no columns

            var sql = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' AND TABLE_NAME='{model.DataSourceEntityName.ToLower()}'";
            var exists = ExecuteSqlQueryCount(sql) > 0;
            if (exists)
                return;

            var sb = new StringBuilder();
            sb.Append("CREATE TABLE ").Append(model.DataSourceEntityName).Append("(\r\n");

            for (var i = 0; i < identityColumns.Length; i++)
            {
                var property = identityColumns[i];
                if (i > 0)
                    sb.Append(",\r\n");
                sb.Append("\t").Append(property.PropertySourceName).Append(" ");
                WriteSqlTypeFromModel(sb, property);
            }
            for (var i = 0; i < nonIdentityColumns.Length; i++)
            {
                var property = nonIdentityColumns[i];
                if (i > 0 || identityColumns.Length > 0)
                    sb.Append(",\r\n");
                sb.Append("\t").Append(property.PropertySourceName).Append(" ");
                WriteSqlTypeFromModel(sb, property);
            }
            if (identityColumns.Length > 0)
            {
                if (identityColumns.Length > 0 || nonIdentityColumns.Length > 0)
                    sb.Append(',');
                sb.Append("\r\n\tCONSTRAINT PK_").Append(model.DataSourceEntityName).Append(" PRIMARY KEY(\r\n");

                for (var i = 0; i < identityColumns.Length; i++)
                {
                    var property = identityColumns[i];
                    if (i > 0)
                        sb.Append(",\r\n");
                    sb.Append("\t\t").Append(property.PropertySourceName);
                }
                sb.Append("\r\n\t)\r\n");
            }
            sb.Append("\r\n)");

            sql = sb.ToString();
            ExecuteSql(sql);
        }

        private void AssureColumns(ModelDetail model, SqlColumnType[] sqlColumns, SqlConstraint[] sqlConstraints)
        {
            var columns = model.Properties.Where(x => !x.IsDataSourceEntity && x.CoreType.HasValue || x.Type == typeof(byte[])).ToArray();

            var sqlStatements = 0;
            var sb = new StringBuilder();

            foreach (var column in columns)
            {
                var sqlColumn = sqlColumns.FirstOrDefault(x => x.Table == model.DataSourceEntityName.ToLower() && x.Column == column.PropertySourceName.ToLower());
                if (sqlColumn == null)
                {
                    sb.Append("ALTER TABLE ").Append(model.DataSourceEntityName.ToLower()).Append(" ADD ").Append(column.Name).Append(" ");
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
                            throw new Exception($"{nameof(ISqlEngine.AssureDataStore)} {nameof(PostgreSqlEngine)} cannot automatically change column with a Primary Key or Identity {model.Type.GetNiceName()}.{column.Name}");

                        var theseSqlConstraints = sqlConstraints.Where(x => (x.PK_Table == model.DataSourceEntityName.ToLower() && x.PK_Column == column.Name) || (x.FK_Table == model.DataSourceEntityName && x.FK_Column == column.Name)).ToArray();
                        if (theseSqlConstraints.Length > 0)
                        {
                            foreach (var sqlConstraint in theseSqlConstraints)
                            {
                                sb.Append("ALTER TABLE ").Append(sqlConstraint.FK_Table.ToLower()).Append(" DROP CONSTRAINT ").Append(sqlConstraint.FK_Name.ToLower()).Append("\r\n");
                                sb.Append("\r\n");
                                sqlStatements++;
                            }
                        }

                        sb.Append("ALTER TABLE ").Append(model.DataSourceEntityName.ToLower()).Append(" ALTER COLUMN ").Append(column.Name.ToLower()).Append(" ");
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
                            throw new Exception($"{nameof(ISqlEngine.AssureDataStore)} {nameof(PostgreSqlEngine)} needs to make {sqlColumn} nullable but cannot automatically change column with a Primary Key or Identity");

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
                    throw new Exception($"{nameof(ISqlEngine.AssureDataStore)} {nameof(PostgreSqlEngine)} does not support automatic constraints with multiple identities {relatedModelDetails.Type.GetNiceName()}");

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
                    sb.Append("ALTER TABLE ").Append(fkTable.ToLower()).Append(" WITH CHECK ADD CONSTRAINT [").Append(constraintName).Append("] FOREIGN KEY ([").Append(fkColumn).Append("]) REFERENCES [").Append(pkTable).Append("]([").Append(pkColumn).Append("])\r\n");
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
            //        sb.Append("ALTER TABLE ").Append(sqlConstraint.FK_Table.ToLower()).Append(" DROP CONSTRAINT ").Append(sqlConstraint.FK_Name.ToLower()).Append("\r\n");
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
                        sb.Append("bit");
                        break;
                    case CoreType.Byte:
                    case CoreType.ByteNullable:
                        sb.Append("smallint");
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
                        sb.Append("real");
                        break;
                    case CoreType.Double:
                    case CoreType.DoubleNullable:
                        sb.Append("float8");
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
                        sb.Append("timestamp(").Append(property.DataSourcePrecisionLength ?? 0).Append(')');
                        break;
                    case CoreType.DateTimeOffset:
                    case CoreType.DateTimeOffsetNullable:
                        sb.Append("timestamptz(").Append(property.DataSourcePrecisionLength ?? 0).Append(')');
                        break;
                    case CoreType.TimeSpan:
                    case CoreType.TimeSpanNullable:
                        sb.Append("time(").Append(property.DataSourcePrecisionLength ?? 0).Append(')');
                        break;
                    case CoreType.Guid:
                    case CoreType.GuidNullable:
                        sb.Append("uuid");
                        break;
                    case CoreType.String:
                        if (property.DataSourcePrecisionLength.HasValue)
                            sb.Append("varchar(").Append(property.DataSourcePrecisionLength.Value).Append(')');
                        else
                            sb.Append("text");
                        break;
                    default:
                        throw new Exception($"Cannot match type {property.Type.GetNiceName()} to an {nameof(PostgreSqlEngine)} type.");
                }
            }
            else if (property.Type == typeof(byte[]))
            {
                if (property.DataSourcePrecisionLength.HasValue)
                    sb.Append("bit(").Append(property.DataSourcePrecisionLength.Value).Append(')');
                else
                    sb.Append("bytea");
            }
            else
            {
                throw new Exception($"Cannot match type {property.Type.GetNiceName()} to an {nameof(PostgreSqlEngine)} type.");
            }

            if (property.IsIdentity && property.IsIdentityAutoGenerated)
            {
                if (!canBeIdentity)
                    throw new Exception($"{nameof(ISqlEngine.AssureDataStore)} {nameof(PostgreSqlEngine)} {property.Type.GetNiceName()} {property.Name} cannot be an auto generated identity");
                sb.Append(" Identity(1,1)");
            }

            if (property.IsDataSourceNotNull) //model checks for identity not null
            {
                sb.Append(" NOT NULL DEFAULT ");
                WriteDefaultValue(sb, property);
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
                        sb.Append("CONVERT(timestamp, 0)");
                        break;
                    case CoreType.Guid:
                    case CoreType.GuidNullable:
                        sb.Append("uuid_nil()");
                        break;
                    case CoreType.String:
                        sb.Append("''");
                        break;
                    default:
                        throw new Exception($"Cannot match type {property.Type.GetNiceName()} to an {nameof(PostgreSqlEngine)} type.");
                }
            }
            else if (property.Type == typeof(byte[]))
            {
                sb.Append("0x");
            }
            else
            {
                throw new Exception($"Cannot match type {property.Type.GetNiceName()} to an {nameof(PostgreSqlEngine)} type.");
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
                    throw new NotImplementedException($"{nameof(ISqlEngine.AssureDataStore)} {nameof(PostgreSqlEngine)} type {sqlColumn.DataType} not implemented.");
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
                    case CoreType.Byte: return sqlColumn.DataType == "smallint" && sqlColumn.IsNullable == false;
                    case CoreType.Int16: return sqlColumn.DataType == "smallint" && sqlColumn.IsNullable == false;
                    case CoreType.Int32: return sqlColumn.DataType == "int" && sqlColumn.IsNullable == false;
                    case CoreType.Int64: return sqlColumn.DataType == "bigint" && sqlColumn.IsNullable == false;
                    case CoreType.Single: return sqlColumn.DataType == "real" && sqlColumn.IsNullable == false;
                    case CoreType.Double: return sqlColumn.DataType == "float8" && sqlColumn.IsNullable == false;
                    case CoreType.Decimal: return sqlColumn.DataType == "decimal" && sqlColumn.IsNullable == false && sqlColumn.NumericPrecision == (property.DataSourcePrecisionLength ?? 19) && sqlColumn.NumericScale == (property.DataSourceScale ?? 5);
                    case CoreType.Char: return sqlColumn.DataType == "varchar" && sqlColumn.IsNullable == false && sqlColumn.CharacterMaximumLength == (property.DataSourcePrecisionLength ?? 1);
                    case CoreType.DateTime: return sqlColumn.DataType == "timestamp" && sqlColumn.IsNullable == false;
                    case CoreType.DateTimeOffset: return sqlColumn.DataType == "timestamptz" && sqlColumn.IsNullable == false && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.TimeSpan: return sqlColumn.DataType == "time" && sqlColumn.IsNullable == false && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.Guid: return sqlColumn.DataType == "uuid" && sqlColumn.IsNullable == false;

                    case CoreType.BooleanNullable: return sqlColumn.DataType == "bit" && sqlColumn.IsNullable == true;
                    case CoreType.ByteNullable: return sqlColumn.DataType == "smallint" && sqlColumn.IsNullable == true;
                    case CoreType.Int16Nullable: return sqlColumn.DataType == "smallint" && sqlColumn.IsNullable == true;
                    case CoreType.Int32Nullable: return sqlColumn.DataType == "integer" && sqlColumn.IsNullable == true;
                    case CoreType.Int64Nullable: return sqlColumn.DataType == "bigint" && sqlColumn.IsNullable == true;
                    case CoreType.SingleNullable: return sqlColumn.DataType == "real" && sqlColumn.IsNullable == true;
                    case CoreType.DoubleNullable: return sqlColumn.DataType == "float8" && sqlColumn.IsNullable == true;
                    case CoreType.DecimalNullable: return sqlColumn.DataType == "decimal" && sqlColumn.IsNullable == true && sqlColumn.NumericPrecision == (property.DataSourcePrecisionLength ?? 19) && sqlColumn.NumericScale == (property.DataSourceScale ?? 5);
                    case CoreType.CharNullable: return sqlColumn.DataType == "varchar" && sqlColumn.IsNullable == true && sqlColumn.CharacterMaximumLength == (property.DataSourcePrecisionLength ?? 1);
                    case CoreType.DateTimeNullable: return sqlColumn.DataType == "timestamp without time zone" && sqlColumn.IsNullable == true;
                    case CoreType.DateTimeOffsetNullable: return sqlColumn.DataType == "timestamptz" && sqlColumn.IsNullable == true && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.TimeSpanNullable: return sqlColumn.DataType == "time" && sqlColumn.IsNullable == true && sqlColumn.DatetimePrecision == (property.DataSourcePrecisionLength ?? 0);
                    case CoreType.GuidNullable: return sqlColumn.DataType == "uuid" && sqlColumn.IsNullable == true;

                    case CoreType.String:
                        if (sqlColumn.NumericPrecision.HasValue)
                            return sqlColumn.DataType == "varchar" && sqlColumn.IsNullable == !property.IsDataSourceNotNull;
                        else
                            return sqlColumn.DataType == "text" && sqlColumn.IsNullable == !property.IsDataSourceNotNull;
                }
            }

            if (property.Type == typeof(byte[]))
                if (sqlColumn.NumericPrecision.HasValue)
                    return sqlColumn.DataType == "bit" && sqlColumn.IsNullable == !property.IsDataSourceNotNull;
                else
                    return sqlColumn.DataType == "bytea" && sqlColumn.IsNullable == !property.IsDataSourceNotNull;

            throw new Exception($"{nameof(ISqlEngine.AssureDataStore)} cannot match type {property.Type.GetNiceName()} to an {nameof(PostgreSqlEngine)} type.");
        }

        private class SqlColumnType
        {
            public string Table { get; set; }
            public string Column { get; set; }
            public string DataType { get; set; }
            public bool IsNullable { get; set; }
            public int? CharacterMaximumLength { get; set; }
            public int? NumericPrecision { get; set; }
            public int? NumericScale { get; set; }
            public int? DatetimePrecision { get; set; }
            public bool IsIdentity { get; set; }
            public bool IsPrimaryKey { get; set; }
        }
        private SqlColumnType[] GetSqlColumns()
        {
            const string query = @"SELECT C.TABLE_NAME, C.COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, DATETIME_PRECISION
	,C.IDENTITY_GENERATION = 'ALWAYS' OR C.IS_GENERATED = 'ALWAYS' AS IS_IDENTITY
	,RC.CONSTRAINT_TYPE = 'PRIMARY KEY' AND C.TABLE_NAME = KF.TABLE_NAME AS IS_PRIMARYKEY
FROM INFORMATION_SCHEMA.COLUMNS C
LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KF ON KF.COLUMN_NAME = C.COLUMN_NAME AND C.TABLE_NAME = KF.TABLE_NAME
LEFT JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS RC ON RC.CONSTRAINT_NAME = KF.CONSTRAINT_NAME
WHERE C.TABLE_SCHEMA != 'pg_catalog'
AND C.TABLE_SCHEMA != 'information_schema'";

            var sqlColumns = ExecuteSqlQuery(query).Select(x => (IList<object>)x).Select(x => new SqlColumnType()
            {
                Table = (string)x[0],
                Column = (string)x[1],
                DataType = (string)x[2],
                IsNullable = (string)x[3] == "YES",
                CharacterMaximumLength = x[4] != DBNull.Value ? (int?)x[4] : null,
                NumericPrecision = x[5] != DBNull.Value ? (int?)x[5] : null,
                NumericScale = x[6] != DBNull.Value ? (int?)x[6] : null,
                DatetimePrecision = x[7] != DBNull.Value ? (int?)x[7] : null,
                IsIdentity = x[8] != DBNull.Value ? (bool)x[8] : false,
                IsPrimaryKey = x[9] != DBNull.Value ? (bool)x[9] : false,
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


        public string GenerateSqlInsert<TModel>(TModel model, Graph<TModel> graph, ModelDetail modelDetail)
        {
            var sbColumns = new CharWriteBuffer();
            var sbValues = new CharWriteBuffer();
            var sbReturns = new CharWriteBuffer();
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
                            sbColumns.Write(property);

                            if (sbValues.Length > 0)
                                sbValues.Write(',');
                            AppendSqlValue(ref sbValues, modelPropertyDetail, value, false, false);
                        }
                    }
                }
                foreach (var modelPropertyDetail in modelDetail.IdentityAutoGeneratedProperties)
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
                            sbReturns.Write(property);

                            if (sbValues.Length > 0)
                                sbValues.Write(',');
                            AppendSqlValue(ref sbReturns, modelPropertyDetail, value, false, false);
                        }
                    }
                }

                string sql;
                if (sbReturns.Length > 0)
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
            }
        }
        public string GenerateSqlUpdate<TModel>(TModel model, Graph<TModel> graph, ModelDetail modelDetail)
        {
            if (modelDetail.IdentityProperties.Count == 0)
                return null;

            var writer = new CharWriteBuffer();
            try
            {
                writer.Write("UPDATE ");
                writer.Write(modelDetail.DataSourceEntityName);
                writer.Write(" SET ");
                bool first = true;
                foreach (var modelPropertyInfo in modelDetail.NonAutoGeneratedNonRelationProperties)
                {
                    if (graph.HasLocalProperty(modelPropertyInfo.Name))
                    {
                        object value = modelPropertyInfo.Getter(model);
                        string property = modelPropertyInfo.PropertySourceName;

                        if (first) first = false;
                        else writer.Write(',');
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
                    object value = modelPropertyInfo.Getter(model);
                    string property = modelPropertyInfo.PropertySourceName;

                    if (first) first = false;
                    else writer.Write(" AND ");
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
        public string GenerateSqlDelete(ICollection ids, ModelDetail modelDetail)
        {
            var sbWhere = new CharWriteBuffer();
            try
            {
                sbWhere.Write($"DELETE FROM ");
                sbWhere.Write(modelDetail.DataSourceEntityName);
                sbWhere.Write(" WHERE ");
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
                        sbWhere.Write(property);
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
                            sbWhere.Write(property);
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
    }
}
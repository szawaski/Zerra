//// Copyright © KaKush LLC
//// Written By Steven Zawaski
//// Licensed to you under the MIT license

//using System.Linq.Expressions;
//using Microsoft.Data.SqlClient;
//using Zerra.Repository.Reflection;
//using Zerra.Reflection;
//using System.Text;
//using Zerra.Logging;
//using System.Data;
//using System.Runtime.CompilerServices;
//using System.Collections;
//using Zerra.Repository.IO;
//using Zerra.Collections;

//namespace Zerra.Repository.Memory
//{
//    /// <summary>
//    /// The core data store engine for Microsoft SQL Server, implementing query, insert, update, delete, and schema generation operations.
//    /// </summary>
//    public sealed partial class MemoryEngine : ITransactStoreEngine
//    {
//        private static readonly ConcurrentFactoryDictionary<Type, object> data = new();

//        /// <inheritdoc/>
//        public IReadOnlyCollection<TModel> ExecuteMany<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
//        {
//            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
//            if (where is not Expression<Func<TModel, bool>> whereTyped)
//                throw new InvalidOperationException();

//            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
//            return whereQuery.ToArray();
//        }
//        /// <inheritdoc/>
//        public TModel? ExecuteFirst<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
//        {
//            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
//            if (where is not Expression<Func<TModel, bool>> whereTyped)
//                throw new InvalidOperationException();

//            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
//            return whereQuery.First();
//        }
//        /// <inheritdoc/>
//        public TModel? ExecuteSingle<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
//        {
//            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
//            if (where is not Expression<Func<TModel, bool>> whereTyped)
//                throw new InvalidOperationException();

//            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
//            return whereQuery.Single();
//        }
//        /// <inheritdoc/>
//        public long ExecuteCount(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail)
//        {
//            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
//            if (where is not Expression<Func<TModel, bool>> whereTyped)
//                throw new InvalidOperationException();

//            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
//            return whereQuery.Count();
//        }
//        /// <inheritdoc/>
//        public bool ExecuteAny(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail)
//        {
//            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
//            if (where is not Expression<Func<TModel, bool>> whereTyped)
//                throw new InvalidOperationException();

//            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
//            return whereQuery.ToArray();
//        }

//        /// <inheritdoc/>
//        public Task<IReadOnlyCollection<TModel>> ExecuteManyAsync<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
//        {
//            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
//            if (where is not Expression<Func<TModel, bool>> whereTyped)
//                throw new InvalidOperationException();

//            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
//            return Task.FromResult<IReadOnlyCollection<TModel>>(whereQuery.ToArray());
//        }
//        /// <inheritdoc/>
//        public Task<TModel?> ExecuteFirstAsync<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
//        {
//            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
//            if (where is not Expression<Func<TModel, bool>> whereTyped)
//                throw new InvalidOperationException();

//            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
//            return Task.FromResult<TModel?>(whereQuery.First());
//        }
//        /// <inheritdoc/>
//        public Task<TModel?> ExecuteSingleAsync<TModel>(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
//        {
//            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
//            if (where is not Expression<Func<TModel, bool>> whereTyped)
//                throw new InvalidOperationException();

//            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
//            return Task.FromResult<TModel?>(whereQuery.Single());
//        }
//        /// <inheritdoc/>
//        public Task<long> ExecuteCountAsync(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail)
//        {
//            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
//            if (where is not Expression<Func<TModel, bool>> whereTyped)
//                throw new InvalidOperationException();

//            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
//            return Task.FromResult(whereQuery.Count());
//        }
//        /// <inheritdoc/>
//        public async Task<bool> ExecuteAnyAsync(Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail)
//        {
//            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
//            if (where is not Expression<Func<TModel, bool>> whereTyped)
//                throw new InvalidOperationException();

//            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
//            return Task.FromResult(whereQuery.Any())
//        }

//        /// <inheritdoc/>
//        public IReadOnlyCollection<object> ExecuteInsertGetIdentities<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
//        {
            
//        }
//        /// <inheritdoc/>
//        public int ExecuteInsert<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
//        {
//            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
//            source.Add(model);
//        }
//        /// <inheritdoc/>
//        public int ExecuteUpdate<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
//        {
//            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
            
//        }
//        /// <inheritdoc/>
//        public int ExecuteDelete(ICollection ids, ModelDetail modelDetail)
//        {
//            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
//        }

//        /// <inheritdoc/>
//        public async Task<IReadOnlyCollection<object>> ExecuteInsertGetIdentitiesAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
//        {
//            var sql = MsSqlEngine.GenerateSqlInsert(model, graph, modelDetail, true);

//            var allValues = new List<object>();
//            using (var connection = new SqlConnection(connectionString))
//            {
//                await connection.OpenAsync();
//                using (var command = connection.CreateCommand())
//                {
//                    command.CommandTimeout = 0;
//                    command.CommandText = sql;
//                    using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
//                    {
//                        if (reader.HasRows)
//                        {
//                            while (await reader.ReadAsync())
//                            {
//                                if (reader.FieldCount == 1)
//                                {
//                                    var value = reader[0];
//                                    allValues.Add(value);
//                                }
//                                else
//                                {
//                                    var values = new List<object>();
//                                    for (var i = 0; i < reader.FieldCount; i++)
//                                    {
//                                        var value = reader[i];
//                                        values.Add(value);
//                                    }
//                                    allValues.Add(values.ToArray());
//                                }
//                            }
//                        }
//                    }
//                }
//            }

//            return allValues;
//        }
//        /// <inheritdoc/>
//        public async Task<int> ExecuteInsertAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
//        {
//            var sql = MsSqlEngine.GenerateSqlInsert(model, graph, modelDetail, false);

//            using (var connection = new SqlConnection(connectionString))
//            {
//                await connection.OpenAsync();
//                using (var command = connection.CreateCommand())
//                {
//                    command.CommandTimeout = 0;
//                    command.CommandText = sql;
//                    return await command.ExecuteNonQueryAsync();
//                }
//            }
//        }
//        /// <inheritdoc/>
//        public async Task<int> ExecuteUpdateAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
//        {
//            var sql = MsSqlEngine.GenerateSqlUpdate(model, graph, modelDetail);
//            if (sql is null)
//                return 0;

//            using (var connection = new SqlConnection(connectionString))
//            {
//                await connection.OpenAsync();
//                using (var command = connection.CreateCommand())
//                {
//                    command.CommandTimeout = 0;
//                    command.CommandText = sql;
//                    return await command.ExecuteNonQueryAsync();
//                }
//            }
//        }
//        /// <inheritdoc/>
//        public async Task<int> ExecuteDeleteAsync(ICollection ids, ModelDetail modelDetail)
//        {
//            var sql = MsSqlEngine.GenerateSqlDelete(ids, modelDetail);

//            using (var connection = new SqlConnection(connectionString))
//            {
//                await connection.OpenAsync();
//                using (var command = connection.CreateCommand())
//                {
//                    command.CommandTimeout = 0;
//                    command.CommandText = sql;
//                    return await command.ExecuteNonQueryAsync();
//                }
//            }
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        internal void ExecuteSql(string sql)
//        {
//            using (var connection = new SqlConnection(connectionString))
//            {
//                connection.Open();
//                using (var command = connection.CreateCommand())
//                {
//                    command.CommandTimeout = 0;
//                    command.CommandText = sql;
//                    _ = command.ExecuteNonQuery();
//                }
//            }
//        }
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private T ExecuteSqlScalar<T>(string sql)
//        {
//            using (var connection = new SqlConnection(connectionString))
//            {
//                connection.Open();
//                using (var command = connection.CreateCommand())
//                {
//                    command.CommandTimeout = 0;
//                    command.CommandText = sql;
//                    return (T)command.ExecuteScalar();
//                }
//            }
//        }
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private ICollection<object> ExecuteSqlQuery(string sql)
//        {
//            var allValues = new List<object>();
//            using (var connection = new SqlConnection(connectionString))
//            {
//                connection.Open();
//                using (var command = connection.CreateCommand())
//                {
//                    command.CommandTimeout = 0;
//                    command.CommandText = sql;
//                    using (var reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
//                    {
//                        if (reader.HasRows)
//                        {
//                            while (reader.Read())
//                            {
//                                if (reader.FieldCount == 1)
//                                {
//                                    var value = reader[0];
//                                    allValues.Add(value);
//                                }
//                                else
//                                {
//                                    var values = new List<object>();
//                                    for (var i = 0; i < reader.FieldCount; i++)
//                                    {
//                                        var value = reader[i];
//                                        values.Add(value);
//                                    }
//                                    allValues.Add(values.ToArray());
//                                }
//                            }
//                        }
//                    }
//                }
//            }

//            return allValues;
//        }


//    }
//}
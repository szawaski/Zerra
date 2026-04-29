// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using Zerra.Repository.Reflection;
using Zerra.Reflection;
using System.Text;
using Zerra.Logging;
using System.Data;
using System.Runtime.CompilerServices;
using System.Collections;
using Zerra.Repository.IO;
using Zerra.Collections;
using Zerra.Map;

namespace Zerra.Repository.Memory
{
    /// <summary>
    /// The core data store engine for Microsoft SQL Server, implementing query, insert, update, delete, and schema generation operations.
    /// </summary>
    public sealed partial class MemoryEngine : ITransactStoreEngine
    {
        private static readonly ConcurrentFactoryDictionary<Type, object> data = new();

        /// <inheritdoc/>
        public IReadOnlyCollection<TModel> ExecuteMany<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
            if (where is not Expression<Func<TModel, bool>> whereTyped)
                throw new InvalidOperationException();

            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            return whereQuery.Select(x => x.Copy(graph)).ToArray();
        }
        /// <inheritdoc/>
        public TModel? ExecuteFirst<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
            if (where is not Expression<Func<TModel, bool>> whereTyped)
                throw new InvalidOperationException();

            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            return whereQuery.First().Copy(graph);
        }
        /// <inheritdoc/>
        public TModel? ExecuteSingle<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
            if (where is not Expression<Func<TModel, bool>> whereTyped)
                throw new InvalidOperationException();

            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            return whereQuery.Single().Copy(graph);
        }
        /// <inheritdoc/>
        public long ExecuteCount<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
            if (where is not Expression<Func<TModel, bool>> whereTyped)
                throw new InvalidOperationException();

            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            return whereQuery.Count();
        }
        /// <inheritdoc/>
        public bool ExecuteAny<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
            if (where is not Expression<Func<TModel, bool>> whereTyped)
                throw new InvalidOperationException();

            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            return whereQuery.Any();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyCollection<TModel>> ExecuteManyAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
            if (where is not Expression<Func<TModel, bool>> whereTyped)
                throw new InvalidOperationException();

            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            return Task.FromResult<IReadOnlyCollection<TModel>>(whereQuery.Select(x => x.Copy(graph)).ToArray());
        }
        /// <inheritdoc/>
        public Task<TModel?> ExecuteFirstAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
            if (where is not Expression<Func<TModel, bool>> whereTyped)
                throw new InvalidOperationException();

            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            return Task.FromResult<TModel?>(whereQuery.First().Copy(graph));
        }
        /// <inheritdoc/>
        public Task<TModel?> ExecuteSingleAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
            if (where is not Expression<Func<TModel, bool>> whereTyped)
                throw new InvalidOperationException();

            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            return Task.FromResult<TModel?>(whereQuery.Single().Copy(graph));
        }
        /// <inheritdoc/>
        public Task<long> ExecuteCountAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
            if (where is not Expression<Func<TModel, bool>> whereTyped)
                throw new InvalidOperationException();

            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            return Task.FromResult<long>(whereQuery.Count());
        }
        /// <inheritdoc/>
        public Task<bool> ExecuteAnyAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var source = (ConcurrentList<TModel>)data.GetOrAdd(typeof(TModel), () => new ConcurrentList<TModel>());
            if (where is not Expression<Func<TModel, bool>> whereTyped)
                throw new InvalidOperationException();

            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            return Task.FromResult(whereQuery.Any());
        }

        /// <inheritdoc/>
        public object ExecuteInsertGetIdentities<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var copy = model.Copy();
            lock (source)
            {
                foreach (var identityProperty in modelDetail.IdentityProperties)
                {
                    if (!identityProperty.IsIdentityAutoGenerated)
                        continue;
                    var newIdentity = GenerateIdentity(source, identityProperty);
                    identityProperty.SetterBoxed(model, newIdentity);
                }
                source.Add(copy);
            }
            var id = ModelAnalyzer.GetIdentity(type, copy);
            return id;
        }
        /// <inheritdoc/>
        public bool ExecuteInsert<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var copy = model.Copy();
            lock (source)
            {
                foreach (var identityProperty in modelDetail.IdentityProperties)
                {
                    if (!identityProperty.IsIdentityAutoGenerated)
                        continue;
                    var newIdentity = GenerateIdentity(source, identityProperty);
                    identityProperty.SetterBoxed(model, newIdentity);
                }
                source.Add(copy);
            }
            return true;
        }
        /// <inheritdoc/>
        public bool ExecuteUpdate<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var identity = ModelAnalyzer.GetIdentity(type, model);
            var existing = source.FirstOrDefault(x => ModelAnalyzer.CompareIdentities(identity, ModelAnalyzer.GetIdentity(type, x)));
            if (existing == null)
                return false;
            model.MapTo(existing, graph);
            return true;
        }
        /// <inheritdoc/>
        public int ExecuteDelete<TModel>(ICollection ids, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var deleteCount = 0;
            foreach (var id in ids)
            {
                var existing = source.FirstOrDefault(x => ModelAnalyzer.CompareIdentities(id, ModelAnalyzer.GetIdentity(type, x)));
                if (existing != null)
                {
                    if (source.Remove(existing))
                        deleteCount++;
                }
            }
            return deleteCount;
        }

        /// <inheritdoc/>
        public Task<object> ExecuteInsertGetIdentitiesAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var copy = model.Copy();
            lock (source)
            {
                foreach (var identityProperty in modelDetail.IdentityProperties)
                {
                    if (!identityProperty.IsIdentityAutoGenerated)
                        continue;
                    var newIdentity = GenerateIdentity(source, identityProperty);
                    identityProperty.SetterBoxed(model, newIdentity);
                }
                source.Add(copy);
            }
            var id = ModelAnalyzer.GetIdentity(type, copy);
            return Task.FromResult(id);
        }
        /// <inheritdoc/>
        public Task<bool> ExecuteInsertAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var copy = model.Copy();
            lock (source)
            {
                foreach (var identityProperty in modelDetail.IdentityProperties)
                {
                    if (!identityProperty.IsIdentityAutoGenerated)
                        continue;
                    var newIdentity = GenerateIdentity(source, identityProperty);
                    identityProperty.SetterBoxed(model, newIdentity);
                }
                source.Add(copy);
            }
            return Task.FromResult(true);
        }
        /// <inheritdoc/>
        public Task<bool> ExecuteUpdateAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var identity = ModelAnalyzer.GetIdentity(type, model);
            var existing = source.FirstOrDefault(x => ModelAnalyzer.CompareIdentities(identity, ModelAnalyzer.GetIdentity(type, x)));
            if (existing == null)
                return Task.FromResult(false);
            model.MapTo(existing, graph);
            return Task.FromResult(true);
        }
        /// <inheritdoc/>
        public Task<int> ExecuteDeleteAsync<TModel>(ICollection ids, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var deleteCount = 0;
            foreach (var id in ids)
            {
                var existing = source.FirstOrDefault(x => ModelAnalyzer.CompareIdentities(id, ModelAnalyzer.GetIdentity(type, x)));
                if (existing != null)
                {
                    if (source.Remove(existing))
                        deleteCount++;
                }
            }
            return Task.FromResult(deleteCount);
        }

        private object GenerateIdentity<TModel>(ConcurrentList<TModel> source, ModelPropertyDetail property)
        {
            if (!property.CoreType.HasValue)
                throw new InvalidOperationException($"Identity property {property.Name} does not have a core type.");
            if (property.CoreType.Value == CoreType.Guid)
                return Guid.NewGuid();

            var last = source.LastOrDefault();
            return property.CoreType.Value switch
            {
                CoreType.Byte => last == null ? (byte)1 : (byte)((byte)property.GetterBoxed(last)! + 1),
                CoreType.SByte => last == null ? (sbyte)1 : (sbyte)((sbyte)property.GetterBoxed(last)! + 1),
                CoreType.Int16 => last == null ? (short)1 : (short)((short)property.GetterBoxed(last)! + 1),
                CoreType.UInt16 => last == null ? (ushort)1 : (ushort)((ushort)property.GetterBoxed(last)! + 1),
                CoreType.Int32 => last == null ? 1 : (int)property.GetterBoxed(last)! + 1,
                CoreType.UInt32 => last == null ? (uint)1 : (uint)((uint)property.GetterBoxed(last)! + 1),
                CoreType.Int64 => (object)(last == null ? 1L : (long)property.GetterBoxed(last)! + 1L),
                CoreType.UInt64 => (object)(last == null ? 1UL : (ulong)((ulong)property.GetterBoxed(last)! + 1UL)),
                _ => throw new InvalidOperationException($"Identity property {property.Name} has unsupported core type {property.CoreType.Value}."),
            };
        }

        /// <inheritdoc />
        public bool ValidateDataSource() => true;

        /// <inheritdoc />
        public IDataStoreGenerationPlan BuildStoreGenerationPlan(bool create, bool update, bool delete, ICollection<ModelDetail> modelDetail)
        {
            return new EmptyDataStoreGenerationPlan();
        }
    }
}
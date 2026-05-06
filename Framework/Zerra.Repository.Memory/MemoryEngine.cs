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
        private static readonly ConcurrentFactoryDictionary<Type, Graph> allLocalMemberGraph = new();

        /// <inheritdoc/>
        public IReadOnlyCollection<TModel> ExecuteMany<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            var localGraph = BuildLocalGraph(type, graph);
            return whereQuery.Select(x => x.Copy(localGraph)).ToArray();
        }
        /// <inheritdoc/>
        public TModel? ExecuteFirst<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            var localGraph = BuildLocalGraph(type, graph);
            var result = whereQuery.FirstOrDefault();
            if (result == null)
                return null;
            return result.Copy(localGraph);
        }
        /// <inheritdoc/>
        public TModel? ExecuteSingle<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            var localGraph = BuildLocalGraph(type, graph);
            var result = whereQuery.SingleOrDefault();
            if (result == null)
                return null;
            return result.Copy(localGraph);
        }
        /// <inheritdoc/>
        public long ExecuteCount<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            return whereQuery.Count();
        }
        /// <inheritdoc/>
        public bool ExecuteAny<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            return whereQuery.Any();
        }

        /// <inheritdoc/>
        public Task<IReadOnlyCollection<TModel>> ExecuteManyAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            var localGraph = BuildLocalGraph(type, graph);
            return Task.FromResult<IReadOnlyCollection<TModel>>(whereQuery.Select(x => x.Copy(localGraph)).ToArray());
        }
        /// <inheritdoc/>
        public Task<TModel?> ExecuteFirstAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            var localGraph = BuildLocalGraph(type, graph);
            var result = whereQuery.FirstOrDefault();
            if (result == null)
                return Task.FromResult<TModel?>(null);
            return Task.FromResult<TModel?>(result.Copy(localGraph));
        }
        /// <inheritdoc/>
        public Task<TModel?> ExecuteSingleAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            var localGraph = BuildLocalGraph(type, graph);
            var result = whereQuery.SingleOrDefault();
            if (result == null)
                return Task.FromResult<TModel?>(null);
            return Task.FromResult<TModel?>(result.Copy(localGraph));
        }
        /// <inheritdoc/>
        public Task<long> ExecuteCountAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            return Task.FromResult<long>(whereQuery.Count());
        }
        /// <inheritdoc/>
        public Task<bool> ExecuteAnyAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take);
            return Task.FromResult(whereQuery.Any());
        }

        /// <inheritdoc/>
        public object ExecuteInsertGetIdentities<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            TModel copy;
            lock (source)
            {
                foreach (var identityProperty in modelDetail.IdentityMembers)
                {
                    if (!identityProperty.IsIdentityAutoGenerated)
                        continue;
                    var newIdentity = GenerateIdentity(source, identityProperty);
                    identityProperty.SetterBoxed(model, newIdentity);
                }
                copy = model.Copy();
                source.Add(copy);
            }
            MapRelated(copy, modelDetail, false);
            var id = ModelAnalyzer.GetIdentity(type, copy);
            return id;
        }
        /// <inheritdoc/>
        public bool ExecuteInsert<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            TModel copy;
            lock (source)
            {
                foreach (var identityProperty in modelDetail.IdentityMembers)
                {
                    if (!identityProperty.IsIdentityAutoGenerated)
                        continue;
                    var newIdentity = GenerateIdentity(source, identityProperty);
                    identityProperty.SetterBoxed(model, newIdentity);
                }
                copy = model.Copy();
                source.Add(copy);
            }
            MapRelated(copy, modelDetail, false);
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
            MapRelated(existing, modelDetail, false);
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
                    MapRelated(existing, modelDetail, true);
                }
            }
            return deleteCount;
        }

        /// <inheritdoc/>
        public Task<object> ExecuteInsertGetIdentitiesAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            TModel copy;
            lock (source)
            {
                foreach (var identityProperty in modelDetail.IdentityMembers)
                {
                    if (!identityProperty.IsIdentityAutoGenerated)
                        continue;
                    var newIdentity = GenerateIdentity(source, identityProperty);
                    identityProperty.SetterBoxed(model, newIdentity);
                }
                copy = model.Copy();
                source.Add(copy);
            }
            MapRelated(copy, modelDetail, false);
            var id = ModelAnalyzer.GetIdentity(type, copy);
            return Task.FromResult(id);
        }
        /// <inheritdoc/>
        public Task<bool> ExecuteInsertAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            TModel copy;
            lock (source)
            {
                foreach (var identityProperty in modelDetail.IdentityMembers)
                {
                    if (!identityProperty.IsIdentityAutoGenerated)
                        continue;
                    var newIdentity = GenerateIdentity(source, identityProperty);
                    identityProperty.SetterBoxed(model, newIdentity);
                }
                copy = model.Copy();
                source.Add(copy);
            }
            MapRelated(copy, modelDetail, false);
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
            MapRelated(existing, modelDetail, false);
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
                    MapRelated(existing, modelDetail, true);
                }
            }
            return Task.FromResult(deleteCount);
        }

        /// <inheritdoc />
        public bool ValidateDataSource() => true;

        /// <inheritdoc />
        public IDataStoreGenerationPlan BuildStoreGenerationPlan(bool create, bool update, bool delete, ICollection<ModelDetail> modelDetail)
        {
            return new EmptyDataStoreGenerationPlan();
        }

        private static Graph GetAllLocalMemberGraph(Type type)
        {
            return allLocalMemberGraph.GetOrAdd(type, (type) =>
            {
                var modelType = ModelAnalyzer.GetModel(type);
                var graph = new Graph();
                foreach (var member in modelType.Members)
                {
                    if (member.IsDataSourceEntity)
                        continue;
                    graph.AddMember(member.Name);
                }
                return graph;
            });
        }
        private static Graph BuildLocalGraph(Type type, Graph? inputGraph)
        {
            if (inputGraph == null)
                return GetAllLocalMemberGraph(type);

            var modelType = ModelAnalyzer.GetModel(type);
            var graph = new Graph();
            foreach (var member in modelType.Members)
            {
                if (member.IsDataSourceEntity)
                    continue;
                if (!inputGraph.HasMember(member.Name))
                    continue;
                graph.AddMember(member.Name);
            }
            return graph;
        }

        private static object GenerateIdentity<TModel>(ConcurrentList<TModel> source, ModelMemberDetail property) where TModel : class, new()
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

        private static void MapRelated(object model, ModelDetail modelDetail, bool isDelete)
        {
            MapRelated(model, modelDetail, isDelete, new Stack<object>());
        }
        private static void MapRelated(object model, ModelDetail modelDetail, bool isDelete, Stack<object> stack)
        {
            foreach (var member in modelDetail.Members)
            {
                if (!isDelete && member.IsRelated)
                {
                    if (data.TryGetValue(member.ActualType, out var sourceObject))
                    {
                        var source = (IList)sourceObject;

                        if (member.IsEnumerable)
                        {
                            var id = ModelAnalyzer.GetIdentity(modelDetail.Type, model);
                            var relatedEnumerable = source.Cast<object>().Where(x => ModelAnalyzer.CompareIdentities(id, ModelAnalyzer.GetForeignIdentity(member.ActualType, member.ForeignIdentity!, x)));
                            if (member.MemberDetail.Type.IsArray)
                            {
                                var constructor = member.MemberDetail.TypeDetail.GetConstructor([typeof(int)]);
                                var array = (Array)constructor.CreatorBoxed([relatedEnumerable.Count()]);
                                var i = 0;
                                foreach (var related in relatedEnumerable)
                                    array.SetValue(related, i++);
                                member.SetterBoxed(model, array);
                            }
                            else
                            {
                                var list = (IList)member.CreatorBoxed!();
                                foreach (var related in relatedEnumerable)
                                    _ = list.Add(related);
                                member.SetterBoxed(model, list);
                            }
                        }
                        else
                        {
                            var foreignId = ModelAnalyzer.GetForeignIdentity(modelDetail.Type, member.ForeignIdentity!, model);
                            var related = source.Cast<object>().FirstOrDefault(x => ModelAnalyzer.CompareIdentities(foreignId, ModelAnalyzer.GetIdentity(member.ActualType, x)));
                            member.SetterBoxed(model, related);
                        }
                    }
                }

                foreach (var foreignReference in member.ForeignReferences)
                {
                    if (data.TryGetValue(foreignReference.Type, out var sourceObject))
                    {
                        var source = (IList)sourceObject;

                        var foreignId = member.GetterBoxed!(model);
                        var relatedEnumerable = source.Cast<object>().Where(x => ModelAnalyzer.CompareIdentities(foreignId, ModelAnalyzer.GetIdentity(foreignReference.Type, x)));
                        foreach (var related in relatedEnumerable)
                        {
                            if (!stack.Contains(related))
                            {
                                stack.Push(related);
                                MapRelated(related, foreignReference, false, stack);
                                _ = stack.Pop();
                            }
                        }
                    }
                }
            }
        }
    }
}
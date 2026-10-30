// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Zerra.Repository.Reflection;
using Zerra.Reflection;
using System.Collections;
using Zerra.Collections;
using Zerra.Map;

namespace Zerra.Repository.Memory
{
    public sealed partial class MemoryEngine
    {
        //per instance, so each engine is its own store; the graph cache below is only type metadata and is shared
        private readonly ConcurrentFactoryDictionary<Type, object> data = new();
        private static readonly ConcurrentFactoryDictionary<Type, Graph> allLocalMemberGraph = new();

        /// <inheritdoc/>
        public IReadOnlyCollection<TModel> ExecuteMany<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take, caseSensitive);
            var localGraph = BuildLocalGraph(type, graph);
            return whereQuery.Select(x => x.Copy(localGraph)).ToArray();
        }
        /// <inheritdoc/>
        public TModel? ExecuteFirst<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take, caseSensitive);
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
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take, caseSensitive);
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
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take, caseSensitive);
            return whereQuery.Count();
        }
        /// <inheritdoc/>
        public bool ExecuteAny<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take, caseSensitive);
            return whereQuery.Any();
        }

        /// <inheritdoc/>
        public object? ExecuteCalculation<TModel>(QueryOperation operation, LambdaExpression? where, LambdaExpression? value, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, null, null, null, caseSensitive);
            return whereQuery.Calculate(operation, value, caseSensitive);
        }
        /// <inheritdoc/>
        public IReadOnlyCollection<(TKey Key, TValue Value)> ExecuteCalculationGroups<TModel, TKey, TValue>(QueryOperation operation, LambdaExpression? where, LambdaExpression groupBy, LambdaExpression? value, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, null, null, null, caseSensitive);
            return whereQuery.CalculateGroups<TModel, TKey, TValue>(operation, groupBy, value, caseSensitive);
        }
        /// <inheritdoc/>
        public IReadOnlyCollection<TValue> ExecuteDistinct<TModel, TValue>(LambdaExpression value, LambdaExpression? where, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, null, null, null, caseSensitive);
            return whereQuery.Distinct<TModel, TValue>(value, caseSensitive);
        }
        /// <inheritdoc/>
        public IReadOnlyCollection<TResult> ExecuteSelect<TModel, TResult>(LambdaExpression select, LambdaExpression? where, QueryOrder? order, int? skip, int? take, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take, caseSensitive);
            return whereQuery.Select<TModel, TResult>(select, caseSensitive);
        }
        /// <inheritdoc/>
        public IReadOnlyCollection<TResult> ExecuteGroup<TModel, TResult>(LambdaExpression groupBy, LambdaExpression select, LambdaExpression? where, LambdaExpression? having, QueryOrder? order, int? skip, int? take, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, null, null, null, caseSensitive);
            return whereQuery.Group<TModel, TResult>(groupBy, select, having, order, skip, take, caseSensitive);
        }
        /// <inheritdoc/>
        public Task<IReadOnlyCollection<TModel>> ExecuteManyAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take, caseSensitive);
            var localGraph = BuildLocalGraph(type, graph);
            return Task.FromResult<IReadOnlyCollection<TModel>>(whereQuery.Select(x => x.Copy(localGraph)).ToArray());
        }
        /// <inheritdoc/>
        public Task<TModel?> ExecuteFirstAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take, caseSensitive);
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
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take, caseSensitive);
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
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take, caseSensitive);
            return Task.FromResult<long>(whereQuery.Count());
        }
        /// <inheritdoc/>
        public Task<bool> ExecuteAnyAsync<TModel>(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take, caseSensitive);
            return Task.FromResult(whereQuery.Any());
        }

        /// <inheritdoc/>
        public Task<object?> ExecuteCalculationAsync<TModel>(QueryOperation operation, LambdaExpression? where, LambdaExpression? value, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, null, null, null, caseSensitive);
            return Task.FromResult<object?>(whereQuery.Calculate(operation, value, caseSensitive));
        }
        /// <inheritdoc/>
        public Task<IReadOnlyCollection<(TKey Key, TValue Value)>> ExecuteCalculationGroupsAsync<TModel, TKey, TValue>(QueryOperation operation, LambdaExpression? where, LambdaExpression groupBy, LambdaExpression? value, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, null, null, null, caseSensitive);
            return Task.FromResult<IReadOnlyCollection<(TKey Key, TValue Value)>>(whereQuery.CalculateGroups<TModel, TKey, TValue>(operation, groupBy, value, caseSensitive));
        }
        /// <inheritdoc/>
        public Task<IReadOnlyCollection<TValue>> ExecuteDistinctAsync<TModel, TValue>(LambdaExpression value, LambdaExpression? where, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, null, null, null, caseSensitive);
            return Task.FromResult<IReadOnlyCollection<TValue>>(whereQuery.Distinct<TModel, TValue>(value, caseSensitive));
        }
        /// <inheritdoc/>
        public Task<IReadOnlyCollection<TResult>> ExecuteSelectAsync<TModel, TResult>(LambdaExpression select, LambdaExpression? where, QueryOrder? order, int? skip, int? take, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, order, skip, take, caseSensitive);
            return Task.FromResult<IReadOnlyCollection<TResult>>(whereQuery.Select<TModel, TResult>(select, caseSensitive));
        }
        /// <inheritdoc/>
        public Task<IReadOnlyCollection<TResult>> ExecuteGroupAsync<TModel, TResult>(LambdaExpression groupBy, LambdaExpression select, LambdaExpression? where, LambdaExpression? having, QueryOrder? order, int? skip, int? take, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var whereQuery = source.AsEnumerable().Query(where, null, null, null, caseSensitive);
            return Task.FromResult<IReadOnlyCollection<TResult>>(whereQuery.Group<TModel, TResult>(groupBy, select, having, order, skip, take, caseSensitive));
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
            //only the generated identities are returned, like a database returns
            object id;
            if (modelDetail.IdentityAutoGeneratedMembers.Count == 1)
            {
                id = modelDetail.IdentityAutoGeneratedMembers[0].GetterBoxed(copy)!;
            }
            else
            {
                var ids = new object[modelDetail.IdentityAutoGeneratedMembers.Count];
                for (var i = 0; i < ids.Length; i++)
                    ids[i] = modelDetail.IdentityAutoGeneratedMembers[i].GetterBoxed(copy)!;
                id = ids;
            }
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
            model.MapTo(existing, BuildLocalGraph(type, graph));
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
        public int ExecuteUpdateWhere<TModel>(TModel model, Graph graph, LambdaExpression where, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var localGraph = BuildLocalGraph(type, graph);
            var count = 0;
            foreach (var existing in source.AsEnumerable().Query(where, null, null, null, caseSensitive).ToArray())
            {
                model.MapTo(existing, localGraph);
                MapRelated(existing, modelDetail, false);
                count++;
            }
            return count;
        }
        /// <inheritdoc/>
        public int ExecuteUpdateWhere<TModel>(LambdaExpression set, LambdaExpression where, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var sets = set.GetProjectionMembers(null);
            var getters = new Func<TModel, object?>[sets.Length];
            var members = new ModelMemberDetail[sets.Length];
            for (var i = 0; i < sets.Length; i++)
            {
                getters[i] = CalculationCommon.CompileBoxed<TModel>(sets[i].Expression!, sets[i].Parameter!, caseSensitive);
                members[i] = modelDetail.GetMember(sets[i].MemberDetail.Name);
            }
            var values = new object?[sets.Length];
            var count = 0;
            foreach (var existing in source.AsEnumerable().Query(where, null, null, null, caseSensitive).ToArray())
            {
                //every value is computed from the model as it was, as a database computes them from the row
                for (var i = 0; i < values.Length; i++)
                    values[i] = getters[i](existing);
                for (var i = 0; i < values.Length; i++)
                    members[i].SetterBoxed(existing, values[i]);
                MapRelated(existing, modelDetail, false);
                count++;
            }
            return count;
        }
        /// <inheritdoc/>
        public int ExecuteDeleteWhere<TModel>(LambdaExpression where, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var count = 0;
            foreach (var existing in source.AsEnumerable().Query(where, null, null, null, caseSensitive).ToArray())
            {
                if (source.Remove(existing))
                    count++;
                MapRelated(existing, modelDetail, true);
            }
            return count;
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
            object id;
            if (modelDetail.IdentityAutoGeneratedMembers.Count == 1)
            {
                id = modelDetail.IdentityAutoGeneratedMembers[0].GetterBoxed(copy)!;
            }
            else
            {
                var ids = new object[modelDetail.IdentityAutoGeneratedMembers.Count];
                for (var i = 0; i < ids.Length; i++)
                    ids[i] = modelDetail.IdentityAutoGeneratedMembers[i].GetterBoxed(copy)!;
                id = ids;
            }
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
            model.MapTo(existing, BuildLocalGraph(type, graph));
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
        /// <inheritdoc/>
        public Task<int> ExecuteUpdateWhereAsync<TModel>(TModel model, Graph graph, LambdaExpression where, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var localGraph = BuildLocalGraph(type, graph);
            var count = 0;
            foreach (var existing in source.AsEnumerable().Query(where, null, null, null, caseSensitive).ToArray())
            {
                model.MapTo(existing, localGraph);
                MapRelated(existing, modelDetail, false);
                count++;
            }
            return Task.FromResult(count);
        }
        /// <inheritdoc/>
        public Task<int> ExecuteUpdateWhereAsync<TModel>(LambdaExpression set, LambdaExpression where, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var sets = set.GetProjectionMembers(null);
            var getters = new Func<TModel, object?>[sets.Length];
            var members = new ModelMemberDetail[sets.Length];
            for (var i = 0; i < sets.Length; i++)
            {
                getters[i] = CalculationCommon.CompileBoxed<TModel>(sets[i].Expression!, sets[i].Parameter!, caseSensitive);
                members[i] = modelDetail.GetMember(sets[i].MemberDetail.Name);
            }
            var values = new object?[sets.Length];
            var count = 0;
            foreach (var existing in source.AsEnumerable().Query(where, null, null, null, caseSensitive).ToArray())
            {
                //every value is computed from the model as it was, as a database computes them from the row
                for (var i = 0; i < values.Length; i++)
                    values[i] = getters[i](existing);
                for (var i = 0; i < values.Length; i++)
                    members[i].SetterBoxed(existing, values[i]);
                MapRelated(existing, modelDetail, false);
                count++;
            }
            return Task.FromResult(count);
        }
        /// <inheritdoc/>
        public Task<int> ExecuteDeleteWhereAsync<TModel>(LambdaExpression where, ModelDetail modelDetail) where TModel : class, new()
        {
            var type = typeof(TModel);
            var source = (ConcurrentList<TModel>)data.GetOrAdd(type, () => new ConcurrentList<TModel>());
            var count = 0;
            foreach (var existing in source.AsEnumerable().Query(where, null, null, null, caseSensitive).ToArray())
            {
                if (source.Remove(existing))
                    count++;
                MapRelated(existing, modelDetail, true);
            }
            return Task.FromResult(count);
        }

        private static Graph GetAllLocalMemberGraph(Type type)
        {
            return allLocalMemberGraph.GetOrAdd(type, (type) =>
            {
                var modelType = ModelAnalyzer.GetModel(type);
                var graph = new Graph();
                foreach (var member in modelType.Members)
                {
                    if (member.IsDataSourceEntity || member.IsRelated)
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
                if (member.IsDataSourceEntity || member.IsRelated)
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

        private void MapRelated(object model, ModelDetail modelDetail, bool isDelete)
        {
            MapRelated(model, modelDetail, isDelete, new Stack<object>());
        }
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Only interface members reach MakeArrayType, models are reference types so the array code is shared.")]
        private void MapRelated(object model, ModelDetail modelDetail, bool isDelete, Stack<object> stack)
        {
            foreach (var member in modelDetail.Members)
            {
                if (!isDelete && member.IsRelated)
                {
                    //a related type nothing was stored for yet has no list; the relation is then empty rather than null so queries over it behave as in a database
                    var source = data.TryGetValue(member.ActualType, out var sourceObject) ? (IList)sourceObject : Array.Empty<object>();

                    if (member.IsEnumerable)
                    {
                        var id = ModelAnalyzer.GetIdentity(modelDetail.Type, model);
                        var relatedEnumerable = source.Cast<object>().Where(x => ModelAnalyzer.CompareIdentities(id, ModelAnalyzer.GetForeignIdentity(member.ActualType, member.ForeignIdentity!, x)));
                        //collections with a creator (List<T>) are filled directly, arrays and the interfaces an array satisfies are filled as arrays
                        if (member.MemberDetail.TypeDetail.HasCreator)
                        {
                            var list = (IList)member.CreatorBoxed();
                            foreach (var related in relatedEnumerable)
                                _ = list.Add(related);
                            member.SetterBoxed(model, list);
                        }
                        else
                        {
                            var matches = relatedEnumerable.ToArray();
                            var array = Array.CreateInstanceFromArrayType(member.Type.IsArray ? member.Type : member.ActualType.MakeArrayType(), matches.Length);
                            for (var i = 0; i < matches.Length; i++)
                                array.SetValue(matches[i], i);
                            member.SetterBoxed(model, array);
                        }
                    }
                    else
                    {
                        var foreignId = ModelAnalyzer.GetForeignIdentity(modelDetail.Type, member.ForeignIdentity!, model);
                        var related = source.Cast<object>().FirstOrDefault(x => ModelAnalyzer.CompareIdentities(foreignId, ModelAnalyzer.GetIdentity(member.ActualType, x)));
                        member.SetterBoxed(model, related);
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

// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;

namespace Zerra.Repository.Test
{
    //generated from the IRepo overloads, every overload passes each of its arguments to the provider
    public class RepoOverloadTests
    {
        [Entity("OverloadModel")]
        public sealed class Model
        {
            [Identity]
            public int ID { get; set; }
        }

        private sealed class RecordingProvider : ITransactStoreProvider<Model>
        {
            public Query? Query { get; private set; }
            public Persist? Persist { get; private set; }
            public void Clear()
            {
                Query = null;
                Persist = null;
            }

            private object? Result(Query query)
            {
                Query = query;
                return query.Operation switch
                {
                    QueryOperation.Many => Array.Empty<Model>(),
                    QueryOperation.EventMany => Array.Empty<EventModel<Model>>(),
                    QueryOperation.Count or QueryOperation.EventCount => 0L,
                    QueryOperation.Any or QueryOperation.EventAny => false,
                    _ => null,
                };
            }
            object? ITransactStoreProvider.Query(Query query) => Result(query);
            Task<object?> ITransactStoreProvider.QueryAsync(Query query) => Task.FromResult(Result(query));
            void ITransactStoreProvider.Persist(Persist persist) => Persist = persist;
            Task ITransactStoreProvider.PersistAsync(Persist persist)
            {
                Persist = persist;
                return Task.CompletedTask;
            }
        }

        private static readonly Model model = new() { ID = 7 };
        private static readonly Model[] models = [new() { ID = 7 }, new() { ID = 8 }];
        private static readonly int[] ids = [7, 8];
        private static readonly object source = new();
        private static readonly PersistEvent evt = new(Guid.NewGuid(), "given", source);
        private static readonly Graph<Model> graph = new(x => x.ID);
        private static readonly Expression<Func<Model, bool>> where = x => x.ID == 7;
        private static readonly QueryOrder<Model> order = QueryOrder<Model>.Create(x => x.ID);
        private static readonly DateTime dateFrom = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime dateTo = new(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        private static (IRepo, RecordingProvider) Create()
        {
            var provider = new RecordingProvider();
            var repo = Repo.New();
            repo.AddProvider<Model>(provider);
            return (repo, provider);
        }

        [Fact]
        public void Sync()
        {
            var (repo, provider) = Create();
            {
                provider.Clear();
                _ = repo.Any<Model>(where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Any, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.Count<Model>();
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Count, q.Operation);
                Assert.Null(q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.Count<Model>(where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Count, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                repo.Create<Model>(model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Create<Model>("evt", model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Create<Model>("evt", source, model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Create<Model>(model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Create<Model>("evt", model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Create<Model>("evt", source, model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Create<Model>(evt, model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Create<Model>(evt, model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Create<Model>(models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Create<Model>("evt", models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Create<Model>("evt", source, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Create<Model>(models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Create<Model>("evt", models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Create<Model>("evt", source, models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Create<Model>(evt, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Create<Model>(evt, models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Delete<Model>(model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Delete<Model>("evt", model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Delete<Model>("evt", source, model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Delete<Model>(model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Delete<Model>("evt", model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Delete<Model>("evt", source, model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Delete<Model>(evt, model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Delete<Model>(evt, model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Delete<Model>(models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Delete<Model>("evt", models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Delete<Model>("evt", source, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Delete<Model>(models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Delete<Model>("evt", models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Delete<Model>("evt", source, models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Delete<Model>(evt, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Delete<Model>(evt, models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.DeleteByID<Model>((object)7);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                repo.DeleteByID<Model>("evt", (object)7);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                repo.DeleteByID<Model>("evt", source, (object)7);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                repo.DeleteByID<Model>((object)7, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                repo.DeleteByID<Model>("evt", (object)7, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                repo.DeleteByID<Model>("evt", source, (object)7, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                repo.DeleteByID<Model>(evt, (object)7);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                repo.DeleteByID<Model>(evt, (object)7, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                repo.DeleteByID<Model>(ids);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                repo.DeleteByID<Model>("evt", ids);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                repo.DeleteByID<Model>("evt", source, ids);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                repo.DeleteByID<Model>(ids, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                repo.DeleteByID<Model>("evt", ids, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                repo.DeleteByID<Model>("evt", source, ids, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                repo.DeleteByID<Model>(evt, ids);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                repo.DeleteByID<Model>(evt, ids, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                _ = repo.EventAny<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventAny, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.EventAny<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventAny, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.EventCount<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventCount, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.EventCount<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventCount, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.EventFirst<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where, order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventFirst, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.EventFirst<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where, order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventFirst, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.EventMany<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where, order, 1, 2, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventMany, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.EventMany<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where, order, 1, 2, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventMany, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.EventSingle<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where, order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventSingle, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.EventSingle<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where, order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventSingle, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.First<Model>(order);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.First, q.Operation);
                Assert.Null(q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.First<Model>(where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.First, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.First<Model>(graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.First, q.Operation);
                Assert.Null(q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.First<Model>(order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.First, q.Operation);
                Assert.Null(q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.First<Model>(where, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.First, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.First<Model>(where, order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.First, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.Many<Model>(1, 2);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Null(q.Where);
                Assert.Null(q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Null(q.Graph);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.Many<Model>(order, 1, 2);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Null(q.Where);
                Assert.Same(order, q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Null(q.Graph);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.Many<Model>(where, order, 1, 2);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Null(q.Graph);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.Many<Model>(graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Null(q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.Many<Model>(1, 2, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Null(q.Where);
                Assert.Null(q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.Many<Model>(order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Null(q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.Many<Model>(order, 1, 2, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Null(q.Where);
                Assert.Same(order, q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.Many<Model>(where, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.Many<Model>(where, order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.Many<Model>(where, order, 1, 2, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.Single<Model>(where, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Single, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.Single<Model>(graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Single, q.Operation);
                Assert.Null(q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.TemporalAny<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Any, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.TemporalAny<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Any, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.TemporalCount<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Count, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.TemporalCount<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Count, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.TemporalFirst<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where, order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.First, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.TemporalFirst<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where, order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.First, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.TemporalMany<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where, order, 1, 2, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = repo.TemporalMany<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where, order, 1, 2, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                repo.Update<Model>(model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Update<Model>("evt", model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Update<Model>("evt", source, model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Update<Model>(model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Update<Model>("evt", model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Update<Model>("evt", source, model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Update<Model>(evt, model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Update<Model>(evt, model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                repo.Update<Model>(models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Update<Model>("evt", models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Update<Model>("evt", source, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Update<Model>(models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Update<Model>("evt", models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Update<Model>("evt", source, models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Update<Model>(evt, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.Update<Model>(evt, models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
        }

        [Fact]
        public async Task Async()
        {
            var (repo, provider) = Create();
            {
                provider.Clear();
                _ = await repo.AnyAsync<Model>(where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Any, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.CountAsync<Model>();
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Count, q.Operation);
                Assert.Null(q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.CountAsync<Model>(where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Count, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>(model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>("evt", model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>("evt", source, model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>(model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>("evt", model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>("evt", source, model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>(evt, model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>(evt, model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>(models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>("evt", models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>("evt", source, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>(models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>("evt", models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>("evt", source, models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>(evt, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>(evt, models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>(model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>("evt", model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>("evt", source, model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>(model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>("evt", model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>("evt", source, model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>(evt, model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>(evt, model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>(models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>("evt", models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>("evt", source, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>(models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>("evt", models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>("evt", source, models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>(evt, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>(evt, models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>((object)7);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>("evt", (object)7);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>("evt", source, (object)7);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>((object)7, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>("evt", (object)7, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>("evt", source, (object)7, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>(evt, (object)7);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>(evt, (object)7, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>(ids);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>("evt", ids);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>("evt", source, ids);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>(ids, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>("evt", ids, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>("evt", source, ids, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>(evt, ids);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>(evt, ids, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                _ = await repo.EventAnyAsync<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventAny, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.EventAnyAsync<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventAny, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.EventCountAsync<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventCount, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.EventCountAsync<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventCount, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.EventFirstAsync<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where, order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventFirst, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.EventFirstAsync<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where, order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventFirst, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.EventManyAsync<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where, order, 1, 2, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventMany, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.EventManyAsync<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where, order, 1, 2, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventMany, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.EventSingleAsync<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where, order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventSingle, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.EventSingleAsync<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where, order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.EventSingle, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.FirstAsync<Model>(order);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.First, q.Operation);
                Assert.Null(q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.FirstAsync<Model>(where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.First, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.FirstAsync<Model>(graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.First, q.Operation);
                Assert.Null(q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.FirstAsync<Model>(order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.First, q.Operation);
                Assert.Null(q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.FirstAsync<Model>(where, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.First, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.FirstAsync<Model>(where, order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.First, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.ManyAsync<Model>(1, 2);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Null(q.Where);
                Assert.Null(q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Null(q.Graph);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.ManyAsync<Model>(order, 1, 2);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Null(q.Where);
                Assert.Same(order, q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Null(q.Graph);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.ManyAsync<Model>(where, order, 1, 2);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Null(q.Graph);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.ManyAsync<Model>(graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Null(q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.ManyAsync<Model>(1, 2, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Null(q.Where);
                Assert.Null(q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.ManyAsync<Model>(order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Null(q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.ManyAsync<Model>(order, 1, 2, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Null(q.Where);
                Assert.Same(order, q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.ManyAsync<Model>(where, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.ManyAsync<Model>(where, order, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.ManyAsync<Model>(where, order, 1, 2, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.SingleAsync<Model>(where, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Single, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.SingleAsync<Model>(graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Single, q.Operation);
                Assert.Null(q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Null(q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Null(q.TemporalSkip);
                Assert.Null(q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.TemporalAnyAsync<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Any, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.TemporalAnyAsync<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Any, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.TemporalCountAsync<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Count, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.TemporalCountAsync<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Count, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Null(q.Graph);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.TemporalFirstAsync<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.First, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.TemporalFirstAsync<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.First, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Null(q.Order);
                Assert.Null(q.Skip);
                Assert.Null(q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.TemporalManyAsync<Model>(TemporalOrder.Oldest, dateFrom, dateTo, 5, 6, where, order, 1, 2, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Equal(dateFrom, q.TemporalDateFrom);
                Assert.Equal(dateTo, q.TemporalDateTo);
                Assert.Null(q.TemporalNumberFrom);
                Assert.Null(q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                _ = await repo.TemporalManyAsync<Model>(TemporalOrder.Oldest, 3UL, 4UL, 5, 6, where, order, 1, 2, graph);
                var q = provider.Query!;
                Assert.Equal(QueryOperation.Many, q.Operation);
                Assert.Same(where, q.Where);
                Assert.Same(order, q.Order);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                Assert.Equal(TemporalOrder.Oldest, q.TemporalOrder);
                Assert.Null(q.TemporalDateFrom);
                Assert.Null(q.TemporalDateTo);
                Assert.Equal(3UL, q.TemporalNumberFrom);
                Assert.Equal(4UL, q.TemporalNumberTo);
                Assert.Equal(5, q.TemporalSkip);
                Assert.Equal(6, q.TemporalTake);
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>(model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>("evt", model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>("evt", source, model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>(model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>("evt", model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>("evt", source, model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>(evt, model);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>(evt, model, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>(models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>("evt", models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>("evt", source, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>(models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>("evt", models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>("evt", source, models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>(evt, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>(evt, models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
        }
    }
}

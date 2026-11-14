// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
using Zerra.Repository.Linq;

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
            public LinqQuery? Query { get; private set; }
            public Persist? Persist { get; private set; }
            public void Clear()
            {
                Query = null;
                Persist = null;
            }

            private object? Result(LinqQuery query)
            {
                Query = query;
                return query.Terminal switch
                {
                    LinqTerminal.Sequence or LinqTerminal.FirstOrDefault or LinqTerminal.SingleOrDefault => Array.CreateInstance(query.ResultType, 0),
                    LinqTerminal.LongCount => 0L,
                    LinqTerminal.Any => false,
                    _ => null,
                };
            }
            object? ITransactStoreProvider.Query(LinqQuery query) => Result(query);
            Task<object?> ITransactStoreProvider.QueryAsync(LinqQuery query, CancellationToken cancellationToken) => Task.FromResult(Result(query));
            object ITransactStoreProvider.QueryStream(LinqQuery query) => throw new NotSupportedException();
            object ITransactStoreProvider.QueryStreamAsync(LinqQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
            void ITransactStoreProvider.Persist(Persist persist) => Persist = persist;
            Task ITransactStoreProvider.PersistAsync(Persist persist, CancellationToken cancellationToken)
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
                repo.CreateMany<Model>(models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.CreateMany<Model>("evt", models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.CreateMany<Model>("evt", source, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.CreateMany<Model>(models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.CreateMany<Model>("evt", models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.CreateMany<Model>("evt", source, models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.CreateMany<Model>(evt, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.CreateMany<Model>(evt, models, graph);
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
                repo.DeleteMany<Model>(models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.DeleteMany<Model>("evt", models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.DeleteMany<Model>("evt", source, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.DeleteMany<Model>(models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.DeleteMany<Model>("evt", models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.DeleteMany<Model>("evt", source, models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.DeleteMany<Model>(evt, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.DeleteMany<Model>(evt, models, graph);
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
                repo.DeleteManyByID<Model>(ids);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                repo.DeleteManyByID<Model>("evt", ids);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                repo.DeleteManyByID<Model>("evt", source, ids);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                repo.DeleteManyByID<Model>(ids, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                repo.DeleteManyByID<Model>("evt", ids, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                repo.DeleteManyByID<Model>("evt", source, ids, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                repo.DeleteManyByID<Model>(evt, ids);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                repo.DeleteManyByID<Model>(evt, ids, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.IDs!.Length);
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
                repo.UpdateMany<Model>(models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.UpdateMany<Model>("evt", models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.UpdateMany<Model>("evt", source, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.UpdateMany<Model>(models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.UpdateMany<Model>("evt", models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.UpdateMany<Model>("evt", source, models, graph);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.UpdateMany<Model>(evt, models);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                repo.UpdateMany<Model>(evt, models, graph);
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
                await repo.CreateAsync<Model>(model, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>("evt", model, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>("evt", source, model, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>(model, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>("evt", model, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>("evt", source, model, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>(evt, model, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.CreateAsync<Model>(evt, model, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.CreateManyAsync<Model>(models, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.CreateManyAsync<Model>("evt", models, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.CreateManyAsync<Model>("evt", source, models, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.CreateManyAsync<Model>(models, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.CreateManyAsync<Model>("evt", models, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.CreateManyAsync<Model>("evt", source, models, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.CreateManyAsync<Model>(evt, models, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.CreateManyAsync<Model>(evt, models, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Create, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>(model, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>("evt", model, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>("evt", source, model, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>(model, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>("evt", model, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>("evt", source, model, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>(evt, model, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.DeleteAsync<Model>(evt, model, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.DeleteManyAsync<Model>(models, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteManyAsync<Model>("evt", models, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteManyAsync<Model>("evt", source, models, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteManyAsync<Model>(models, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteManyAsync<Model>("evt", models, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteManyAsync<Model>("evt", source, models, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteManyAsync<Model>(evt, models, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteManyAsync<Model>(evt, models, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>((object)7, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>("evt", (object)7, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>("evt", source, (object)7, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>((object)7, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>("evt", (object)7, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>("evt", source, (object)7, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>(evt, (object)7, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                await repo.DeleteByIDAsync<Model>(evt, (object)7, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(7, Assert.Single(p.IDs!));
            }
            {
                provider.Clear();
                await repo.DeleteManyByIDAsync<Model>(ids, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteManyByIDAsync<Model>("evt", ids, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteManyByIDAsync<Model>("evt", source, ids, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteManyByIDAsync<Model>(ids, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteManyByIDAsync<Model>("evt", ids, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteManyByIDAsync<Model>("evt", source, ids, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteManyByIDAsync<Model>(evt, ids, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                await repo.DeleteManyByIDAsync<Model>(evt, ids, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Delete, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.IDs!.Length);
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>(model, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>("evt", model, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>("evt", source, model, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>(model, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>("evt", model, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>("evt", source, model, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>(evt, model, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.UpdateAsync<Model>(evt, model, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Same(model, Assert.Single(p.Models!));
            }
            {
                provider.Clear();
                await repo.UpdateManyAsync<Model>(models, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.UpdateManyAsync<Model>("evt", models, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.UpdateManyAsync<Model>("evt", source, models, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.UpdateManyAsync<Model>(models, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.UpdateManyAsync<Model>("evt", models, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Null(p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.UpdateManyAsync<Model>("evt", source, models, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal("evt", p.Event.Name);
                Assert.Same(source, p.Event.Source);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.UpdateManyAsync<Model>(evt, models, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Null(p.Graph);
                Assert.Equal(2, p.Models!.Length);
            }
            {
                provider.Clear();
                await repo.UpdateManyAsync<Model>(evt, models, graph, TestContext.Current.CancellationToken);
                var p = provider.Persist!;
                Assert.Equal(PersistOperation.Update, p.Operation);
                Assert.Equal(evt.ID, p.Event.ID);
                Assert.Equal(graph.Signature, p.Graph?.Signature);
                Assert.Equal(2, p.Models!.Length);
            }
        }
        //a change by condition passes its event, condition, and values, whichever overload it takes
        [Fact]
        public async Task TemporalRoots()
        {
            var (repo, provider) = Create();
            var cancellationToken = TestContext.Current.CancellationToken;
            {
                provider.Clear();
                _ = repo.TemporalQuery<Model>(TemporalOrder.Oldest, dateFrom, dateTo, graph: graph).Where(where).OrderBy(x => x.ID).Skip(1).Take(2).ToArray();
                var q = provider.Query!;
                Assert.Equal(LinqTerminal.Sequence, q.Terminal);
                Assert.Equal(typeof(Model), q.ModelType);
                Assert.False(q.ReadsEvents);
                Assert.Equal(1, q.Skip);
                Assert.Equal(2, q.Take);
                Assert.Single(q.Order!);
                Assert.Equal(graph.Signature, q.Graph?.Signature);
                var t = q.Temporal!;
                Assert.Equal(typeof(Model), t.ModelType);
                Assert.False(t.Events);
                Assert.Equal(TemporalOrder.Oldest, t.Order);
                Assert.Equal(dateFrom, t.DateFrom);
                Assert.Equal(dateTo, t.DateTo);
                Assert.Null(t.NumberFrom);
                Assert.Null(t.NumberTo);
                Assert.Null(t.Graph);
            }
            {
                provider.Clear();
                _ = await repo.TemporalQuery<Model>(numberFrom: 3, numberTo: 4).FirstOrDefaultAsync(where, cancellationToken);
                var q = provider.Query!;
                Assert.Equal(LinqTerminal.FirstOrDefault, q.Terminal);
                Assert.Equal(1, q.Take);
                var t = q.Temporal!;
                Assert.Equal(TemporalOrder.Newest, t.Order);
                Assert.Null(t.DateFrom);
                Assert.Null(t.DateTo);
                Assert.Equal(3UL, t.NumberFrom);
                Assert.Equal(4UL, t.NumberTo);
            }
            {
                provider.Clear();
                _ = repo.EventQuery<Model>(TemporalOrder.Oldest, numberFrom: 3, graph: graph).Where(x => x.Model.ID == 7 && x.EventName == "given").LongCount();
                var q = provider.Query!;
                Assert.Equal(LinqTerminal.LongCount, q.Terminal);
                Assert.Equal(typeof(EventModel<Model>), q.ModelType);
                Assert.True(q.ReadsEvents);
                Assert.Null(q.Graph);
                Assert.Equal(graph.Signature, q.ModelGraph?.Signature);
                var t = q.Temporal!;
                Assert.Equal(typeof(Model), t.ModelType);
                Assert.True(t.Events);
                Assert.Equal(3UL, t.NumberFrom);
                Assert.Equal(graph.Signature, t.Graph?.Signature);
            }
            {
                provider.Clear();
                _ = await repo.EventQuery<Model>(dateTo: dateTo).AnyAsync(x => x.Model.ID == 7, cancellationToken);
                var q = provider.Query!;
                Assert.Equal(LinqTerminal.Any, q.Terminal);
                Assert.Equal(dateTo, q.Temporal!.DateTo);
            }
            {
                provider.Clear();
                _ = repo.Query<Model>().Where(where).ToArray();
                Assert.Null(provider.Query!.Temporal);
            }
        }

        [Fact]
        public async Task ChangesByCondition()
        {
            var (repo, provider) = Create();
            var cancellationToken = TestContext.Current.CancellationToken;
            Expression<Func<Model, Model>> set = x => new Model { ID = x.ID };
            void Check(PersistOperation operation, string? name, object? eventSource, bool setsByModel, bool computes)
            {
                var p = provider.Persist!;
                Assert.Equal(operation, p.Operation);
                if (name is null)
                    Assert.False(String.IsNullOrWhiteSpace(p.Event.Name));
                else
                    Assert.Equal(name, p.Event.Name);
                Assert.Same(eventSource, p.Event.Source);
                Assert.Same(where, p.Where);
                if (setsByModel)
                {
                    Assert.Same(model, Assert.Single(p.Models!));
                    Assert.Equal(graph.Signature, p.Graph?.Signature);
                }
                if (computes)
                    Assert.Same(set, p.Set);
                provider.Clear();
            }

            _ = repo.Update(where, model, graph); Check(PersistOperation.Update, null, null, true, false);
            _ = repo.Update("evt", where, model, graph); Check(PersistOperation.Update, "evt", null, true, false);
            _ = repo.Update("evt", source, where, model, graph); Check(PersistOperation.Update, "evt", source, true, false);
            _ = repo.Update(evt, where, model, graph); Check(PersistOperation.Update, "given", source, true, false);
            _ = await repo.UpdateAsync(where, model, graph, cancellationToken); Check(PersistOperation.Update, null, null, true, false);
            _ = await repo.UpdateAsync("evt", where, model, graph, cancellationToken); Check(PersistOperation.Update, "evt", null, true, false);
            _ = await repo.UpdateAsync("evt", source, where, model, graph, cancellationToken); Check(PersistOperation.Update, "evt", source, true, false);
            _ = await repo.UpdateAsync(evt, where, model, graph, cancellationToken); Check(PersistOperation.Update, "given", source, true, false);

            _ = repo.Update(where, set); Check(PersistOperation.Update, null, null, false, true);
            _ = repo.Update("evt", where, set); Check(PersistOperation.Update, "evt", null, false, true);
            _ = repo.Update("evt", source, where, set); Check(PersistOperation.Update, "evt", source, false, true);
            _ = repo.Update(evt, where, set); Check(PersistOperation.Update, "given", source, false, true);
            _ = await repo.UpdateAsync(where, set, cancellationToken); Check(PersistOperation.Update, null, null, false, true);
            _ = await repo.UpdateAsync("evt", where, set, cancellationToken); Check(PersistOperation.Update, "evt", null, false, true);
            _ = await repo.UpdateAsync("evt", source, where, set, cancellationToken); Check(PersistOperation.Update, "evt", source, false, true);
            _ = await repo.UpdateAsync(evt, where, set, cancellationToken); Check(PersistOperation.Update, "given", source, false, true);

            _ = repo.Delete(where); Check(PersistOperation.Delete, null, null, false, false);
            _ = repo.Delete("evt", where); Check(PersistOperation.Delete, "evt", null, false, false);
            _ = repo.Delete("evt", source, where); Check(PersistOperation.Delete, "evt", source, false, false);
            _ = repo.Delete(evt, where); Check(PersistOperation.Delete, "given", source, false, false);
            _ = await repo.DeleteAsync(where, cancellationToken); Check(PersistOperation.Delete, null, null, false, false);
            _ = await repo.DeleteAsync("evt", where, cancellationToken); Check(PersistOperation.Delete, "evt", null, false, false);
            _ = await repo.DeleteAsync("evt", source, where, cancellationToken); Check(PersistOperation.Delete, "evt", source, false, false);
            _ = await repo.DeleteAsync(evt, where, cancellationToken); Check(PersistOperation.Delete, "given", source, false, false);
        }

        [Fact]
        public async Task CollectionAsSingleModel_NamesManyMethod()
        {
            var (repo, _) = Create();
            var list = new List<Model>(models);

            Assert.EndsWith("Use CreateMany.", Assert.Throws<Exception>(() => repo.Create(list)).Message);
            Assert.EndsWith("Use UpdateMany.", Assert.Throws<Exception>(() => repo.Update(list)).Message);
            Assert.EndsWith("Use DeleteMany.", Assert.Throws<Exception>(() => repo.Delete(list)).Message);
            Assert.EndsWith("Use CreateMany.", (await Assert.ThrowsAsync<Exception>(() => repo.CreateAsync(list, TestContext.Current.CancellationToken))).Message);
            Assert.EndsWith("Use UpdateMany.", (await Assert.ThrowsAsync<Exception>(() => repo.UpdateAsync(list, TestContext.Current.CancellationToken))).Message);
            Assert.EndsWith("Use DeleteMany.", (await Assert.ThrowsAsync<Exception>(() => repo.DeleteAsync(list, TestContext.Current.CancellationToken))).Message);
        }
    }
}

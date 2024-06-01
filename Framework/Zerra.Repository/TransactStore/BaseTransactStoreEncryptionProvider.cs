// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Zerra.Collections;
using Zerra.Encryption;
using Zerra.Map;
using Zerra.Providers;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract class BaseTransactStoreEncryptionProvider<TNextProviderInterface, TModel> : BaseTransactStoreLayerProvider<TNextProviderInterface, TModel>, IEncryptionProvider
        where TNextProviderInterface : ITransactStoreProvider<TModel>
        where TModel : class, new()
    {
        private const string encryptionPrefix = "<encrypted>";

        public virtual bool Enabled { get { return true; } }
        public virtual Graph<TModel>? Properties { get { return null; } }
        public abstract SymmetricKey EncryptionKey { get; }

        private const SymmetricAlgorithmType encryptionAlgorithm = SymmetricAlgorithmType.AESwithShift;

        private static Expression<Func<TModel, bool>>? EncryptWhere(Expression<Func<TModel, bool>>? expression)
        {
            return expression;
        }
        private static QueryOrder<TModel>? EncryptOrder(QueryOrder<TModel>? order)
        {
            return order;
        }

        public TModel DecryptModel(TModel model, Graph<TModel>? graph, bool newCopy)
        {
            if (!this.Enabled)
                return model;

            var properties = GetEncryptableProperties(typeof(TModel), this.Properties);
            if (properties.Length == 0)
                return model;

            graph = graph == null ? null : new Graph<TModel>(graph);

            if (newCopy)
                model = Mapper.Map<TModel, TModel>(model, graph);

            foreach (var property in properties)
            {
                if (graph == null || graph.HasLocalProperty(property.Name))
                {
                    if (property.TypeDetail.CoreType == CoreType.String)
                    {
                        var encrypted = (string?)property.GetterBoxed(model);
                        if (encrypted != null)
                        {
                            try
                            {
                                if (encrypted.Length > encryptionPrefix.Length && encrypted.Substring(0, encryptionPrefix.Length) == encryptionPrefix)
                                {
                                    encrypted = encrypted.Substring(encryptionPrefix.Length, encrypted.Length - encryptionPrefix.Length);
                                    var plain = SymmetricEncryptor.Decrypt(encryptionAlgorithm, EncryptionKey, encrypted);
                                    property.SetterBoxed(model, plain);
                                }
                            }
                            catch { }
                        }
                    }
                    else if (property.Type == typeof(byte[]))
                    {
                        var encrypted = (byte[]?)property.GetterBoxed(model);
                        if (encrypted != null)
                        {
                            try
                            {
                                var plain = SymmetricEncryptor.Decrypt(encryptionAlgorithm, EncryptionKey, encrypted);
                                property.SetterBoxed(model, plain);
                            }
                            catch { }
                        }
                    }
                }
            }

            return model;
        }
        public ICollection<TModel> DecryptModels(ICollection<TModel> models, Graph<TModel>? graph, bool newCopy)
        {
            if (!this.Enabled)
                return models;

            var properties = GetEncryptableProperties(typeof(TModel), this.Properties);
            if (properties.Length == 0)
                return models;

            graph = graph == null ? null : new Graph<TModel>(graph);

            if (newCopy)
                models = Mapper.Map<ICollection<TModel>, TModel[]>(models, graph);

            foreach (var model in models)
            {
                foreach (var property in properties)
                {
                    if (graph == null || graph.HasLocalProperty(property.Name))
                    {
                        if (property.TypeDetail.CoreType == CoreType.String)
                        {
                            var encrypted = (string?)property.GetterBoxed(model);
                            if (encrypted != null)
                            {
                                try
                                {
                                    if (encrypted.Length > encryptionPrefix.Length && encrypted.Substring(0, encryptionPrefix.Length) == encryptionPrefix)
                                    {
                                        encrypted = encrypted.Substring(encryptionPrefix.Length, encrypted.Length - encryptionPrefix.Length);
                                        var plain = SymmetricEncryptor.Decrypt(encryptionAlgorithm, EncryptionKey, encrypted);
                                        property.SetterBoxed(model, plain);
                                    }
                                }
                                catch { }
                            }
                        }
                        else if (property.Type == typeof(byte[]))
                        {
                            var encrypted = (byte[]?)property.GetterBoxed(model);
                            if (encrypted != null)
                            {
                                try
                                {
                                    var plain = SymmetricEncryptor.Decrypt(encryptionAlgorithm, EncryptionKey, encrypted);
                                    property.SetterBoxed(model, plain);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }

            return models;
        }

        public override sealed void OnQueryIncludingBase(Graph<TModel>? graph)
        {
            ProviderRelation?.OnQueryIncludingBase(graph);
        }
        public override sealed ICollection<TModel> OnGetIncludingBase(ICollection<TModel> models, Graph<TModel>? graph)
        {
            var returnModels1 = DecryptModels(models, graph, false);
            if (ProviderRelation == null)
                return returnModels1;
            var returnModels2 = ProviderRelation.OnGetIncludingBase(returnModels1, graph);
            return returnModels2;
        }
        public override sealed async Task<ICollection<TModel>> OnGetIncludingBaseAsync(ICollection<TModel> models, Graph<TModel>? graph)
        {
            var returnModels1 = DecryptModels(models, graph, false);
            if (ProviderRelation == null)
                return returnModels1;
            var returnModels2 = await ProviderRelation.OnGetIncludingBaseAsync(returnModels1, graph);
            return returnModels2;
        }

        public override sealed object Many(Query<TModel> query)
        {
            var whereCompressed = EncryptWhere(query.Where);
            var orderCompressed = EncryptOrder(query.Order);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var encryptedModels = (ICollection<TModel>)NextProvider.Query(appenedQuery)!;

            if (encryptedModels.Count == 0)
                return encryptedModels;

            var models = DecryptModels(encryptedModels, query.Graph, true);
            return models;
        }
        public override sealed object? First(Query<TModel> query)
        {
            var whereCompressed = EncryptWhere(query.Where);
            var orderCompressed = EncryptOrder(query.Order);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var encryptedModels = (TModel?)(NextProvider.Query(appenedQuery));

            if (encryptedModels == null)
                return null;

            var model = DecryptModel(encryptedModels, query.Graph, true);
            return model;
        }
        public override sealed object? Single(Query<TModel> query)
        {
            var whereCompressed = EncryptWhere(query.Where);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;

            var encryptedModels = (TModel?)(NextProvider.Query(appenedQuery));

            if (encryptedModels == null)
                return null;

            var model = DecryptModel(encryptedModels, query.Graph, true);
            return model;
        }
        public override sealed object Count(Query<TModel> query)
        {
            var whereCompressed = EncryptWhere(query.Where);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;

            var count = NextProvider.Query(appenedQuery)!;
            return count;
        }
        public override sealed object Any(Query<TModel> query)
        {
            var whereCompressed = EncryptWhere(query.Where);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;

            var any = NextProvider.Query(appenedQuery)!;
            return any;
        }
        public override sealed object EventMany(Query<TModel> query)
        {
            var whereCompressed = EncryptWhere(query.Where);
            var orderCompressed = EncryptOrder(query.Order);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var eventModels = (ICollection<EventModel<TModel>>)NextProvider.Query(appenedQuery)!;

            if (eventModels.Count == 0)
                return eventModels;

            var encryptedModels = eventModels.Select(x => x.Model).ToArray();
            var models = DecryptModels(encryptedModels, query.Graph, true);
            var i = 0;
            foreach (var eventModel in eventModels)
            {
                eventModel.Model = encryptedModels[i];
                i++;
            }

            return eventModels;
        }

        public override sealed async Task<object?> ManyAsync(Query<TModel> query)
        {
            var whereCompressed = EncryptWhere(query.Where);
            var orderCompressed = EncryptOrder(query.Order);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var encryptedModels = (ICollection<TModel>)(await NextProvider.QueryAsync(appenedQuery))!;

            if (encryptedModels.Count == 0)
                return encryptedModels;

            var models = DecryptModels(encryptedModels, query.Graph, true);
            return models;
        }
        public override sealed async Task<object?> FirstAsync(Query<TModel> query)
        {
            var whereCompressed = EncryptWhere(query.Where);
            var orderCompressed = EncryptOrder(query.Order);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var encryptedModels = (TModel?)(await NextProvider.QueryAsync(appenedQuery));

            if (encryptedModels == null)
                return null;

            var model = DecryptModels(new TModel[] { encryptedModels }, query.Graph, true).FirstOrDefault();
            return model;
        }
        public override sealed async Task<object?> SingleAsync(Query<TModel> query)
        {
            var whereCompressed = EncryptWhere(query.Where);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;

            var encryptedModels = (TModel?)(await NextProvider.QueryAsync(appenedQuery));

            if (encryptedModels == null)
                return null;

            var model = DecryptModels(new TModel[] { encryptedModels }, query.Graph, true).FirstOrDefault();
            return model;
        }
        public override sealed Task<object?> CountAsync(Query<TModel> query)
        {
            var whereCompressed = EncryptWhere(query.Where);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;

            var count = NextProvider.QueryAsync(appenedQuery);
            return count;
        }
        public override sealed Task<object?> AnyAsync(Query<TModel> query)
        {
            var whereCompressed = EncryptWhere(query.Where);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;

            var any = NextProvider.QueryAsync(appenedQuery);
            return any;
        }
        public override sealed async Task<object?> EventManyAsync(Query<TModel> query)
        {
            var whereCompressed = EncryptWhere(query.Where);
            var orderCompressed = EncryptOrder(query.Order);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var eventModels = (ICollection<EventModel<TModel>>)(await NextProvider.QueryAsync(appenedQuery))!;

            if (eventModels.Count == 0)
                return eventModels;

            var encryptedModels = eventModels.Select(x => x.Model).ToArray();
            var models = DecryptModels(encryptedModels, query.Graph, true);
            var i = 0;
            foreach (var eventModel in eventModels)
            {
                eventModel.Model = encryptedModels[i];
                i++;
            }

            return eventModels;
        }

        public TModel[] EncryptModels(TModel[] models, Graph<TModel>? graph, bool newCopy)
        {
            if (!this.Enabled)
                return models;

            var properties = GetEncryptableProperties(typeof(TModel), this.Properties);
            if (properties.Length == 0)
                return models;

            if (graph != null)
            {
                graph = new Graph<TModel>(graph);

                //add identites for copying
                graph.AddProperties(ModelAnalyzer.GetIdentityPropertyNames(typeof(TModel)));
            }

            if (newCopy)
                models = Mapper.Map<TModel[], TModel[]>(models, graph);

            foreach (var model in models)
            {
                foreach (var property in properties)
                {
                    if (graph == null || graph.HasLocalProperty(property.Name))
                    {
                        if (property.TypeDetail.CoreType == CoreType.String)
                        {
                            var plain = (string?)property.GetterBoxed(model);
                            if (plain != null)
                            {
                                if (plain.Length <= encryptionPrefix.Length || plain.Substring(0, encryptionPrefix.Length) != encryptionPrefix)
                                {
                                    plain = encryptionPrefix + plain;
                                    var encrypted = SymmetricEncryptor.Encrypt(encryptionAlgorithm, EncryptionKey, plain);
                                    property.SetterBoxed(model, encrypted);
                                }
                            }
                        }
                        else if (property.Type == typeof(byte[]))
                        {
                            var plain = (byte[]?)property.GetterBoxed(model);
                            if (plain != null)
                            {
                                var encrypted = SymmetricEncryptor.Encrypt(encryptionAlgorithm, EncryptionKey, plain);
                                property.SetterBoxed(model, encrypted);
                            }
                        }
                    }
                }
            }
            return models;
        }

        public override sealed void Create(Persist<TModel> persist)
        {
            if (persist.Models == null || persist.Models.Length == 0)
                return;

            var encryptedModels = EncryptModels(persist.Models, persist.Graph, true);
            NextProvider.Persist(new Create<TModel>(encryptedModels, persist.Graph));

            for (var i = 0; i < persist.Models.Length; i++)
            {
                var identity = ModelAnalyzer.GetIdentity(encryptedModels[i]);
                ModelAnalyzer.SetIdentity(persist.Models[i], identity);
            }
        }
        public override sealed void Update(Persist<TModel> persist)
        {
            if (persist.Models == null || persist.Models.Length == 0)
                return;

            ICollection<TModel> encryptedModels = EncryptModels(persist.Models, persist.Graph, true);
            NextProvider.Persist(new Update<TModel>(persist.Event, encryptedModels, persist.Graph));
        }
        public override sealed void Delete(Persist<TModel> persist)
        {
            NextProvider.Persist(persist);
        }

        public override sealed async Task CreateAsync(Persist<TModel> persist)
        {
            if (persist.Models == null || persist.Models.Length == 0)
                return;

            var encryptedModels = EncryptModels(persist.Models, persist.Graph, true);
            await NextProvider.PersistAsync(new Create<TModel>(encryptedModels, persist.Graph));

            for (var i = 0; i < persist.Models.Length; i++)
            {
                var identity = ModelAnalyzer.GetIdentity(encryptedModels[i]);
                ModelAnalyzer.SetIdentity(persist.Models[i], identity);
            }
        }
        public override sealed Task UpdateAsync(Persist<TModel> persist)
        {
            if (persist.Models == null || persist.Models.Length == 0)
                return Task.CompletedTask;

            ICollection<TModel> encryptedModels = EncryptModels(persist.Models, persist.Graph, true);
            return NextProvider.PersistAsync(new Update<TModel>(persist.Event, encryptedModels, persist.Graph));
        }
        public override sealed Task DeleteAsync(Persist<TModel> persist)
        {
            return NextProvider.PersistAsync(persist);
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, MemberDetail[]> encryptableProperties = new();
        public static MemberDetail[] GetEncryptableProperties(Type type, Graph? graph)
        {
            var key = new TypeKey(graph?.Signature, type);
            var props = encryptableProperties.GetOrAdd(key, (_) =>
            {
                var typeDetails = TypeAnalyzer.GetTypeDetail(type);
                var propertyDetails = typeDetails.MemberDetails.Where(x => x.Type == typeof(string) || x.Type == typeof(byte[])).ToArray();
                if (graph != null)
                {
                    propertyDetails = propertyDetails.Where(x => graph.HasLocalProperty(x.Name)).ToArray();
                }
                return propertyDetails;
            });
            return props;
        }
    }
}

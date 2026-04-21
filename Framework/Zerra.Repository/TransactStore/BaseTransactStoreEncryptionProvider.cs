// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Zerra.Collections;
using Zerra.Encryption;
using Zerra.Map;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract class BaseTransactStoreEncryptionProvider<TNextProviderInterface, TModel> : BaseTransactStoreLayerProvider<TNextProviderInterface, TModel>
        where TNextProviderInterface : ITransactStoreProvider<TModel>
        where TModel : class, new()
    {
        private const string encryptionPrefix = "<encrypted>";

        public virtual bool Enabled { get { return true; } }
        public virtual Graph<TModel>? Properties { get { return null; } }
        public abstract SymmetricKey EncryptionKey { get; }

        private const SymmetricAlgorithmType encryptionAlgorithm = SymmetricAlgorithmType.AESwithShift;

        public BaseTransactStoreEncryptionProvider(TNextProviderInterface nextProvider)
            : base(nextProvider)
        {
        }

        private static LambdaExpression? EncryptWhere(LambdaExpression? expression)
        {
            return expression;
        }
        private static QueryOrder? EncryptOrder(QueryOrder? order)
        {
            return order;
        }

        public TModel DecryptModel(TModel model, Graph? graph, bool newCopy)
        {
            if (!this.Enabled)
                return model;

            var properties = GetEncryptableProperties(typeof(TModel), this.Properties);
            if (properties.Length == 0)
                return model;

            graph = graph is null ? null : new Graph<TModel>(graph);

            if (newCopy)
                model = model.Copy();

            foreach (var property in properties)
            {
                if (graph is null || graph.HasMember(property.Name))
                {
                    if (property.TypeDetail.CoreType == CoreType.String)
                    {
                        var encrypted = (string?)property.GetterBoxed(model);
                        if (encrypted is not null)
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
                        if (encrypted is not null)
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
        public IEnumerable DecryptModels(IEnumerable models, Graph? graph, bool newCopy)
        {
            if (!this.Enabled)
                return models;

            var properties = GetEncryptableProperties(typeof(TModel), this.Properties);
            if (properties.Length == 0)
                return models;

            graph = graph is null ? null : new Graph<TModel>(graph);

            if (newCopy)
            {
                var copies = new List<TModel>();
                foreach (TModel model in models)
                    copies.Add(model.Copy());
                models = copies;
            }

            foreach (var model in models)
            {
                foreach (var property in properties)
                {
                    if (graph is null || graph.HasMember(property.Name))
                    {
                        if (property.TypeDetail.CoreType == CoreType.String)
                        {
                            var encrypted = (string?)property.GetterBoxed(model);
                            if (encrypted is not null)
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
                            if (encrypted is not null)
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

        public override sealed void OnQueryIncludingBase(Graph? graph)
        {
            ProviderRelation?.OnQueryIncludingBase(graph);
        }
        public override sealed IEnumerable OnGetIncludingBase(IEnumerable models, Graph? graph)
        {
            var returnModels1 = DecryptModels(models, graph, false);
            if (ProviderRelation is null)
                return returnModels1;
            var returnModels2 = ProviderRelation.OnGetIncludingBase(returnModels1, graph);
            return returnModels2;
        }
        public override sealed async Task<IEnumerable> OnGetIncludingBaseAsync(IEnumerable models, Graph? graph)
        {
            var returnModels1 = DecryptModels(models, graph, false);
            if (ProviderRelation is null)
                return returnModels1;
            var returnModels2 = await ProviderRelation.OnGetIncludingBaseAsync(returnModels1, graph);
            return returnModels2;
        }

        public override sealed object Many(Query query)
        {
            var whereCompressed = EncryptWhere(query.Where);
            var orderCompressed = EncryptOrder(query.Order);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var encryptedModels = (IReadOnlyCollection<TModel>)NextProvider.Query(appenedQuery)!;

            if (encryptedModels.Count == 0)
                return encryptedModels;

            var models = DecryptModels(encryptedModels, query.Graph, true);
            return models;
        }
        public override sealed object? First(Query query)
        {
            var whereCompressed = EncryptWhere(query.Where);
            var orderCompressed = EncryptOrder(query.Order);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var encryptedModels = (TModel?)(NextProvider.Query(appenedQuery));

            if (encryptedModels is null)
                return null;

            var model = DecryptModel(encryptedModels, query.Graph, true);
            return model;
        }
        public override sealed object? Single(Query query)
        {
            var whereCompressed = EncryptWhere(query.Where);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;

            var encryptedModels = (TModel?)(NextProvider.Query(appenedQuery));

            if (encryptedModels is null)
                return null;

            var model = DecryptModel(encryptedModels, query.Graph, true);
            return model;
        }
        public override sealed object Count(Query query)
        {
            var whereCompressed = EncryptWhere(query.Where);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;

            var count = NextProvider.Query(appenedQuery)!;
            return count;
        }
        public override sealed object Any(Query query)
        {
            var whereCompressed = EncryptWhere(query.Where);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;

            var any = NextProvider.Query(appenedQuery)!;
            return any;
        }
        public override sealed object EventMany(Query query)
        {
            var whereCompressed = EncryptWhere(query.Where);
            var orderCompressed = EncryptOrder(query.Order);

            var appenedQuery = new Query(query);
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

        public override sealed async Task<object?> ManyAsync(Query query)
        {
            var whereCompressed = EncryptWhere(query.Where);
            var orderCompressed = EncryptOrder(query.Order);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var encryptedModels = (IReadOnlyCollection<TModel>)(await NextProvider.QueryAsync(appenedQuery))!;

            if (encryptedModels.Count == 0)
                return encryptedModels;

            var models = DecryptModels(encryptedModels, query.Graph, true);
            return models;
        }
        public override sealed async Task<object?> FirstAsync(Query query)
        {
            var whereCompressed = EncryptWhere(query.Where);
            var orderCompressed = EncryptOrder(query.Order);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var encryptedModels = (TModel?)(await NextProvider.QueryAsync(appenedQuery));

            if (encryptedModels is null)
                return null;

            var model = DecryptModels(new object[] { encryptedModels }, query.Graph, true).Cast<TModel>().FirstOrDefault();
            return model;
        }
        public override sealed async Task<object?> SingleAsync(Query query)
        {
            var whereCompressed = EncryptWhere(query.Where);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;

            var encryptedModels = (TModel?)(await NextProvider.QueryAsync(appenedQuery));

            if (encryptedModels is null)
                return null;

            var model = DecryptModels(new TModel[] { encryptedModels }, query.Graph, true).Cast<TModel>().FirstOrDefault();
            return model;
        }
        public override sealed Task<object?> CountAsync(Query query)
        {
            var whereCompressed = EncryptWhere(query.Where);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;

            var count = NextProvider.QueryAsync(appenedQuery);
            return count;
        }
        public override sealed Task<object?> AnyAsync(Query query)
        {
            var whereCompressed = EncryptWhere(query.Where);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;

            var any = NextProvider.QueryAsync(appenedQuery);
            return any;
        }
        public override sealed async Task<object?> EventManyAsync(Query query)
        {
            var whereCompressed = EncryptWhere(query.Where);
            var orderCompressed = EncryptOrder(query.Order);

            var appenedQuery = new Query(query);
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

        public object[] EncryptModels(object[] models, Graph? graph, bool newCopy)
        {
            if (!this.Enabled)
                return models;

            var properties = GetEncryptableProperties(typeof(TModel), this.Properties);
            if (properties.Length == 0)
                return models;

            if (graph is not null)
            {
                graph = new Graph<TModel>(graph);

                //add identites for copying
                graph.AddMembers(ModelAnalyzer.GetIdentityPropertyNames(typeof(TModel)));
            }

            if (newCopy)
            {
                var copy = new object[models.Length];
                for (var i = 0; i < models.Length; i++)
                    copy[i] = models[i].CopyObject();
            }

            foreach (var model in models)
            {
                foreach (var property in properties)
                {
                    if (graph is null || graph.HasMember(property.Name))
                    {
                        if (property.TypeDetail.CoreType == CoreType.String)
                        {
                            var plain = (string?)property.GetterBoxed(model);
                            if (plain is not null)
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
                            if (plain is not null)
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

        public override sealed void Create(Persist persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var encryptedModels = EncryptModels(persist.Models, persist.Graph, true);
            NextProvider.Persist(new Create(encryptedModels, persist.Graph));

            for (var i = 0; i < persist.Models.Length; i++)
            {
                var identity = ModelAnalyzer.GetIdentity(modelType, encryptedModels[i]);
                ModelAnalyzer.SetIdentity(modelType, persist.Models[i], identity);
            }
        }
        public override sealed void Update(Persist persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var encryptedModels = EncryptModels(persist.Models, persist.Graph, true);
            NextProvider.Persist(new Update(persist.Event, encryptedModels, persist.Graph));
        }
        public override sealed void Delete(Persist persist)
        {
            NextProvider.Persist(persist);
        }

        public override sealed async Task CreateAsync(Persist persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var encryptedModels = EncryptModels(persist.Models, persist.Graph, true);
            await NextProvider.PersistAsync(new Create(encryptedModels, persist.Graph));

            for (var i = 0; i < persist.Models.Length; i++)
            {
                var identity = ModelAnalyzer.GetIdentity(modelType, encryptedModels[i]);
                ModelAnalyzer.SetIdentity(modelType, persist.Models[i], identity);
            }
        }
        public override sealed Task UpdateAsync(Persist persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return Task.CompletedTask;

            var encryptedModels = EncryptModels(persist.Models, persist.Graph, true);
            return NextProvider.PersistAsync(new Update(persist.Event, encryptedModels, persist.Graph));
        }
        public override sealed Task DeleteAsync(Persist persist)
        {
            return NextProvider.PersistAsync(persist);
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, MemberDetail[]> encryptableProperties = new();
        public static MemberDetail[] GetEncryptableProperties(Type type, Graph? graph)
        {
            var key = new TypeKey(graph?.Signature, type);
            var props = encryptableProperties.GetOrAdd(key, type, graph, static (type, graph) =>
            {
                var typeDetails = TypeAnalyzer.GetTypeDetail(type);
                var propertyDetails = typeDetails.Members.Where(x => x.Type == typeof(string) || x.Type == typeof(byte[])).ToArray();
                if (graph is not null)
                {
                    propertyDetails = propertyDetails.Where(x => graph.HasMember(x.Name)).ToArray();
                }
                return propertyDetails;
            });
            return props;
        }
    }
}

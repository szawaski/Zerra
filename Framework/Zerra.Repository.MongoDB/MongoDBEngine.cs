// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections;
using System.Data;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Zerra.Repository.Linq;
using Zerra.Repository.Reflection;
using Zerra.Repository.Transactions;

namespace Zerra.Repository.MongoDB
{
    /// <summary>
    /// A MongoDB data store engine. Each model is a collection named by its entity, each model a document with its members as fields:
    /// a single identity is the document's _id, an enum is stored as its name, a TimeSpan or TimeOnly as ticks, and a date in UTC to the millisecond.
    /// Queries run as aggregation pipelines; a relation in the same database is read with a $lookup or a query of its own.
    /// </summary>
    public sealed class MongoDBEngine : ITransactStoreEngine
    {
        private const string IdentitiesCollection = "_identities";
        private static readonly AggregateOptions aggregateOptions = new() { AllowDiskUse = true };
        private static readonly FindOneAndUpdateOptions<BsonDocument> identityOptions = new() { IsUpsert = true, ReturnDocument = ReturnDocument.After };

        private readonly string connectionString;
        private readonly MongoClient client;
        private readonly IMongoDatabase database;
        //transactions need a replica set or a sharded cluster, asked of the server once
        private int supportsTransactions = -1;

        /// <summary>
        /// Initializes a new instance of <see cref="MongoDBEngine"/>.
        /// </summary>
        /// <param name="connectionString">The MongoDB connection string, naming the database, such as mongodb://localhost:27017/store.</param>
        public MongoDBEngine(string connectionString)
        {
            if (String.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));
            var url = MongoUrl.Create(connectionString);
            if (String.IsNullOrWhiteSpace(url.DatabaseName))
                throw new ArgumentException("The connection string names no database, such as mongodb://localhost:27017/store", nameof(connectionString));
            this.connectionString = connectionString;
            MongoDBSerializers.Ensure();
            this.client = new MongoClient(url);
            this.database = client.GetDatabase(url.DatabaseName);
        }

        /// <summary>
        /// Gets the connection string this engine was created with.
        /// </summary>
        /// <returns>The connection string.</returns>
        public string GetConnectionString() => connectionString;

        internal IMongoDatabase Database => database;

        /// <inheritdoc />
        public bool SupportsTransactions
        {
            get
            {
                if (supportsTransactions < 0)
                {
                    var hello = database.RunCommand<BsonDocument>(new BsonDocument("hello", 1));
                    supportsTransactions = hello.Contains("setName") || (hello.TryGetValue("msg", out var msg) && msg.IsString && msg.AsString == "isdbgrid") ? 1 : 0;
                }
                return supportsTransactions == 1;
            }
        }

        //the session of an operation, which is the transaction's when there is one
        private readonly struct EngineSession : IDisposable
        {
            public readonly IClientSessionHandle? Session;
            private readonly RepoTransactionPart? part;
            public EngineSession(IClientSessionHandle? session, RepoTransactionPart? part)
            {
                this.Session = session;
                this.part = part;
            }
            public void Dispose() => part?.Exit();
        }
        private EngineSession OpenSession()
        {
            var transaction = RepoTransaction.Current;
            if (transaction is null)
                return default;
            var part = transaction.Enter(connectionString, nameof(MongoDBEngine), BeginTransactionPart);
            return new EngineSession(part.Session, part);
        }
        private async ValueTask<EngineSession> OpenSessionAsync(CancellationToken cancellationToken)
        {
            var transaction = RepoTransaction.Current;
            if (transaction is null)
                return default;
            var part = await transaction.EnterAsync(connectionString, nameof(MongoDBEngine), BeginTransactionPartAsync, cancellationToken);
            return new EngineSession(part.Session, part);
        }
        private MongoDBTransactionPart BeginTransactionPart(IsolationLevel isolationLevel)
        {
            var session = client.StartSession();
            try
            {
                session.StartTransaction();
                return new MongoDBTransactionPart(session);
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }
        private async Task<MongoDBTransactionPart> BeginTransactionPartAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
        {
            var session = await client.StartSessionAsync(cancellationToken: cancellationToken);
            try
            {
                session.StartTransaction();
                return new MongoDBTransactionPart(session);
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }

        private IMongoCollection<BsonDocument> Collection(string name) => database.GetCollection<BsonDocument>(name);

        private List<BsonDocument> Aggregate(IClientSessionHandle? session, string collection, List<BsonDocument> stages)
        {
            var pipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(stages);
            using (var cursor = session is null ? Collection(collection).Aggregate(pipeline, aggregateOptions) : Collection(collection).Aggregate(session, pipeline, aggregateOptions))
                return cursor.ToList();
        }
        private async Task<List<BsonDocument>> AggregateAsync(IClientSessionHandle? session, string collection, List<BsonDocument> stages, CancellationToken cancellationToken)
        {
            var pipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(stages);
            using (var cursor = session is null ? await Collection(collection).AggregateAsync(pipeline, aggregateOptions, cancellationToken) : await Collection(collection).AggregateAsync(session, pipeline, aggregateOptions, cancellationToken))
                return await cursor.ToListAsync(cancellationToken);
        }
        //the rows of a statement's columns, read from the reply as their types
        private List<object?[]> AggregateRows(IClientSessionHandle? session, MongoDBStatement statement, List<BsonDocument> stages)
        {
            var pipeline = PipelineDefinition<BsonDocument, object?[]>.Create(stages, new MongoDBRowSerializer(statement.Plan));
            using (var cursor = session is null ? Collection(statement.Collection).Aggregate(pipeline, aggregateOptions) : Collection(statement.Collection).Aggregate(session, pipeline, aggregateOptions))
                return cursor.ToList();
        }
        private async Task<List<object?[]>> AggregateRowsAsync(IClientSessionHandle? session, MongoDBStatement statement, List<BsonDocument> stages, CancellationToken cancellationToken)
        {
            var pipeline = PipelineDefinition<BsonDocument, object?[]>.Create(stages, new MongoDBRowSerializer(statement.Plan));
            using (var cursor = session is null ? await Collection(statement.Collection).AggregateAsync(pipeline, aggregateOptions, cancellationToken) : await Collection(statement.Collection).AggregateAsync(session, pipeline, aggregateOptions, cancellationToken))
                return await cursor.ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public object? ExecuteLinq<TModel, TResult>(LinqQuery query, ModelDetail modelDetail) where TModel : class, new()
        {
            var statement = LinqMongoDBConverter.ConvertLinq(query, modelDetail);
            using (var session = OpenSession())
            {
                if (query.ReadsElements)
                {
                    var plan = statement.Plan;
                    var rows = AggregateRows(session.Session, statement, statement.Stages);
                    var pieces = ReadPieces(session.Session, statement, rows);
                    var results = new List<TResult>(rows.Count);
                    foreach (var row in rows)
                        results.Add((TResult)BuildElement(plan, row, pieces)!);
                    return results;
                }
                var documents = Aggregate(session.Session, statement.Collection, statement.Stages);
                switch (query.Terminal)
                {
                    case LinqTerminal.Any:
                        return documents.Count > 0;
                    case LinqTerminal.Count:
                    case LinqTerminal.LongCount:
                        return LinqMongoDBConverter.FromBson(documents.Count == 0 ? 0 : documents[0]["n"], typeof(TResult));
                    case LinqTerminal.Sum:
                    case LinqTerminal.Min:
                    case LinqTerminal.Max:
                    case LinqTerminal.Average:
                        //the sum of nothing is 0, the others of nothing are null
                        if (documents.Count == 0)
                            return query.Terminal == LinqTerminal.Sum ? LinqMongoDBConverter.FromBson(0, typeof(TResult)) : null;
                        return LinqMongoDBConverter.FromBson(documents[0]["v"], typeof(TResult));
                    default:
                        throw new NotSupportedException($"{query.Terminal} isn't supported by MongoDB");
                }
            }
        }

        /// <inheritdoc />
        public async Task<object?> ExecuteLinqAsync<TModel, TResult>(LinqQuery query, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var statement = LinqMongoDBConverter.ConvertLinq(query, modelDetail);
            using (var session = await OpenSessionAsync(cancellationToken))
            {
                if (query.ReadsElements)
                {
                    var plan = statement.Plan;
                    var rows = await AggregateRowsAsync(session.Session, statement, statement.Stages, cancellationToken);
                    var pieces = await ReadPiecesAsync(session.Session, statement, rows, cancellationToken);
                    var results = new List<TResult>(rows.Count);
                    foreach (var row in rows)
                        results.Add((TResult)BuildElement(plan, row, pieces)!);
                    return results;
                }
                var documents = await AggregateAsync(session.Session, statement.Collection, statement.Stages, cancellationToken);
                switch (query.Terminal)
                {
                    case LinqTerminal.Any:
                        return documents.Count > 0;
                    case LinqTerminal.Count:
                    case LinqTerminal.LongCount:
                        return LinqMongoDBConverter.FromBson(documents.Count == 0 ? 0 : documents[0]["n"], typeof(TResult));
                    case LinqTerminal.Sum:
                    case LinqTerminal.Min:
                    case LinqTerminal.Max:
                    case LinqTerminal.Average:
                        if (documents.Count == 0)
                            return query.Terminal == LinqTerminal.Sum ? LinqMongoDBConverter.FromBson(0, typeof(TResult)) : null;
                        return LinqMongoDBConverter.FromBson(documents[0]["v"], typeof(TResult));
                    default:
                        throw new NotSupportedException($"{query.Terminal} isn't supported by MongoDB");
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<TResult> ExecuteLinqStream<TModel, TResult>(LinqQuery query, ModelDetail modelDetail) where TModel : class, new()
        {
            var statement = LinqMongoDBConverter.ConvertLinq(query, modelDetail);
            //relations are read for all the rows at once, and a transaction's session holds one operation at a time
            if (statement.Pieces is not null || RepoTransaction.Current is not null)
                return (IEnumerable<TResult>)ExecuteLinq<TModel, TResult>(query, modelDetail)!;
            return Stream<TResult>(statement);
        }
        private IEnumerable<TResult> Stream<TResult>(MongoDBStatement statement)
        {
            var pipeline = PipelineDefinition<BsonDocument, object?[]>.Create(statement.Stages, new MongoDBRowSerializer(statement.Plan));
            using (var cursor = Collection(statement.Collection).Aggregate(pipeline, aggregateOptions))
            {
                while (cursor.MoveNext())
                {
                    foreach (var row in cursor.Current)
                        yield return (TResult)BuildElement(statement.Plan, row, null)!;
                }
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TResult> ExecuteLinqStreamAsync<TModel, TResult>(LinqQuery query, ModelDetail modelDetail, [EnumeratorCancellation] CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var statement = LinqMongoDBConverter.ConvertLinq(query, modelDetail);
            if (statement.Pieces is not null || RepoTransaction.Current is not null)
            {
                foreach (var element in (List<TResult>)(await ExecuteLinqAsync<TModel, TResult>(query, modelDetail, cancellationToken))!)
                    yield return element;
                yield break;
            }
            var pipeline = PipelineDefinition<BsonDocument, object?[]>.Create(statement.Stages, new MongoDBRowSerializer(statement.Plan));
            using (var cursor = await Collection(statement.Collection).AggregateAsync(pipeline, aggregateOptions, cancellationToken))
            {
                while (await cursor.MoveNextAsync(cancellationToken))
                {
                    foreach (var row in cursor.Current)
                        yield return (TResult)BuildElement(statement.Plan, row, null)!;
                }
            }
        }

        private static object? BuildElement(LinqPlan plan, object?[] row, LinqPieceElements[]? pieces)
        {
            //a related model read in the row is built into its slot, which the shape copies
            foreach (var relation in plan.Relations)
            {
                if (relation.JoinedKey is null)
                    continue;
                if (row[relation.JoinedKey.Index] is null)
                {
                    row[relation.Column!.Index] = null;
                    continue;
                }
                var model = relation.Model!.Creator();
                for (var i = 0; i < relation.ModelMembers!.Length; i++)
                {
                    var value = row[relation.ModelColumns![i].Index];
                    if (value is not null)
                        relation.ModelMembers[i].SetterBoxed(model, value);
                }
                row[relation.Column!.Index] = model;
            }
            return plan.Shape!.Build(row, pieces);
        }

        //the rows of each relation for all the parents at once, matched to them by the key
        private LinqPieceElements[]? ReadPieces(IClientSessionHandle? session, MongoDBStatement statement, List<object?[]> rows)
        {
            if (statement.Pieces is null)
                return null;
            var pieces = new LinqPieceElements[statement.Pieces.Length];
            for (var p = 0; p < pieces.Length; p++)
            {
                var piece = statement.Plan.Pieces[p];
                var pieceStatement = statement.Pieces[p];
                var elements = piece.NewElements();
                pieces[p] = elements;
                var match = KeyMatch(piece, pieceStatement, rows);
                if (match is null)
                    continue;
                var stages = new List<BsonDocument>(pieceStatement.Stages.Count + 1) { match };
                stages.AddRange(pieceStatement.Stages);
                var pieceRows = AggregateRows(session, pieceStatement, stages);
                var piecePieces = ReadPieces(session, pieceStatement, pieceRows);
                AddPieceElements(piece, pieceStatement.Plan, pieceRows, piecePieces, elements);
            }
            return pieces;
        }
        private async Task<LinqPieceElements[]?> ReadPiecesAsync(IClientSessionHandle? session, MongoDBStatement statement, List<object?[]> rows, CancellationToken cancellationToken)
        {
            if (statement.Pieces is null)
                return null;
            var pieces = new LinqPieceElements[statement.Pieces.Length];
            for (var p = 0; p < pieces.Length; p++)
            {
                var piece = statement.Plan.Pieces[p];
                var pieceStatement = statement.Pieces[p];
                var elements = piece.NewElements();
                pieces[p] = elements;
                var match = KeyMatch(piece, pieceStatement, rows);
                if (match is null)
                    continue;
                var stages = new List<BsonDocument>(pieceStatement.Stages.Count + 1) { match };
                stages.AddRange(pieceStatement.Stages);
                var pieceRows = await AggregateRowsAsync(session, pieceStatement, stages, cancellationToken);
                var piecePieces = await ReadPiecesAsync(session, pieceStatement, pieceRows, cancellationToken);
                AddPieceElements(piece, pieceStatement.Plan, pieceRows, piecePieces, elements);
            }
            return pieces;
        }
        //the related documents of the parents' keys, or null when no parent has one
        private static BsonDocument? KeyMatch(LinqPiece piece, MongoDBStatement pieceStatement, List<object?[]> rows)
        {
            var keyFields = pieceStatement.KeyFields!;
            if (!piece.CompositeKey)
            {
                var seen = new HashSet<object>();
                var keys = new BsonArray();
                foreach (var row in rows)
                {
                    var key = row[piece.ParentColumn];
                    if (key is not null && seen.Add(key))
                        keys.Add(LinqMongoDBConverter.ToBson(key));
                }
                if (keys.Count == 0)
                    return null;
                return new BsonDocument("$match", new BsonDocument(keyFields[0], new BsonDocument("$in", keys)));
            }
            var seenKeys = new HashSet<LinqKey>();
            var any = new BsonArray();
            foreach (var row in rows)
            {
                var key = LinqKey.Of(row, piece.ParentColumns);
                if (key is null || !seenKeys.Add(key))
                    continue;
                var match = new BsonDocument();
                for (var k = 0; k < keyFields.Length; k++)
                    match[keyFields[k]] = LinqMongoDBConverter.ToBson(row[piece.ParentColumns[k]]);
                any.Add(match);
            }
            if (any.Count == 0)
                return null;
            return new BsonDocument("$match", new BsonDocument("$or", any));
        }
        private static void AddPieceElements(LinqPiece piece, LinqPlan plan, List<object?[]> rows, LinqPieceElements[]? pieces, LinqPieceElements elements)
        {
            foreach (var row in rows)
            {
                var element = BuildElement(plan, row, pieces);
                if (piece.CompositeKey)
                {
                    if (LinqKey.Of(row, plan.ParentKeyColumns!) is { } key)
                        elements.Add(key, element);
                }
                else if (row[plan.ParentKeyColumn!.Value] is { } key)
                {
                    elements.Add(key, element);
                }
            }
        }

        //a document of the model's stored members, a member the graph leaves out written as its default, or as null when it can be
        private static BsonDocument ToDocument(object model, Graph? graph, ModelDetail modelDetail)
        {
            var document = new BsonDocument();
            BsonDocument? identities = modelDetail.IdentityMembers.Count > 1 ? new BsonDocument() : null;
            foreach (var member in modelDetail.Members)
            {
                if (member.StoreName is null || member.IsRelated)
                    continue;
                object? value;
                if (member.IsIdentity || graph is null || graph.HasMember(member.Name))
                    value = member.GetterBoxed(model);
                else
                    value = member.IsNullable ? null : member.CreatorBoxed();
                //an enum left out is its default, the first it names when it doesn't name 0
                var bson = value is Enum && member.EnumDefaultText is not null && graph is not null && !member.IsIdentity && !graph.HasMember(member.Name) ? member.EnumDefaultText : LinqMongoDBConverter.ToBson(value, member);
                if (member.IsIdentity && identities is null)
                {
                    document.InsertAt(0, new BsonElement(LinqMongoDBConverter.IdField, bson));
                    continue;
                }
                if (member.IsIdentity)
                    identities?.Add(member.StoreName, bson);
                document[member.StoreName] = bson;
            }
            if (identities is not null)
                document.InsertAt(0, new BsonElement(LinqMongoDBConverter.IdField, identities));
            return document;
        }
        private static BsonDocument IdFilter(object model, ModelDetail modelDetail)
        {
            if (modelDetail.IdentityMembers.Count == 0)
                throw new NotSupportedException($"{modelDetail.Type.Name} has no identity to find it by");
            return new BsonDocument(LinqMongoDBConverter.IdField, IdValue(modelDetail, i => modelDetail.IdentityMembers[i].GetterBoxed(model)));
        }
        private static BsonValue IdValue(ModelDetail modelDetail, Func<int, object?> value)
        {
            if (modelDetail.IdentityMembers.Count == 1)
                return LinqMongoDBConverter.ToBson(value(0));
            var identities = new BsonDocument();
            for (var i = 0; i < modelDetail.IdentityMembers.Count; i++)
                identities.Add(modelDetail.IdentityMembers[i].StoreName, LinqMongoDBConverter.ToBson(value(i)));
            return identities;
        }
        private static BsonDocument? SetDocument(object model, Graph? graph, ModelDetail modelDetail)
        {
            BsonDocument? set = null;
            foreach (var member in modelDetail.NonAutoGeneratedNonRelationMembers)
            {
                if (member.IsIdentity || member.StoreName is null || (graph is not null && !graph.HasMember(member.Name)))
                    continue;
                (set ??= new BsonDocument())[member.StoreName] = LinqMongoDBConverter.ToBson(member.GetterBoxed(model), member);
            }
            return set is null ? null : new BsonDocument("$set", set);
        }

        //an identity the store generates: a Guid is a new one, a number the next of a counter document per collection
        private void AssignIdentities(IClientSessionHandle? session, object[] models, ModelDetail modelDetail)
        {
            foreach (var member in modelDetail.IdentityAutoGeneratedMembers)
            {
                var type = member.ActualType;
                if (type == typeof(Guid))
                {
                    foreach (var model in models)
                    {
                        if (member.GetterBoxed(model) is not Guid guid || guid == Guid.Empty)
                            member.SetterBoxed(model, Guid.NewGuid());
                    }
                    continue;
                }
                if (type != typeof(int) && type != typeof(long) && type != typeof(short))
                    throw new NotSupportedException($"MongoDB generates a Guid or a number identity, not {type.Name} for {modelDetail.Type.Name}.{member.Name}");
                var filter = new BsonDocument(LinqMongoDBConverter.IdField, modelDetail.DataSourceEntityName + "." + member.StoreName);
                var update = new BsonDocument("$inc", new BsonDocument("value", (long)models.Length));
                var counters = Collection(IdentitiesCollection);
                var counter = session is null ? counters.FindOneAndUpdate(filter, update, identityOptions) : counters.FindOneAndUpdate(session, filter, update, identityOptions);
                SetIdentities(member, models, counter["value"].ToInt64());
            }
        }
        private async Task AssignIdentitiesAsync(IClientSessionHandle? session, object[] models, ModelDetail modelDetail, CancellationToken cancellationToken)
        {
            foreach (var member in modelDetail.IdentityAutoGeneratedMembers)
            {
                var type = member.ActualType;
                if (type == typeof(Guid))
                {
                    foreach (var model in models)
                    {
                        if (member.GetterBoxed(model) is not Guid guid || guid == Guid.Empty)
                            member.SetterBoxed(model, Guid.NewGuid());
                    }
                    continue;
                }
                if (type != typeof(int) && type != typeof(long) && type != typeof(short))
                    throw new NotSupportedException($"MongoDB generates a Guid or a number identity, not {type.Name} for {modelDetail.Type.Name}.{member.Name}");
                var filter = new BsonDocument(LinqMongoDBConverter.IdField, modelDetail.DataSourceEntityName + "." + member.StoreName);
                var update = new BsonDocument("$inc", new BsonDocument("value", (long)models.Length));
                var counters = Collection(IdentitiesCollection);
                var counter = session is null ? await counters.FindOneAndUpdateAsync(filter, update, identityOptions, cancellationToken) : await counters.FindOneAndUpdateAsync(session, filter, update, identityOptions, cancellationToken);
                SetIdentities(member, models, counter["value"].ToInt64());
            }
        }
        private static void SetIdentities(ModelMemberDetail member, object[] models, long last)
        {
            var next = last - models.Length + 1;
            foreach (var model in models)
            {
                var value = next++;
                member.SetterBoxed(model, member.ActualType == typeof(int) ? (object)(int)value : member.ActualType == typeof(short) ? (short)value : value);
            }
        }
        private static object Identities(object model, ModelDetail modelDetail)
        {
            var members = modelDetail.IdentityAutoGeneratedMembers.Count > 0 ? modelDetail.IdentityAutoGeneratedMembers : modelDetail.IdentityMembers;
            if (members.Count == 1)
                return members[0].GetterBoxed(model)!;
            var values = new List<object>(members.Count);
            foreach (var member in members)
                values.Add(member.GetterBoxed(model)!);
            return values;
        }

        //several writes outside a transaction are all or nothing in a transaction of their own when the server has them
        private IClientSessionHandle? BeginLocal(IClientSessionHandle? session)
        {
            if (session is not null || !SupportsTransactions)
                return null;
            var local = client.StartSession();
            local.StartTransaction();
            return local;
        }
        private async Task<IClientSessionHandle?> BeginLocalAsync(IClientSessionHandle? session, CancellationToken cancellationToken)
        {
            if (session is not null || !SupportsTransactions)
                return null;
            var local = await client.StartSessionAsync(cancellationToken: cancellationToken);
            local.StartTransaction();
            return local;
        }

        /// <inheritdoc />
        public object ExecuteInsertGetIdentities<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            using (var session = OpenSession())
            {
                var models = new object[] { model };
                AssignIdentities(session.Session, models, modelDetail);
                Insert(session.Session, ToDocument(model, graph, modelDetail), modelDetail);
                return Identities(model, modelDetail);
            }
        }
        /// <inheritdoc />
        public bool ExecuteInsert<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            using (var session = OpenSession())
            {
                if (modelDetail.IdentityAutoGeneratedMembers.Count > 0)
                    AssignIdentities(session.Session, [model], modelDetail);
                Insert(session.Session, ToDocument(model, graph, modelDetail), modelDetail);
                return true;
            }
        }
        private void Insert(IClientSessionHandle? session, BsonDocument document, ModelDetail modelDetail)
        {
            var collection = Collection(modelDetail.DataSourceEntityName);
            if (session is null)
                collection.InsertOne(document);
            else
                collection.InsertOne(session, document);
        }
        /// <inheritdoc />
        public bool ExecuteUpdate<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var update = SetDocument(model, graph, modelDetail);
            if (update is null)
                return false;
            var filter = IdFilter(model, modelDetail);
            using (var session = OpenSession())
            {
                var collection = Collection(modelDetail.DataSourceEntityName);
                var result = session.Session is null ? collection.UpdateOne(filter, update) : collection.UpdateOne(session.Session, filter, update);
                return result.MatchedCount > 0;
            }
        }
        /// <inheritdoc />
        public object[] ExecuteInsertManyGetIdentities<TModel>(object[] models, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            ExecuteInsertMany<TModel>(models, graph, modelDetail);
            var identities = new object[models.Length];
            for (var i = 0; i < models.Length; i++)
                identities[i] = Identities(models[i], modelDetail);
            return identities;
        }
        /// <inheritdoc />
        public int ExecuteInsertMany<TModel>(object[] models, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            if (models.Length == 0)
                return 0;
            using (var session = OpenSession())
            {
                var local = BeginLocal(session.Session);
                try
                {
                    var active = session.Session ?? local;
                    if (modelDetail.IdentityAutoGeneratedMembers.Count > 0)
                        AssignIdentities(active, models, modelDetail);
                    var documents = new BsonDocument[models.Length];
                    for (var i = 0; i < models.Length; i++)
                        documents[i] = ToDocument(models[i], graph, modelDetail);
                    var collection = Collection(modelDetail.DataSourceEntityName);
                    if (active is null)
                        collection.InsertMany(documents);
                    else
                        collection.InsertMany(active, documents);
                    local?.CommitTransaction();
                    return documents.Length;
                }
                finally
                {
                    local?.Dispose();
                }
            }
        }
        /// <inheritdoc />
        public int ExecuteUpdateMany<TModel>(object[] models, Graph? graph, ModelDetail modelDetail) where TModel : class, new()
        {
            var writes = UpdateWrites(models, graph, modelDetail);
            if (writes.Count == 0)
                return 0;
            using (var session = OpenSession())
            {
                var local = BeginLocal(session.Session);
                try
                {
                    var active = session.Session ?? local;
                    var collection = Collection(modelDetail.DataSourceEntityName);
                    var result = active is null ? collection.BulkWrite(writes) : collection.BulkWrite(active, writes);
                    //committed only when every model was written, a model that isn't stored fails them all
                    if (result.MatchedCount == writes.Count)
                        local?.CommitTransaction();
                    return (int)result.MatchedCount;
                }
                finally
                {
                    local?.Dispose();
                }
            }
        }
        private static List<WriteModel<BsonDocument>> UpdateWrites(object[] models, Graph? graph, ModelDetail modelDetail)
        {
            var writes = new List<WriteModel<BsonDocument>>(models.Length);
            foreach (var model in models)
            {
                var update = SetDocument(model, graph, modelDetail);
                if (update is not null)
                    writes.Add(new UpdateOneModel<BsonDocument>(IdFilter(model, modelDetail), update));
            }
            return writes;
        }
        /// <inheritdoc />
        public int ExecuteDelete<TModel>(ICollection ids, ModelDetail modelDetail) where TModel : class, new()
        {
            var filter = DeleteFilter(ids, modelDetail);
            using (var session = OpenSession())
            {
                var collection = Collection(modelDetail.DataSourceEntityName);
                var result = session.Session is null ? collection.DeleteMany(filter) : collection.DeleteMany(session.Session, filter);
                return (int)result.DeletedCount;
            }
        }
        private static BsonDocument DeleteFilter(ICollection ids, ModelDetail modelDetail)
        {
            var values = new BsonArray();
            foreach (var id in ids)
            {
                if (modelDetail.IdentityMembers.Count == 1)
                {
                    values.Add(LinqMongoDBConverter.ToBson(id));
                }
                else
                {
                    var parts = new List<object?>();
                    foreach (var part in (IEnumerable)id)
                        parts.Add(part);
                    values.Add(IdValue(modelDetail, i => parts[i]));
                }
            }
            return new BsonDocument(LinqMongoDBConverter.IdField, new BsonDocument("$in", values));
        }

        //a condition through relations finds the documents' ids with their lookups first, a condition of the document's own fields is the filter
        private BsonDocument WhereFilter(IClientSessionHandle? session, LambdaExpression where, ModelDetail modelDetail)
        {
            var filter = LinqMongoDBConverter.ConvertPersistWhere(where, modelDetail, out var lookups);
            if (filter is not null)
                return filter;
            lookups!.Add(new BsonDocument("$project", new BsonDocument(LinqMongoDBConverter.IdField, 1)));
            var ids = new BsonArray();
            foreach (var document in Aggregate(session, modelDetail.DataSourceEntityName, lookups))
                ids.Add(document[LinqMongoDBConverter.IdField]);
            return new BsonDocument(LinqMongoDBConverter.IdField, new BsonDocument("$in", ids));
        }
        private async Task<BsonDocument> WhereFilterAsync(IClientSessionHandle? session, LambdaExpression where, ModelDetail modelDetail, CancellationToken cancellationToken)
        {
            var filter = LinqMongoDBConverter.ConvertPersistWhere(where, modelDetail, out var lookups);
            if (filter is not null)
                return filter;
            lookups!.Add(new BsonDocument("$project", new BsonDocument(LinqMongoDBConverter.IdField, 1)));
            var ids = new BsonArray();
            foreach (var document in await AggregateAsync(session, modelDetail.DataSourceEntityName, lookups, cancellationToken))
                ids.Add(document[LinqMongoDBConverter.IdField]);
            return new BsonDocument(LinqMongoDBConverter.IdField, new BsonDocument("$in", ids));
        }

        /// <inheritdoc />
        public int ExecuteUpdateWhere<TModel>(TModel model, Graph graph, LambdaExpression where, ModelDetail modelDetail) where TModel : class, new()
        {
            var update = SetDocument(model, graph, modelDetail);
            if (update is null)
                return 0;
            using (var session = OpenSession())
            {
                var filter = WhereFilter(session.Session, where, modelDetail);
                var collection = Collection(modelDetail.DataSourceEntityName);
                var result = session.Session is null ? collection.UpdateMany(filter, update) : collection.UpdateMany(session.Session, filter, update);
                return (int)result.MatchedCount;
            }
        }
        /// <inheritdoc />
        public int ExecuteUpdateWhere<TModel>(LambdaExpression set, LambdaExpression where, ModelDetail modelDetail) where TModel : class, new()
        {
            var update = PipelineUpdate(set, modelDetail);
            using (var session = OpenSession())
            {
                var filter = WhereFilter(session.Session, where, modelDetail);
                var collection = Collection(modelDetail.DataSourceEntityName);
                var result = session.Session is null ? collection.UpdateMany(filter, update) : collection.UpdateMany(session.Session, filter, update);
                return (int)result.MatchedCount;
            }
        }
        //the members set from the document's own fields, as an update pipeline
        private static UpdateDefinition<BsonDocument> PipelineUpdate(LambdaExpression set, ModelDetail modelDetail)
        {
            var values = LinqMongoDBConverter.ConvertPersistUpdate(ProjectionMember.FromSet(set), modelDetail);
            return new PipelineUpdateDefinition<BsonDocument>(PipelineDefinition<BsonDocument, BsonDocument>.Create(new[] { new BsonDocument("$set", values) }));
        }
        /// <inheritdoc />
        public int ExecuteDeleteWhere<TModel>(LambdaExpression where, ModelDetail modelDetail) where TModel : class, new()
        {
            using (var session = OpenSession())
            {
                var filter = WhereFilter(session.Session, where, modelDetail);
                var collection = Collection(modelDetail.DataSourceEntityName);
                var result = session.Session is null ? collection.DeleteMany(filter) : collection.DeleteMany(session.Session, filter);
                return (int)result.DeletedCount;
            }
        }

        /// <inheritdoc />
        public async Task<object> ExecuteInsertGetIdentitiesAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            using (var session = await OpenSessionAsync(cancellationToken))
            {
                await AssignIdentitiesAsync(session.Session, [model], modelDetail, cancellationToken);
                await InsertAsync(session.Session, ToDocument(model, graph, modelDetail), modelDetail, cancellationToken);
                return Identities(model, modelDetail);
            }
        }
        /// <inheritdoc />
        public async Task<bool> ExecuteInsertAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            using (var session = await OpenSessionAsync(cancellationToken))
            {
                if (modelDetail.IdentityAutoGeneratedMembers.Count > 0)
                    await AssignIdentitiesAsync(session.Session, [model], modelDetail, cancellationToken);
                await InsertAsync(session.Session, ToDocument(model, graph, modelDetail), modelDetail, cancellationToken);
                return true;
            }
        }
        private Task InsertAsync(IClientSessionHandle? session, BsonDocument document, ModelDetail modelDetail, CancellationToken cancellationToken)
        {
            var collection = Collection(modelDetail.DataSourceEntityName);
            return session is null ? collection.InsertOneAsync(document, null, cancellationToken) : collection.InsertOneAsync(session, document, null, cancellationToken);
        }
        /// <inheritdoc />
        public async Task<bool> ExecuteUpdateAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var update = SetDocument(model, graph, modelDetail);
            if (update is null)
                return false;
            var filter = IdFilter(model, modelDetail);
            using (var session = await OpenSessionAsync(cancellationToken))
            {
                var collection = Collection(modelDetail.DataSourceEntityName);
                var result = session.Session is null ? await collection.UpdateOneAsync(filter, update, null, cancellationToken) : await collection.UpdateOneAsync(session.Session, filter, update, null, cancellationToken);
                return result.MatchedCount > 0;
            }
        }
        /// <inheritdoc />
        public async Task<object[]> ExecuteInsertManyGetIdentitiesAsync<TModel>(object[] models, Graph? graph, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            await ExecuteInsertManyAsync<TModel>(models, graph, modelDetail, cancellationToken);
            var identities = new object[models.Length];
            for (var i = 0; i < models.Length; i++)
                identities[i] = Identities(models[i], modelDetail);
            return identities;
        }
        /// <inheritdoc />
        public async Task<int> ExecuteInsertManyAsync<TModel>(object[] models, Graph? graph, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            if (models.Length == 0)
                return 0;
            using (var session = await OpenSessionAsync(cancellationToken))
            {
                var local = await BeginLocalAsync(session.Session, cancellationToken);
                try
                {
                    var active = session.Session ?? local;
                    if (modelDetail.IdentityAutoGeneratedMembers.Count > 0)
                        await AssignIdentitiesAsync(active, models, modelDetail, cancellationToken);
                    var documents = new BsonDocument[models.Length];
                    for (var i = 0; i < models.Length; i++)
                        documents[i] = ToDocument(models[i], graph, modelDetail);
                    var collection = Collection(modelDetail.DataSourceEntityName);
                    if (active is null)
                        await collection.InsertManyAsync(documents, null, cancellationToken);
                    else
                        await collection.InsertManyAsync(active, documents, null, cancellationToken);
                    if (local is not null)
                        await local.CommitTransactionAsync(cancellationToken);
                    return documents.Length;
                }
                finally
                {
                    local?.Dispose();
                }
            }
        }
        /// <inheritdoc />
        public async Task<int> ExecuteUpdateManyAsync<TModel>(object[] models, Graph? graph, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var writes = UpdateWrites(models, graph, modelDetail);
            if (writes.Count == 0)
                return 0;
            using (var session = await OpenSessionAsync(cancellationToken))
            {
                var local = await BeginLocalAsync(session.Session, cancellationToken);
                try
                {
                    var active = session.Session ?? local;
                    var collection = Collection(modelDetail.DataSourceEntityName);
                    var result = active is null ? await collection.BulkWriteAsync(writes, null, cancellationToken) : await collection.BulkWriteAsync(active, writes, null, cancellationToken);
                    if (local is not null && result.MatchedCount == writes.Count)
                        await local.CommitTransactionAsync(cancellationToken);
                    return (int)result.MatchedCount;
                }
                finally
                {
                    local?.Dispose();
                }
            }
        }
        /// <inheritdoc />
        public async Task<int> ExecuteDeleteAsync<TModel>(ICollection ids, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var filter = DeleteFilter(ids, modelDetail);
            using (var session = await OpenSessionAsync(cancellationToken))
            {
                var collection = Collection(modelDetail.DataSourceEntityName);
                var result = session.Session is null ? await collection.DeleteManyAsync(filter, cancellationToken) : await collection.DeleteManyAsync(session.Session, filter, null, cancellationToken);
                return (int)result.DeletedCount;
            }
        }
        /// <inheritdoc />
        public async Task<int> ExecuteUpdateWhereAsync<TModel>(TModel model, Graph graph, LambdaExpression where, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var update = SetDocument(model, graph, modelDetail);
            if (update is null)
                return 0;
            using (var session = await OpenSessionAsync(cancellationToken))
            {
                var filter = await WhereFilterAsync(session.Session, where, modelDetail, cancellationToken);
                var collection = Collection(modelDetail.DataSourceEntityName);
                var result = session.Session is null ? await collection.UpdateManyAsync(filter, update, null, cancellationToken) : await collection.UpdateManyAsync(session.Session, filter, update, null, cancellationToken);
                return (int)result.MatchedCount;
            }
        }
        /// <inheritdoc />
        public async Task<int> ExecuteUpdateWhereAsync<TModel>(LambdaExpression set, LambdaExpression where, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            var update = PipelineUpdate(set, modelDetail);
            using (var session = await OpenSessionAsync(cancellationToken))
            {
                var filter = await WhereFilterAsync(session.Session, where, modelDetail, cancellationToken);
                var collection = Collection(modelDetail.DataSourceEntityName);
                var result = session.Session is null ? await collection.UpdateManyAsync(filter, update, null, cancellationToken) : await collection.UpdateManyAsync(session.Session, filter, update, null, cancellationToken);
                return (int)result.MatchedCount;
            }
        }
        /// <inheritdoc />
        public async Task<int> ExecuteDeleteWhereAsync<TModel>(LambdaExpression where, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new()
        {
            using (var session = await OpenSessionAsync(cancellationToken))
            {
                var filter = await WhereFilterAsync(session.Session, where, modelDetail, cancellationToken);
                var collection = Collection(modelDetail.DataSourceEntityName);
                var result = session.Session is null ? await collection.DeleteManyAsync(filter, cancellationToken) : await collection.DeleteManyAsync(session.Session, filter, null, cancellationToken);
                return (int)result.DeletedCount;
            }
        }

        //the BSON type a member's values are stored as
        private static string BsonTypeName(ModelMemberDetail member)
        {
            if (member.EnumType is not null)
                return "string";
            var type = member.ActualType;
            if (type == typeof(string) || type == typeof(char))
                return "string";
            if (type == typeof(bool))
                return "bool";
            if (type == typeof(int) || type == typeof(short) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(ushort))
                return "int";
            if (type == typeof(long) || type == typeof(uint) || type == typeof(TimeSpan) || type == typeof(TimeOnly))
                return "long";
            if (type == typeof(double) || type == typeof(float))
                return "double";
            if (type == typeof(decimal) || type == typeof(ulong))
                return "decimal";
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly))
                return "date";
            if (type == typeof(Guid) || type == typeof(byte[]))
                return "binData";
            throw new NotSupportedException($"{member.Name} of {type.Name} can't be in a unique index on MongoDB");
        }

        /// <inheritdoc />
        public IDataStoreGenerationPlan BuildStoreGenerationPlan(bool createDataStore, bool columns, bool constraints, bool dropColumns, ICollection<ModelDetail> modelDetails)
        {
            var steps = new List<MongoDBDataStoreGenerationPlan.Step>();
            if (!columns && !constraints)
                return new MongoDBDataStoreGenerationPlan(steps);

            var existing = new HashSet<string>(database.ListCollectionNames().ToList());
            foreach (var model in modelDetails)
            {
                var name = model.DataSourceEntityName;
                var exists = existing.Contains(name);
                if (!exists)
                {
                    if (!columns)
                        continue;
                    steps.Add(new($"Create Collection {name}", () => database.CreateCollection(name)));
                }

                var indexes = new Dictionary<string, BsonDocument>();
                if (model.IdentityMembers.Count > 1)
                {
                    //several identities are unique as the _id document, each is also a field queries compare, which this index is for
                    var keys = new BsonDocument();
                    foreach (var identity in model.IdentityMembers)
                        keys[identity.StoreName] = 1;
                    indexes["IX_" + name + "_Identity"] = new BsonDocument { { "key", keys }, { "unique", true } };
                }
                foreach (var index in model.Indexes)
                {
                    var keys = new BsonDocument();
                    BsonDocument? partial = null;
                    for (var i = 0; i < index.Members.Count; i++)
                    {
                        var field = LinqMongoDBConverter.FieldName(index.Members[i], model);
                        keys[field] = index.Descending[i] ? -1 : 1;
                        //a unique index allows many nulls as in SQL, so it only holds documents with a value in every field
                        if (index.IsUnique && (index.Members[i].IsNullable || !index.Members[i].IsDataSourceNotNull))
                            (partial ??= new BsonDocument())[field] = new BsonDocument("$type", BsonTypeName(index.Members[i]));
                    }
                    var definition = new BsonDocument { { "key", keys }, { "unique", index.IsUnique } };
                    if (partial is not null)
                        definition["partialFilterExpression"] = partial;
                    indexes[index.StoreName] = definition;
                }

                var current = new Dictionary<string, BsonDocument>();
                if (exists)
                {
                    foreach (var index in Collection(name).Indexes.List().ToList())
                    {
                        var indexName = index["name"].AsString;
                        if (indexName != "_id_")
                            current[indexName] = index;
                    }
                }
                foreach (var index in indexes)
                {
                    if (current.TryGetValue(index.Key, out var found))
                    {
                        var same = found["key"].Equals(index.Value["key"]) && (found.TryGetValue("unique", out var unique) && unique.ToBoolean()) == index.Value["unique"].AsBoolean
                            && Equals(found.GetValue("partialFilterExpression", BsonNull.Value), index.Value.GetValue("partialFilterExpression", BsonNull.Value));
                        if (same)
                            continue;
                        steps.Add(new($"Drop Index {index.Key} on {name}", () => Collection(name).Indexes.DropOne(index.Key)));
                    }
                    //a new unique index can fail on the documents
                    if (index.Value["unique"].AsBoolean && !constraints)
                        continue;
                    var keys = index.Value["key"].AsBsonDocument;
                    var options = new CreateIndexOptions<BsonDocument> { Name = index.Key, Unique = index.Value["unique"].AsBoolean };
                    if (index.Value.TryGetValue("partialFilterExpression", out var filter))
                        options.PartialFilterExpression = filter.AsBsonDocument;
                    steps.Add(new($"Create Index {index.Key} on {name} {keys}", () => Collection(name).Indexes.CreateOne(new CreateIndexModel<BsonDocument>(keys, options))));
                }
                //a member that can't be null is missing from documents written before it, filled with the type's default when added with constraints as SQL fills a new column, otherwise it fails as SQL does on a null
                if (exists && constraints)
                {
                    foreach (var member in model.Members)
                    {
                        if (member.StoreName is null || member.IsRelated || member.IsIdentity || !member.IsDataSourceNotNull)
                            continue;
                        var field = LinqMongoDBConverter.FieldName(member, model);
                        var missing = new BsonDocument(field, new BsonDocument("$exists", false));
                        if (Collection(name).CountDocuments(missing, new CountOptions { Limit = 1 }) == 0)
                            continue;
                        if (!columns)
                        {
                            steps.Add(new($"Check {field} on {name} is in every document", () => throw new InvalidOperationException($"{nameof(MongoDBEngine)} {name} has documents without {field}, which can't be null")));
                            continue;
                        }
                        var value = member.EnumType is not null ? (BsonValue)member.EnumDefaultText : member.Type == typeof(string) ? String.Empty : LinqMongoDBConverter.ToBson(member.CreatorBoxed());
                        var set = new BsonDocument("$set", new BsonDocument(field, value));
                        steps.Add(new($"Set {field} to its default on {name} where it is missing", () => Collection(name).UpdateMany(missing, set)));
                    }
                }
                foreach (var index in current)
                {
                    if (!indexes.ContainsKey(index.Key))
                        steps.Add(new($"Drop Index {index.Key} on {name}", () => Collection(name).Indexes.DropOne(index.Key)));
                }
            }
            return new MongoDBDataStoreGenerationPlan(steps);
        }
    }
}

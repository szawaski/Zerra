// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using MongoDB.Bson;
using Zerra.Repository.Linq;

namespace Zerra.Repository.MongoDB
{
    /// <summary>
    /// The aggregation pipeline a query runs on a collection, with the plan that reads its documents and the statements of the relations the plan reads, one per piece.
    /// A piece's pipeline starts with a match of the keys of the parents read before it.
    /// </summary>
    internal sealed class MongoDBStatement
    {
        public string Collection { get; }
        public List<BsonDocument> Stages { get; }
        public LinqPlan Plan { get; }
        public string[]? KeyFields { get; }
        public MongoDBStatement[]? Pieces { get; }

        public MongoDBStatement(string collection, List<BsonDocument> stages, LinqPlan plan, string[]? keyFields, MongoDBStatement[]? pieces)
        {
            this.Collection = collection;
            this.Stages = stages;
            this.Plan = plan;
            this.KeyFields = keyFields;
            this.Pieces = pieces;
        }
    }
}

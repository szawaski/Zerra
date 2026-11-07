// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using MongoDB.Driver;
using Zerra.Repository.Transactions;

namespace Zerra.Repository.MongoDB
{
    /// <summary>
    /// A MongoDB session with a transaction started, which every operation of the repository transaction on the database runs in.
    /// </summary>
    internal sealed class MongoDBTransactionPart : RepoTransactionPart
    {
        public IClientSessionHandle Session { get; }

        public MongoDBTransactionPart(IClientSessionHandle session)
        {
            this.Session = session;
        }

        public override void Commit() => Session.CommitTransaction();
        public override Task CommitAsync(CancellationToken cancellationToken) => Session.CommitTransactionAsync(cancellationToken);
        public override void Rollback() => Session.AbortTransaction();
        public override Task RollbackAsync() => Session.AbortTransactionAsync(CancellationToken.None);
        public override void Close() => Session.Dispose();
    }
}

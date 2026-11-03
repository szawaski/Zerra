// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.ComponentModel;
using System.Data.Common;

namespace Zerra.Repository.Transactions
{
    /// <summary>
    /// The work of one engine in a <see cref="RepoTransaction"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class RepoTransactionPart
    {
        internal readonly SemaphoreSlim Gate = new(1, 1);
        internal object Key = null!;
        internal string Name = null!;
        internal volatile bool Closed;
        private volatile bool streaming;
        internal bool Streaming => streaming;

        /// <summary>Whether this part commits after the others, for an engine whose commit is the least likely to fail.</summary>
        public virtual bool CommitsLast => false;

        /// <summary>Ends an operation started with <see cref="RepoTransaction.Enter{TPart}(object, string, Func{System.Data.IsolationLevel, TPart})"/>.</summary>
        public void Exit()
        {
            if (streaming)
                streaming = false;
            _ = Gate.Release();
        }

        /// <summary>Marks the part as held by a stream until <see cref="Exit"/>, so another operation of the transaction is refused rather than waiting on the stream it may run inside of.</summary>
        public void HoldForStream() => streaming = true;

        /// <summary>Commits the work.</summary>
        public abstract void Commit();
        /// <summary>Commits the work.</summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public abstract Task CommitAsync(CancellationToken cancellationToken);
        /// <summary>Rolls back the work.</summary>
        public abstract void Rollback();
        /// <summary>Rolls back the work.</summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public abstract Task RollbackAsync();
        /// <summary>Releases what the part holds, after it has committed or rolled back.</summary>
        public abstract void Close();
    }

    /// <summary>
    /// The work of a database engine in a <see cref="RepoTransaction"/>, on one connection.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class DbRepoTransactionPart : RepoTransactionPart
    {
        /// <summary>The connection all the work runs on.</summary>
        public DbConnection Connection { get; }
        /// <summary>The database transaction.</summary>
        public DbTransaction Transaction { get; }

        /// <summary>Initializes a part over an open connection and its transaction.</summary>
        /// <param name="connection">The connection.</param>
        /// <param name="transaction">The transaction.</param>
        public DbRepoTransactionPart(DbConnection connection, DbTransaction transaction)
        {
            this.Connection = connection;
            this.Transaction = transaction;
        }

        /// <inheritdoc />
        public override void Commit() => Transaction.Commit();
        /// <inheritdoc />
        public override Task CommitAsync(CancellationToken cancellationToken) => Transaction.CommitAsync(cancellationToken);
        /// <inheritdoc />
        public override void Rollback() => Transaction.Rollback();
        /// <inheritdoc />
        public override Task RollbackAsync() => Transaction.RollbackAsync(CancellationToken.None);
        /// <inheritdoc />
        public override void Close()
        {
            Transaction.Dispose();
            Connection.Dispose();
        }
    }
}

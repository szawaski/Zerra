// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.ComponentModel;
using System.Data;
using Zerra.CQRS.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// The current unit of work, which the engines find through <see cref="Current"/>. Each engine joins when it is first used.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class RepoTransaction : IRepoTransaction
    {
        /// <summary>The transaction of the current flow of work, if any.</summary>
        public static RepoTransaction? Current => BusAmbient.Current as RepoTransaction;

        private readonly IsolationLevel isolationLevel;
        private readonly List<RepoTransactionPart> parts = new();
        private readonly SemaphoreSlim gate = new(1, 1);
        private volatile bool finished;

        private RepoTransaction(IsolationLevel isolationLevel) => this.isolationLevel = isolationLevel;

        //not async, a value set in an async method is gone when it returns to its caller
        internal static RepoTransaction Begin(IsolationLevel isolationLevel)
        {
            if (Current is { finished: false })
                throw new InvalidOperationException("A transaction is already open in this flow of work");
            var transaction = new RepoTransaction(isolationLevel);
            BusAmbient.Current = transaction;
            return transaction;
        }

        /// <summary>Gets the part of an engine, which joins the transaction if it is new. Parallel calls can use the part at once.</summary>
        /// <typeparam name="TPart">The type of the part.</typeparam>
        /// <param name="key">What identifies the store, so engines on the same store share a part.</param>
        /// <param name="name">The name of the engine, for errors.</param>
        /// <param name="begin">Starts the part.</param>
        /// <returns>The part.</returns>
        public TPart Enlist<TPart>(object key, string name, Func<IsolationLevel, TPart> begin) where TPart : RepoTransactionPart
        {
            gate.Wait();
            try
            {
                ThrowIfFinished();
                foreach (var existing in parts)
                {
                    if (existing.Key.Equals(key))
                        return (TPart)existing;
                }
                var part = begin(isolationLevel);
                part.Key = key;
                part.Name = name;
                parts.Add(part);
                return part;
            }
            finally
            {
                _ = gate.Release();
            }
        }
        /// <summary>Gets the part of an engine for one operation, holding it until <see cref="RepoTransactionPart.Exit"/>, as a connection runs one operation at a time.</summary>
        /// <typeparam name="TPart">The type of the part.</typeparam>
        /// <param name="key">What identifies the store, so engines on the same store share a part.</param>
        /// <param name="name">The name of the engine, for errors.</param>
        /// <param name="begin">Starts the part.</param>
        /// <returns>The part.</returns>
        public TPart Enter<TPart>(object key, string name, Func<IsolationLevel, TPart> begin) where TPart : RepoTransactionPart
        {
            while (true)
            {
                var part = Enlist(key, name, begin);
                part.Gate.Wait();
                if (!part.Closed && !finished)
                    return part;
                //it finished or rolled back while this waited its turn, a rollback leaves the next part to start
                part.Exit();
            }
        }
        /// <summary>Gets the part of an engine for one operation, holding it until <see cref="RepoTransactionPart.Exit"/>, as a connection runs one operation at a time.</summary>
        /// <typeparam name="TPart">The type of the part.</typeparam>
        /// <param name="key">What identifies the store, so engines on the same store share a part.</param>
        /// <param name="name">The name of the engine, for errors.</param>
        /// <param name="begin">Starts the part.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>The part.</returns>
        public async Task<TPart> EnterAsync<TPart>(object key, string name, Func<IsolationLevel, CancellationToken, Task<TPart>> begin, CancellationToken cancellationToken) where TPart : RepoTransactionPart
        {
            while (true)
            {
                TPart? part = null;
                await gate.WaitAsync(cancellationToken);
                try
                {
                    ThrowIfFinished();
                    foreach (var existing in parts)
                    {
                        if (existing.Key.Equals(key))
                        {
                            part = (TPart)existing;
                            break;
                        }
                    }
                    if (part is null)
                    {
                        part = await begin(isolationLevel, cancellationToken);
                        part.Key = key;
                        part.Name = name;
                        parts.Add(part);
                    }
                }
                finally
                {
                    _ = gate.Release();
                }
                await part.Gate.WaitAsync(cancellationToken);
                if (!part.Closed && !finished)
                    return part;
                //it finished or rolled back while this waited its turn, a rollback leaves the next part to start
                part.Exit();
            }
        }

        private void ThrowIfFinished()
        {
            if (finished)
                throw new InvalidOperationException("The transaction has already committed or rolled back");
        }

        //the flow that opened the transaction goes on without it, a task it started that is still using it gets an error
        private RepoTransactionPart[] Finish()
        {
            finished = true;
            if (ReferenceEquals(BusAmbient.Current, this))
                BusAmbient.Current = null;
            gate.Wait();
            try
            {
                return parts.Where(x => !x.CommitsLast).Concat(parts.Where(x => x.CommitsLast)).ToArray();
            }
            finally
            {
                _ = gate.Release();
            }
        }

        /// <inheritdoc />
        public void Commit()
        {
            ThrowIfFinished();
            var ordered = Finish();
            //what a part does to commit is not itself in a transaction
            var ambient = BusAmbient.Current;
            BusAmbient.Current = null;
            var index = 0;
            try
            {
                for (; index < ordered.Length; index++)
                {
                    ordered[index].Gate.Wait();
                    ordered[index].Commit();
                }
            }
            catch (Exception ex)
            {
                for (var i = index; i < ordered.Length; i++)
                    RollbackPart(ordered[i], i != index);
                ThrowCommitFailure(ordered, index, ex);
                throw;
            }
            finally
            {
                foreach (var part in ordered)
                    ClosePart(part);
                BusAmbient.Current = ambient;
            }
        }
        /// <inheritdoc />
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfFinished();
            //before the first await, so the flow that opened the transaction sees it finished
            return CommitAsync(Finish(), cancellationToken);
        }
        private static async Task CommitAsync(RepoTransactionPart[] ordered, CancellationToken cancellationToken)
        {
            BusAmbient.Current = null;
            var index = 0;
            try
            {
                for (; index < ordered.Length; index++)
                {
                    await ordered[index].Gate.WaitAsync(CancellationToken.None);
                    await ordered[index].CommitAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                for (var i = index; i < ordered.Length; i++)
                    await RollbackPartAsync(ordered[i], i != index);
                ThrowCommitFailure(ordered, index, ex);
                throw;
            }
            finally
            {
                foreach (var part in ordered)
                    ClosePart(part);
            }
        }
        private static void ClosePart(RepoTransactionPart part)
        {
            part.Closed = true;
            try
            {
                part.Close();
            }
            finally
            {
                part.Exit();
            }
        }
        private static void ThrowCommitFailure(RepoTransactionPart[] ordered, int failed, Exception ex)
        {
            if (failed > 0)
                throw new RepoPartialCommitException(ordered.Take(failed).Select(x => x.Name).ToArray(), ordered.Skip(failed).Select(x => x.Name).ToArray(), ex);
        }

        //closing the connection after a failed rollback rolls it back in the store
        private static void RollbackPart(RepoTransactionPart part, bool wait)
        {
            try
            {
                if (wait)
                    part.Gate.Wait();
                part.Rollback();
            }
            catch { }
        }
        private static async Task RollbackPartAsync(RepoTransactionPart part, bool wait)
        {
            try
            {
                if (wait)
                    await part.Gate.WaitAsync();
                await part.RollbackAsync();
            }
            catch { }
        }

        /// <inheritdoc />
        public void Rollback()
        {
            gate.Wait();
            try
            {
                ThrowIfFinished();
                foreach (var part in parts)
                {
                    RollbackPart(part, true);
                    ClosePart(part);
                }
                parts.Clear();
            }
            finally
            {
                _ = gate.Release();
            }
        }
        /// <inheritdoc />
        public async Task RollbackAsync()
        {
            await gate.WaitAsync();
            try
            {
                ThrowIfFinished();
                foreach (var part in parts)
                {
                    await RollbackPartAsync(part, true);
                    ClosePart(part);
                }
                parts.Clear();
            }
            finally
            {
                _ = gate.Release();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            //a commit in an async method it called can leave this flow still holding it
            if (finished)
            {
                if (ReferenceEquals(BusAmbient.Current, this))
                    BusAmbient.Current = null;
                return;
            }
            var ordered = Finish();
            foreach (var part in ordered)
            {
                RollbackPart(part, true);
                ClosePart(part);
            }
        }
        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            if (finished)
            {
                if (ReferenceEquals(BusAmbient.Current, this))
                    BusAmbient.Current = null;
                return default;
            }
            //before the first await, so the flow that opened the transaction sees it finished
            return DisposeAsync(Finish());
        }
        private static async ValueTask DisposeAsync(RepoTransactionPart[] ordered)
        {
            foreach (var part in ordered)
            {
                await RollbackPartAsync(part, true);
                ClosePart(part);
            }
        }
    }
}

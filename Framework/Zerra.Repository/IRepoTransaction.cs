// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// A unit of work over the repository. Disposing it without committing rolls it back.
    /// </summary>
    public interface IRepoTransaction : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Commits the work on every engine used, one engine at a time.
        /// </summary>
        /// <exception cref="RepoPartialCommitException">An engine failed after another had committed.</exception>
        void Commit();
        /// <summary>
        /// Commits the work on every engine used, one engine at a time.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="RepoPartialCommitException">An engine failed after another had committed.</exception>
        Task CommitAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Undoes the work so far on every engine used. The transaction stays open, and what comes next is new work in it to commit or roll back.
        /// </summary>
        void Rollback();
        /// <summary>
        /// Undoes the work so far on every engine used. The transaction stays open, and what comes next is new work in it to commit or roll back.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RollbackAsync();
    }
}

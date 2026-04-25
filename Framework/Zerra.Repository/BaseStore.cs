// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Base class for store implementations that provide data access logic within a repository context.
    /// </summary>
    public abstract class BaseStore
    {
        /// <summary>
        /// Gets the internal repository interface from the current context.
        /// </summary>
        public IRepoInternal Repo => Context.Repo;

        private RepoContext? context = null;

        /// <summary>
        /// Gets the current <see cref="RepoContext"/> for this store.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the store has not been initialized.</exception>
        public RepoContext Context => context ?? throw new InvalidOperationException($"{nameof(BaseStore)} not initialized");

        /// <summary>
        /// Initializes the store with the specified <see cref="RepoContext"/>.
        /// </summary>
        /// <param name="context">The repository context to associate with this store.</param>
        public void Initialize(RepoContext context)
        {
            this.context = context;
            OnInitialize(context);
        }

        /// <summary>
        /// Called when the store is initialized. Override to perform custom initialization logic.
        /// </summary>
        /// <param name="context">The repository context provided during initialization.</param>
        public virtual void OnInitialize(RepoContext context) { }
    }
}

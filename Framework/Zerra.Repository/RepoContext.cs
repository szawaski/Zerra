// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Provides contextual access to a repository instance during a scoped operation.
    /// </summary>
    public sealed class RepoContext
    {
        /// <summary>
        /// Gets the repository instance associated with this context.
        /// </summary>
        public IRepoInternal Repo { get; init; }

        internal RepoContext(IRepoInternal repo)
        {
            this.Repo = repo;
        }
    }
}

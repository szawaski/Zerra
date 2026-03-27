// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public sealed class RepoContext
    {
        public IRepoInternal Repo { get; init; }
        internal RepoContext(IRepoInternal repo)
        {
            this.Repo = repo;
        }
    }
}

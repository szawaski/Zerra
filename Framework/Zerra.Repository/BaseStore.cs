// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public abstract class BaseStore
    {
        public IRepoInternal Repo => Context.Repo;

        private RepoContext? context = null;

        public RepoContext Context => context ?? throw new InvalidOperationException($"{nameof(BaseStore)} not initialized");

        public void Initialize(RepoContext context)
        {
            this.context = context;
            OnInitialize(context);
        }   

        public virtual void OnInitialize(RepoContext context) { }
    }
}

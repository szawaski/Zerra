// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public interface IProviderRelation<TModel> : IProviderRelation
        where TModel : class, new()
    {
        IReadOnlyCollection<TModel> OnGetIncludingBase(IReadOnlyCollection<TModel> models, Graph? graph);
        Task<IReadOnlyCollection<TModel>> OnGetIncludingBaseAsync(IReadOnlyCollection<TModel> models, Graph? graph);
    }
}

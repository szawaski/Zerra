// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Zerra.Repository
{
    public interface IProviderRelation<TModel> : IProviderRelation
        where TModel : class, new()
    {
        void OnQueryIncludingBase(Graph<TModel>? graph);

        IReadOnlyCollection<TModel> OnGetIncludingBase(IReadOnlyCollection<TModel> models, Graph<TModel>? graph);
        Task<IReadOnlyCollection<TModel>> OnGetIncludingBaseAsync(IReadOnlyCollection<TModel> models, Graph<TModel>? graph);

        Expression<Func<TModel, bool>>? GetWhereExpressionIncludingBase(Graph<TModel>? graph);
    }
}

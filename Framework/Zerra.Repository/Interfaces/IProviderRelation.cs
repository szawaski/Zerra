// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Zerra.Repository
{
    public interface IProviderRelation<TModel> : IProviderRelation
        where TModel : class, new()
    {
        void OnQueryIncludingBase(Graph<TModel> graph);

        ICollection<TModel> OnGetIncludingBase(ICollection<TModel> models, Graph<TModel> graph);
        Task<ICollection<TModel>> OnGetIncludingBaseAsync(ICollection<TModel> models, Graph<TModel> graph);

        Expression<Func<TModel, bool>> GetWhereExpressionIncludingBase(Graph<TModel> graph);
    }

    public interface IProviderRelation
    {
        void OnQueryIncludingBase(Graph graph);

        ICollection OnGetIncludingBase(ICollection models, Graph graph);
        Task<ICollection> OnGetIncludingBaseAsync(ICollection models, Graph graph);

        Expression GetWhereExpressionIncludingBase(Graph graph);
    }
}

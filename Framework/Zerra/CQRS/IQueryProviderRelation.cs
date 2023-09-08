// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface IQueryProviderRelation<TModel> : IQueryProviderRelation
        where TModel : class, new()
    {
        void OnQueryIncludingBase(Graph<TModel> graph);

        IEnumerable<TModel> OnGetIncludingBase(IEnumerable<TModel> models, Graph<TModel> graph);
        Task<IEnumerable<TModel>> OnGetIncludingBaseAsync(IEnumerable<TModel> models, Graph<TModel> graph);

        Expression<Func<TModel, bool>> GetWhereExpressionIncludingBase(Graph<TModel> graph);
    }

    public interface IQueryProviderRelation
    {
        void OnQueryIncludingBase(Graph graph);

        IEnumerable OnGetIncludingBase(IEnumerable models, Graph graph);
        Task<IEnumerable> OnGetIncludingBaseAsync(IEnumerable models, Graph graph);

        Expression GetWhereExpressionIncludingBase(Graph graph);
    }
}
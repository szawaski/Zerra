// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    public interface IProviderRelation
    {
        void OnQueryIncludingBase(Graph? graph);

        IEnumerable OnGetIncludingBase(IEnumerable models, Graph? graph);
        Task<IEnumerable> OnGetIncludingBaseAsync(IEnumerable models, Graph? graph);

        LambdaExpression? GetWhereExpressionIncludingBase(Graph? graph);
    }
}

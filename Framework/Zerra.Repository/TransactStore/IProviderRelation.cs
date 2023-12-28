// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Zerra.Repository
{
    public interface IProviderRelation
    {
        void OnQueryIncludingBase(Graph? graph);

        ICollection OnGetIncludingBase(ICollection models, Graph? graph);
        Task<ICollection> OnGetIncludingBaseAsync(ICollection models, Graph? graph);

        Expression? GetWhereExpressionIncludingBase(Graph? graph);
    }
}

// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository.Linq
{
    //a query's graph whose relations go through layers carries the condition each layer adds to its rows, by the related model and the relation's graph, so a query without layers carries nothing for it
    internal sealed class LinqLayeredGraph(Graph graph, Func<Type, Graph?, LambdaExpression?> relationWhere) : Graph(graph)
    {
        public readonly Func<Type, Graph?, LambdaExpression?> RelationWhere = relationWhere;
    }
}

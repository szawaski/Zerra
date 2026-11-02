// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// A query whose values a condition looks in, written into the statement when it runs on the same store and read first when it doesn't.
    /// </summary>
    internal sealed class LinqSubquery(IRepoInternal repo, LinqQuery query)
    {
        public static readonly MethodInfo ContainsMethod = typeof(LinqSubquery).GetMethod(nameof(Contains))!;

        public LinqQuery Query { get; } = query;

        public bool Contains(object? value) => Read().Contains(value!);

        public HashSet<object> Read() => Values((IEnumerable)repo.Query(Query)!);
        public async Task<HashSet<object>> ReadAsync(CancellationToken cancellationToken) => Values((IEnumerable)(await repo.QueryAsync(Query, cancellationToken))!);

        private static HashSet<object> Values(IEnumerable rows)
        {
            var values = new HashSet<object>();
            foreach (var value in rows)
            {
                if (value is not null)
                    values.Add(value);
            }
            return values;
        }

        //a query's Contains in a condition becomes a call on the subquery, so what follows sees one form whatever the type of the values
        public static LambdaExpression Rewrite(LambdaExpression condition, out bool found)
        {
            var rewriter = new Rewriter();
            var body = rewriter.Visit(condition.Body)!;
            found = rewriter.Found;
            return found ? Expression.Lambda(condition.Type, body, condition.Parameters) : condition;
        }

        private sealed class Rewriter : ExpressionVisitor
        {
            public bool Found;

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.Name != "Contains" || node.Method.DeclaringType != typeof(System.Linq.Queryable) || node.Arguments.Count != 2)
                    return base.VisitMethodCall(node);
                object? source;
                try
                {
                    source = Expression.Lambda<Func<object?>>(Expression.Convert(node.Arguments[0], typeof(object))).Compile()();
                }
                catch (InvalidOperationException ex)
                {
                    throw new NotSupportedException("A query a condition looks in is built from itself alone, not from the model", ex);
                }
                if (source is not IQueryable { Provider: RepoQueryProvider provider } queryable)
                    throw new NotSupportedException("A query a condition looks in comes from IRepo.Query");
                Found = true;
                return Expression.Call(Expression.Constant(provider.ReadSubquery(queryable.Expression)), ContainsMethod, Expression.Convert(Visit(node.Arguments[1])!, typeof(object)));
            }
        }
    }
}

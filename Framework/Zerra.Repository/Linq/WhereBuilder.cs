// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Linq
{
    /// <summary>
    /// Builds a composable LINQ where expression for <typeparamref name="TModel"/> by combining multiple predicates with AND/OR logic and grouping.
    /// </summary>
    /// <typeparam name="TModel">The model type to filter.</typeparam>
    public sealed partial class WhereBuilder<TModel>
        where TModel : class, new()
    {
        private readonly List<ExpressionStackItem> expressionStack = new();

        /// <summary>
        /// Initializes a new <see cref="WhereBuilder{TModel}"/> with an initial filter expression.
        /// </summary>
        /// <param name="expression">The initial predicate expression.</param>
        public WhereBuilder(Expression<Func<TModel, bool>> expression)
        {
            this.expressionStack.Add(new ExpressionStackItem(expression, false, false, false, false));
        }

        /// <summary>
        /// Appends a predicate combined with a logical AND.
        /// </summary>
        /// <param name="expression">The predicate expression to AND with the current expression.</param>
        public void And(Expression<Func<TModel, bool>> expression)
        {
            this.expressionStack.Add(new ExpressionStackItem(expression, true, false, false, false));
        }

        /// <summary>
        /// Appends a predicate combined with a logical OR.
        /// </summary>
        /// <param name="expression">The predicate expression to OR with the current expression.</param>
        public void Or(Expression<Func<TModel, bool>> expression)
        {
            this.expressionStack.Add(new ExpressionStackItem(expression, false, true, false, false));
        }

        /// <summary>
        /// Begins a grouped sub-expression combined with a logical AND.
        /// </summary>
        public void StartGroupAnd()
        {
            this.expressionStack.Add(new ExpressionStackItem(null, true, false, true, false));
        }

        /// <summary>
        /// Begins a grouped sub-expression combined with a logical OR.
        /// </summary>
        public void StartGroupOr()
        {
            this.expressionStack.Add(new ExpressionStackItem(null, false, true, true, false));
        }

        /// <summary>
        /// Ends the current grouped sub-expression.
        /// </summary>
        public void EndGroup()
        {
            this.expressionStack.Add(new ExpressionStackItem(null, false, false, false, true));
        }

        /// <summary>
        /// Returns a string representation of the built expression.
        /// </summary>
        /// <returns>A string representation of the composed predicate expression.</returns>
        public override string ToString()
        {
            var result = Build();
            return LinqStringConverter.Convert(result);
        }

        /// <summary>
        /// Builds the composed predicate expression from all added expressions and groups.
        /// </summary>
        /// <returns>A combined <see cref="Expression{TDelegate}"/> representing the full predicate.</returns>
        public Expression<Func<TModel, bool>> Build()
        {
            var parameter = Expression.Parameter(typeof(TModel), "$root");

            var i = 0;
            var exp = BuildGroup(ref i, parameter);
            exp ??= Expression.Constant(true, typeof(bool));

            var lambda = Expression.Lambda<Func<TModel, bool>>(exp, parameter);
            return lambda;
        }
        private Expression? BuildGroup(ref int i, ParameterExpression parameter)
        {
            Expression? exp = null;
            while (i < expressionStack.Count)
            {
                var item = expressionStack[i];
                if (item.EndGroup)
                {
                    break;
                }
                else if (exp is null)
                {
                    if (item.Expression is null)
                        throw new ArgumentException("Invalid Linq Where grouping structure");
                    exp = WhereBuilder<TModel>.Rebind(item.Expression, parameter);
                }
                else if (item.StartGroup)
                {
                    i++;
                    var group = BuildGroup(ref i, parameter);
                    if (group is not null)
                    {
                        if (item.And)
                        {
                            exp = Expression.AndAlso(exp, group);
                        }
                        else if (item.Or)
                        {
                            exp = Expression.OrElse(exp, group);
                        }
                    }
                }
                else if (item.And)
                {
                    exp = Expression.AndAlso(exp, WhereBuilder<TModel>.Rebind(item.Expression!, parameter));
                }
                else if (item.Or)
                {
                    exp = Expression.OrElse(exp, WhereBuilder<TModel>.Rebind(item.Expression!, parameter));
                }
                i++;
            }
            return exp;
        }
        private static Expression Rebind(Expression<Func<TModel, bool>> expression, ParameterExpression parameter)
        {
            return LinqRebinder.RebindExpression(expression.Body, expression.Parameters.First(), parameter);
        }
    }
}
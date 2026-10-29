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
        /// <returns>This builder, so calls can chain.</returns>
        public WhereBuilder<TModel> And(Expression<Func<TModel, bool>> expression)
        {
            this.expressionStack.Add(new ExpressionStackItem(expression, true, false, false, false));
            return this;
        }

        /// <summary>
        /// Appends a predicate combined with a logical OR.
        /// </summary>
        /// <param name="expression">The predicate expression to OR with the current expression.</param>
        /// <returns>This builder, so calls can chain.</returns>
        public WhereBuilder<TModel> Or(Expression<Func<TModel, bool>> expression)
        {
            this.expressionStack.Add(new ExpressionStackItem(expression, false, true, false, false));
            return this;
        }

        /// <summary>
        /// Begins a grouped sub-expression combined with a logical AND.
        /// </summary>
        /// <returns>This builder, so calls can chain.</returns>
        public WhereBuilder<TModel> StartGroupAnd()
        {
            this.expressionStack.Add(new ExpressionStackItem(null, true, false, true, false));
            return this;
        }

        /// <summary>
        /// Begins a grouped sub-expression combined with a logical OR.
        /// </summary>
        /// <returns>This builder, so calls can chain.</returns>
        public WhereBuilder<TModel> StartGroupOr()
        {
            this.expressionStack.Add(new ExpressionStackItem(null, false, true, true, false));
            return this;
        }

        /// <summary>
        /// Ends the current grouped sub-expression.
        /// </summary>
        /// <returns>This builder, so calls can chain.</returns>
        public WhereBuilder<TModel> EndGroup()
        {
            this.expressionStack.Add(new ExpressionStackItem(null, false, false, false, true));
            return this;
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
        //And binds tighter than Or as in C#, so the items are an Or of And runs, with a group as one term; the first item's And or Or has nothing before it to join
        private Expression? BuildGroup(ref int i, ParameterExpression parameter)
        {
            Expression? ored = null;
            Expression? run = null;
            while (i < expressionStack.Count)
            {
                var item = expressionStack[i];
                if (item.EndGroup)
                    break;

                Expression? term;
                if (item.StartGroup)
                {
                    i++;
                    term = BuildGroup(ref i, parameter);
                }
                else
                {
                    if (item.Expression is null)
                        throw new ArgumentException("Invalid Linq Where grouping structure");
                    term = WhereBuilder<TModel>.Rebind(item.Expression, parameter);
                }

                if (term is not null)
                {
                    if (run is null)
                    {
                        run = term;
                    }
                    else if (item.Or)
                    {
                        ored = ored is null ? run : Expression.OrElse(ored, run);
                        run = term;
                    }
                    else
                    {
                        run = Expression.AndAlso(run, term);
                    }
                }
                i++;
            }
            if (run is null)
                return ored;
            return ored is null ? run : Expression.OrElse(ored, run);
        }
        private static Expression Rebind(Expression<Func<TModel, bool>> expression, ParameterExpression parameter)
        {
            return LinqRebinder.RebindExpression(expression.Body, expression.Parameters.First(), parameter);
        }
    }
}
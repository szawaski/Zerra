// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using Zerra.Linq;
using Zerra.Repository.IO;
using Zerra.Reflection;
using Zerra.Repository.Linq;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.PostgreSql
{
    /// <summary>
    /// Converts LINQ expressions into PostgreSQL query strings.
    /// </summary>
    public sealed class LinqPostgreSqlConverter : BaseLinqSqlConverter
    {
        private static readonly LinqPostgreSqlConverter instance = new();
        /// <summary>Converts a LINQ query to its statements in one pass: the statement over its model and, nested, one per relation read after the rows, each selecting the rows of every parent the parent's statement selects.</summary>
        /// <param name="query">The query.</param>
        /// <param name="modelDetail">Metadata about the model being queried.</param>
        /// <returns>The statement, the plan of its rows, and the statements of the relations.</returns>
        public static LinqStatement ConvertLinq(LinqQuery query, ModelDetail modelDetail)
        {
            return instance.ConvertLinqInternal(query, modelDetail);
        }
        /// <summary>Converts the condition of an update or delete to its SQL, starting with WHERE, for the engine to append to its UPDATE or DELETE statement.</summary>
        /// <param name="where">The condition selecting the rows.</param>
        /// <param name="modelDetail">Metadata about the model being changed.</param>
        /// <returns>The SQL from WHERE on.</returns>
        public static string ConvertPersistWhere(LambdaExpression where, ModelDetail modelDetail)
        {
            return instance.ConvertPersistWhereInternal(where, modelDetail);
        }
        /// <summary>
        /// Converts an update of every row matching a condition to values computed from the row into its SQL statement.
        /// </summary>
        /// <param name="sets">The members to set from <see cref="ProjectionMember.FromSet"/>, each with its value in terms of the row.</param>
        /// <param name="where">The condition selecting the rows.</param>
        /// <param name="modelDetail">Metadata about the model being changed.</param>
        /// <returns>The SQL.</returns>
        public static string ConvertPersistUpdate(ProjectionMember[] sets, LambdaExpression where, ModelDetail modelDetail)
        {
            return instance.ConvertPersistUpdateInternal(sets, where, modelDetail);
        }

        /// <inheritdoc/>
        protected override void ConvertToSqlLambda(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            context.MemberContext.OperatorStack.Push(Operator.Lambda);

            var lambda = (LambdaExpression)exp;

            if (lambda.Parameters.Count != 1)
                throw new NotSupportedException("Can only parse a lambda with one parameter.");
            var parameter = lambda.Parameters[0];
            if (parameter.Name is null)
                throw new Exception($"Parameter has no name {parameter.Type.Name}");

            var modelDetail = ModelAnalyzer.GetModel(parameter.Type);

            if (context.RootDependant.ModelDetail.Type != modelDetail.Type)
                throw new Exception($"Lambda type {modelDetail.Type.Name} does not match the root type {context.RootDependant.ModelDetail.Type.Name}");

            if (context.MemberContext.ModelStack.Count > 0)
            {
                var callingModel = context.MemberContext.ModelStack.Peek();
                var property = context.MemberContext.MemberLambdaStack.Peek();

                var modelProperty = callingModel.GetMember(property.Member.Name);

                sb.Write(' ');

                WriteRelationKey(ref sb, modelDetail.DataSourceEntityName, CallingTable(property, callingModel, context), RelationKey(modelProperty, callingModel));

                sb.Write(" AND");
            }

            //All writes the related condition inverted to find a related row that fails it
            var invertBody = context.MemberContext.InvertRelatedLambda && context.MemberContext.ModelStack.Count > 0;
            if (invertBody)
                context.MemberContext.InvertRelatedLambda = false;

            context.MemberContext.DependantStack.Push(context.RootDependant);
            context.MemberContext.ModelStack.Push(modelDetail);
            context.MemberContext.ModelContexts.Add(parameter, modelDetail);

            sb.Write('(');
            if (invertBody)
                context.InvertStack++;
            ConvertToSql(lambda.Body, ref sb, context);
            if (invertBody)
                context.InvertStack--;
            sb.Write(')');

            _ = context.MemberContext.DependantStack.Pop();
            _ = context.MemberContext.ModelStack.Pop();
            _ = context.MemberContext.ModelContexts.Remove(parameter);

            _ = context.MemberContext.OperatorStack.Pop();
        }
        /// <inheritdoc/>
        protected override void ConvertToSqlCall(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            var call = (MethodCallExpression)exp;

            if (IsEvaluatable(exp))
            {
                ConvertToSqlEvaluate(exp, ref sb, context);
                return;
            }

            var lastOperator = context.MemberContext.OperatorStack.Peek();
            context.MemberContext.OperatorStack.Push(Operator.Call);

            if (call.Type == typeof(bool) && lastOperator != Operator.And && lastOperator != Operator.Or && lastOperator != Operator.Not && (lastOperator != Operator.Lambda || context.IsOrderBy))
            {
                //a condition used as a value such as x.Name.StartsWith("a") == false
                var invertStack = context.InvertStack;
                context.InvertStack = 0;
                sb.Write('(');
                ConvertToSqlCallMethod(call, ref sb, context);
                sb.Write(')');
                context.InvertStack = invertStack;
            }
            else
            {
                ConvertToSqlCallMethod(call, ref sb, context);
            }

            _ = context.MemberContext.OperatorStack.Pop();
        }
        private void ConvertToSqlCallMethod(MethodCallExpression call, ref CharWriter sb, BuilderContext context)
        {
            if (TryConvertToSqlFunction(call, ref sb, context))
                return;
            if (call.Method.DeclaringType is null)
                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

            if (call.Method.DeclaringType == typeof(Enumerable) || call.Method.DeclaringType == typeof(MemoryExtensions) || call.Method.DeclaringType == typeof(Queryable))
            {
                switch (call.Method.Name)
                {
                    case "All":
                        {
                            if (call.Arguments.Count != 2)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            var subWhereArgument = call.Arguments[1];
                            if (subWhereArgument.NodeType == ExpressionType.Quote)
                                subWhereArgument = ((UnaryExpression)subWhereArgument).Operand;
                            LambdaExpression? subWhere = (LambdaExpression)subWhereArgument;
                            if (UnwrapWhere(call.Arguments[0], ref subWhere, true) is not MemberExpression subMember || subMember.Expression is null)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            //all related rows match when there isn't a related row that fails the condition
                            if (!context.Inverted)
                                sb.Write("NOT ");

                            var subMemberModel = ModelAnalyzer.GetModel(subMember.Expression.Type);
                            var propertyInfo = subMemberModel.GetMember(subMember.Member.Name);
                            if (propertyInfo.ForeignIdentities is null)
                                throw new Exception($"{propertyInfo.Type.Name} missing Foreign Identity");

                            var subModelInfo = ModelAnalyzer.GetModel(propertyInfo.ActualType);

                            //the parent's identity is looked for among the related keys, a key of several members is a related row that exists with the match and the condition
                            var severalKeys = propertyInfo.ForeignIdentities!.Length > 1;
                            if (severalKeys)
                                context.MemberContext.InCallNoRender++;
                            else
                                context.MemberContext.InCallRenderIdentity++;
                            ConvertToSqlMember(subMember, ref sb, context);
                            if (severalKeys)
                                context.MemberContext.InCallNoRender--;
                            else
                                context.MemberContext.InCallRenderIdentity--;

                            (context.MemberContext.CallingTables ??= new())[subMember] = RelationTable(subMember.Expression!, context);

                            context.MemberContext.MemberLambdaStack.Push(subMember);
                            context.MemberContext.ModelStack.Push(subMemberModel);

                            sb.Write(severalKeys ? "EXISTS(" : "=ANY(");
                            context.MemberContext.InvertRelatedLambda = true;
                            ConvertSubquery(ref sb, false, severalKeys ? subWhere ?? Expression.Lambda(subModelInfo.LambdaDelegateType, Expression.Constant(true), Expression.Parameter(subModelInfo.Type, "x")) : subWhere, new Graph(propertyInfo.ForeignIdentities!), subModelInfo, context.MemberContext);
                            sb.Write(')');

                            _ = context.MemberContext.ModelStack.Pop();
                            _ = context.MemberContext.MemberLambdaStack.Pop();
                            break;
                        }
                    case "Any":
                        {
                            if (call.Arguments.Count != 1 && call.Arguments.Count != 2)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            LambdaExpression? subWhere = null;
                            if (call.Arguments.Count > 1)
                            {
                                var subWhereArgument = call.Arguments[1];
                                if (subWhereArgument.NodeType == ExpressionType.Quote)
                                    subWhereArgument = ((UnaryExpression)subWhereArgument).Operand;
                                subWhere = (LambdaExpression)subWhereArgument;
                            }
                            if (UnwrapWhere(call.Arguments[0], ref subWhere, false) is not MemberExpression subMember || subMember.Expression is null)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            if (context.Inverted)
                                sb.Write("NOT ");

                            var subMemberModel = ModelAnalyzer.GetModel(subMember.Expression.Type);
                            var propertyInfo = subMemberModel.GetMember(subMember.Member.Name);
                            if (propertyInfo.ForeignIdentities is null)
                                throw new Exception($"{propertyInfo.Type.Name} missing Foreign Identity");

                            var subModelInfo = ModelAnalyzer.GetModel(propertyInfo.ActualType);

                            //the parent's identity is looked for among the related keys, a key of several members is a related row that exists with the match and the condition
                            var severalKeys = propertyInfo.ForeignIdentities!.Length > 1;
                            if (severalKeys)
                                context.MemberContext.InCallNoRender++;
                            else
                                context.MemberContext.InCallRenderIdentity++;
                            ConvertToSqlMember(subMember, ref sb, context);
                            if (severalKeys)
                                context.MemberContext.InCallNoRender--;
                            else
                                context.MemberContext.InCallRenderIdentity--;

                            (context.MemberContext.CallingTables ??= new())[subMember] = RelationTable(subMember.Expression!, context);

                            context.MemberContext.MemberLambdaStack.Push(subMember);
                            context.MemberContext.ModelStack.Push(subMemberModel);

                            sb.Write(severalKeys ? "EXISTS(" : "=ANY(");

                            ConvertSubquery(ref sb, false, severalKeys ? subWhere ?? Expression.Lambda(subModelInfo.LambdaDelegateType, Expression.Constant(true), Expression.Parameter(subModelInfo.Type, "x")) : subWhere, new Graph(propertyInfo.ForeignIdentities!), subModelInfo, context.MemberContext);
                            sb.Write(')');

                            _ = context.MemberContext.ModelStack.Pop();
                            _ = context.MemberContext.MemberLambdaStack.Pop();
                            break;
                        }
                    case "Count":
                    case "LongCount":
                        {
                            if (call.Arguments.Count != 1 && call.Arguments.Count != 2)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            LambdaExpression? subWhere = null;
                            if (call.Arguments.Count > 1)
                            {
                                var subWhereArgument = call.Arguments[1];
                                if (subWhereArgument.NodeType == ExpressionType.Quote)
                                    subWhereArgument = ((UnaryExpression)subWhereArgument).Operand;
                                subWhere = (LambdaExpression)subWhereArgument;
                            }
                            if (UnwrapWhere(call.Arguments[0], ref subWhere, false) is not MemberExpression subMember || subMember.Expression is null)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            context.MemberContext.InCallNoRender++;
                            ConvertToSqlMember(subMember, ref sb, context);
                            context.MemberContext.InCallNoRender--;
                            var subMemberTable = RelationTable(subMember.Expression!, context);

                            var subMemberModel = ModelAnalyzer.GetModel(subMember.Expression.Type);
                            var propertyInfo = subMemberModel.GetMember(subMember.Member.Name);
                            if (propertyInfo.ForeignIdentities is null)
                                throw new Exception($"{propertyInfo.Type.Name} missing Foreign Identity");

                            var subModelInfo = ModelAnalyzer.GetModel(propertyInfo.ActualType);

                            if (subWhere is null)
                            {
                                //without a condition there is no lambda to write the match to the related rows

                                sb.Write("(SELECT COUNT(1)");
                                GenerateFrom(subModelInfo, ref sb);
                                sb.Write("WHERE ");
                                WriteRelationKey(ref sb, subModelInfo.DataSourceEntityName, subMemberTable, RelationKey(propertyInfo, subMemberModel));
                                sb.Write(')');
                                break;
                            }

                            (context.MemberContext.CallingTables ??= new())[subMember] = subMemberTable;

                            context.MemberContext.MemberLambdaStack.Push(subMember);
                            context.MemberContext.ModelStack.Push(subMemberModel);

                            sb.Write('(');
                            ConvertSubquery(ref sb, true, subWhere, new Graph(propertyInfo.ForeignIdentities!), subModelInfo, context.MemberContext);
                            sb.Write(')');

                            _ = context.MemberContext.ModelStack.Pop();
                            _ = context.MemberContext.MemberLambdaStack.Pop();
                            break;
                        }
                    case "Sum":
                    case "Min":
                    case "Max":
                    case "Average":
                        {
                            if (call.Arguments.Count != 2)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            LambdaExpression? subWhere = null;
                            if (UnwrapWhere(call.Arguments[0], ref subWhere, false) is not MemberExpression subMember || subMember.Expression is null)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            var selectorArgument = call.Arguments[1];
                            if (selectorArgument.NodeType == ExpressionType.Quote)
                                selectorArgument = ((UnaryExpression)selectorArgument).Operand;
                            if (selectorArgument is not LambdaExpression selector || selector.Parameters.Count != 1)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            var subMemberModel = ModelAnalyzer.GetModel(subMember.Expression.Type);
                            var propertyInfo = subMemberModel.GetMember(subMember.Member.Name);
                            if (propertyInfo.ForeignIdentities is null)
                                throw new Exception($"{propertyInfo.Type.Name} missing Foreign Identity");

                            var subModelInfo = ModelAnalyzer.GetModel(propertyInfo.ActualType);

                            context.MemberContext.InCallNoRender++;
                            ConvertToSqlMember(subMember, ref sb, context);
                            context.MemberContext.InCallNoRender--;
                            var subMemberTable = RelationTable(subMember.Expression!, context);

                            //the selector is written first so the joins it needs are known before the FROM
                            var subDependant = new ParameterDependant(subModelInfo, null);
                            var sbSelector = new CharWriter();
                            var sbWhere = new CharWriter();
                            try
                            {
                                var selectorParameter = selector.Parameters[0];
                                context.MemberContext.DependantStack.Push(subDependant);
                                context.MemberContext.ModelStack.Push(subModelInfo);
                                context.MemberContext.ModelContexts.Add(selectorParameter, subModelInfo);

                                ConvertToSql(selector.Body, ref sbSelector, new BuilderContext(subDependant, context.MemberContext));

                                _ = context.MemberContext.ModelContexts.Remove(selectorParameter);

                                if (subWhere is not null)
                                {
                                    context.MemberContext.OperatorStack.Push(Operator.Lambda);
                                    context.MemberContext.ModelContexts.Add(subWhere.Parameters[0], subModelInfo);
                                    ConvertToSql(subWhere.Body, ref sbWhere, new BuilderContext(subDependant, context.MemberContext));
                                    _ = context.MemberContext.ModelContexts.Remove(subWhere.Parameters[0]);
                                    _ = context.MemberContext.OperatorStack.Pop();
                                }
                                _ = context.MemberContext.ModelStack.Pop();
                                _ = context.MemberContext.DependantStack.Pop();

                                sb.Write("(SELECT ");
                                switch (call.Method.Name)
                                {
                                    case "Sum":
                                        //SUM of no rows is NULL where the LINQ sum is 0
                                        sb.Write("COALESCE(SUM(");
                                        sb.Write(sbSelector);
                                        sb.Write("),0)");
                                        break;
                                    case "Min":
                                        sb.Write("MIN(");
                                        sb.Write(sbSelector);
                                        sb.Write(')');
                                        break;
                                    case "Max":
                                        sb.Write("MAX(");
                                        sb.Write(sbSelector);
                                        sb.Write(')');
                                        break;
                                    case "Average":
                                        sb.Write("AVG(");
                                        sb.Write(sbSelector);
                                        sb.Write(')');
                                        break;
                                }
                                GenerateFrom(subModelInfo, ref sb);
                                GenerateJoin(subDependant, ref sb);
                                sb.Write("WHERE ");
                                WriteRelationKey(ref sb, subModelInfo.DataSourceEntityName, subMemberTable, RelationKey(propertyInfo, subMemberModel));
                                if (sbWhere.Length > 0)
                                {
                                    sb.Write(" AND(");
                                    sb.Write(sbWhere);
                                    sb.Write(')');
                                }
                                sb.Write(')');
                            }
                            finally
                            {
                                sbSelector.Dispose();
                                sbWhere.Dispose();
                            }
                            break;
                        }
                    case "First":
                    case "FirstOrDefault":
                    case "Single":
                    case "SingleOrDefault":
                    case "Last":
                    case "LastOrDefault":
                    case "ElementAt":
                    case "ElementAtOrDefault":
                        {
                            //a member of one related row, read after the call: a subquery of one row over the related rows filtered, ordered, and paged by the calls before it
                            if (context.MemberContext.MemberAccessStack.Count == 0)
                                throw new NotSupportedException($"{call.Method.Name} of a related collection reads a member of the row, such as .{call.Method.Name}().Name");

                            var last = call.Method.Name is "Last" or "LastOrDefault";
                            int? skip = null;
                            LambdaExpression? subWhere = null;
                            var orders = new List<(LambdaExpression Expression, bool Descending)>();
                            if (call.Method.Name is "ElementAt" or "ElementAtOrDefault")
                            {
                                if (call.Arguments.Count != 2)
                                    throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
                                skip = (int)Evaluate(call.Arguments[1])!;
                            }
                            else if (call.Arguments.Count == 2)
                            {
                                var predicateArgument = call.Arguments[1];
                                if (predicateArgument.NodeType == ExpressionType.Quote)
                                    predicateArgument = ((UnaryExpression)predicateArgument).Operand;
                                subWhere = (LambdaExpression)predicateArgument;
                            }
                            else if (call.Arguments.Count != 1)
                            {
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
                            }

                            var source = call.Arguments[0];
                            while (source is MethodCallExpression chain && (chain.Method.DeclaringType == typeof(Enumerable) || chain.Method.DeclaringType == typeof(Queryable)) && chain.Arguments.Count == 2)
                            {
                                var argument = chain.Arguments[1];
                                if (argument.NodeType == ExpressionType.Quote)
                                    argument = ((UnaryExpression)argument).Operand;
                                switch (chain.Method.Name)
                                {
                                    case "Where":
                                        {
                                            var where = (LambdaExpression)argument;
                                            subWhere = subWhere is null ? where : Expression.Lambda(where.Type, Expression.AndAlso(where.Body, LinqRebinder.RebindExpression(subWhere.Body, subWhere.Parameters[0], where.Parameters[0])), where.Parameters);
                                            break;
                                        }
                                    case "OrderBy":
                                    case "ThenBy":
                                        orders.Insert(0, ((LambdaExpression)argument, false));
                                        break;
                                    case "OrderByDescending":
                                    case "ThenByDescending":
                                        orders.Insert(0, ((LambdaExpression)argument, true));
                                        break;
                                    case "Skip":
                                        skip = (skip ?? 0) + (int)Evaluate(chain.Arguments[1])!;
                                        break;
                                    default:
                                        throw new NotSupportedException($"{chain.Method.Name} before {call.Method.Name} of a related collection is not supported");
                                }
                                source = chain.Arguments[0];
                            }
                            if (source is not MemberExpression subMember || subMember.Expression is null)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
                            if (last)
                            {
                                //the last row is the first in the reverse order
                                if (orders.Count == 0)
                                    throw new NotSupportedException($"{call.Method.Name} of a related collection needs an order");
                                for (var i = 0; i < orders.Count; i++)
                                    orders[i] = (orders[i].Expression, !orders[i].Descending);
                            }

                            var subMemberModel = ModelAnalyzer.GetModel(subMember.Expression.Type);
                            var propertyInfo = subMemberModel.GetMember(subMember.Member.Name);
                            if (propertyInfo.ForeignIdentities is null)
                                throw new Exception($"{propertyInfo.Type.Name} missing Foreign Identity");

                            var subModelInfo = ModelAnalyzer.GetModel(propertyInfo.ActualType);

                            context.MemberContext.InCallNoRender++;
                            ConvertToSqlMember(subMember, ref sb, context);
                            context.MemberContext.InCallNoRender--;
                            var subMemberTable = RelationTable(subMember.Expression!, context);

                            //the member read from the row is the pending member accesses over a parameter for the row, written with the condition and the order first so the joins they need are known before the FROM
                            var accesses = new MemberExpression[context.MemberContext.MemberAccessStack.Count];
                            for (var i = 0; i < accesses.Length; i++)
                                accesses[i] = context.MemberContext.MemberAccessStack.Pop();
                            var rowParameter = Expression.Parameter(subModelInfo.Type, "row");
                            Expression selector = rowParameter;
                            for (var i = 0; i < accesses.Length; i++)
                                selector = Expression.MakeMemberAccess(selector, accesses[i].Member);

                            var subDependant = new ParameterDependant(subModelInfo, null);
                            var sbSelector = new CharWriter();
                            var sbWhere = new CharWriter();
                            var sbOrder = new CharWriter();
                            try
                            {
                                context.MemberContext.DependantStack.Push(subDependant);
                                context.MemberContext.ModelStack.Push(subModelInfo);
                                context.MemberContext.OperatorStack.Push(Operator.Lambda);

                                context.MemberContext.ModelContexts.Add(rowParameter, subModelInfo);
                                ConvertToSql(selector, ref sbSelector, new BuilderContext(subDependant, context.MemberContext) { IsOrderBy = true });
                                _ = context.MemberContext.ModelContexts.Remove(rowParameter);

                                if (subWhere is not null)
                                {
                                    context.MemberContext.ModelContexts.Add(subWhere.Parameters[0], subModelInfo);
                                    sbWhere.Write('(');
                                    ConvertToSql(subWhere.Body, ref sbWhere, new BuilderContext(subDependant, context.MemberContext));
                                    sbWhere.Write(')');
                                    _ = context.MemberContext.ModelContexts.Remove(subWhere.Parameters[0]);
                                }

                                foreach (var (orderExpression, descending) in orders)
                                {
                                    if (sbOrder.Length > 0)
                                        sbOrder.Write(',');
                                    context.MemberContext.ModelContexts.Add(orderExpression.Parameters[0], subModelInfo);
                                    sbOrder.Write('(');
                                    ConvertToSql(orderExpression.Body, ref sbOrder, new BuilderContext(subDependant, context.MemberContext) { IsOrderBy = true, IsSort = true });
                                    sbOrder.Write(')');
                                    _ = context.MemberContext.ModelContexts.Remove(orderExpression.Parameters[0]);
                                    if (descending)
                                        sbOrder.Write("DESC");
                                }

                                _ = context.MemberContext.OperatorStack.Pop();
                                _ = context.MemberContext.ModelStack.Pop();
                                _ = context.MemberContext.DependantStack.Pop();

                                sb.Write("(SELECT ");
                                sb.Write(sbSelector);
                                GenerateFrom(subModelInfo, ref sb);
                                GenerateJoin(subDependant, ref sb);
                                sb.Write("WHERE ");
                                WriteRelationKey(ref sb, subModelInfo.DataSourceEntityName, subMemberTable, RelationKey(propertyInfo, subMemberModel));
                                if (sbWhere.Length > 0)
                                {
                                    sb.Write(" AND");
                                    sb.Write(sbWhere);
                                }
                                if (sbOrder.Length > 0)
                                {
                                    sb.Write(" ORDER BY");
                                    sb.Write(sbOrder);
                                }
                                sb.Write(" LIMIT 1");
                                if (skip.HasValue)
                                {
                                    sb.Write(" OFFSET ");
                                    sb.Write(skip.Value);
                                }
                                sb.Write(')');
                            }
                            finally
                            {
                                sbSelector.Dispose();
                                sbWhere.Dispose();
                                sbOrder.Dispose();
                                for (var i = accesses.Length - 1; i >= 0; i--)
                                    context.MemberContext.MemberAccessStack.Push(accesses[i]);
                            }
                            break;
                        }
                    case "Contains":
                        {
                            //C# 14 calls an enum's with a comparer of null, a type without IEquatable
                            if (call.Arguments.Count != 2 && (call.Arguments.Count != 3 || !IsNull(call.Arguments[2])))
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            var callingObject = call.Arguments[0];
                            var lambda = call.Arguments[1];

                            ConvertToSql(lambda, ref sb, context);

                            sb.Write(context.Inverted ? " NOT IN " : " IN ");

                            ConvertToSql(EnumList(callingObject, lambda, context), ref sb, context);
                            break;
                        }
                    default:
                        throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
                }
            }
            else if (call.Method.DeclaringType == typeof(string))
            {
                switch (call.Method.Name)
                {
                    case "Contains":
                    case "StartsWith":
                    case "EndsWith":
                        {
                            if ((call.Arguments.Count != 1 && call.Arguments.Count != 2) || call.Object is null)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            var ignoreCase = false;
                            if (call.Arguments.Count == 2)
                            {
                                if (call.Arguments[1].Type != typeof(StringComparison) || !IsEvaluatable(call.Arguments[1]))
                                    throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
                                var comparison = (StringComparison)Evaluate(call.Arguments[1])!;
                                ignoreCase = comparison == StringComparison.OrdinalIgnoreCase || comparison == StringComparison.CurrentCultureIgnoreCase || comparison == StringComparison.InvariantCultureIgnoreCase;
                            }

                            var text = call.Arguments[0];
                            var anyStart = call.Method.Name != "StartsWith";
                            var anyEnd = call.Method.Name != "EndsWith";

                            var inverted = context.Inverted;
                            var invertStack = context.InvertStack;
                            context.InvertStack = 0;

                            ConvertToSql(call.Object, ref sb, context);

                            if (ignoreCase)
                                sb.Write(inverted ? " NOT ILIKE " : " ILIKE ");
                            else
                                sb.Write(inverted ? " NOT LIKE " : " LIKE ");

                            //wildcard characters in the text are escaped so they match themselves
                            if (IsEvaluatable(text))
                            {
                                var value = Evaluate(text);
                                if (value is null)
                                {
                                    sb.Write("NULL");
                                }
                                else
                                {
                                    var valueText = value is char c ? c.ToString() : (string)value;
                                    valueText = valueText.Replace("!", "!!").Replace("%", "!%").Replace("_", "!_").Replace("'", "''");
                                    sb.Write('\'');
                                    if (anyStart)
                                        sb.Write('%');
                                    sb.Write(valueText);
                                    if (anyEnd)
                                        sb.Write('%');
                                    sb.Write('\'');
                                }
                            }
                            else
                            {
                                if (anyStart)
                                    sb.Write("'%'||");
                                sb.Write("REPLACE(REPLACE(REPLACE(");
                                ConvertToSql(text, ref sb, context);
                                sb.Write(",'!','!!'),'%','!%'),'_','!_')");
                                if (anyEnd)
                                    sb.Write("||'%'");
                            }

                            sb.Write(" ESCAPE '!'");

                            context.InvertStack = invertStack;
                            break;
                        }
                    case "Equals":
                        {
                            //instance a.Equals(b) or a.Equals(b, comparison), static string.Equals(a, b) or string.Equals(a, b, comparison)
                            Expression left;
                            Expression right;
                            Expression? comparisonArgument;
                            if (call.Object is not null)
                            {
                                if (call.Arguments.Count != 1 && call.Arguments.Count != 2)
                                    throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
                                left = call.Object;
                                right = call.Arguments[0];
                                comparisonArgument = call.Arguments.Count == 2 ? call.Arguments[1] : null;
                            }
                            else
                            {
                                if (call.Arguments.Count != 2 && call.Arguments.Count != 3)
                                    throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
                                left = call.Arguments[0];
                                right = call.Arguments[1];
                                comparisonArgument = call.Arguments.Count == 3 ? call.Arguments[2] : null;
                            }

                            var ignoreCase = false;
                            if (comparisonArgument is not null)
                            {
                                if (comparisonArgument.Type != typeof(StringComparison) || !IsEvaluatable(comparisonArgument))
                                    throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
                                var comparison = (StringComparison)Evaluate(comparisonArgument)!;
                                ignoreCase = comparison == StringComparison.OrdinalIgnoreCase || comparison == StringComparison.CurrentCultureIgnoreCase || comparison == StringComparison.InvariantCultureIgnoreCase;
                            }

                            var inverted = context.Inverted;
                            var invertStack = context.InvertStack;
                            context.InvertStack = 0;

                            if (IsNull(right) || IsNull(left))
                            {
                                if (IsNull(left))
                                    left = right;
                                ConvertToSql(left, ref sb, context);
                                sb.Write(inverted ? " IS NOT NULL" : " IS NULL");
                            }
                            else
                            {
                                sb.Write(ignoreCase ? "LOWER(" : "(");
                                ConvertToSql(left, ref sb, context);
                                sb.Write(inverted ? ")!=" : ")=");
                                sb.Write(ignoreCase ? "LOWER(" : "(");
                                ConvertToSql(right, ref sb, context);
                                sb.Write(')');
                            }

                            context.InvertStack = invertStack;
                            break;
                        }
                    case "IsNullOrEmpty":
                    case "IsNullOrWhiteSpace":
                        {
                            if (call.Arguments.Count != 1)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            var inverted = context.Inverted;
                            var invertStack = context.InvertStack;
                            context.InvertStack = 0;

                            if (call.Method.Name == "IsNullOrEmpty")
                            {
                                sb.Write("COALESCE(");
                                ConvertToSql(call.Arguments[0], ref sb, context);
                                sb.Write(",'')");
                            }
                            else
                            {
                                sb.Write("COALESCE(TRIM(");
                                ConvertToSql(call.Arguments[0], ref sb, context);
                                sb.Write("),'')");
                            }
                            sb.Write(inverted ? "!=''" : "=''");

                            context.InvertStack = invertStack;
                            break;
                        }
                    case "ToUpper":
                    case "ToUpperInvariant":
                    case "ToLower":
                    case "ToLowerInvariant":
                    case "Trim":
                    case "TrimStart":
                    case "TrimEnd":
                        {
                            if (call.Arguments.Count != 0 || call.Object is null)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name} with arguments");

                            var invertStack = context.InvertStack;
                            context.InvertStack = 0;

                            switch (call.Method.Name)
                            {
                                case "ToUpper":
                                case "ToUpperInvariant":
                                    sb.Write("UPPER(");
                                    ConvertToSql(call.Object, ref sb, context);
                                    sb.Write(')');
                                    break;
                                case "ToLower":
                                case "ToLowerInvariant":
                                    sb.Write("LOWER(");
                                    ConvertToSql(call.Object, ref sb, context);
                                    sb.Write(')');
                                    break;
                                case "Trim":
                                    sb.Write("TRIM(");
                                    ConvertToSql(call.Object, ref sb, context);
                                    sb.Write(')');
                                    break;
                                case "TrimStart":
                                    sb.Write("LTRIM(");
                                    ConvertToSql(call.Object, ref sb, context);
                                    sb.Write(')');
                                    break;
                                case "TrimEnd":
                                    sb.Write("RTRIM(");
                                    ConvertToSql(call.Object, ref sb, context);
                                    sb.Write(')');
                                    break;
                            }

                            context.InvertStack = invertStack;
                            break;
                        }
                    case "Substring":
                        {
                            if ((call.Arguments.Count != 1 && call.Arguments.Count != 2) || call.Object is null)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            var invertStack = context.InvertStack;
                            context.InvertStack = 0;

                            //SQL positions start at 1
                            sb.Write("SUBSTRING(");
                            ConvertToSql(call.Object, ref sb, context);
                            sb.Write(" FROM (");
                            ConvertToSql(call.Arguments[0], ref sb, context);
                            sb.Write(")+1");
                            if (call.Arguments.Count == 2)
                            {
                                sb.Write(" FOR (");
                                ConvertToSql(call.Arguments[1], ref sb, context);
                                sb.Write(')');
                            }
                            sb.Write(')');

                            context.InvertStack = invertStack;
                            break;
                        }
                    case "IndexOf":
                        {
                            if (call.Arguments.Count != 1 || call.Object is null)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            var invertStack = context.InvertStack;
                            context.InvertStack = 0;

                            //SQL positions start at 1 and not found is 0
                            sb.Write("(STRPOS(");
                            ConvertToSql(call.Object, ref sb, context);
                            sb.Write(',');
                            ConvertToSql(call.Arguments[0], ref sb, context);
                            sb.Write(")-1)");

                            context.InvertStack = invertStack;
                            break;
                        }
                    case "Replace":
                        {
                            if (call.Arguments.Count != 2 || call.Object is null)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            var invertStack = context.InvertStack;
                            context.InvertStack = 0;

                            sb.Write("REPLACE(");
                            ConvertToSql(call.Object, ref sb, context);
                            sb.Write(',');
                            ConvertToSql(call.Arguments[0], ref sb, context);
                            sb.Write(',');
                            ConvertToSql(call.Arguments[1], ref sb, context);
                            sb.Write(')');

                            context.InvertStack = invertStack;
                            break;
                        }
                    case "Concat":
                        {
                            var invertStack = context.InvertStack;
                            context.InvertStack = 0;

                            //CONCAT treats NULL as empty like string.Concat
                            IReadOnlyList<Expression> values = call.Arguments.Count == 1 && call.Arguments[0] is NewArrayExpression valuesArray ? valuesArray.Expressions : call.Arguments;
                            sb.Write("CONCAT(");
                            for (var i = 0; i < values.Count; i++)
                            {
                                if (i > 0)
                                    sb.Write(',');
                                sb.Write('(');
                                ConvertToSql(values[i], ref sb, context);
                                sb.Write(')');
                            }
                            sb.Write(')');

                            context.InvertStack = invertStack;
                            break;
                        }
                    default:
                        throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
                }
            }
            else if (call.Method.DeclaringType == typeof(Math))
            {
                var invertStack = context.InvertStack;
                context.InvertStack = 0;

                switch (call.Method.Name)
                {
                    case "Abs":
                    case "Ceiling":
                    case "Floor":
                    case "Sqrt":
                        {
                            if (call.Arguments.Count != 1)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
                            sb.Write(call.Method.Name switch
                            {
                                "Abs" => "ABS((",
                                "Ceiling" => "CEILING((",
                                "Floor" => "FLOOR((",
                                _ => "SQRT((",
                            });
                            ConvertToSql(call.Arguments[0], ref sb, context);
                            sb.Write("))");
                            break;
                        }
                    case "Pow":
                        {
                            if (call.Arguments.Count != 2)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
                            sb.Write("POWER((");
                            ConvertToSql(call.Arguments[0], ref sb, context);
                            sb.Write("),(");
                            ConvertToSql(call.Arguments[1], ref sb, context);
                            sb.Write("))");
                            break;
                        }
                    case "Round":
                        ConvertToSqlRound(call, ref sb, context);
                        break;
                    default:
                        throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
                }

                context.InvertStack = invertStack;
            }
            else
            {
                var typeDetails = TypeAnalyzer.GetTypeDetail(call.Method.DeclaringType);
                if (typeDetails.HasIEnumerableGeneric)
                {
                    switch (call.Method.Name)
                    {
                        case "Contains":
                            {
                                if (call.Arguments.Count != 1 || call.Object is null)
                                    throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                                var callingObject = call.Object;
                                var lambda = call.Arguments[0];


                                if (TryConvertToSqlKeyRows(callingObject, lambda, ref sb, context))

                                    break;

                                ConvertToSql(lambda, ref sb, context);

                                sb.Write(context.Inverted ? " NOT IN " : " IN ");

                                ConvertToSql(EnumList(callingObject, lambda, context), ref sb, context);
                                break;
                            }
                        default:
                            throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
                    }
                }
                else
                {
                    throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
                }
            }
        }
        //a Where before Any, Count, or an aggregate folds into its condition, so x.Lines.Where(l => a).Any(l => b) reads the rows where a and b; before All it reads the rows where a that fail b, since All is written as not any failing
        private static Expression UnwrapWhere(Expression source, ref LambdaExpression? condition, bool all)
        {
            while (source is MethodCallExpression { Method.Name: "Where" } chain && (chain.Method.DeclaringType == typeof(Enumerable) || chain.Method.DeclaringType == typeof(Queryable)) && chain.Arguments.Count == 2)
            {
                var argument = chain.Arguments[1];
                if (argument.NodeType == ExpressionType.Quote)
                    argument = ((UnaryExpression)argument).Operand;
                var where = (LambdaExpression)argument;
                if (condition is null)
                    condition = where;
                else if (all)
                    condition = Expression.Lambda(where.Type, Expression.OrElse(Expression.Not(where.Body), LinqRebinder.RebindExpression(condition.Body, condition.Parameters[0], where.Parameters[0])), where.Parameters);
                else
                    condition = Expression.Lambda(where.Type, Expression.AndAlso(where.Body, LinqRebinder.RebindExpression(condition.Body, condition.Parameters[0], where.Parameters[0])), where.Parameters);
                source = chain.Arguments[0];
            }
            return source;
        }
        /// <inheritdoc/>
        protected override void ConvertToSqlParameterModel(ModelDetail modelDetail, ref CharWriter sb, BuilderContext context, bool parameterInContext)
        {
            var member = context.MemberContext.MemberAccessStack.Pop();

            var modelProperty = modelDetail.GetMember(member.Member.Name);
            if (modelProperty.IsDataSourceEntity)
            {
                var subModelInfo = ModelAnalyzer.GetModel(modelProperty.ActualType);

                if (parameterInContext)
                {
                    var parentDependant = context.MemberContext.DependantStack.Peek();
                    if (!parentDependant.Dependants.TryGetValue(modelProperty.Name, out var dependant))
                    {
                        dependant = new ParameterDependant(subModelInfo, modelProperty, parentDependant);
                        parentDependant.Dependants.Add(modelProperty.Name, dependant);
                    }
                    context.MemberContext.DependantStack.Push(dependant);
                }
                if (context.MemberContext.MemberAccessStack.Count > 0)
                {
                    context.MemberContext.ModelStack.Push(subModelInfo);
                    ConvertToSqlParameterModel(subModelInfo, ref sb, context, parameterInContext);
                    _ = context.MemberContext.ModelStack.Pop();
                }
                else
                {
                    //Can't reference a table but it's probably a null checking expression we want to skip
                    //so trying NULL here which produces NULL in NULL
                    sb.Write("NULL");
                }
                if (parameterInContext)
                {
                    _ = context.MemberContext.DependantStack.Pop();
                }
            }
            else if (context.MemberContext.InCallNoRender > 0)
            {

            }
            else if (context.MemberContext.InCallRenderIdentity > 0)
            {
                if (modelDetail.IdentityMembers.Count != 1)
                    throw new NotSupportedException($"Relational queries support only one identity on {modelDetail.Type.Name}");
                var modelIdentity = modelDetail.IdentityMembers[0];

                sb.Write((parameterInContext ? context.MemberContext.DependantStack.Peek().TableName : modelDetail.DataSourceEntityName).ToLower());
                sb.Write('.');
                sb.Write(modelIdentity.StoreName.ToLower());
            }
            else
            {
                if (context.MemberContext.MemberAccessStack.Count > 0)
                {
                    //functions of a column such as x.Date.Year are written by ConvertToSqlMemberFunction before reaching here
                    var memberProperty = context.MemberContext.MemberAccessStack.Peek();
                    throw new NotSupportedException($"{member.Member.Name}.{memberProperty.Member.Name} not supported");
                }

                sb.Write((parameterInContext ? context.MemberContext.DependantStack.Peek().TableName : modelDetail.DataSourceEntityName).ToLower());
                sb.Write('.');
                sb.Write(modelProperty.StoreName.ToLower());
                var lastOperator = context.MemberContext.OperatorStack.Peek();
                if ((modelProperty.ActualType == typeof(bool) || modelProperty.ActualType == typeof(bool?)) && (lastOperator == Operator.And || lastOperator == Operator.Or || lastOperator == Operator.Not || (lastOperator == Operator.Lambda && !context.IsOrderBy)))
                {
                    //a boolean member used as a condition is written as a comparison, which also carries any NOT around it
                    sb.Write(context.Inverted ? "=FALSE" : "=TRUE");
                }
                else if (lastOperator == Operator.And || lastOperator == Operator.Or)
                {
                    sb.Write(" IS NOT NULL");
                }
            }

            context.MemberContext.MemberAccessStack.Push(member);
        }
        /// <inheritdoc/>
        protected override void ConvertToSqlConditional(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            context.MemberContext.OperatorStack.Push(Operator.Conditional);

            var conditional = (ConditionalExpression)exp;

            sb.Write("CASE WHEN(");

            ConvertToSql(conditional.Test, ref sb, context);

            sb.Write(")THEN(");

            ConvertToSql(conditional.IfTrue, ref sb, context);

            sb.Write(")ELSE(");

            ConvertToSql(conditional.IfFalse, ref sb, context);

            sb.Write(")END");

            _ = context.MemberContext.OperatorStack.Pop();
        }
        /// <inheritdoc/>
        protected override bool ConvertToSqlMemberFunction(MemberExpression member, ref CharWriter sb, BuilderContext context)
        {
            var declaringType = member.Member.DeclaringType;
            string? prefix = null;
            //DATE_PART is a double precision so the parts are cast to the integer they are in .NET
            var suffix = ") AS integer)";

            if (declaringType == typeof(string))
            {
                if (member.Member.Name == "Length")
                {
                    prefix = "LENGTH(";
                    suffix = ")";
                }
            }
            else if (declaringType == typeof(DateTime) || declaringType == typeof(DateTimeOffset) || declaringType == typeof(DateOnly))
            {
                switch (member.Member.Name)
                {
                    case "Year": prefix = "CAST(DATE_PART('year',"; break;
                    case "Month": prefix = "CAST(DATE_PART('month',"; break;
                    case "Day": prefix = "CAST(DATE_PART('day',"; break;
                    case "Hour": prefix = "CAST(DATE_PART('hour',"; break;
                    case "Minute": prefix = "CAST(DATE_PART('minute',"; break;
                    case "Second":
                        //seconds include the fraction
                        prefix = "CAST(FLOOR(DATE_PART('second',";
                        suffix = ")) AS integer)";
                        break;
                    case "Millisecond":
                        //milliseconds include the seconds
                        prefix = "(CAST(FLOOR(DATE_PART('milliseconds',";
                        suffix = ")) AS integer)%1000)";
                        break;
                    case "DayOfYear": prefix = "CAST(DATE_PART('doy',"; break;
                    case "DayOfWeek": prefix = "CAST(DATE_PART('dow',"; break;
                    case "Date":
                        if (declaringType != typeof(DateOnly))
                        {
                            prefix = "CAST(";
                            suffix = " AS date)";
                        }
                        break;
                }
            }
            else if (declaringType == typeof(TimeOnly) || declaringType == typeof(TimeSpan))
            {
                //a TimeSpan is stored as a time
                switch (member.Member.Name)
                {
                    case "Hour":
                    case "Hours":
                        prefix = "CAST(DATE_PART('hour',";
                        break;
                    case "Minute":
                    case "Minutes":
                        prefix = "CAST(DATE_PART('minute',";
                        break;
                    case "Second":
                    case "Seconds":
                        prefix = "CAST(FLOOR(DATE_PART('second',";
                        suffix = ")) AS integer)";
                        break;
                    case "Millisecond":
                    case "Milliseconds":
                        prefix = "(CAST(FLOOR(DATE_PART('milliseconds',";
                        suffix = ")) AS integer)%1000)";
                        break;
                    case "TotalHours":
                        if (declaringType == typeof(TimeSpan))
                        {
                            prefix = "(EXTRACT(EPOCH FROM ";
                            suffix = ")/3600)";
                        }
                        break;
                    case "TotalMinutes":
                        if (declaringType == typeof(TimeSpan))
                        {
                            prefix = "(EXTRACT(EPOCH FROM ";
                            suffix = ")/60)";
                        }
                        break;
                    case "TotalSeconds":
                        if (declaringType == typeof(TimeSpan))
                        {
                            prefix = "EXTRACT(EPOCH FROM ";
                            suffix = ")";
                        }
                        break;
                    case "TotalMilliseconds":
                        if (declaringType == typeof(TimeSpan))
                        {
                            prefix = "(EXTRACT(EPOCH FROM ";
                            suffix = ")*1000)";
                        }
                        break;
                }
            }
            else if (declaringType is not null && declaringType.Name == nullableTypeName)
            {
                if (member.Member.Name == "Value")
                {
                    //the column is the value or NULL
                    ConvertToSql(member.Expression!, ref sb, context);
                    return true;
                }
                if (member.Member.Name == "HasValue")
                {
                    var lastOperator = context.MemberContext.OperatorStack.Peek();
                    var inverted = context.Inverted;
                    context.MemberContext.OperatorStack.Push(Operator.Call);
                    var invertStack = context.InvertStack;
                    context.InvertStack = 0;

                    if (lastOperator == Operator.And || lastOperator == Operator.Or || lastOperator == Operator.Not || (lastOperator == Operator.Lambda && !context.IsOrderBy))
                    {
                        ConvertToSql(member.Expression!, ref sb, context);
                        sb.Write(inverted ? " IS NULL" : " IS NOT NULL");
                    }
                    else
                    {
                        sb.Write('(');
                        ConvertToSql(member.Expression!, ref sb, context);
                        sb.Write(" IS NOT NULL)");
                    }

                    context.InvertStack = invertStack;
                    _ = context.MemberContext.OperatorStack.Pop();
                    return true;
                }
            }

            if (prefix is null)
                return false;

            context.MemberContext.OperatorStack.Push(Operator.Call);
            var functionInvertStack = context.InvertStack;
            context.InvertStack = 0;

            sb.Write(prefix);
            ConvertToSql(member.Expression!, ref sb, context);
            sb.Write(suffix);

            context.InvertStack = functionInvertStack;
            _ = context.MemberContext.OperatorStack.Pop();
            return true;
        }
        /// <inheritdoc/>
        protected override void ConvertToSqlCoalesce(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            var coalesce = (BinaryExpression)exp;
            if (coalesce.Conversion is not null)
                throw new NotSupportedException("Cannot convert a coalesce with a conversion");

            var lastOperator = context.MemberContext.OperatorStack.Peek();
            var isCondition = exp.Type == typeof(bool) && (lastOperator == Operator.And || lastOperator == Operator.Or || lastOperator == Operator.Not || (lastOperator == Operator.Lambda && !context.IsOrderBy));
            var inverted = context.Inverted;

            context.MemberContext.OperatorStack.Push(Operator.Call);
            var invertStack = context.InvertStack;
            context.InvertStack = 0;

            sb.Write("COALESCE((");
            ConvertToSql(coalesce.Left, ref sb, context);
            sb.Write("),(");
            ConvertToSql(coalesce.Right, ref sb, context);
            sb.Write("))");

            context.InvertStack = invertStack;
            _ = context.MemberContext.OperatorStack.Pop();

            //a boolean used as a condition is written as a comparison
            if (isCondition)
                sb.Write(inverted ? "=FALSE" : "=TRUE");
        }
        /// <inheritdoc/>
        protected override void ConvertToSqlStringConcat(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            var binary = (BinaryExpression)exp;

            context.MemberContext.OperatorStack.Push(Operator.Call);
            var invertStack = context.InvertStack;
            context.InvertStack = 0;

            //CONCAT treats NULL as empty like string addition, || would be NULL
            sb.Write("CONCAT((");
            ConvertToSql(binary.Left, ref sb, context);
            sb.Write("),(");
            ConvertToSql(binary.Right, ref sb, context);
            sb.Write("))");

            context.InvertStack = invertStack;
            _ = context.MemberContext.OperatorStack.Pop();
        }

        /// <inheritdoc/>
        protected override void ConvertToSqlValueRender(Type type, object? value, ref CharWriter sb, BuilderContext context)
        {
            //avoid TypeDetail for AOT support

            CoreType? coreType = null;
            if (TypeLookup.GetCoreType(type, out var coreTypeLookup))
                coreType = coreTypeLookup;

            if (value is null)
            {
                sb.Write("NULL");
                return;
            }

            if (type.Name == nullableTypeName)
            {
                type = type.GetGenericArguments()[0];
                coreType = null;
                if (TypeLookup.GetCoreType(type, out coreTypeLookup))
                    coreType = coreTypeLookup;
            }

            if (coreType.HasValue)
            {
                switch (coreType.Value)
                {
                    case CoreType.Boolean:
                        var lastOperator = context.MemberContext.OperatorStack.Peek();
                        if (lastOperator == Operator.And || lastOperator == Operator.Or || lastOperator == Operator.Not || lastOperator == Operator.Lambda)
                            sb.Write((bool)value != context.Inverted ? "1=1" : "1=0");
                        else
                            sb.Write((bool)value ? "TRUE" : "FALSE");
                        return;
                    case CoreType.Byte: sb.Write((byte)value); return;
                    case CoreType.SByte: sb.Write((sbyte)value); return;
                    case CoreType.Int16: sb.Write((short)value); return;
                    case CoreType.UInt16: sb.Write((ushort)value); return;
                    case CoreType.Int32: sb.Write((int)value); return;
                    case CoreType.UInt32: sb.Write((uint)value); return;
                    case CoreType.Int64: sb.Write((long)value); return;
                    case CoreType.UInt64: sb.Write((ulong)value); return;
                    case CoreType.Single: sb.Write((float)value); return;
                    case CoreType.Double: sb.Write((double)value); return;
                    case CoreType.Decimal: sb.Write((decimal)value); return;
                    case CoreType.Char:
                        if ((char)value == '\'')
                        {
                            sb.Write("\'\'\'\'");
                        }
                        else
                        {
                            sb.Write('\'');
                            sb.Write((char)value);
                            sb.Write('\'');
                        }
                        return;
                    case CoreType.DateTime:
                        sb.Write('\'');
                        sb.Write((DateTime)value, CharWriter.DateTimeFormat.PostgreSql);
                        sb.Write('\'');
                        return;
                    case CoreType.DateTimeOffset:
                        sb.Write('\'');
                        sb.Write((DateTimeOffset)value, CharWriter.DateTimeFormat.PostgreSql);
                        sb.Write('\'');
                        return;
                    case CoreType.TimeSpan:
                        sb.Write('\'');
                        sb.Write((TimeSpan)value, CharWriter.TimeFormat.PostgreSql);
                        sb.Write('\'');
                        return;
                    case CoreType.DateOnly:
                        sb.Write('\'');
                        sb.Write((DateOnly)value, CharWriter.DateTimeFormat.PostgreSql);
                        sb.Write('\'');
                        return;
                    case CoreType.TimeOnly:
                        sb.Write('\'');
                        sb.Write((TimeOnly)value, CharWriter.TimeFormat.PostgreSql);
                        sb.Write('\'');
                        return;
                    case CoreType.Guid:
                        sb.Write('\'');
                        sb.Write((Guid)value);
                        sb.Write('\'');
                        return;
                    case CoreType.String:
                        sb.Write('\'');
                        sb.Write(((string)value).Replace("'", "''"));
                        sb.Write('\''); return;
                }
            }

            if (type.IsEnum)
            {
                //an enum isn't a column type, so it's compared to a number column that C# converted to the underlying type
                if (Enum.GetUnderlyingType(type) == typeof(ulong))
                    sb.Write(System.Convert.ToUInt64(value));
                else
                    sb.Write(System.Convert.ToInt64(value));
                return;
            }

            if (type.IsArray || type.Name == "ReadOnlySpan`1" || type.Name == "Span`1")
            {
                var arrayType = type.IsArray ? type.GetElementType()! : type.GetGenericArguments()[0];
                if (arrayType == typeof(byte))
                {
                    sb.Write("decode('");
                    sb.Write((byte[])value, CharWriter.ByteFormat.Hex);
                    sb.Write("','hex')");
                    return;
                }
                else
                {
                    sb.Write('(');

                    var builderLength = sb.Length;

                    var first = true;
                    foreach (var item in (IEnumerable)value)
                    {
                        if (!first)
                            sb.Write(',');
                        ConvertToSqlValue(arrayType, item, ref sb, context);
                        first = false;
                    }

                    if (builderLength == sb.Length)
                        sb.Write("NULL");

                    sb.Write(')');
                    return;
                }
            }

            if (value is IEnumerable enumerable)
            {
                sb.Write('(');

                var builderLength = sb.Length;

                Type? trueInnerType = null;

                var first = true;
                foreach (var item in enumerable)
                {
                    if (!first)
                        sb.Write(',');
                    trueInnerType ??= item.GetType();
                    ConvertToSqlValue(trueInnerType, item, ref sb, context);
                    first = false;
                }

                if (builderLength == sb.Length)
                    sb.Write("NULL");

                sb.Write(')');
                return;
            }

            if (type == typeof(object))
            {
                sb.Write('\'');
                sb.Write(value.ToString()!.Replace("\'", "''"));
                sb.Write('\'');
                return;
            }

            throw new NotImplementedException($"{type.Name} value {value?.ToString()} not converted");
        }

        /// <inheritdoc/>
        protected override void ConvertPersistWhere(ref CharWriter sb, LambdaExpression where, ModelDetail modelDetail, MemberContext operationContext)
        {
            var rootDependant = new ParameterDependant(modelDetail, null);
            var sbWhere = new CharWriter();
            try
            {
                GenerateWhere(where, ref sbWhere, rootDependant, operationContext);
                if (rootDependant.Dependants.Count == 0)
                {
                    sb.Write(' ');
                    sb.Write(sbWhere);
                    return;
                }

                //a related model in the condition needs its join, which UPDATE and DELETE write differently in each database, so the matching rows are selected instead
                //an identity of several members is a row of values
                var several = modelDetail.IdentityMembers.Count > 1;
                sb.Write(several ? " WHERE (" : " WHERE ");
                for (var i = 0; i < modelDetail.IdentityMembers.Count; i++)
                {
                    if (i > 0)
                        sb.Write(',');
                    sb.Write(modelDetail.DataSourceEntityName.ToLower());
                    sb.Write('.');
                    sb.Write(modelDetail.IdentityMembers[i].StoreName.ToLower());
                }
                sb.Write(several ? ") IN (SELECT " : " IN (SELECT ");
                for (var i = 0; i < modelDetail.IdentityMembers.Count; i++)
                {
                    if (i > 0)
                        sb.Write(',');
                    sb.Write(modelDetail.DataSourceEntityName.ToLower());
                    sb.Write('.');
                    sb.Write(modelDetail.IdentityMembers[i].StoreName.ToLower());
                }
                GenerateFrom(modelDetail, ref sb);
                GenerateJoin(rootDependant, ref sb);
                sb.Write(sbWhere);
                sb.Write(')');
            }
            finally
            {
                sbWhere.Dispose();
            }
        }
        /// <inheritdoc/>
        protected override void ConvertPersistUpdate(ref CharWriter sb, ProjectionMember[] sets, LambdaExpression where, ModelDetail modelDetail, MemberContext operationContext)
        {
            var rootDependant = new ParameterDependant(modelDetail, null);
            sb.Write("UPDATE ");
            if (modelDetail.DataSourceSchemaName is not null)
            {
                sb.Write(modelDetail.DataSourceSchemaName.ToLower());
                sb.Write('.');
            }
            sb.Write(modelDetail.DataSourceEntityName.ToLower());
            sb.Write(" SET ");
            for (var i = 0; i < sets.Length; i++)
            {
                if (i > 0)
                    sb.Write(',');
                sb.Write(modelDetail.GetMember(sets[i].MemberDetail.Name).StoreName.ToLower());
                sb.Write('=');
                ConvertModelExpression(EnumAssigned(sets[i].Expression!, modelDetail.GetMember(sets[i].MemberDetail.Name), operationContext), sets[i].Parameter!, ref sb, rootDependant, modelDetail, operationContext);
            }
            ConvertPersistWhere(ref sb, where, modelDetail, operationContext);
        }
        /// <inheritdoc/>
        protected override LinqStatement ConvertLinq(LinqQuery query, LinqPlan plan, ModelDetail modelDetail, List<object>? parameters)
        {
            var operationContext = new MemberContext();
            var rootDependant = new ParameterDependant(modelDetail, null);
            var sb = new CharWriter();
            var sbFrom = new CharWriter();
            var sbWhere = new CharWriter();
            var sbEnd = new CharWriter();
            try
            {
                //a count or aggregate of a page reads the rows of the page from a derived table
                var pagedValue = !query.ReadsElements && (query.Skip.HasValue || query.Take.HasValue) && !(query.Distinct && query.Terminal is LinqTerminal.Count or LinqTerminal.LongCount);
                var deferColumns = false;
                switch (query.Terminal)
                {
                    case LinqTerminal.Count:
                    case LinqTerminal.LongCount:
                        if (query.Distinct)
                            goto default;
                        sb.Write("SELECT COUNT(1)");
                        break;
                    case LinqTerminal.Any:
                        sb.Write("SELECT 1");
                        break;
                    case LinqTerminal.Sum:
                    case LinqTerminal.Min:
                    case LinqTerminal.Max:
                    case LinqTerminal.Average:
                        sb.Write("SELECT ");
                        ConvertLinqColumn(plan.Columns[0], plan, ref sb, rootDependant, modelDetail, operationContext, pagedValue);
                        break;
                    default:
                        if (query.Distinct && plan.Pieces.Count > 0)
                            throw new NotSupportedException("Distinct with a relation read after the rows isn't supported");
                        sb.Write(query.Distinct ? "SELECT DISTINCT " : "SELECT ");
                        deferColumns = plan.ReadsOnlyMembers;
                        if (!deferColumns)
                        {
                            for (var i = 0; i < plan.Columns.Count; i++)
                            {
                                if (i > 0)
                                    sb.Write(',');
                                ConvertLinqColumn(plan.Columns[i], plan, ref sb, rootDependant, modelDetail, operationContext);
                                sb.Write(" AS C");
                                sb.Write(i);
                            }
                        }
                        break;
                }
                if (pagedValue)
                {
                    sb.Write(" FROM(SELECT ");
                    if (plan.Columns.Count > 0)
                        ConvertModelExpression(plan.Columns[0].Expression!, plan.Columns[0].Parameter!, ref sb, rootDependant, modelDetail, operationContext);
                    else
                        sb.Write('1');
                    sb.Write(" AS V0");
                }
                GenerateFrom(modelDetail, ref sbFrom);
                GenerateWhere(query.Where, ref sbWhere, rootDependant, operationContext);
                var paged = false;
                if (plan.KeyParts is not null)
                {
                    sbEnd.Write("GROUP BY ");
                    for (var i = 0; i < plan.KeyParts.Length; i++)
                    {
                        if (i > 0)
                            sbEnd.Write(',');
                        ConvertModelExpression(plan.KeyParts[i], plan.KeyParameter!, ref sbEnd, rootDependant, modelDetail, operationContext);
                    }
                    AppendLineBreak(ref sbEnd);
                    //a condition or an order on the results names their members, each of which is the column that computes it
                    var columns = ResultColumns(plan, rootDependant, modelDetail, operationContext);
                    if (query.Having is not null)
                    {
                        sbEnd.Write("HAVING");
                        ConvertResultExpression(query.Having, columns, false, ref sbEnd, rootDependant, operationContext);
                        AppendLineBreak(ref sbEnd);
                    }
                    if (query.ResultOrder is not null)
                    {
                        sbEnd.Write("ORDER BY");
                        var resultOrders = query.ResultOrder;
                        for (var o = 0; o < resultOrders.Length; o++)
                        {
                            if (o > 0)
                                sbEnd.Write(',');
                            ConvertResultExpression(resultOrders[o].Expression, columns, true, ref sbEnd, rootDependant, operationContext);
                            if (resultOrders[o].Descending)
                                sbEnd.Write("DESC");
                        }
                        AppendLineBreak(ref sbEnd);
                    }
                    if (query.Skip.HasValue || query.Take.HasValue)
                        WriteLinqPage(query.Skip, query.Take, ref sbEnd);
                }
                else if (query.Terminal == LinqTerminal.Any && !pagedValue)
                {
                    sbEnd.Write("LIMIT 1");
                }
                else if (query.ReadsElements || query.Skip.HasValue || query.Take.HasValue)
                {
                    paged = query.Skip.HasValue || query.Take.HasValue;
                    if (paged && plan.Pieces.Count > 0 && modelDetail.IdentityMembers.Count == 1)
                    {
                        //a paged statement with relations is also where they select their parents' keys, so its order ends with the identity for both to pick the same rows
                        sbEnd.Write("ORDER BY");
                        if (query.Order is not null)
                        {
                            foreach (var orderExp in query.Order)
                            {
                                ConvertToSql(orderExp.Expression, ref sbEnd, new BuilderContext(rootDependant, operationContext) { IsOrderBy = true, IsSort = true });
                                if (orderExp.Descending)
                                    sbEnd.Write("DESC");
                                sbEnd.Write(',');
                            }
                        }
                        sbEnd.Write(' ');
                        sbEnd.Write(modelDetail.DataSourceEntityName.ToLower());
                        sbEnd.Write('.');
                        sbEnd.Write(modelDetail.IdentityMembers[0].StoreName.ToLower());
                        AppendLineBreak(ref sbEnd);
                        WriteLinqPage(query.Skip, query.Take, ref sbEnd);
                    }
                    else if (query.Order is null && paged)
                    {
                        WriteLinqPage(query.Skip, query.Take, ref sbEnd);
                    }
                    else
                    {
                        GenerateOrderSkipTake(query.Order, query.Skip, query.Take, ref sbEnd, rootDependant, operationContext);
                    }
                }
                if (deferColumns)
                    ConvertLinqMemberColumns(plan, ref sb, rootDependant, modelDetail, operationContext);
                GenerateJoin(rootDependant, ref sbFrom);
                sb.Write(sbFrom);
                sb.Write(sbWhere);
                sb.Write(sbEnd);
                if (pagedValue)
                    sb.Write(")AS Z");
                //the different elements counted are the rows of the statement
                var sql = sb.ToString();
                if (query.Distinct && query.Terminal is LinqTerminal.Count or LinqTerminal.LongCount)
                    sql = $"SELECT COUNT(1) FROM ({sql}) AS Z";
                return new LinqStatement(sql, plan, ConvertLinqPieces(plan, modelDetail, rootDependant, 0, ref sbWhere, ref sbEnd, paged, null, null));
            }
            finally
            {
                sbEnd.Dispose();
                sbWhere.Dispose();
                sbFrom.Dispose();
                sb.Dispose();
            }
        }
        private static void WriteLinqPage(int? skip, int? take, ref CharWriter sb)
        {
            if (take.HasValue)
            {
                sb.Write("LIMIT ");
                sb.Write(take.Value);
                sb.Write(' ');
            }
            if (skip.HasValue)
            {
                sb.Write("OFFSET ");
                sb.Write(skip.Value);
                sb.Write(' ');
            }
        }
        //each relation's rows are those of the parents the statement selects: the key they match by, selected with the statement's from, where, and when paged its order and page
        //only the key is selected, so the parent's joins are only those its where or order uses
        private LinqStatement[]? ConvertLinqPieces(LinqPlan plan, ModelDetail modelDetail, ParameterDependant rootDependant, int whereStart, ref CharWriter sbWhere, ref CharWriter sbEnd, bool paged, LinqPiece? parent, string? parentKeys)
        {
            if (plan.Pieces.Count == 0)
                return null;
            var conditions = rootDependant.Dependants.Count == 0 ? null : sbWhere.ToSpan().Slice(whereStart).ToString() + (paged ? sbEnd.ToString() : null);
            var statements = new LinqStatement[plan.Pieces.Count];
            for (var p = 0; p < statements.Length; p++)
            {
                var piece = plan.Pieces[p];
                var sbKeys = new CharWriter();
                try
                {
                    //a relation keyed by the identity a single relation was found by has the same keys to look for
                    if (parentKeys is not null && !piece.CompositeKey && piece.ParentKeys[0].StoreName == parent!.RelatedKey.StoreName)
                    {
                        sbKeys.Write(parentKeys);
                        statements[p] = ConvertLinqPiece(piece, ref sbKeys);
                        continue;
                    }
                    sbKeys.Write("SELECT ");
                    for (var k = 0; k < piece.ParentKeys.Length; k++)
                    {
                        if (k > 0)
                            sbKeys.Write(',');
                        sbKeys.Write(modelDetail.DataSourceEntityName.ToLower());
                        sbKeys.Write('.');
                        sbKeys.Write(piece.ParentKeys[k].StoreName.ToLower());
                        if (piece.CompositeKey)
                        {
                            sbKeys.Write(" AS k");
                            sbKeys.Write(k);
                        }
                    }
                    GenerateFrom(modelDetail, ref sbKeys);
                    if (conditions is not null)
                        GenerateJoin(rootDependant, ref sbKeys, conditions);
                    sbKeys.Write(sbWhere);
                    if (paged)
                        sbKeys.Write(sbEnd);
                    statements[p] = ConvertLinqPiece(piece, ref sbKeys);
                }
                finally
                {
                    sbKeys.Dispose();
                }
            }
            return statements;
        }
        private LinqStatement ConvertLinqPiece(LinqPiece piece, ref CharWriter sbKeys)
        {
            var plan = piece.Plan;
            var modelDetail = piece.RelatedModel;
            var operationContext = new MemberContext();
            var rootDependant = new ParameterDependant(modelDetail, null);
            var sb = new CharWriter();
            var sbFrom = new CharWriter();
            var sbWhere = new CharWriter();
            var sbEnd = new CharWriter();
            try
            {
                //rows per parent are numbered for each parent, and the page is the numbers wanted; relations of these rows read those of all the parent's rows, which the keys then pick from
                var numbered = piece.Skip.HasValue || piece.Take.HasValue;
                if (numbered)
                {
                    sb.Write("SELECT ");
                    for (var i = 0; i < plan.Columns.Count; i++)
                    {
                        if (i > 0)
                            sb.Write(',');
                        sb.Write("C");
                        sb.Write(i);
                    }
                    sb.Write(" FROM (");
                }
                sb.Write("SELECT ");
                for (var i = 0; i < plan.Columns.Count; i++)
                {
                    if (i > 0)
                        sb.Write(',');
                    ConvertLinqColumn(plan.Columns[i], plan, ref sb, rootDependant, modelDetail, operationContext);
                    sb.Write(" AS C");
                    sb.Write(i);
                }
                GenerateFrom(modelDetail, ref sbFrom);
                if (piece.CompositeKey)
                {
                    //a key of several columns isn't one value to look for, so a parent's row with each of them exists
                    sbWhere.Write("WHERE EXISTS(SELECT 1 FROM (");
                    sbWhere.Write(sbKeys);
                    sbWhere.Write(") AS p WHERE ");
                    for (var k = 0; k < piece.RelatedKeys.Length; k++)
                    {
                        if (k > 0)
                            sbWhere.Write(" AND ");
                        sbWhere.Write("p.k");
                        sbWhere.Write(k);
                        sbWhere.Write('=');
                        sbWhere.Write(modelDetail.DataSourceEntityName.ToLower());
                        sbWhere.Write('.');
                        sbWhere.Write(piece.RelatedKeys[k].StoreName.ToLower());
                    }
                    sbWhere.Write(')');
                }
                else
                {
                    sbWhere.Write("WHERE ");
                    sbWhere.Write(modelDetail.DataSourceEntityName.ToLower());
                    sbWhere.Write('.');
                    sbWhere.Write(piece.RelatedKey.StoreName.ToLower());
                    sbWhere.Write(" IN (");
                    sbWhere.Write(sbKeys);
                    sbWhere.Write(')');
                }
                //the parent's keys are their own statement, so this one's condition is what follows them
                var whereStart = sbWhere.Length;
                if (piece.Where is not null)
                {
                    sbWhere.Write("AND(");
                    ConvertToSql(piece.Where, ref sbWhere, new BuilderContext(rootDependant, operationContext));
                    sbWhere.Write(')');
                }
                AppendLineBreak(ref sbWhere);
                GenerateOrderSkipTake(piece.Order, null, null, ref sbEnd, rootDependant, operationContext);
                if (numbered)
                {
                    sb.Write(",ROW_NUMBER() OVER (PARTITION BY ");
                    for (var k = 0; k < piece.RelatedKeys.Length; k++)
                    {
                        if (k > 0)
                            sb.Write(',');
                        sb.Write(modelDetail.DataSourceEntityName.ToLower());
                        sb.Write(".");
                        sb.Write(piece.RelatedKeys[k].StoreName.ToLower());
                    }
                    sb.Write(" ");
                    if (piece.Order is null)
                    {
                        sb.Write("ORDER BY ");
                        sb.Write(modelDetail.DataSourceEntityName.ToLower());
                        sb.Write(".");
                        sb.Write((modelDetail.IdentityMembers.Count == 1 ? modelDetail.IdentityMembers[0].StoreName : piece.RelatedKey.StoreName).ToLower());
                        sb.Write("");
                    }
                    else
                    {
                        sb.Write(sbEnd);
                    }
                    sb.Write(") AS ZR");
                }
                GenerateJoin(rootDependant, ref sbFrom);
                sb.Write(sbFrom);
                sb.Write(sbWhere);
                if (numbered)
                {
                    sb.Write(") AS Z WHERE ");
                    if (piece.Skip.HasValue)
                    {
                        sb.Write("ZR>");
                        sb.Write(piece.Skip.Value);
                    }
                    if (piece.Take.HasValue)
                    {
                        if (piece.Skip.HasValue)
                            sb.Write(" AND ");
                        sb.Write("ZR<=");
                        sb.Write((piece.Skip ?? 0) + piece.Take.Value);
                    }
                    sb.Write(" ORDER BY ZR");
                    AppendLineBreak(ref sb);
                }
                else
                {
                    sb.Write(sbEnd);
                }
                return new LinqStatement(sb.ToString(), plan, ConvertLinqPieces(plan, modelDetail, rootDependant, whereStart, ref sbWhere, ref sbEnd, false, piece, plan.Pieces.Count > 0 && piece.Single && !piece.CompositeKey && piece.Where is null && !numbered ? sbKeys.ToString() : null));
            }
            finally
            {
                sbEnd.Dispose();
                sbWhere.Dispose();
                sbFrom.Dispose();
                sb.Dispose();
            }
        }
        //without a join a column can only be the model's, and a shorter statement is quicker for the store to take in
        private void ConvertLinqMemberColumns(LinqPlan plan, ref CharWriter sb, ParameterDependant rootDependant, ModelDetail modelDetail, MemberContext operationContext)
        {
            var joined = rootDependant.Dependants.Count > 0;
            for (var i = 0; i < plan.Columns.Count; i++)
            {
                if (i > 0)
                    sb.Write(',');
                if (joined)
                    ConvertLinqColumn(plan.Columns[i], plan, ref sb, rootDependant, modelDetail, operationContext);
                else
                    sb.Write(plan.Columns[i].Member!.StoreName.ToLower());
                sb.Write(" AS C");
                sb.Write(i);
            }
        }
        private void ConvertLinqColumn(LinqColumn column, LinqPlan plan, ref CharWriter sb, ParameterDependant rootDependant, ModelDetail modelDetail, MemberContext operationContext, bool derived = false)
        {
            switch (column.Kind)
            {
                case LinqColumnKind.RowNumber:
                    ConvertRowNumber(column.Orders, ref sb, rootDependant, operationContext);
                    break;
                case LinqColumnKind.Value:
                    if (column.Member is not null)
                    {
                        sb.Write(modelDetail.DataSourceEntityName.ToLower());
                        sb.Write('.');
                        sb.Write(column.Member.StoreName.ToLower());
                    }
                    else
                    {
                        ConvertLinqValue(column, derived, ref sb, rootDependant, modelDetail, operationContext);
                    }
                    break;
                case LinqColumnKind.KeyPart:
                    ConvertModelExpression(plan.KeyParts![column.KeyPart], plan.KeyParameter!, ref sb, rootDependant, modelDetail, operationContext);
                    break;
                case LinqColumnKind.Count:
                    sb.Write("COUNT(1)");
                    break;
                case LinqColumnKind.CountIf:
                    sb.Write("COUNT(*) FILTER (WHERE ");
                    ConvertLinqValue(column, derived, ref sb, rootDependant, modelDetail, operationContext);
                    sb.Write(")");
                    break;
                case LinqColumnKind.CountDistinct:
                    sb.Write("COUNT(DISTINCT ");
                    ConvertLinqValue(column, derived, ref sb, rootDependant, modelDetail, operationContext);
                    sb.Write(')');
                    break;
                case LinqColumnKind.Sum:
                    //SUM of no rows is NULL where the LINQ sum is 0
                    sb.Write("COALESCE(SUM(");
                    ConvertLinqValue(column, derived, ref sb, rootDependant, modelDetail, operationContext);
                    sb.Write("),0)");
                    break;
                case LinqColumnKind.Average:
                    {
                        //the average of integers is a double in LINQ
                        var valueType = Nullable.GetUnderlyingType(column.Expression!.Type) ?? column.Expression.Type;
                        if (valueType == typeof(int) || valueType == typeof(long) || valueType == typeof(short) || valueType == typeof(byte) || valueType == typeof(uint) || valueType == typeof(ulong) || valueType == typeof(ushort) || valueType == typeof(sbyte))
                        {
                            sb.Write("AVG(CAST(");
                            ConvertLinqValue(column, derived, ref sb, rootDependant, modelDetail, operationContext);
                            sb.Write(" AS double precision))");
                        }
                        else
                        {
                            sb.Write("AVG(");
                            ConvertLinqValue(column, derived, ref sb, rootDependant, modelDetail, operationContext);
                            sb.Write(')');
                        }
                        break;
                    }
                case LinqColumnKind.Join:
                    {
                        var sbValue = new CharWriter();
                        try
                        {
                            ConvertLinqValue(column, derived, ref sbValue, rootDependant, modelDetail, operationContext);
                            sb.Write("COALESCE(");
                            sb.Write(StringAggregate($"COALESCE({sbValue.ToString()},'')", $"'{column.Separator!.Replace("'", "''")}'", true, null));
                            sb.Write(",'')");
                        }
                        finally
                        {
                            sbValue.Dispose();
                        }
                        break;
                    }
                case LinqColumnKind.Min:
                    sb.Write("MIN(");
                    ConvertLinqValue(column, derived, ref sb, rootDependant, modelDetail, operationContext);
                    sb.Write(')');
                    break;
                default:
                    sb.Write("MAX(");
                    ConvertLinqValue(column, derived, ref sb, rootDependant, modelDetail, operationContext);
                    sb.Write(')');
                    break;
            }
        }
        private void ConvertLinqValue(LinqColumn column, bool derived, ref CharWriter sb, ParameterDependant rootDependant, ModelDetail modelDetail, MemberContext operationContext)
        {
            if (derived)
            {
                sb.Write("V0");
                return;
            }
            ConvertModelExpression(column.Expression!, column.Parameter!, ref sb, rootDependant, modelDetail, operationContext);
        }
        //the members of a grouped result that are one column each, with the column's SQL, for its having and order
        private Dictionary<string, string> ResultColumns(LinqPlan plan, ParameterDependant rootDependant, ModelDetail modelDetail, MemberContext operationContext)
        {
            var columns = new Dictionary<string, string>();
            var shape = plan.Shape!;
            if (shape.Kind != LinqShapeKind.Object)
                return columns;
            if (shape.ArgumentNames is not null)
            {
                for (var i = 0; i < shape.ArgumentNames.Length; i++)
                    AddResultColumn(columns, shape.ArgumentNames[i], shape.Arguments![i], plan, rootDependant, modelDetail, operationContext);
            }
            if (shape.Members is not null)
            {
                for (var i = 0; i < shape.Members.Length; i++)
                    AddResultColumn(columns, shape.Members[i].Name, shape.Values![i], plan, rootDependant, modelDetail, operationContext);
            }
            return columns;
        }
        private void AddResultColumn(Dictionary<string, string> columns, string name, LinqShape value, LinqPlan plan, ParameterDependant rootDependant, ModelDetail modelDetail, MemberContext operationContext)
        {
            if (value.Kind == LinqShapeKind.Object && value.ArgumentNames is not null)
            {
                for (var i = 0; i < value.ArgumentNames.Length; i++)
                    AddResultColumn(columns, $"{name}.{value.ArgumentNames[i]}", value.Arguments![i], plan, rootDependant, modelDetail, operationContext);
                return;
            }
            if (value.Kind != LinqShapeKind.Column)
                return;
            var sbColumn = new CharWriter();
            try
            {
                ConvertLinqColumn(value.Column!, plan, ref sbColumn, rootDependant, modelDetail, operationContext);
                columns[name] = sbColumn.ToString();
                AddResultEnum(name, value.Column!, plan, operationContext);
            }
            finally
            {
                sbColumn.Dispose();
            }
        }
        private void ConvertResultExpression(LambdaExpression lambda, Dictionary<string, string> columns, bool isOrder, ref CharWriter sb, ParameterDependant rootDependant, MemberContext operationContext)
        {
            operationContext.ResultColumns ??= new();
            operationContext.ResultColumns[lambda.Parameters[0]] = columns;
            operationContext.OperatorStack.Push(Operator.Lambda);
            sb.Write('(');
            ConvertToSql(lambda.Body, ref sb, new BuilderContext(rootDependant, operationContext) { IsOrderBy = isOrder, IsSort = isOrder });
            sb.Write(')');
            _ = operationContext.OperatorStack.Pop();
            _ = operationContext.ResultColumns.Remove(lambda.Parameters[0]);
        }
        /// <inheritdoc/>
        protected override void GenerateWhere(LambdaExpression? where, ref CharWriter sb, ParameterDependant rootDependant, MemberContext operationContext)
        {
            if (where is null)
                return;

            sb.Write("WHERE");
            var context = new BuilderContext(rootDependant, operationContext);
            ConvertToSql(where, ref sb, context);
            AppendLineBreak(ref sb);
        }
        /// <inheritdoc/>
        protected override void GenerateOrderSkipTake(LinqOrder[]? order, int? skip, int? take, ref CharWriter sb, ParameterDependant rootDependant, MemberContext operationContext)
        {
            if (order?.Length > 0)
            {
                sb.Write("ORDER BY");
                var hasOrder = false;
                foreach (var orderExp in order)
                {
                    var context = new BuilderContext(rootDependant, operationContext) { IsOrderBy = true, IsSort = true };
                    if (hasOrder)
                        sb.Write(',');
                    else
                        hasOrder = true;
                    ConvertToSql(orderExp.Expression, ref sb, context);
                    if (orderExp.Descending)
                        sb.Write("DESC");
                }
                AppendLineBreak(ref sb);
            }
            else if (skip.HasValue || take.HasValue)
            {
                sb.Write("ORDER BY CURRENT_TIMESTAMP");
                AppendLineBreak(ref sb);
            }

            if (skip.HasValue || take.HasValue)
            {
                sb.Write("OFFSET ");
                sb.Write(skip ?? 0);
                sb.Write(" ROWS");
                AppendLineBreak(ref sb);
                if (take.HasValue)
                {
                    sb.Write("FETCH NEXT ");
                    sb.Write(take.Value);
                    sb.Write(" ROWS ONLY");
                    AppendLineBreak(ref sb);
                }
            }
        }
        /// <inheritdoc/>
        protected override void GenerateSelect(bool count, Graph? graph, ModelDetail modelDetail, ref CharWriter sb)
        {
            if (count)
            {
                sb.Write("SELECT COUNT(1)");
                AppendLineBreak(ref sb);
                return;
            }
            sb.Write("SELECT");
            GenerateSelectProperties(graph, modelDetail, ref sb);
        }
        /// <inheritdoc/>
        protected override void GenerateSelectProperties(Graph? graph, ModelDetail modelDetail, ref CharWriter sb)
        {
            AppendLineBreak(ref sb);
            if (graph is null || (graph.IncludeAllMembers && !graph.HasRemovedMembers))
            {
                sb.Write(modelDetail.DataSourceEntityName.ToLower());
                sb.Write(".*");
                AppendLineBreak(ref sb);
            }
            else
            {
                var passedfirst = false;
                foreach (var member in modelDetail.Members)
                {
                    if (graph is not null && !graph.HasMember(member.Name))
                        continue;

                    if (member.StoreName is not null && member.ForeignIdentities is null)
                    {
                        if (passedfirst)
                            sb.Write(',');
                        else
                            passedfirst = true;
                        sb.Write(modelDetail.DataSourceEntityName.ToLower());
                        sb.Write('.');
                        sb.Write(member.Name.ToLower());
                        AppendLineBreak(ref sb);
                    }
                }
                if (!passedfirst)
                {
                    sb.Write(" 1");
                }
            }
        }
        /// <inheritdoc/>
        protected override void GenerateFrom(ModelDetail modelDetail, ref CharWriter sb)
        {
            sb.Write(" FROM ");
            if (modelDetail.DataSourceSchemaName is not null)
            {
                sb.Write(modelDetail.DataSourceSchemaName.ToLower());
                sb.Write('.');
            }
            sb.Write(modelDetail.DataSourceEntityName.ToLower());
            AppendLineBreak(ref sb);
        }
        /// <inheritdoc/>
        protected override string FunctionTemplate(Function function)
        {
            return function switch
            {
                Function.NumberText => "CAST({0} AS text)",
                Function.BoolText => "CASE WHEN {0} THEN 'True' ELSE 'False' END",
                Function.GuidText => "CAST({0} AS text)",
                Function.GuidParse => "CAST({0} AS uuid)",
                Function.PadLeft => "CASE WHEN LENGTH({0})>={1} THEN {0} ELSE LPAD({0},{1},{2}) END",
                Function.PadRight => "CASE WHEN LENGTH({0})>={1} THEN {0} ELSE RPAD({0},{1},{2}) END",
                Function.Concat => "CONCAT({0},{1})",
                Function.DateAddMicroseconds => "({0}+({1})*INTERVAL '1 microsecond')",
                Function.DateAddMonths => "({0}+({1})*INTERVAL '1 month')",
                Function.DateOnlyAddDays => "CAST({0}+({1})*INTERVAL '1 day' AS date)",
                Function.DateOnlyAddMonths => "CAST({0}+({1})*INTERVAL '1 month' AS date)",
                Function.TimeAddMicroseconds => "({0}+({1})*INTERVAL '1 microsecond')",
                Function.DateDiffMicroseconds => "CAST(EXTRACT(EPOCH FROM ({0})-({1}))*1000000 AS bigint)",
                Function.TimeDiffMicroseconds => "CAST(EXTRACT(EPOCH FROM ({0})-({1}))*1000000 AS bigint)",
                Function.SpanMicroseconds => "CAST(EXTRACT(EPOCH FROM {0})*1000000 AS bigint)",
                Function.IntegerDivide => "(({0})/CAST({1} AS bigint))",
                Function.Modulo => "(({0})%{1})",
                Function.RoundWhole => "ROUND(CAST({0} AS numeric),0)",
                Function.RoundPlaces => "ROUND(CAST({0} AS numeric),{1})",
                Function.PowerOfTen => "POWER(CAST(10 AS numeric),{0})",
                Function.DateText => "TO_CHAR({0},{1})",
                _ => throw new NotSupportedException(function.ToString()),
            };
        }
        /// <inheritdoc/>
        protected override string CastType(Type type)
        {
            if (type == typeof(int)) return "integer";
            if (type == typeof(long)) return "bigint";
            if (type == typeof(short) || type == typeof(byte)) return "smallint";
            if (type == typeof(decimal)) return "numeric";
            if (type == typeof(double)) return "double precision";
            if (type == typeof(float)) return "real";
            if (type == typeof(string)) return "text";
            throw new NotSupportedException($"A cast to {type.Name} isn't supported");
        }
        /// <inheritdoc/>
        protected override string DateFormat(string format)
        {
            var sb = new System.Text.StringBuilder("'");
            for (var i = 0; i < format.Length;)
            {
                var c = format[i];
                var run = 1;
                while (i + run < format.Length && format[i + run] == c)
                    run++;
                var token = (c, run) switch
                {
                    ('y', 4) => "YYYY",
                    ('y', 2) => "YY",
                    ('M', 4) => "FMMonth",
                    ('M', 3) => "Mon",
                    ('M', 2) => "MM",
                    ('M', 1) => "FMMM",
                    ('d', 4) => "FMDay",
                    ('d', 3) => "Dy",
                    ('d', 2) => "DD",
                    ('d', 1) => "FMDD",
                    ('H', 2) => "HH24",
                    ('H', 1) => "FMHH24",
                    ('h', 2) => "HH12",
                    ('h', 1) => "FMHH12",
                    ('m', 2) => "MI",
                    ('m', 1) => "FMMI",
                    ('s', 2) => "SS",
                    ('s', 1) => "FMSS",
                    ('f', 3) => "MS",
                    ('t', 2) => "AM",
                    _ => null,
                };
                if (token is null)
                {
                    if (Char.IsLetter(c))
                        throw new NotSupportedException($"The date format {new string(c, run)} isn't supported");
                    _ = sb.Append('"').Append(c == '\'' ? "''" : c.ToString(), 0, c == '\'' ? 2 : 1).Append('"');
                    i++;
                    continue;
                }
                _ = sb.Append(token);
                i += run;
            }
            return sb.Append('\'').ToString();
        }
        /// <inheritdoc/>
        protected override string StringAggregate(string value, string separator, bool constantSeparator, string? order)
        {
            return order is null ? $"STRING_AGG(CAST({value} AS text),{separator})" : $"STRING_AGG(CAST({value} AS text),{separator} ORDER BY {order})";
        }
        /// <inheritdoc/>
        protected override string QuoteColumn(string table, string column) => $"{table.ToLower()}.{column.ToLower()}";
        /// <inheritdoc/>
        protected override string BooleanCondition(bool inverted) => inverted ? "=FALSE" : "=TRUE";
        /// <inheritdoc/>
        protected override string BooleanValue(bool value) => value ? "TRUE" : "FALSE";

        /// <inheritdoc/>
        protected override void GenerateJoin(ParameterDependant dependant, ref CharWriter sb) => GenerateJoin(dependant, ref sb, null);
        private void GenerateJoin(ParameterDependant dependant, ref CharWriter sb, string? used)
        {
            foreach (var child in dependant.Dependants.Values)
            {
                if (used is not null && !UsesTable(used, child.TableName.ToLower()) && !UsesJoin(child, used))
                    continue;

                if (child.ParentMember is null)
                    throw new Exception($"{child.ModelDetail.Type.Name} is not a child member");
                if (child.ParentMember.ForeignIdentities is null)
                    throw new Exception($"{child.ParentMember.Type.Name} missing Foreign Identity");

                sb.Write("LEFT JOIN ");
                if (child.ModelDetail.DataSourceSchemaName is not null)
                {
                    sb.Write(child.ModelDetail.DataSourceSchemaName.ToLower());
                    sb.Write('.');
                }
                sb.Write(child.ModelDetail.DataSourceEntityName.ToLower());
                if (child.TableName != child.ModelDetail.DataSourceEntityName)
                {
                    sb.Write(" AS ");
                    sb.Write(child.TableName.ToLower());
                }
                sb.Write(" ON ");
                WriteRelationKey(ref sb, dependant.TableName, child.TableName, RelationKey(child.ParentMember!, dependant.ModelDetail));

                AppendLineBreak(ref sb);

                GenerateJoin(child, ref sb, used);
            }
        }
        private static bool UsesJoin(ParameterDependant dependant, string conditions)
        {
            foreach (var child in dependant.Dependants.Values)
            {
                if (UsesTable(conditions, child.TableName.ToLower()) || UsesJoin(child, conditions))
                    return true;
            }
            return false;
        }
        //names aren't quoted, so a table's columns are its name and a dot not following more of a name
        private static bool UsesTable(string conditions, string table)
        {
            var index = 0;
            while ((index = conditions.IndexOf(table, index, StringComparison.Ordinal)) >= 0)
            {
                var end = index + table.Length;
                if (end < conditions.Length && conditions[end] == '.' && (index == 0 || (!Char.IsLetterOrDigit(conditions[index - 1]) && conditions[index - 1] != '_')))
                    return true;
                index = end;
            }
            return false;
        }

        //the columns of a relation's key matched pair by pair, the foreign key of the table holding it to the identity of the table it refers to
        private static void WriteRelationKey(ref CharWriter sb, string keyTable, string identityTable, (ModelMemberDetail Key, ModelMemberDetail Identity)[] pairs)
        {
            for (var i = 0; i < pairs.Length; i++)
            {
                if (i > 0)
                    sb.Write(" AND ");
                sb.Write(keyTable.ToLower());
                sb.Write('.');
                sb.Write(pairs[i].Key.StoreName.ToLower());
                sb.Write('=');
                sb.Write(identityTable.ToLower());
                sb.Write('.');
                sb.Write(pairs[i].Identity.StoreName.ToLower());
            }
        }
        /// <inheritdoc/>
        protected override void GeneratePage(int? skip, int? take, bool ordered, ref CharWriter sb, MemberContext operationContext)
        {
            if (take.HasValue)
            {
                sb.Write(" LIMIT ");
                sb.Write(take.Value);
            }
            if (skip.HasValue)
            {
                sb.Write(" OFFSET ");
                sb.Write(skip.Value);
            }
        }
        /// <inheritdoc/>
        protected override void AppendLineBreak(ref CharWriter sb)
        {
            sb.Write(Environment.NewLine);
        }

        /// <inheritdoc/>
        protected override string? OperatorToString(Operator operation, Type type)
        {
            return operation switch
            {
                Operator.Null => null,
                Operator.Not => null,
                Operator.New => throw new InvalidOperationException(),
                Operator.Lambda => throw new InvalidOperationException(),
                Operator.Evaluate => throw new InvalidOperationException(),
                Operator.Conditional => throw new InvalidOperationException(),
                Operator.Call => throw new InvalidOperationException(),
                Operator.Negative => "-",
                Operator.And => "AND",
                Operator.Or => "OR",
                Operator.Equals => "=",
                Operator.NotEquals => "!=",
                Operator.LessThanOrEquals => "<=",
                Operator.GreaterThanOrEquals => ">=",
                Operator.LessThan => "<",
                Operator.GreaterThan => ">",
                Operator.Divide => "/",
                Operator.Subtract => "-",
                Operator.Add => "+",
                Operator.Multiply => "*",
                Operator.Modulus => "%",
                Operator.EqualsNull => "IS",
                Operator.NotEqualsNull => "IS NOT",
                Operator.BitwiseAnd => "&",
                Operator.BitwiseOr => "|",
                Operator.BitwiseXor => "#",
                Operator.BitwiseNot => "~",
                _ => throw new NotImplementedException(),
            };
        }
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Zerra.Reflection;
using Zerra.Repository.IO;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.MsSql
{
    /// <summary>
    /// Converts LINQ expressions into MS SQL query strings.
    /// </summary>
    public sealed class LinqMsSqlConverter : BaseLinqSqlConverter
    {
        private static readonly LinqMsSqlConverter instance = new();
        /// <summary>
        /// Converts a LINQ query into an MS SQL query string.
        /// </summary>
        /// <returns>The SQL query string.</returns>
        public static string Convert(QueryOperation select, LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail)
        {
            return instance.ConvertInternal(select, where, order, skip, take, graph, modelDetail);
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

            //root type is object now so we can't do this
            //if (context.RootDependant.ModelDetail.Type != modelDetail.Type)
            //    throw new Exception($"Lambda type {modelDetail.Type.Name} does not match the root type {context.RootDependant.ModelDetail.Type.Name}");

            var invertBody = false;
            if (context.MemberContext.ModelStack.Count > 0)
            {
                var callingModel = context.MemberContext.ModelStack.Peek();
                var property = context.MemberContext.MemberLambdaStack.Peek();

                if (callingModel.IdentityMembers.Count != 1)
                    throw new NotSupportedException($"Relational queries support only one identity on {callingModel.Type.Name}");
                var callingModelIdentity = callingModel.IdentityMembers[0];

                var modelProperty = callingModel.GetMember(property.Member.Name);

                sb.Write('[');
                sb.Write(callingModel.DataSourceEntityName);
                sb.Write("].[");
                sb.Write(callingModelIdentity.PropertySourceName);
                sb.Write("]=");
                sb.Write('[');
                sb.Write(modelDetail.DataSourceEntityName);
                sb.Write("].[");
                sb.Write(modelProperty.ForeignIdentity);
                sb.Write("]AND");

                invertBody = context.MemberContext.InvertRelatedLambda;
                context.MemberContext.InvertRelatedLambda = false;
            }

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
                sb.Write("CASE WHEN ");
                ConvertToSqlCallMethod(call, ref sb, context);
                sb.Write(" THEN 1 ELSE 0 END");
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

                            var subMember = (MemberExpression)call.Arguments[0];
                            if (subMember.Expression is null)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            var subWhereArgument = call.Arguments[1];
                            if (subWhereArgument.NodeType == ExpressionType.Quote)
                                subWhereArgument = ((UnaryExpression)subWhereArgument).Operand;
                            var subWhere = (LambdaExpression)subWhereArgument;

                            //all related rows match when there isn't a related row that fails the condition
                            if (!context.Inverted)
                                sb.Write("NOT ");

                            var subMemberModel = ModelAnalyzer.GetModel(subMember.Expression.Type);
                            var propertyInfo = subMemberModel.GetMember(subMember.Member.Name);
                            if (propertyInfo.ForeignIdentity is null)
                                throw new Exception($"{propertyInfo.Type.Name} missing Foreign Identity");

                            var subModelInfo = ModelAnalyzer.GetModel(propertyInfo.ActualType);

                            context.MemberContext.InCallRenderIdentity++;
                            ConvertToSqlMember(subMember, ref sb, context);
                            context.MemberContext.InCallRenderIdentity--;

                            context.MemberContext.MemberLambdaStack.Push(subMember);
                            context.MemberContext.ModelStack.Push(subMemberModel);

                            sb.Write("=ANY(");
                            context.MemberContext.InvertRelatedLambda = true;
                            Convert(ref sb, QueryOperation.Many, subWhere, null, null, null, new Graph(propertyInfo.ForeignIdentity), subModelInfo, context.MemberContext);
                            sb.Write(')');

                            _ = context.MemberContext.ModelStack.Pop();
                            _ = context.MemberContext.MemberLambdaStack.Pop();
                            break;
                        }
                    case "Any":
                        {
                            if (call.Arguments.Count != 1 && call.Arguments.Count != 2)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            var subMember = (MemberExpression)call.Arguments[0];
                            if (subMember.Expression is null)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            LambdaExpression? subWhere = null;
                            if (call.Arguments.Count > 1)
                            {
                                var subWhereArgument = call.Arguments[1];
                                if (subWhereArgument.NodeType == ExpressionType.Quote)
                                    subWhereArgument = ((UnaryExpression)subWhereArgument).Operand;
                                subWhere = (LambdaExpression)subWhereArgument;
                            }

                            if (context.Inverted)
                                sb.Write("NOT ");

                            var subMemberModel = ModelAnalyzer.GetModel(subMember.Expression.Type);
                            var propertyInfo = subMemberModel.GetMember(subMember.Member.Name);
                            if (propertyInfo.ForeignIdentity is null)
                                throw new Exception($"{propertyInfo.Type.Name} missing Foreign Identity");

                            var subModelInfo = ModelAnalyzer.GetModel(propertyInfo.ActualType);

                            context.MemberContext.InCallRenderIdentity++;
                            ConvertToSqlMember(subMember, ref sb, context);
                            context.MemberContext.InCallRenderIdentity--;

                            context.MemberContext.MemberLambdaStack.Push(subMember);
                            context.MemberContext.ModelStack.Push(subMemberModel);

                            sb.Write("=ANY(");
                            Convert(ref sb, QueryOperation.Many, subWhere, null, null, null, new Graph(propertyInfo.ForeignIdentity), subModelInfo, context.MemberContext);
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

                            var subMember = (MemberExpression)call.Arguments[0];
                            if (subMember.Expression is null)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            context.MemberContext.InCallNoRender++;
                            ConvertToSqlMember(subMember, ref sb, context);
                            context.MemberContext.InCallNoRender--;

                            var subMemberModel = ModelAnalyzer.GetModel(subMember.Expression.Type);
                            var propertyInfo = subMemberModel.GetMember(subMember.Member.Name);
                            if (propertyInfo.ForeignIdentity is null)
                                throw new Exception($"{propertyInfo.Type.Name} missing Foreign Identity");

                            var subModelInfo = ModelAnalyzer.GetModel(propertyInfo.ActualType);

                            if (call.Arguments.Count == 1)
                            {
                                //without a condition there is no lambda to write the match to the related rows
                                if (subMemberModel.IdentityMembers.Count != 1)
                                    throw new NotSupportedException($"Relational queries support only one identity on {subMemberModel.Type.Name}");
                                var subMemberModelIdentity = subMemberModel.IdentityMembers[0];

                                sb.Write("(SELECT COUNT(1)");
                                GenerateFrom(subModelInfo, ref sb);
                                sb.Write("WHERE[");
                                sb.Write(subMemberModel.DataSourceEntityName);
                                sb.Write("].[");
                                sb.Write(subMemberModelIdentity.PropertySourceName);
                                sb.Write("]=[");
                                sb.Write(subModelInfo.DataSourceEntityName);
                                sb.Write("].[");
                                sb.Write(propertyInfo.ForeignIdentity);
                                sb.Write("])");
                                break;
                            }

                            var subWhereArgument = call.Arguments[1];
                            if (subWhereArgument.NodeType == ExpressionType.Quote)
                                subWhereArgument = ((UnaryExpression)subWhereArgument).Operand;
                            var subWhere = (LambdaExpression)subWhereArgument;

                            context.MemberContext.MemberLambdaStack.Push(subMember);
                            context.MemberContext.ModelStack.Push(subMemberModel);

                            sb.Write('(');
                            Convert(ref sb, QueryOperation.Count, subWhere, null, null, null, new Graph(propertyInfo.ForeignIdentity), subModelInfo, context.MemberContext);
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

                            if (call.Arguments[0] is not MemberExpression subMember || subMember.Expression is null)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            var selectorArgument = call.Arguments[1];
                            if (selectorArgument.NodeType == ExpressionType.Quote)
                                selectorArgument = ((UnaryExpression)selectorArgument).Operand;
                            if (selectorArgument is not LambdaExpression selector || selector.Parameters.Count != 1)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            var subMemberModel = ModelAnalyzer.GetModel(subMember.Expression.Type);
                            var propertyInfo = subMemberModel.GetMember(subMember.Member.Name);
                            if (propertyInfo.ForeignIdentity is null)
                                throw new Exception($"{propertyInfo.Type.Name} missing Foreign Identity");
                            if (subMemberModel.IdentityMembers.Count != 1)
                                throw new NotSupportedException($"Relational queries support only one identity on {subMemberModel.Type.Name}");
                            var subMemberModelIdentity = subMemberModel.IdentityMembers[0];

                            var subModelInfo = ModelAnalyzer.GetModel(propertyInfo.ActualType);

                            context.MemberContext.InCallNoRender++;
                            ConvertToSqlMember(subMember, ref sb, context);
                            context.MemberContext.InCallNoRender--;

                            var selectorType = selector.Body.Type;
                            if (selectorType.Name == nullableTypeName)
                                selectorType = selectorType.GetGenericArguments()[0];
                            var averageAsFloat = call.Method.Name == "Average" && (selectorType == typeof(byte) || selectorType == typeof(sbyte) || selectorType == typeof(short) || selectorType == typeof(ushort) || selectorType == typeof(int) || selectorType == typeof(uint) || selectorType == typeof(long) || selectorType == typeof(ulong));

                            //the selector is written first so the joins it needs are known before the FROM
                            var subDependant = new ParameterDependant(subModelInfo, null);
                            var sbSelector = new CharWriter();
                            try
                            {
                                var selectorParameter = selector.Parameters[0];
                                context.MemberContext.DependantStack.Push(subDependant);
                                context.MemberContext.ModelStack.Push(subModelInfo);
                                context.MemberContext.ModelContexts.Add(selectorParameter, subModelInfo);

                                ConvertToSql(selector.Body, ref sbSelector, new BuilderContext(subDependant, context.MemberContext));

                                _ = context.MemberContext.ModelContexts.Remove(selectorParameter);
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
                                        //AVG of integers is an integer in MS SQL where the LINQ average is a double
                                        if (averageAsFloat)
                                        {
                                            sb.Write("AVG(CAST(");
                                            sb.Write(sbSelector);
                                            sb.Write(" AS float))");
                                        }
                                        else
                                        {
                                            sb.Write("AVG(");
                                            sb.Write(sbSelector);
                                            sb.Write(')');
                                        }
                                        break;
                                }
                                GenerateFrom(subModelInfo, ref sb);
                                GenerateJoin(subDependant, ref sb);
                                sb.Write("WHERE[");
                                sb.Write(subMemberModel.DataSourceEntityName);
                                sb.Write("].[");
                                sb.Write(subMemberModelIdentity.PropertySourceName);
                                sb.Write("]=[");
                                sb.Write(subModelInfo.DataSourceEntityName);
                                sb.Write("].[");
                                sb.Write(propertyInfo.ForeignIdentity);
                                sb.Write("])");
                            }
                            finally
                            {
                                sbSelector.Dispose();
                            }
                            break;
                        }
                    case "Contains":
                        {
                            if (call.Arguments.Count != 2)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                            var callingObject = call.Arguments[0];
                            var lambda = call.Arguments[1];

                            ConvertToSql(lambda, ref sb, context);

                            sb.Write(context.Inverted ? "NOT IN" : "IN");

                            ConvertToSql(callingObject, ref sb, context);
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

                            if (ignoreCase)
                                sb.Write("LOWER(");
                            ConvertToSql(call.Object, ref sb, context);
                            if (ignoreCase)
                                sb.Write(')');

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
                                    if (ignoreCase)
                                        valueText = valueText.ToLowerInvariant();
                                    valueText = valueText.Replace("!", "!!").Replace("%", "!%").Replace("_", "!_").Replace("[", "![").Replace("'", "''");
                                    sb.Write("N'");
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
                                    sb.Write("N'%'+");
                                if (ignoreCase)
                                    sb.Write("LOWER(");
                                sb.Write("REPLACE(REPLACE(REPLACE(REPLACE(");
                                ConvertToSql(text, ref sb, context);
                                sb.Write(",N'!',N'!!'),N'%',N'!%'),N'_',N'!_'),N'[',N'![')");
                                if (ignoreCase)
                                    sb.Write(')');
                                if (anyEnd)
                                    sb.Write("+N'%'");
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

                            //DATALENGTH counts trailing spaces, comparing to '' in MS SQL ignores them
                            if (call.Method.Name == "IsNullOrEmpty")
                            {
                                sb.Write("COALESCE(DATALENGTH(");
                                ConvertToSql(call.Arguments[0], ref sb, context);
                                sb.Write("),0)");
                            }
                            else
                            {
                                sb.Write("COALESCE(DATALENGTH(LTRIM(RTRIM(");
                                ConvertToSql(call.Arguments[0], ref sb, context);
                                sb.Write("))),0)");
                            }
                            sb.Write(inverted ? "!=0" : "=0");

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
                                    sb.Write("LTRIM(RTRIM(");
                                    ConvertToSql(call.Object, ref sb, context);
                                    sb.Write("))");
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
                            sb.Write(",(");
                            ConvertToSql(call.Arguments[0], ref sb, context);
                            sb.Write(")+1,");
                            if (call.Arguments.Count == 2)
                            {
                                sb.Write('(');
                                ConvertToSql(call.Arguments[1], ref sb, context);
                                sb.Write(')');
                            }
                            else
                            {
                                //the byte length is at least the character length
                                sb.Write("DATALENGTH(");
                                ConvertToSql(call.Object, ref sb, context);
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
                            sb.Write("(CHARINDEX(");
                            ConvertToSql(call.Arguments[0], ref sb, context);
                            sb.Write(',');
                            ConvertToSql(call.Object, ref sb, context);
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
                            //MS SQL CONCAT needs at least two values
                            if (values.Count < 2)
                                sb.Write(",N''");
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
                        {
                            Expression? digits = null;
                            Expression? mode = null;
                            if (call.Arguments.Count == 2)
                            {
                                if (call.Arguments[1].Type == typeof(MidpointRounding))
                                    mode = call.Arguments[1];
                                else
                                    digits = call.Arguments[1];
                            }
                            else if (call.Arguments.Count == 3)
                            {
                                digits = call.Arguments[1];
                                mode = call.Arguments[2];
                            }
                            else if (call.Arguments.Count != 1)
                            {
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
                            }

                            //SQL rounds a midpoint away from zero
                            if (mode is not null && (!IsEvaluatable(mode) || (MidpointRounding)Evaluate(mode)! != MidpointRounding.AwayFromZero))
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}, SQL only rounds with {nameof(MidpointRounding)}.{nameof(MidpointRounding.AwayFromZero)}");

                            sb.Write("ROUND((");
                            ConvertToSql(call.Arguments[0], ref sb, context);
                            sb.Write("),");
                            if (digits is null)
                            {
                                sb.Write('0');
                            }
                            else
                            {
                                sb.Write('(');
                                ConvertToSql(digits, ref sb, context);
                                sb.Write(')');
                            }
                            sb.Write(')');
                            break;
                        }
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

                                ConvertToSql(lambda, ref sb, context);

                                sb.Write(context.Inverted ? "NOT IN" : "IN");

                                ConvertToSql(callingObject, ref sb, context);
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
                    if (!parentDependant.Dependants.TryGetValue(subModelInfo.Type, out var dependant))
                    {
                        dependant = new ParameterDependant(subModelInfo, modelProperty);
                        parentDependant.Dependants.Add(subModelInfo.Type, dependant);
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

                sb.Write('[');
                sb.Write(modelDetail.DataSourceEntityName);
                sb.Write("].[");
                sb.Write(modelIdentity.PropertySourceName);
                sb.Write("]");
            }
            else
            {
                if (context.MemberContext.MemberAccessStack.Count > 0)
                {
                    //functions of a column such as x.Date.Year are written by ConvertToSqlMemberFunction before reaching here
                    var memberProperty = context.MemberContext.MemberAccessStack.Peek();
                    throw new NotSupportedException($"{member.Member.Name}.{memberProperty.Member.Name} not supported");
                }

                sb.Write('[');
                sb.Write(modelDetail.DataSourceEntityName);
                sb.Write(']');
                sb.Write('.');
                sb.Write('[');
                sb.Write(modelProperty.PropertySourceName);
                sb.Write(']');
                var lastOperator = context.MemberContext.OperatorStack.Peek();
                if ((modelProperty.ActualType == typeof(bool) || modelProperty.ActualType == typeof(bool?)) && (lastOperator == Operator.And || lastOperator == Operator.Or || lastOperator == Operator.Not || (lastOperator == Operator.Lambda && !context.IsOrderBy)))
                {
                    //a boolean member used as a condition is written as a comparison, which also carries any NOT around it
                    sb.Write(context.Inverted ? "=0" : "=1");
                }
                else if (lastOperator == Operator.And || lastOperator == Operator.Or)
                {
                    sb.Write("IS NOT NULL");
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
            var suffix = ")";

            if (declaringType == typeof(string))
            {
                if (member.Member.Name == "Length")
                    prefix = "LEN(";
            }
            else if (declaringType == typeof(DateTime) || declaringType == typeof(DateTimeOffset) || declaringType == typeof(DateOnly))
            {
                switch (member.Member.Name)
                {
                    case "Year": prefix = "DATEPART(year,"; break;
                    case "Month": prefix = "DATEPART(month,"; break;
                    case "Day": prefix = "DATEPART(day,"; break;
                    case "Hour": prefix = "DATEPART(hour,"; break;
                    case "Minute": prefix = "DATEPART(minute,"; break;
                    case "Second": prefix = "DATEPART(second,"; break;
                    case "Millisecond": prefix = "DATEPART(millisecond,"; break;
                    case "DayOfYear": prefix = "DATEPART(dayofyear,"; break;
                    case "DayOfWeek":
                        //weekday depends on DATEFIRST, this makes Sunday 0 like DayOfWeek
                        prefix = "((DATEPART(weekday,";
                        suffix = ")+@@DATEFIRST+6)%7)";
                        break;
                    case "Date":
                        if (declaringType != typeof(DateOnly))
                        {
                            prefix = "CAST(";
                            suffix = " AS date)";
                        }
                        break;
                }
            }
            else if (declaringType == typeof(TimeOnly))
            {
                switch (member.Member.Name)
                {
                    case "Hour": prefix = "DATEPART(hour,"; break;
                    case "Minute": prefix = "DATEPART(minute,"; break;
                    case "Second": prefix = "DATEPART(second,"; break;
                    case "Millisecond": prefix = "DATEPART(millisecond,"; break;
                }
            }
            else if (declaringType == typeof(TimeSpan))
            {
                //a TimeSpan is stored as a time
                switch (member.Member.Name)
                {
                    case "Hours": prefix = "DATEPART(hour,"; break;
                    case "Minutes": prefix = "DATEPART(minute,"; break;
                    case "Seconds": prefix = "DATEPART(second,"; break;
                    case "Milliseconds": prefix = "DATEPART(millisecond,"; break;
                    case "TotalHours":
                        prefix = "(DATEDIFF_BIG(microsecond,CAST('00:00:00' AS time),";
                        suffix = ")/3600000000.0)";
                        break;
                    case "TotalMinutes":
                        prefix = "(DATEDIFF_BIG(microsecond,CAST('00:00:00' AS time),";
                        suffix = ")/60000000.0)";
                        break;
                    case "TotalSeconds":
                        prefix = "(DATEDIFF_BIG(microsecond,CAST('00:00:00' AS time),";
                        suffix = ")/1000000.0)";
                        break;
                    case "TotalMilliseconds":
                        prefix = "(DATEDIFF_BIG(microsecond,CAST('00:00:00' AS time),";
                        suffix = ")/1000.0)";
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
                        sb.Write("CASE WHEN ");
                        ConvertToSql(member.Expression!, ref sb, context);
                        sb.Write(" IS NOT NULL THEN 1 ELSE 0 END");
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
                sb.Write(inverted ? "=0" : "=1");
        }
        /// <inheritdoc/>
        protected override void ConvertToSqlStringConcat(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            var binary = (BinaryExpression)exp;

            context.MemberContext.OperatorStack.Push(Operator.Call);
            var invertStack = context.InvertStack;
            context.InvertStack = 0;

            //CONCAT treats NULL as empty like string addition
            sb.Write("CONCAT((");
            ConvertToSql(binary.Left, ref sb, context);
            sb.Write("),(");
            ConvertToSql(binary.Right, ref sb, context);
            sb.Write("))");

            context.InvertStack = invertStack;
            _ = context.MemberContext.OperatorStack.Pop();
        }

        /// <inheritdoc/>
        protected override bool ConvertToSqlValueRender(MemberExpression? memberProperty, Type type, object? value, ref CharWriter sb, BuilderContext context)
        {
            //avoid TypeDetail for AOT support

            CoreType? coreType = null;
            if (TypeLookup.GetCoreType(type, out var coreTypeLookup))
                coreType = coreTypeLookup;

            if (value is null)
            {
                sb.Write("NULL");
                return false;
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
                            sb.Write((bool)value ? '1' : '0');
                        return false;
                    case CoreType.Byte: sb.Write((byte)value); return false;
                    case CoreType.SByte: sb.Write((sbyte)value); return false;
                    case CoreType.Int16: sb.Write((short)value); return false;
                    case CoreType.UInt16: sb.Write((ushort)value); return false;
                    case CoreType.Int32: sb.Write((int)value); return false;
                    case CoreType.UInt32: sb.Write((uint)value); return false;
                    case CoreType.Int64: sb.Write((long)value); return false;
                    case CoreType.UInt64: sb.Write((ulong)value); return false;
                    case CoreType.Single: sb.Write((float)value); return false;
                    case CoreType.Double: sb.Write((double)value); return false;
                    case CoreType.Decimal: sb.Write((decimal)value); return false;
                    case CoreType.Char:
                        if ((char)value == '\'')
                        {
                            sb.Write("\'\'\'\'");
                        }
                        else
                        {
                            sb.Write("N'");
                            sb.Write((char)value);
                            sb.Write('\'');
                        }
                        return false;
                    case CoreType.DateTime:
                        if (memberProperty is not null)
                        {
                            switch (memberProperty.Member.Name)
                            {
                                case "Year":
                                    sb.Write('\'');
                                    sb.Write(((DateTime)value).Year);
                                    sb.Write('\'');
                                    return true;
                                case "Month":
                                    sb.Write('\'');
                                    sb.Write(((DateTime)value).Month);
                                    sb.Write('\'');
                                    return true;
                                case "Day":
                                    sb.Write('\'');
                                    sb.Write(((DateTime)value).Day);
                                    sb.Write('\'');
                                    return true;
                                case "Hour":
                                    sb.Write('\'');
                                    sb.Write(((DateTime)value).Hour);
                                    sb.Write('\'');
                                    return true;
                                case "Minute":
                                    sb.Write('\'');
                                    sb.Write(((DateTime)value).Minute);
                                    sb.Write('\'');
                                    return true;
                                case "Second":
                                    sb.Write('\'');
                                    sb.Write(((DateTime)value).Second);
                                    sb.Write('\'');
                                    return true;
                                case "Millisecond":
                                    sb.Write('\'');
                                    sb.Write(((DateTime)value).Millisecond);
                                    sb.Write('\'');
                                    return true;
                                case "DayOfYear":
                                    sb.Write('\'');
                                    sb.Write(((DateTime)value).DayOfYear);
                                    sb.Write('\'');
                                    return true;
                                case "DayOfWeek":
                                    sb.Write('\'');
                                    sb.Write(((int)((DateTime)value).DayOfWeek).ToString());
                                    sb.Write('\'');
                                    return true;
                            }
                        }
                        sb.Write('\'');
                        sb.Write((DateTime)value, CharWriter.DateTimeFormat.MsSql);
                        sb.Write('\'');
                        return false;
                    case CoreType.DateTimeOffset:
                        if (memberProperty is not null)
                        {
                            switch (memberProperty.Member.Name)
                            {
                                case "Year":
                                    sb.Write('\'');
                                    sb.Write(((DateTimeOffset)value).Year);
                                    sb.Write('\'');
                                    return true;
                                case "Month":
                                    sb.Write('\'');
                                    sb.Write(((DateTimeOffset)value).Month);
                                    sb.Write('\'');
                                    return true;
                                case "Day":
                                    sb.Write('\'');
                                    sb.Write(((DateTimeOffset)value).Day);
                                    sb.Write('\'');
                                    return true;
                                case "Hour":
                                    sb.Write('\'');
                                    sb.Write(((DateTimeOffset)value).Hour);
                                    sb.Write('\'');
                                    return true;
                                case "Minute":
                                    sb.Write('\'');
                                    sb.Write(((DateTimeOffset)value).Minute);
                                    sb.Write('\'');
                                    return true;
                                case "Second":
                                    sb.Write('\'');
                                    sb.Write(((DateTimeOffset)value).Second);
                                    sb.Write('\'');
                                    return true;
                                case "Millisecond":
                                    sb.Write('\'');
                                    sb.Write(((DateTimeOffset)value).Millisecond);
                                    sb.Write('\'');
                                    return true;
                                case "DayOfYear":
                                    sb.Write('\'');
                                    sb.Write(((DateTimeOffset)value).DayOfYear);
                                    sb.Write('\'');
                                    return true;
                                case "DayOfWeek":
                                    sb.Write('\'');
                                    sb.Write(((int)((DateTimeOffset)value).DayOfWeek).ToString());
                                    sb.Write('\'');
                                    return true;
                            }
                        }
                        sb.Write('\'');
                        sb.Write((DateTimeOffset)value, CharWriter.DateTimeFormat.MsSql);
                        sb.Write('\'');
                        return false;
                    case CoreType.TimeSpan:
                        if (memberProperty is not null)
                        {
                            switch (memberProperty.Member.Name)
                            {
                                case "Hours":
                                    sb.Write('\'');
                                    sb.Write(((TimeSpan)value).Hours);
                                    sb.Write('\'');
                                    return true;
                                case "Minutes":
                                    sb.Write('\'');
                                    sb.Write(((TimeSpan)value).Minutes);
                                    sb.Write('\'');
                                    return true;
                                case "Seconds":
                                    sb.Write('\'');
                                    sb.Write(((TimeSpan)value).Seconds);
                                    sb.Write('\'');
                                    return true;
                                case "Milliseconds":
                                    sb.Write('\'');
                                    sb.Write(((TimeSpan)value).Milliseconds);
                                    sb.Write('\'');
                                    return true;
                                case "TotalHours":
                                    sb.Write('\'');
                                    sb.Write(((TimeSpan)value).TotalHours);
                                    sb.Write('\'');
                                    return true;
                                case "TotalMinutes":
                                    sb.Write('\'');
                                    sb.Write(((TimeSpan)value).TotalMinutes);
                                    sb.Write('\'');
                                    return true;
                                case "TotalSeconds":
                                    sb.Write('\'');
                                    sb.Write(((TimeSpan)value).TotalSeconds);
                                    sb.Write('\'');
                                    return true;
                                case "TotalMilliseconds":
                                    sb.Write('\'');
                                    sb.Write(((TimeSpan)value).TotalMilliseconds);
                                    sb.Write('\'');
                                    return true;
                            }
                        }
                        sb.Write('\'');
                        sb.Write((TimeSpan)value, CharWriter.TimeFormat.MsSql);
                        sb.Write('\'');
                        return false;
                    case CoreType.DateOnly:
                        if (memberProperty is not null)
                        {
                            switch (memberProperty.Member.Name)
                            {
                                case "Year":
                                    sb.Write('\'');
                                    sb.Write(((DateOnly)value).Year);
                                    sb.Write('\'');
                                    return true;
                                case "Month":
                                    sb.Write('\'');
                                    sb.Write(((DateOnly)value).Month);
                                    sb.Write('\'');
                                    return true;
                                case "Day":
                                    sb.Write('\'');
                                    sb.Write(((DateOnly)value).Day);
                                    sb.Write('\'');
                                    return true;
                                case "DayOfYear":
                                    sb.Write('\'');
                                    sb.Write(((DateOnly)value).DayOfYear);
                                    sb.Write('\'');
                                    return true;
                                case "DayOfWeek":
                                    sb.Write('\'');
                                    sb.Write(((int)((DateOnly)value).DayOfWeek).ToString());
                                    sb.Write('\'');
                                    return true;
                            }
                        }
                        sb.Write('\'');
                        sb.Write((DateOnly)value, CharWriter.DateTimeFormat.MsSql);
                        sb.Write('\'');
                        return false;
                    case CoreType.TimeOnly:
                        if (memberProperty is not null)
                        {
                            switch (memberProperty.Member.Name)
                            {
                                case "Hour":
                                    sb.Write('\'');
                                    sb.Write(((TimeOnly)value).Hour);
                                    sb.Write('\'');
                                    return true;
                                case "Minute":
                                    sb.Write('\'');
                                    sb.Write(((TimeOnly)value).Minute);
                                    sb.Write('\'');
                                    return true;
                                case "Second":
                                    sb.Write('\'');
                                    sb.Write(((TimeOnly)value).Second);
                                    sb.Write('\'');
                                    return true;
                                case "Millisecond":
                                    sb.Write('\'');
                                    sb.Write(((TimeOnly)value).Millisecond);
                                    sb.Write('\'');
                                    return true;
                            }
                        }
                        sb.Write('\'');
                        sb.Write((TimeOnly)value, CharWriter.TimeFormat.MsSql);
                        sb.Write('\'');
                        return false;
                    case CoreType.Guid:
                        sb.Write('\'');
                        sb.Write((Guid)value);
                        sb.Write('\'');
                        return false;
                    case CoreType.String:
                        sb.Write("N'");
                        sb.Write(((string)value).Replace("'", "''"));
                        sb.Write('\''); return false;
                }
            }

            if (type.IsEnum)
            {
                //an enum isn't a column type, so it's compared to a number column that C# converted to the underlying type
                if (Enum.GetUnderlyingType(type) == typeof(ulong))
                    sb.Write(System.Convert.ToUInt64(value));
                else
                    sb.Write(System.Convert.ToInt64(value));
                return false;
            }

            if (type.IsArray || type.Name == "ReadOnlySpan`1" || type.Name == "Span`1")
            {
                var arrayType = type.IsArray ? type.GetElementType()! : type.GetGenericArguments()[0];
                if (arrayType == typeof(byte))
                {
                    sb.Write("0x");
                    sb.Write((byte[])value, CharWriter.ByteFormat.Hex);
                    return false;
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
                    return false;
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
                return false;
            }

            if (type == typeof(object))
            {
                sb.Write("N'");
                sb.Write(value.ToString()!.Replace("\'", "''"));
                sb.Write('\'');
                return false;
            }

            throw new NotImplementedException($"{type.Name} value {value?.ToString()} not converted");
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
        protected override void GenerateOrderSkipTake(QueryOrder? order, int? skip, int? take, ref CharWriter sb, ParameterDependant rootDependant, MemberContext operationContext)
        {
            if (order?.OrderExpressions.Length > 0)
            {
                sb.Write("ORDER BY");
                var hasOrder = false;
                foreach (var orderExp in order.OrderExpressions)
                {
                    var context = new BuilderContext(rootDependant, operationContext) { IsOrderBy = true };
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
        protected override void GenerateSelect(QueryOperation select, Graph? graph, ModelDetail modelDetail, ref CharWriter sb)
        {
            switch (select)
            {
                case QueryOperation.Many:
                    sb.Write("SELECT");
                    GenerateSelectProperties(graph, modelDetail, ref sb);
                    break;
                case QueryOperation.First:
                    sb.Write("SELECT TOP(1)");
                    GenerateSelectProperties(graph, modelDetail, ref sb);
                    break;
                case QueryOperation.Single:
                    sb.Write("SELECT TOP(2)");
                    GenerateSelectProperties(graph, modelDetail, ref sb);
                    break;
                case QueryOperation.Count:
                    sb.Write("SELECT COUNT(1)");
                    break;
                case QueryOperation.Any:
                    sb.Write("SELECT TOP(1) 1");
                    break;
            }
        }
        /// <inheritdoc/>
        protected override void GenerateSelectProperties(Graph? graph, ModelDetail modelDetail, ref CharWriter sb)
        {
            AppendLineBreak(ref sb);
            if (graph is null || (graph.IncludeAllMembers && !graph.HasRemovedMembers))
            {
                sb.Write('[');
                sb.Write(modelDetail.DataSourceEntityName);
                sb.Write("].*");
                AppendLineBreak(ref sb);
            }
            else
            {
                var passedfirst = false;
                foreach (var member in modelDetail.Members)
                {
                    if (graph is not null && !graph.HasMember(member.Name))
                        continue;

                    if (member.PropertySourceName is not null && member.ForeignIdentity is null)
                    {
                        if (passedfirst)
                            sb.Write(',');
                        else
                            passedfirst = true;
                        sb.Write('[');
                        sb.Write(modelDetail.DataSourceEntityName);
                        sb.Write("].[");
                        sb.Write(member.Name);
                        sb.Write(']');
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
            sb.Write(" FROM[");
            sb.Write(modelDetail.DataSourceEntityName);
            sb.Write(']');
            AppendLineBreak(ref sb);
        }
        /// <inheritdoc/>
        protected override void GenerateJoin(ParameterDependant dependant, ref CharWriter sb)
        {
            foreach (var child in dependant.Dependants.Values)
            {
                if (child.ModelDetail.IdentityMembers.Count != 1)
                    throw new NotSupportedException($"Relational queries support only one identity on {child.ModelDetail.Type.Name}");
                var dependantIdentity = child.ModelDetail.IdentityMembers[0];

                sb.Write("LEFT JOIN[");
                sb.Write(child.ModelDetail.DataSourceEntityName);
                sb.Write("]ON[");
                sb.Write(dependant.ModelDetail.DataSourceEntityName);
                sb.Write("].[");
                sb.Write(child.ParentMember?.ForeignIdentity);
                sb.Write("]=[");
                sb.Write(child.ModelDetail.DataSourceEntityName);
                sb.Write("].[");
                sb.Write(dependantIdentity.PropertySourceName);
                sb.Write(']');

                AppendLineBreak(ref sb);

                GenerateJoin(child, ref sb);
            }
        }
        /// <inheritdoc/>
        protected override void GenerateEnding(QueryOperation select, Graph? graph, ModelDetail modelDetail, ref CharWriter sb)
        {

        }

        /// <inheritdoc/>
        protected override void AppendLineBreak(ref CharWriter sb)
        {
            sb.Write(Environment.NewLine);
        }

        /// <inheritdoc/>
        protected override string? OperatorToString(Operator operation)
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
                Operator.BitwiseXor => "^",
                Operator.BitwiseNot => "~",
                _ => throw new NotImplementedException(),
            };
        }
    }
}
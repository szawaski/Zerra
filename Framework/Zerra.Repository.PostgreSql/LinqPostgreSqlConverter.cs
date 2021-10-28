// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Zerra.IO;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.PostgreSql
{
    public static class LinqPostgreSqlConverter
    {
        public static string Convert(QueryOperation select, Expression where, QueryOrder order, int? skip, int? take, Graph graph, ModelDetail modelDetail)
        {
            var operationContext = new MemberContext();
            var sb = new CharWriteBuffer();
            try
            {
                Convert(ref sb, select, where, order, skip, take, graph, modelDetail, operationContext);
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }
        }

        private static void Convert(ref CharWriteBuffer sb, QueryOperation select, Expression where, QueryOrder order, int? skip, int? take, Graph graph, ModelDetail modelDetail, MemberContext operationContext)
        {
            var hasWhere = where != null;
            var hasOrderSkipTake = (select == QueryOperation.Many || select == QueryOperation.First) && (order?.OrderExpressions.Length > 0 || skip > 0 || take > 0);

            GenerateSelect(select, graph, modelDetail, ref sb);

            GenerateFrom(modelDetail, ref sb);

            if (hasWhere || hasOrderSkipTake)
            {
                var rootDependant = new ParameterDependant(modelDetail, null);
                var sbWhereOrder = new CharWriteBuffer();
                try
                {
                    if (hasWhere)
                        GenerateWhere(where, ref sbWhereOrder, rootDependant, operationContext);
                    if (hasOrderSkipTake)
                        GenerateOrderSkipTake(order, skip, take, ref sbWhereOrder, rootDependant, operationContext);

                    GenerateJoin(rootDependant, ref sb);

                    sb.Write(sbWhereOrder);
                }
                finally
                {
                    sbWhereOrder.Dispose();
                }
            }

            GenerateEnding(select, graph, modelDetail, ref sb);
        }

        private static void ConvertToSql(Expression exp, ref CharWriteBuffer sb, BuilderContext context)
        {
            switch (exp.NodeType)
            {
                case ExpressionType.Add:
                    ConvertToSqlBinary(Operator.Add, exp, ref sb, context);
                    break;
                case ExpressionType.AddAssign:
                    throw new NotImplementedException();
                case ExpressionType.AddAssignChecked:
                    throw new NotImplementedException();
                case ExpressionType.AddChecked:
                    ConvertToSqlBinary(Operator.Add, exp, ref sb, context);
                    break;
                case ExpressionType.And:
                    ConvertToSqlBinary(context.Inverted ? Operator.Or : Operator.And, exp, ref sb, context);
                    break;
                case ExpressionType.AndAlso:
                    ConvertToSqlBinary(context.Inverted ? Operator.Or : Operator.And, exp, ref sb, context);
                    break;
                case ExpressionType.AndAssign:
                    throw new NotImplementedException();
                case ExpressionType.ArrayIndex:
                    throw new NotImplementedException();
                case ExpressionType.ArrayLength:
                    throw new NotImplementedException();
                case ExpressionType.Assign:
                    throw new NotImplementedException();
                case ExpressionType.Block:
                    throw new NotImplementedException();
                case ExpressionType.Call:
                    ConvertToSqlCall(exp, ref sb, context);
                    break;
                case ExpressionType.Coalesce:
                    throw new NotImplementedException();
                case ExpressionType.Conditional:
                    ConvertToSqlConditional(exp, ref sb, context);
                    break;
                case ExpressionType.Constant:
                    ConvertToSqlConstant(exp, ref sb, context);
                    break;
                case ExpressionType.Convert:
                    ConvertToSqlUnary(Operator.Null, null, exp, ref sb, context);
                    break;
                case ExpressionType.ConvertChecked:
                    ConvertToSqlUnary(Operator.Null, null, exp, ref sb, context);
                    break;
                case ExpressionType.DebugInfo:
                    throw new NotImplementedException();
                case ExpressionType.Decrement:
                    throw new NotImplementedException();
                case ExpressionType.Default:
                    throw new NotImplementedException();
                case ExpressionType.Divide:
                    ConvertToSqlBinary(Operator.Divide, exp, ref sb, context);
                    break;
                case ExpressionType.DivideAssign:
                    throw new NotImplementedException();
                case ExpressionType.Dynamic:
                    throw new NotImplementedException();
                case ExpressionType.Equal:
                    ConvertToSqlBinary(context.Inverted ? Operator.NotEquals : Operator.Equals, exp, ref sb, context);
                    break;
                case ExpressionType.ExclusiveOr:
                    throw new NotImplementedException();
                case ExpressionType.ExclusiveOrAssign:
                    throw new NotImplementedException();
                case ExpressionType.Extension:
                    throw new NotImplementedException();
                case ExpressionType.Goto:
                    throw new NotImplementedException();
                case ExpressionType.GreaterThan:
                    ConvertToSqlBinary(context.Inverted ? Operator.LessThanOrEquals : Operator.GreaterThan, exp, ref sb, context);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    ConvertToSqlBinary(context.Inverted ? Operator.LessThan : Operator.GreaterThanOrEquals, exp, ref sb, context);
                    break;
                case ExpressionType.Increment:
                    throw new NotImplementedException();
                case ExpressionType.Index:
                    throw new NotImplementedException();
                case ExpressionType.Invoke:
                    throw new NotImplementedException();
                case ExpressionType.IsFalse:
                    throw new NotImplementedException();
                case ExpressionType.IsTrue:
                    throw new NotImplementedException();
                case ExpressionType.Label:
                    throw new NotImplementedException();
                case ExpressionType.Lambda:
                    ConvertToSqlLambda(exp, ref sb, context);
                    break;
                case ExpressionType.LeftShift:
                    throw new NotImplementedException();
                case ExpressionType.LeftShiftAssign:
                    throw new NotImplementedException();
                case ExpressionType.LessThan:
                    ConvertToSqlBinary(context.Inverted ? Operator.GreaterThanOrEquals : Operator.LessThan, exp, ref sb, context);
                    break;
                case ExpressionType.LessThanOrEqual:
                    ConvertToSqlBinary(context.Inverted ? Operator.GreaterThan : Operator.LessThanOrEquals, exp, ref sb, context);
                    break;
                case ExpressionType.ListInit:
                    throw new NotImplementedException();
                case ExpressionType.Loop:
                    throw new NotImplementedException();
                case ExpressionType.MemberAccess:
                    ConvertToSqlMember(exp, ref sb, context);
                    break;
                case ExpressionType.MemberInit:
                    throw new NotImplementedException();
                case ExpressionType.Modulo:
                    ConvertToSqlBinary(Operator.Modulus, exp, ref sb, context);
                    break;
                case ExpressionType.ModuloAssign:
                    throw new NotImplementedException();
                case ExpressionType.Multiply:
                    ConvertToSqlBinary(Operator.Multiply, exp, ref sb, context);
                    break;
                case ExpressionType.MultiplyAssign:
                    throw new NotImplementedException();
                case ExpressionType.MultiplyAssignChecked:
                    throw new NotImplementedException();
                case ExpressionType.MultiplyChecked:
                    ConvertToSqlBinary(Operator.Multiply, exp, ref sb, context);
                    break;
                case ExpressionType.Negate:
                    ConvertToSqlUnary(Operator.Negative, null, exp, ref sb, context);
                    break;
                case ExpressionType.NegateChecked:
                    ConvertToSqlUnary(Operator.Negative, null, exp, ref sb, context);
                    break;
                case ExpressionType.New:
                    ConvertToSqlNew(exp, ref sb, context);
                    break;
                case ExpressionType.NewArrayBounds:
                    throw new NotImplementedException();
                case ExpressionType.NewArrayInit:
                    throw new NotImplementedException();
                case ExpressionType.Not:
                    context.InvertStack++;
                    ConvertToSqlUnary(Operator.Null, null, exp, ref sb, context);
                    context.InvertStack--;
                    break;
                case ExpressionType.NotEqual:
                    ConvertToSqlBinary(context.Inverted ? Operator.Equals : Operator.NotEquals, exp, ref sb, context);
                    break;
                case ExpressionType.OnesComplement:
                    throw new NotImplementedException();
                case ExpressionType.Or:
                    ConvertToSqlBinary(context.Inverted ? Operator.And : Operator.Or, exp, ref sb, context);
                    break;
                case ExpressionType.OrAssign:
                    throw new NotImplementedException();
                case ExpressionType.OrElse:
                    ConvertToSqlBinary(context.Inverted ? Operator.And : Operator.Or, exp, ref sb, context);
                    break;
                case ExpressionType.Parameter:
                    ConvertToSqlParameter(exp, ref sb, context);
                    break;
                case ExpressionType.PostDecrementAssign:
                    throw new NotImplementedException();
                case ExpressionType.PostIncrementAssign:
                    throw new NotImplementedException();
                case ExpressionType.Power:
                    throw new NotImplementedException();
                case ExpressionType.PowerAssign:
                    throw new NotImplementedException();
                case ExpressionType.PreDecrementAssign:
                    throw new NotImplementedException();
                case ExpressionType.PreIncrementAssign:
                    throw new NotImplementedException();
                case ExpressionType.Quote:
                    throw new NotImplementedException();
                case ExpressionType.RightShift:
                    throw new NotImplementedException();
                case ExpressionType.RightShiftAssign:
                    throw new NotImplementedException();
                case ExpressionType.RuntimeVariables:
                    throw new NotImplementedException();
                case ExpressionType.Subtract:
                    ConvertToSqlBinary(Operator.Subtract, exp, ref sb, context);
                    break;
                case ExpressionType.SubtractAssign:
                    throw new NotImplementedException();
                case ExpressionType.SubtractAssignChecked:
                    throw new NotImplementedException();
                case ExpressionType.SubtractChecked:
                    ConvertToSqlBinary(Operator.Subtract, exp, ref sb, context);
                    break;
                case ExpressionType.Switch:
                    throw new NotImplementedException();
                case ExpressionType.Throw:
                    throw new NotImplementedException();
                case ExpressionType.Try:
                    throw new NotImplementedException();
                case ExpressionType.TypeAs:
                    throw new NotImplementedException();
                case ExpressionType.TypeEqual:
                    throw new NotImplementedException();
                case ExpressionType.TypeIs:
                    throw new NotImplementedException();
                case ExpressionType.UnaryPlus:
                    throw new NotImplementedException();
                case ExpressionType.Unbox:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }
        private static void ConvertToSqlLambda(Expression exp, ref CharWriteBuffer sb, BuilderContext context)
        {
            context.MemberContext.OperatorStack.Push(Operator.Lambda);

            var lambda = exp as LambdaExpression;

            if (lambda.Parameters.Count != 1)
                throw new NotSupportedException("Can only parse a lambda with one parameter.");
            var parameter = lambda.Parameters[0];
            var modelDetail = ModelAnalyzer.GetModel(parameter.Type);

            if (context.RootDependant.ModelDetail.Type != modelDetail.Type)
                throw new Exception($"Lambda type {modelDetail.Type.GetNiceName()} does not match the root type {context.RootDependant.ModelDetail.Type.GetNiceName()}");

            if (context.MemberContext.ModelStack.Count > 0)
            {
                var callingModel = context.MemberContext.ModelStack.Peek();
                var property = context.MemberContext.MemberLambdaStack.Peek();

                if (callingModel.IdentityProperties.Count != 1)
                    throw new NotSupportedException($"Only one identity supported on {callingModel.Type.GetNiceName()}");
                var callingModelIdentity = callingModel.IdentityProperties[0];
                var modelProperty = callingModel.GetProperty(property.Member.Name);

                sb.Write(callingModel.DataSourceEntityName.ToLower());
                sb.Write('.');
                sb.Write(callingModelIdentity.PropertySourceName.ToLower());
                sb.Write('=');
                sb.Write(modelDetail.DataSourceEntityName.ToLower());
                sb.Write('.');
                sb.Write(modelProperty.ForeignIdentity.ToLower());
                sb.Write(" AND");
            }

            context.MemberContext.DependantStack.Push(context.RootDependant);
            context.MemberContext.ModelStack.Push(modelDetail);
            context.MemberContext.ModelContexts.Add(parameter.Name, modelDetail);

            sb.Write('(');
            ConvertToSql(lambda.Body, ref sb, context);
            sb.Write(')');

            context.MemberContext.DependantStack.Pop();
            context.MemberContext.ModelStack.Pop();
            context.MemberContext.ModelContexts.Remove(parameter.Name);

            context.MemberContext.OperatorStack.Pop();
        }
        private static void ConvertToSqlUnary(Operator prefixOperation, string suffixOperation, Expression exp, ref CharWriteBuffer sb, BuilderContext context)
        {
            context.MemberContext.OperatorStack.Push(prefixOperation);

            var unary = exp as UnaryExpression;
            sb.Write(OperatorToString(prefixOperation));

            ConvertToSql(unary.Operand, ref sb, context);

            sb.Write(suffixOperation);

            context.MemberContext.OperatorStack.Pop();
        }
        private static void ConvertToSqlBinary(Operator operation, Expression exp, ref CharWriteBuffer sb, BuilderContext context)
        {
            context.MemberContext.OperatorStack.Push(operation);

            var binary = exp as BinaryExpression;

            var binaryLeft = binary.Left;
            var binaryRight = binary.Right;

            if (operation == Operator.Equals || operation == Operator.NotEquals)
            {
                var leftNull = IsNull(binaryLeft);
                var rightNull = false;
                if (leftNull == false)
                    rightNull = IsNull(binaryRight);
                if (leftNull || rightNull)
                {
                    operation = operation == Operator.Equals ? Operator.EqualsNull : Operator.NotEqualsNull;
                    if (leftNull)
                        binaryLeft = binaryRight;
                    binaryRight = null;
                }
            }

            sb.Write('(');
            ConvertToSql(binaryLeft, ref sb, context);
            sb.Write(')');

            sb.Write(OperatorToString(operation));

            if (binaryRight != null)
            {
                sb.Write('(');
                ConvertToSql(binaryRight, ref sb, context);
                sb.Write(')');
            }
            else
            {
                sb.Write(" NULL");
            }

            context.MemberContext.OperatorStack.Pop();
        }
        private static void ConvertToSqlMember(Expression exp, ref CharWriteBuffer sb, BuilderContext context)
        {
            var member = exp as MemberExpression;

            if (member.Expression == null)
            {
                ConvertToSqlEvaluate(member, ref sb, context);
            }
            else
            {
                context.MemberContext.MemberAccessStack.Push(member);
                ConvertToSql(member.Expression, ref sb, context);
                context.MemberContext.MemberAccessStack.Pop();
            }
        }
        private static void ConvertToSqlConstant(Expression exp, ref CharWriteBuffer sb, BuilderContext context)
        {
            var constant = exp as ConstantExpression;

            ConvertToSqlConstantStack(constant.Type, constant.Value, ref sb, context);
        }
        private static void ConvertToSqlConstantStack(Type type, object value, ref CharWriteBuffer sb, BuilderContext context)
        {
            if (context.MemberContext.MemberAccessStack.Count > 0)
            {
                var memberProperty = context.MemberContext.MemberAccessStack.Pop();
                if (value == null)
                {
                    sb.Write("NULL");
                }
                else
                {
                    switch (memberProperty.Member.MemberType)
                    {
                        case MemberTypes.Field:
                            {
                                var field = memberProperty.Member as FieldInfo;
                                object fieldValue = field.GetValue(value);
                                ConvertToSqlConstantStack(field.FieldType, fieldValue, ref sb, context);
                                break;
                            }
                        case MemberTypes.Property:
                            {
                                var property = memberProperty.Member as PropertyInfo;
                                object propertyValue = property.GetValue(value);
                                ConvertToSqlConstantStack(property.PropertyType, propertyValue, ref sb, context);
                                break;
                            }
                        default:
                            throw new NotImplementedException();
                    }
                }

                context.MemberContext.MemberAccessStack.Push(memberProperty);
            }
            else
            {
                ConvertToSqlValue(type, value, ref sb, context);
            }
        }
        private static void ConvertToSqlCall(Expression exp, ref CharWriteBuffer sb, BuilderContext context)
        {
            context.MemberContext.OperatorStack.Push(Operator.Call);

            var call = exp as MethodCallExpression;
            bool isEvaluatable = IsEvaluatable(exp);
            if (isEvaluatable)
            {
                ConvertToSqlEvaluate(exp, ref sb, context);
            }
            else
            {
                if (call.Method.DeclaringType == typeof(Enumerable) || call.Method.DeclaringType == typeof(Queryable))
                {
                    switch (call.Method.Name)
                    {
                        case "All":
                            {
                                if (call.Arguments.Count != 2)
                                    throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                                var subMember = (MemberExpression)call.Arguments[0];
                                var subWhere = call.Arguments.Count > 1 ? call.Arguments[1] : null;

                                if (context.Inverted)
                                    sb.Write("NOT ");

                                context.MemberContext.InCallRenderMemberIdentity = true;
                                ConvertToSqlMember(subMember, ref sb, context);
                                context.MemberContext.InCallRenderMemberIdentity = false;

                                var subMemberModel = ModelAnalyzer.GetModel(subMember.Expression.Type);
                                var propertyInfo = subMemberModel.GetProperty(subMember.Member.Name);
                                var subModelInfo = ModelAnalyzer.GetModel(propertyInfo.InnerType);

                                context.MemberContext.MemberLambdaStack.Push(subMember);
                                context.MemberContext.ModelStack.Push(subMemberModel);

                                sb.Write("=ALL(");
                                Convert(ref sb, QueryOperation.Many, subWhere, null, null, null, new Graph(propertyInfo.ForeignIdentity), subModelInfo, context.MemberContext);
                                sb.Write(')');

                                context.MemberContext.ModelStack.Pop();
                                context.MemberContext.MemberLambdaStack.Pop();
                                break;
                            }
                        case "Any":
                            {
                                if (call.Arguments.Count != 1 && call.Arguments.Count != 2)
                                    throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                                var subMember = (MemberExpression)call.Arguments[0];
                                var subWhere = call.Arguments.Count > 1 ? call.Arguments[1] : null;

                                if (context.Inverted)
                                    sb.Write("NOT ");

                                context.MemberContext.InCallRenderMemberIdentity = true;
                                ConvertToSqlMember(subMember, ref sb, context);
                                context.MemberContext.InCallRenderMemberIdentity = false;

                                var subMemberModel = ModelAnalyzer.GetModel(subMember.Expression.Type);
                                var propertyInfo = subMemberModel.GetProperty(subMember.Member.Name);
                                var subModelInfo = ModelAnalyzer.GetModel(propertyInfo.InnerType);

                                context.MemberContext.MemberLambdaStack.Push(subMember);
                                context.MemberContext.ModelStack.Push(subMemberModel);

                                sb.Write("=ANY(");
                                Convert(ref sb, QueryOperation.Many, subWhere, null, null, null, new Graph(propertyInfo.ForeignIdentity), subModelInfo, context.MemberContext);
                                sb.Write(')');

                                context.MemberContext.ModelStack.Pop();
                                context.MemberContext.MemberLambdaStack.Pop();
                                break;
                            }
                        case "Count":
                            {
                                if (call.Arguments.Count != 1 && call.Arguments.Count != 2)
                                    throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                                var subMember = (MemberExpression)call.Arguments[0];
                                var subWhere = call.Arguments.Count > 1 ? call.Arguments[1] : null;

                                context.MemberContext.InCallNoRender = true;
                                ConvertToSqlMember(subMember, ref sb, context);
                                context.MemberContext.InCallNoRender = false;

                                var subMemberModel = ModelAnalyzer.GetModel(subMember.Expression.Type);
                                var propertyInfo = subMemberModel.GetProperty(subMember.Member.Name);
                                var subModelInfo = ModelAnalyzer.GetModel(propertyInfo.InnerType);

                                context.MemberContext.MemberLambdaStack.Push(subMember);
                                context.MemberContext.ModelStack.Push(subMemberModel);

                                sb.Write('(');
                                Convert(ref sb, QueryOperation.Count, subWhere, null, null, null, new Graph(propertyInfo.ForeignIdentity), subModelInfo, context.MemberContext);
                                sb.Write(')');

                                context.MemberContext.ModelStack.Pop();
                                context.MemberContext.MemberLambdaStack.Pop();
                                break;
                            }
                        case "Contains":
                            {
                                if (call.Arguments.Count != 2)
                                    throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                                var callingObject = call.Arguments[0];
                                var lambda = call.Arguments[1];

                                ConvertToSql(lambda, ref sb, context);

                                sb.Write(context.Inverted ? " NOT IN " : " IN ");

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
                            {
                                if (call.Arguments.Count != 1)
                                    throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                                var callingObject = call.Object;
                                var text = call.Arguments[0];

                                ConvertToSql(callingObject, ref sb, context);

                                sb.Write(context.Inverted ? "NOT LIKE '%'+" : "LIKE '%'+");

                                ConvertToSql(text, ref sb, context);

                                sb.Write("+'%'");
                                break;
                            }
                        default:
                            throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
                    }
                }
                else
                {
                    var typeDetails = TypeAnalyzer.GetType(call.Method.DeclaringType);
                    if (typeDetails.IsIEnumerableGeneric && TypeLookup.CoreTypesWithNullables.Contains(typeDetails.IEnumerableGenericInnerType))
                    {
                        switch (call.Method.Name)
                        {
                            case "Contains":
                                {
                                    if (call.Arguments.Count != 1)
                                        throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");

                                    var callingObject = call.Object;
                                    var lambda = call.Arguments[0];

                                    ConvertToSql(lambda, ref sb, context);

                                    sb.Write(context.Inverted ? " NOT IN " : " IN ");

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

            context.MemberContext.OperatorStack.Pop();
        }
        private static void ConvertToSqlNew(Expression exp, ref CharWriteBuffer sb, BuilderContext context)
        {
            context.MemberContext.OperatorStack.Push(Operator.New);

            var newExp = exp as NewExpression;

            var argumentTypes = newExp.Arguments.Select(x => x.Type).ToArray();
            var constructor = newExp.Type.GetConstructor(argumentTypes);

            var parameters = new List<object>();
            foreach (var argument in newExp.Arguments)
            {
                object argumentValue = Expression.Lambda(argument).Compile().DynamicInvoke();
                parameters.Add(argumentValue);
            }

            var value = constructor.Invoke(parameters.ToArray());
            ConvertToSqlValue(newExp.Type, value, ref sb, context);

            context.MemberContext.OperatorStack.Pop();
        }
        private static void ConvertToSqlParameter(Expression exp, ref CharWriteBuffer sb, BuilderContext context)
        {
            var parameter = exp as ParameterExpression;

            var modelDetail = context.MemberContext.ModelContexts[parameter.Name];

            var parameterInContext = modelDetail == context.MemberContext.ModelStack.Peek();

            ConvertToSqlParameterModel(modelDetail, ref sb, context, parameterInContext);
        }
        private static void ConvertToSqlParameterModel(ModelDetail modelDetail, ref CharWriteBuffer sb, BuilderContext context, bool parameterInContext)
        {
            var member = context.MemberContext.MemberAccessStack.Pop();

            var modelProperty = modelDetail.GetProperty(member.Member.Name);
            if (modelProperty.IsDataSourceEntity && context.MemberContext.MemberAccessStack.Count > 0)
            {
                var subModelInfo = ModelAnalyzer.GetModel(modelProperty.InnerType);
                context.MemberContext.ModelStack.Push(subModelInfo);
                if (parameterInContext)
                {
                    var parentDependant = context.MemberContext.DependantStack.Peek();
                    if (!parentDependant.Dependants.TryGetValue(subModelInfo.Type, out ParameterDependant dependant))
                    {
                        dependant = new ParameterDependant(subModelInfo, modelProperty);
                        parentDependant.Dependants.Add(subModelInfo.Type, dependant);
                    }
                    context.MemberContext.DependantStack.Push(dependant);
                }
                ConvertToSqlParameterModel(subModelInfo, ref sb, context, parameterInContext);
                if (parameterInContext)
                {
                    context.MemberContext.DependantStack.Pop();
                }
                context.MemberContext.ModelStack.Pop();
            }
            else if (context.MemberContext.InCallNoRender)
            {

            }
            else if (context.MemberContext.InCallRenderMemberIdentity)
            {
                if (modelDetail.IdentityProperties.Count != 1)
                    throw new NotSupportedException($"Only one identity supported on {modelDetail.Type.GetNiceName()}");
                var modelIdentity = modelDetail.IdentityProperties[0];

                sb.Write(modelDetail.DataSourceEntityName.ToLower());
                sb.Write('.');
                sb.Write(modelIdentity.PropertySourceName.ToLower());
            }
            else
            {
                bool closeBrace = false;

                if (context.MemberContext.MemberAccessStack.Count > 0)
                {
                    bool memberPropertyHandled = false;
                    var memberProperty = context.MemberContext.MemberAccessStack.Pop();

                    if (member.Type.Name == typeof(Nullable<>).Name && memberProperty.Member.Name == "Value")
                    {
                        memberPropertyHandled = true;
                    }
                    else if (member.Type == typeof(DateTime))
                    {
                        memberPropertyHandled = true;
                        closeBrace = true;
                        switch (memberProperty.Member.Name)
                        {
                            case "Year":
                                sb.Write("DATEPART(year,");
                                break;
                            case "Month":
                                sb.Write("DATEPART(month,");
                                break;
                            case "Day":
                                sb.Write("DATEPART(day,");
                                break;
                            case "Hour":
                                sb.Write("DATEPART(hour,");
                                break;
                            case "Minute":
                                sb.Write("DATEPART(minute,");
                                break;
                            case "Second":
                                sb.Write("DATEPART(second,");
                                break;
                            case "Millisecond":
                                sb.Write("DATEPART(millisecond,");
                                break;
                            case "DayOfYear":
                                sb.Write("DATEPART(dayofyear,");
                                break;
                            case "DayOfWeek":
                                sb.Write("DATEPART(weekday,");
                                break;
                            default:
                                memberPropertyHandled = false;
                                break;
                        }
                    }

                    if (!memberPropertyHandled)
                        throw new NotSupportedException($"{member.Member.Name}.{memberProperty.Member.Name} not supported");
                    context.MemberContext.MemberAccessStack.Push(memberProperty);
                }


                sb.Write(modelDetail.DataSourceEntityName.ToLower());
                sb.Write('.');
                sb.Write(modelProperty.PropertySourceName.ToLower());
                var lastOperator = context.MemberContext.OperatorStack.Peek();
                if (lastOperator == Operator.And || lastOperator == Operator.Or)
                {
                    if (modelProperty.InnerType == typeof(bool))
                        sb.Write("=1");
                    else
                        sb.Write("IS NOT NULL");
                }

                if (closeBrace)
                {
                    sb.Write(')');
                }
            }

            context.MemberContext.MemberAccessStack.Push(member);
        }
        private static void ConvertToSqlConditional(Expression exp, ref CharWriteBuffer sb, BuilderContext context)
        {
            context.MemberContext.OperatorStack.Push(Operator.Conditional);

            var conditional = exp as ConditionalExpression;

            sb.Write("CASE WHEN(");

            ConvertToSql(conditional.Test, ref sb, context);

            sb.Write(")THEN(");

            ConvertToSql(conditional.IfTrue, ref sb, context);

            sb.Write(")ELSE(");

            ConvertToSql(conditional.IfFalse, ref sb, context);

            sb.Write(")END");

            context.MemberContext.OperatorStack.Pop();
        }
        private static void ConvertToSqlEvaluate(Expression exp, ref CharWriteBuffer sb, BuilderContext context)
        {
            context.MemberContext.OperatorStack.Push(Operator.Evaluate);

            var value = Evaluate(exp);
            ConvertToSqlValue(exp.Type, value, ref sb, context);

            context.MemberContext.OperatorStack.Pop();
        }

        private static void ConvertToSqlValue(Type type, object value, ref CharWriteBuffer sb, BuilderContext context)
        {
            MemberExpression memberProperty = null;

            if (context.MemberContext.MemberAccessStack.Count > 0)
                memberProperty = context.MemberContext.MemberAccessStack.Pop();

            bool memberPropertyHandled = ConvertToSqlValueRender(memberProperty, type, value, ref sb, context);

            if (memberProperty != null)
            {
                if (!memberPropertyHandled)
                    throw new NotSupportedException($"{type.FullName}.{memberProperty.Member.Name} not supported");
                context.MemberContext.MemberAccessStack.Push(memberProperty);
            }
        }
        private static bool ConvertToSqlValueRender(MemberExpression memberProperty, Type type, object value, ref CharWriteBuffer sb, BuilderContext context)
        {
            var typeDetails = TypeAnalyzer.GetType(type);

            if (value == null)
            {
                sb.Write("NULL");
                return false;
            }

            if (typeDetails.IsNullable)
            {
                type = typeDetails.InnerTypes[0];
                typeDetails = typeDetails.InnerTypeDetails[0];
            }

            if (type.IsEnum)
            {
                sb.Write('\'');
                sb.Write(value.ToString());
                sb.Write('\'');
                return false;
            }

            if (type.IsArray)
            {
                Type arrayType = typeDetails.InnerTypes[0];
                if (arrayType == typeof(byte))
                {
                    var hex = BitConverter.ToString((byte[])value).Replace("-", "\\x");
                    sb.Write("E'\\x");
                    sb.Write(hex);
                    sb.Write('\'');
                    return false;
                }
                else
                {
                    sb.Write('(');

                    var builderLength = sb.Length;

                    bool first = true;
                    foreach (object item in (IEnumerable)value)
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

            if (typeDetails.IsIEnumerableGeneric)
            {
                sb.Write('(');

                var builderLength = sb.Length;

                bool first = true;
                foreach (object item in (IEnumerable)value)
                {
                    if (!first)
                        sb.Write(',');
                    ConvertToSqlValue(typeDetails.IEnumerableGenericInnerType, item, ref sb, context);
                    first = false;
                }

                if (builderLength == sb.Length)
                    sb.Write("NULL");

                sb.Write(')');
                return false;
            }

            if (TypeLookup.CoreTypeLookup(type, out CoreType coreType))
            {
                switch (coreType)
                {
                    case CoreType.Boolean:
                        var lastOperator = context.MemberContext.OperatorStack.Peek();
                        if (lastOperator == Operator.And || lastOperator == Operator.Or || lastOperator == Operator.Lambda)
                            sb.Write((bool)value ? "1=1" : "1=0");
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
                            sb.Write('\'');
                            sb.Write((char)value);
                            sb.Write('\'');
                        }
                        return false;
                    case CoreType.DateTime:
                        if (memberProperty != null)
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
                        //sb.Append('\'').Append(((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff")).Append('\'');
                        sb.Write('\'');
                        sb.Write((DateTime)value, DateTimeFormat.ISO8601);
                        sb.Write('\'');
                        return false;
                    case CoreType.DateTimeOffset:
                        if (memberProperty != null)
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
                        //sb.Append('\'').Append(((DateTimeOffset)value).ToString("yyyy-MM-dd HH:mm:ss.fff")).Append('\'');
                        sb.Write('\'');
                        sb.Write((DateTimeOffset)value, DateTimeFormat.ISO8601);
                        sb.Write('\'');
                        return false;
                    case CoreType.Guid:
                        sb.Write('\'');
                        sb.Write((Guid)value);
                        sb.Write('\'');
                        return false;
                    case CoreType.String:
                        sb.Write('\'');
                        sb.Write(((string)value).Replace("'", "''"));
                        sb.Write('\''); return false;
                }
            }

            if (type == typeof(object))
            {
                sb.Write('\'');
                sb.Write(value.ToString().Replace("\'", "''"));
                sb.Write('\'');
                return false;
            }

            throw new NotImplementedException($"{type.GetNiceName()} value {value?.ToString()} not converted");
        }

        private static bool IsEvaluatable(Expression exp)
        {
            return exp.NodeType switch
            {
                ExpressionType.Add => IsEvaluatableBinary(exp),
                ExpressionType.AddAssign => IsEvaluatableBinary(exp),
                ExpressionType.AddAssignChecked => IsEvaluatableBinary(exp),
                ExpressionType.AddChecked => IsEvaluatableBinary(exp),
                ExpressionType.And => IsEvaluatableBinary(exp),
                ExpressionType.AndAlso => IsEvaluatableBinary(exp),
                ExpressionType.AndAssign => IsEvaluatableBinary(exp),
                ExpressionType.ArrayIndex => IsEvaluatableBinary(exp),
                ExpressionType.ArrayLength => IsEvaluatableUnary(exp),
                ExpressionType.Assign => IsEvaluatableBinary(exp),
                ExpressionType.Block => IsEvaluatableBlock(exp),
                ExpressionType.Call => IsEvaluatableCall(exp),
                ExpressionType.Coalesce => throw new NotImplementedException(),
                ExpressionType.Conditional => throw new NotImplementedException(),
                ExpressionType.Constant => IsEvaluatableConstant(exp),
                ExpressionType.Convert => IsEvaluatableUnary(exp),
                ExpressionType.ConvertChecked => throw new NotImplementedException(),
                ExpressionType.DebugInfo => throw new NotImplementedException(),
                ExpressionType.Decrement => IsEvaluatableUnary(exp),
                ExpressionType.Default => throw new NotImplementedException(),
                ExpressionType.Divide => IsEvaluatableBinary(exp),
                ExpressionType.DivideAssign => IsEvaluatableBinary(exp),
                ExpressionType.Dynamic => throw new NotImplementedException(),
                ExpressionType.Equal => IsEvaluatableBinary(exp),
                ExpressionType.ExclusiveOr => IsEvaluatableBinary(exp),
                ExpressionType.ExclusiveOrAssign => IsEvaluatableBinary(exp),
                ExpressionType.Extension => throw new NotImplementedException(),
                ExpressionType.Goto => throw new NotImplementedException(),
                ExpressionType.GreaterThan => throw new NotImplementedException(),
                ExpressionType.GreaterThanOrEqual => throw new NotImplementedException(),
                ExpressionType.Increment => IsEvaluatableUnary(exp),
                ExpressionType.Index => throw new NotImplementedException(),
                ExpressionType.Invoke => throw new NotImplementedException(),
                ExpressionType.IsFalse => throw new NotImplementedException(),
                ExpressionType.IsTrue => throw new NotImplementedException(),
                ExpressionType.Label => throw new NotImplementedException(),
                ExpressionType.Lambda => throw new NotImplementedException(),
                ExpressionType.LeftShift => IsEvaluatableBinary(exp),
                ExpressionType.LeftShiftAssign => IsEvaluatableBinary(exp),
                ExpressionType.LessThan => throw new NotImplementedException(),
                ExpressionType.LessThanOrEqual => throw new NotImplementedException(),
                ExpressionType.ListInit => throw new NotImplementedException(),
                ExpressionType.Loop => throw new NotImplementedException(),
                ExpressionType.MemberAccess => IsEvaluatableMemberAccess(exp),
                ExpressionType.MemberInit => throw new NotImplementedException(),
                ExpressionType.Modulo => IsEvaluatableBinary(exp),
                ExpressionType.ModuloAssign => IsEvaluatableBinary(exp),
                ExpressionType.Multiply => IsEvaluatableBinary(exp),
                ExpressionType.MultiplyAssign => IsEvaluatableBinary(exp),
                ExpressionType.MultiplyAssignChecked => IsEvaluatableBinary(exp),
                ExpressionType.MultiplyChecked => IsEvaluatableBinary(exp),
                ExpressionType.Negate => IsEvaluatableUnary(exp),
                ExpressionType.NegateChecked => IsEvaluatableUnary(exp),
                ExpressionType.New => throw new NotImplementedException(),
                ExpressionType.NewArrayBounds => throw new NotImplementedException(),
                ExpressionType.NewArrayInit => throw new NotImplementedException(),
                ExpressionType.Not => IsEvaluatableUnary(exp),
                ExpressionType.NotEqual => IsEvaluatableBinary(exp),
                ExpressionType.OnesComplement => IsEvaluatableUnary(exp),
                ExpressionType.Or => IsEvaluatableBinary(exp),
                ExpressionType.OrAssign => IsEvaluatableBinary(exp),
                ExpressionType.OrElse => IsEvaluatableBinary(exp),
                ExpressionType.Parameter => false,
                ExpressionType.PostDecrementAssign => IsEvaluatableUnary(exp),
                ExpressionType.PostIncrementAssign => IsEvaluatableUnary(exp),
                ExpressionType.Power => IsEvaluatableBinary(exp),
                ExpressionType.PowerAssign => IsEvaluatableBinary(exp),
                ExpressionType.PreDecrementAssign => IsEvaluatableUnary(exp),
                ExpressionType.PreIncrementAssign => IsEvaluatableUnary(exp),
                ExpressionType.Quote => throw new NotImplementedException(),
                ExpressionType.RightShift => IsEvaluatableBinary(exp),
                ExpressionType.RightShiftAssign => IsEvaluatableBinary(exp),
                ExpressionType.RuntimeVariables => throw new NotImplementedException(),
                ExpressionType.Subtract => IsEvaluatableBinary(exp),
                ExpressionType.SubtractAssign => IsEvaluatableBinary(exp),
                ExpressionType.SubtractAssignChecked => IsEvaluatableBinary(exp),
                ExpressionType.SubtractChecked => IsEvaluatableBinary(exp),
                ExpressionType.Switch => throw new NotImplementedException(),
                ExpressionType.Throw => throw new NotImplementedException(),
                ExpressionType.Try => throw new NotImplementedException(),
                ExpressionType.TypeAs => IsEvaluatableUnary(exp),
                ExpressionType.TypeEqual => IsEvaluatableUnary(exp),
                ExpressionType.TypeIs => IsEvaluatableUnary(exp),
                ExpressionType.UnaryPlus => IsEvaluatableUnary(exp),
                ExpressionType.Unbox => IsEvaluatableUnary(exp),
                _ => throw new NotImplementedException(),
            };
            ;
        }
        private static bool IsEvaluatableUnary(Expression exp)
        {
            var unary = exp as UnaryExpression;
            return IsEvaluatable(unary.Operand);
        }
        private static bool IsEvaluatableBinary(Expression exp)
        {
            var binary = exp as BinaryExpression;
            return IsEvaluatable(binary.Left) && IsEvaluatable(binary.Right);
        }
        private static bool IsEvaluatableConstant(Expression exp)
        {
            return true;
        }
        private static bool IsEvaluatableBlock(Expression exp)
        {
            var block = exp as BlockExpression;
            foreach (var variable in block.Variables)
                if (!IsEvaluatable(variable))
                    return false;
            foreach (var expression in block.Expressions)
                if (!IsEvaluatable(expression))
                    return false;
            return true;
        }
        private static bool IsEvaluatableCall(Expression exp)
        {
            var call = exp as MethodCallExpression;

            foreach (var arg in call.Arguments)
                if (!IsEvaluatable(arg))
                    return false;

            if (call.Object != null)
                return IsEvaluatable(call.Object);

            return true;
        }
        private static bool IsEvaluatableMemberAccess(Expression exp)
        {
            var member = exp as MemberExpression;
            if (member.Expression == null)
            {
                return true;
            }
            return IsEvaluatable(member.Expression);
        }

        private static bool IsNull(Expression exp)
        {
            return exp.NodeType switch
            {
                ExpressionType.Add => throw new NotImplementedException(),
                ExpressionType.AddAssign => throw new NotImplementedException(),
                ExpressionType.AddAssignChecked => throw new NotImplementedException(),
                ExpressionType.AddChecked => throw new NotImplementedException(),
                ExpressionType.And => throw new NotImplementedException(),
                ExpressionType.AndAlso => throw new NotImplementedException(),
                ExpressionType.AndAssign => throw new NotImplementedException(),
                ExpressionType.ArrayIndex => throw new NotImplementedException(),
                ExpressionType.ArrayLength => throw new NotImplementedException(),
                ExpressionType.Assign => throw new NotImplementedException(),
                ExpressionType.Block => throw new NotImplementedException(),
                ExpressionType.Call => IsNullCall(exp),
                ExpressionType.Coalesce => throw new NotImplementedException(),
                ExpressionType.Conditional => throw new NotImplementedException(),
                ExpressionType.Constant => IsNullConstant(exp),
                ExpressionType.Convert => IsNullUnary(exp),
                ExpressionType.ConvertChecked => throw new NotImplementedException(),
                ExpressionType.DebugInfo => throw new NotImplementedException(),
                ExpressionType.Decrement => throw new NotImplementedException(),
                ExpressionType.Default => throw new NotImplementedException(),
                ExpressionType.Divide => throw new NotImplementedException(),
                ExpressionType.DivideAssign => throw new NotImplementedException(),
                ExpressionType.Dynamic => throw new NotImplementedException(),
                ExpressionType.Equal => throw new NotImplementedException(),
                ExpressionType.ExclusiveOr => throw new NotImplementedException(),
                ExpressionType.ExclusiveOrAssign => throw new NotImplementedException(),
                ExpressionType.Extension => throw new NotImplementedException(),
                ExpressionType.Goto => throw new NotImplementedException(),
                ExpressionType.GreaterThan => throw new NotImplementedException(),
                ExpressionType.GreaterThanOrEqual => throw new NotImplementedException(),
                ExpressionType.Increment => throw new NotImplementedException(),
                ExpressionType.Index => throw new NotImplementedException(),
                ExpressionType.Invoke => throw new NotImplementedException(),
                ExpressionType.IsFalse => throw new NotImplementedException(),
                ExpressionType.IsTrue => throw new NotImplementedException(),
                ExpressionType.Label => throw new NotImplementedException(),
                ExpressionType.Lambda => throw new NotImplementedException(),
                ExpressionType.LeftShift => throw new NotImplementedException(),
                ExpressionType.LeftShiftAssign => throw new NotImplementedException(),
                ExpressionType.LessThan => throw new NotImplementedException(),
                ExpressionType.LessThanOrEqual => throw new NotImplementedException(),
                ExpressionType.ListInit => throw new NotImplementedException(),
                ExpressionType.Loop => throw new NotImplementedException(),
                ExpressionType.MemberAccess => IsNullMemberAccess(exp),
                ExpressionType.MemberInit => throw new NotImplementedException(),
                ExpressionType.Modulo => throw new NotImplementedException(),
                ExpressionType.ModuloAssign => throw new NotImplementedException(),
                ExpressionType.Multiply => throw new NotImplementedException(),
                ExpressionType.MultiplyAssign => throw new NotImplementedException(),
                ExpressionType.MultiplyAssignChecked => throw new NotImplementedException(),
                ExpressionType.MultiplyChecked => throw new NotImplementedException(),
                ExpressionType.Negate => throw new NotImplementedException(),
                ExpressionType.NegateChecked => throw new NotImplementedException(),
                ExpressionType.New => throw new NotImplementedException(),
                ExpressionType.NewArrayBounds => throw new NotImplementedException(),
                ExpressionType.NewArrayInit => throw new NotImplementedException(),
                ExpressionType.Not => throw new NotImplementedException(),
                ExpressionType.NotEqual => throw new NotImplementedException(),
                ExpressionType.OnesComplement => throw new NotImplementedException(),
                ExpressionType.Or => throw new NotImplementedException(),
                ExpressionType.OrAssign => throw new NotImplementedException(),
                ExpressionType.OrElse => throw new NotImplementedException(),
                ExpressionType.Parameter => false,
                ExpressionType.PostDecrementAssign => throw new NotImplementedException(),
                ExpressionType.PostIncrementAssign => throw new NotImplementedException(),
                ExpressionType.Power => throw new NotImplementedException(),
                ExpressionType.PowerAssign => throw new NotImplementedException(),
                ExpressionType.PreDecrementAssign => throw new NotImplementedException(),
                ExpressionType.PreIncrementAssign => throw new NotImplementedException(),
                ExpressionType.Quote => throw new NotImplementedException(),
                ExpressionType.RightShift => throw new NotImplementedException(),
                ExpressionType.RightShiftAssign => throw new NotImplementedException(),
                ExpressionType.RuntimeVariables => throw new NotImplementedException(),
                ExpressionType.Subtract => throw new NotImplementedException(),
                ExpressionType.SubtractAssign => throw new NotImplementedException(),
                ExpressionType.SubtractAssignChecked => throw new NotImplementedException(),
                ExpressionType.SubtractChecked => throw new NotImplementedException(),
                ExpressionType.Switch => throw new NotImplementedException(),
                ExpressionType.Throw => throw new NotImplementedException(),
                ExpressionType.Try => throw new NotImplementedException(),
                ExpressionType.TypeAs => throw new NotImplementedException(),
                ExpressionType.TypeEqual => throw new NotImplementedException(),
                ExpressionType.TypeIs => throw new NotImplementedException(),
                ExpressionType.UnaryPlus => throw new NotImplementedException(),
                ExpressionType.Unbox => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };
            ;
        }
        private static bool IsNullUnary(Expression exp)
        {
            var unary = exp as UnaryExpression;
            return IsNull(unary.Operand);
        }
        private static bool IsNullConstant(Expression exp)
        {
            var constant = exp as ConstantExpression;
            return constant.Value == null;
        }
        private static bool IsNullCall(Expression exp)
        {
            var call = exp as MethodCallExpression;
            if (call.Object == null)
            {
                bool result = true;
                foreach (var arg in call.Arguments)
                {
                    result &= IsNull(arg);
                    if (!result)
                        break;
                }
                return result;
            }
            else
            {
                return IsNull(call.Object);
            }
        }
        private static bool IsNullMemberAccess(Expression exp)
        {
            var member = exp as MemberExpression;

            object value;
            if (member.Expression == null)
            {
                value = Evaluate(member);
            }
            else
            {
                var isEvaluatable = IsEvaluatable(member.Expression);
                if (!isEvaluatable)
                    return false;

                var expressionValue = member.Expression == null ? null : Evaluate(member.Expression);
                switch (member.Member.MemberType)
                {
                    case MemberTypes.Field:
                        var fieldInfo = (FieldInfo)member.Member;
                        if (expressionValue == null && !fieldInfo.IsStatic)
                            return true;
                        value = fieldInfo.GetValue(expressionValue);
                        break;
                    case MemberTypes.Property:
                        var propertyInfo = (PropertyInfo)member.Member;
                        if (expressionValue == null && !propertyInfo.GetMethod.IsStatic)
                            return true;
                        value = propertyInfo.GetValue(expressionValue);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            return value == null;
        }

        private static object Evaluate(Expression exp)
        {
            return exp.NodeType switch
            {
                ExpressionType.Constant => EvaluateConstant(exp),
                ExpressionType.MemberAccess => EvaluateMemberAccess(exp),
                _ => EvaluateInvoke(exp),
            };
            ;
        }
        private static object EvaluateConstant(Expression exp)
        {
            var constant = exp as ConstantExpression;
            return constant.Value;
        }
        private static object EvaluateMemberAccess(Expression exp)
        {
            var member = exp as MemberExpression;
            var expressionValue = member.Expression == null ? null : Evaluate(member.Expression);

            object value;
            switch (member.Member.MemberType)
            {
                case MemberTypes.Field:
                    var fieldInfo = (FieldInfo)member.Member;
                    if (expressionValue == null && !fieldInfo.IsStatic)
                        return null;
                    value = fieldInfo.GetValue(expressionValue);
                    break;
                case MemberTypes.Property:
                    var propertyInfo = (PropertyInfo)member.Member;
                    if (expressionValue == null && !propertyInfo.GetMethod.IsStatic)
                        return null;
                    value = propertyInfo.GetValue(expressionValue);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return value;
        }
        private static object EvaluateInvoke(Expression exp)
        {
            var value = Expression.Lambda(exp).Compile().DynamicInvoke();
            return value;
        }

        private static void GenerateWhere(Expression where, ref CharWriteBuffer sb, ParameterDependant rootDependant, MemberContext operationContext)
        {
            if (where == null)
                return;

            sb.Write("WHERE");
            var context = new BuilderContext(rootDependant, operationContext);
            ConvertToSql(where, ref sb, context);
            AppendLineBreak(ref sb);
        }
        private static void GenerateOrderSkipTake(QueryOrder order, int? skip, int? take, ref CharWriteBuffer sb, ParameterDependant rootDependant, MemberContext operationContext)
        {
            if (order?.OrderExpressions.Length > 0)
            {
                sb.Write("ORDER BY");
                var hasOrder = false;
                foreach (var orderExp in order.OrderExpressions)
                {
                    var context = new BuilderContext(rootDependant, operationContext);
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
        private static void GenerateSelect(QueryOperation select, Graph graph, ModelDetail modelDetail, ref CharWriteBuffer sb)
        {
            switch (select)
            {
                case QueryOperation.Many:
                    sb.Write("SELECT");
                    GenerateSelectProperties(graph, modelDetail, ref sb);
                    break;
                case QueryOperation.First:
                    sb.Write("SELECT");
                    GenerateSelectProperties(graph, modelDetail, ref sb);
                    break;
                case QueryOperation.Single:
                    sb.Write("SELECT");
                    GenerateSelectProperties(graph, modelDetail, ref sb);
                    break;
                case QueryOperation.Count:
                    sb.Write("SELECT COUNT(1)");
                    break;
                case QueryOperation.Any:
                    sb.Write("SELECT 1");
                    break;
            }
        }
        private static void GenerateSelectProperties(Graph graph, ModelDetail modelDetail, ref CharWriteBuffer sb)
        {
            AppendLineBreak(ref sb);
            if (graph.IncludeAllProperties)
            {
                sb.Write(modelDetail.DataSourceEntityName.ToLower());
                sb.Write(".*");
                AppendLineBreak(ref sb);
            }
            else if (graph.LocalProperties.Count == 0)
            {
                sb.Write(" 1");
            }
            else
            {
                var passedfirst = false;
                foreach (var property in graph.LocalProperties)
                {
                    if (modelDetail.TryGetProperty(property, out ModelPropertyDetail modelProperty))
                    {
                        if (modelProperty.PropertySourceName != null && modelProperty.ForeignIdentity == null)
                        {
                            if (passedfirst)
                                sb.Write(',');
                            else
                                passedfirst = true;
                            sb.Write(modelDetail.DataSourceEntityName.ToLower());
                            sb.Write('.');
                            sb.Write(property.ToLower());
                            AppendLineBreak(ref sb);
                        }
                    }
                }
            }
        }
        private static void GenerateFrom(ModelDetail modelDetail, ref CharWriteBuffer sb)
        {
            sb.Write("FROM ");
            sb.Write(modelDetail.DataSourceEntityName.ToLower());
            AppendLineBreak(ref sb);
        }
        private static void GenerateJoin(ParameterDependant dependant, ref CharWriteBuffer sb)
        {
            foreach (var child in dependant.Dependants.Values)
            {
                if (child.ModelDetail.IdentityProperties.Count != 1)
                    throw new NotSupportedException($"Only one identity supported on {child.ModelDetail.Type.GetNiceName()}");
                var dependantIdentity = child.ModelDetail.IdentityProperties[0];

                sb.Write("JOIN ");
                sb.Write(child.ModelDetail.DataSourceEntityName.ToLower());
                sb.Write(" ON ");
                sb.Write(dependant.ModelDetail.DataSourceEntityName.ToLower());
                sb.Write('.');
                sb.Write(child.ParentMember?.ForeignIdentity.ToLower());
                sb.Write('=');
                sb.Write(child.ModelDetail.DataSourceEntityName.ToLower());
                sb.Write('.');
                sb.Write(dependantIdentity.PropertySourceName.ToLower());

                AppendLineBreak(ref sb);

                GenerateJoin(child, ref sb);
            }
        }
        private static void GenerateEnding(QueryOperation select, Graph graph, ModelDetail modelDetail, ref CharWriteBuffer sb)
        {
            switch (select)
            {
                case QueryOperation.Many:
                    break;
                case QueryOperation.First:
                    sb.Write("LIMIT 1");
                    break;
                case QueryOperation.Single:
                    sb.Write("LIMIT 2");
                    break;
                case QueryOperation.Count:
                    break;
                case QueryOperation.Any:
                    sb.Write("LIMIT 1");
                    break;
            }
        }

        private static void AppendLineBreak(ref CharWriteBuffer sb)
        {
            sb.Write(Environment.NewLine);
        }

        private static string OperatorToString(Operator operation)
        {
            return operation switch
            {
                Operator.Null => null,
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
                _ => throw new NotImplementedException(),
            };
        }

        private class BuilderContext
        {
            public ParameterDependant RootDependant;

            public MemberContext MemberContext;

            public int InvertStack;
            public bool Inverted { get { return InvertStack % 2 != 0; } }

            public BuilderContext(ParameterDependant rootDependant, MemberContext memberContext)
            {
                this.RootDependant = rootDependant;
                this.MemberContext = memberContext;
                this.InvertStack = 0;
            }
        }

        private class MemberContext
        {
            public Dictionary<string, ModelDetail> ModelContexts { get; private set; }
            public Stack<ModelDetail> ModelStack { get; private set; }
            public Stack<Operator> OperatorStack { get; private set; }
            public Stack<MemberExpression> MemberAccessStack { get; private set; }
            public Stack<MemberExpression> MemberLambdaStack { get; private set; }
            public Stack<ParameterDependant> DependantStack { get; private set; }

            public bool InCallRenderMemberIdentity { get; set; }
            public bool InCallNoRender { get; set; }

            public MemberContext()
            {
                this.ModelContexts = new Dictionary<string, ModelDetail>();
                this.ModelStack = new Stack<ModelDetail>();
                this.OperatorStack = new Stack<Operator>();
                this.MemberAccessStack = new Stack<MemberExpression>();
                this.MemberLambdaStack = new Stack<MemberExpression>();
                this.DependantStack = new Stack<ParameterDependant>();
                this.InCallRenderMemberIdentity = false;
                this.InCallNoRender = false;
            }
        }

        private class ParameterDependant
        {
            public ModelDetail ModelDetail;
            public ModelPropertyDetail ParentMember;
            public Dictionary<Type, ParameterDependant> Dependants;

            public ParameterDependant(ModelDetail modelDetail, ModelPropertyDetail parentMember)
            {
                this.ModelDetail = modelDetail;
                this.ParentMember = parentMember;
                this.Dependants = new Dictionary<Type, ParameterDependant>();
            }
        }

        private enum Operator
        {
            Null,
            New,
            Lambda,
            Evaluate,
            Conditional,
            Call,
            Negative,
            And,
            Or,
            Equals,
            NotEquals,
            LessThanOrEquals,
            GreaterThanOrEquals,
            LessThan,
            GreaterThan,
            Divide,
            Subtract,
            Add,
            Multiply,
            Modulus,
            EqualsNull,
            NotEqualsNull
        }
    }
}
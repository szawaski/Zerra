// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using Zerra.IO;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.MsSql
{
    public class LinqMsSqlConverter : BaseLinqSqlConverter
    {
        private static readonly LinqMsSqlConverter instance = new LinqMsSqlConverter();
        public static string Convert(QueryOperation select, Expression where, QueryOrder order, int? skip, int? take, Graph graph, ModelDetail modelDetail)
        {
            return instance.ConvertInternal(select, where, order, skip, take, graph, modelDetail);
        }

        protected override void ConvertToSqlLambda(Expression exp, ref CharWriteBuffer sb, BuilderContext context)
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
                    throw new NotSupportedException($"Relational queries support only one identity on {callingModel.Type.GetNiceName()}");
                var callingModelIdentity = callingModel.IdentityProperties[0];

                var modelProperty = callingModel.GetProperty(property.Member.Name);

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
        protected override void ConvertToSqlCall(Expression exp, ref CharWriteBuffer sb, BuilderContext context)
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

                                var subMemberModel = ModelAnalyzer.GetModel(subMember.Expression.Type);
                                var propertyInfo = subMemberModel.GetProperty(subMember.Member.Name);
                                var subModelInfo = ModelAnalyzer.GetModel(propertyInfo.InnerType);

                                context.MemberContext.InCallRenderIdentity++;
                                ConvertToSqlMember(subMember, ref sb, context);
                                context.MemberContext.InCallRenderIdentity--;

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

                                var subMemberModel = ModelAnalyzer.GetModel(subMember.Expression.Type);
                                var propertyInfo = subMemberModel.GetProperty(subMember.Member.Name);
                                var subModelInfo = ModelAnalyzer.GetModel(propertyInfo.InnerType);

                                context.MemberContext.InCallRenderIdentity++;
                                ConvertToSqlMember(subMember, ref sb, context);
                                context.MemberContext.InCallRenderIdentity--;

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

                                context.MemberContext.InCallNoRender++;
                                ConvertToSqlMember(subMember, ref sb, context);
                                context.MemberContext.InCallNoRender--;

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
                    var typeDetails = TypeAnalyzer.GetTypeDetail(call.Method.DeclaringType);
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

            context.MemberContext.OperatorStack.Pop();
        }
        protected override void ConvertToSqlParameterModel(ModelDetail modelDetail, ref CharWriteBuffer sb, BuilderContext context, bool parameterInContext)
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
            else if (context.MemberContext.InCallNoRender > 0)
            {

            }
            else if (context.MemberContext.InCallRenderIdentity > 0)
            {
                if (modelDetail.IdentityProperties.Count != 1)
                    throw new NotSupportedException($"Relational queries support only one identity on {modelDetail.Type.GetNiceName()}");
                var modelIdentity = modelDetail.IdentityProperties[0];

                sb.Write('[');
                sb.Write(modelDetail.DataSourceEntityName);
                sb.Write("].[");
                sb.Write(modelIdentity.PropertySourceName);
                sb.Write("]");
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


                sb.Write('[');
                sb.Write(modelDetail.DataSourceEntityName);
                sb.Write(']');
                sb.Write('.');
                sb.Write('[');
                sb.Write(modelProperty.PropertySourceName);
                sb.Write(']');
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
        protected override void ConvertToSqlConditional(Expression exp, ref CharWriteBuffer sb, BuilderContext context)
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
        protected override bool ConvertToSqlValueRender(MemberExpression memberProperty, Type type, object value, ref CharWriteBuffer sb, BuilderContext context)
        {
            var typeDetails = TypeAnalyzer.GetTypeDetail(type);

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
                    sb.Write("0x");
                    sb.Write((byte[])value, ByteFormat.Hex);
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
                        sb.Write('\'');
                        sb.Write((DateTime)value, DateTimeFormat.MsSql);
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
                        sb.Write('\'');
                        sb.Write((DateTimeOffset)value, DateTimeFormat.MsSqlOffset);
                        sb.Write('\'');
                        return false;
                    case CoreType.TimeSpan:
                        if (memberProperty != null)
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
                        sb.Write((TimeSpan)value, TimeFormat.MsSql);
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

        protected override void GenerateWhere(Expression where, ref CharWriteBuffer sb, ParameterDependant rootDependant, MemberContext operationContext)
        {
            if (where == null)
                return;

            sb.Write("WHERE");
            var context = new BuilderContext(rootDependant, operationContext);
            ConvertToSql(where, ref sb, context);
            AppendLineBreak(ref sb);
        }
        protected override void GenerateOrderSkipTake(QueryOrder order, int? skip, int? take, ref CharWriteBuffer sb, ParameterDependant rootDependant, MemberContext operationContext)
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
        protected override void GenerateSelect(QueryOperation select, Graph graph, ModelDetail modelDetail, ref CharWriteBuffer sb)
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
        protected override void GenerateSelectProperties(Graph graph, ModelDetail modelDetail, ref CharWriteBuffer sb)
        {
            AppendLineBreak(ref sb);
            if (graph.IncludeAllProperties)
            {
                sb.Write('[');
                sb.Write(modelDetail.DataSourceEntityName);
                sb.Write("].*");
                AppendLineBreak(ref sb);
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
                            sb.Write('[');
                            sb.Write(modelDetail.DataSourceEntityName);
                            sb.Write("].[");
                            sb.Write(property);
                            sb.Write(']');
                            AppendLineBreak(ref sb);
                        }
                    }
                }
                if (!passedfirst)
                {
                    sb.Write(" 1");
                }
            }
        }
        protected override void GenerateFrom(ModelDetail modelDetail, ref CharWriteBuffer sb)
        {
            sb.Write("FROM[");
            sb.Write(modelDetail.DataSourceEntityName);
            sb.Write(']');
            AppendLineBreak(ref sb);
        }
        protected override void GenerateJoin(ParameterDependant dependant, ref CharWriteBuffer sb)
        {
            foreach (var child in dependant.Dependants.Values)
            {
                if (child.ModelDetail.IdentityProperties.Count != 1)
                    throw new NotSupportedException($"Relational queries support only one identity on {child.ModelDetail.Type.GetNiceName()}");
                var dependantIdentity = child.ModelDetail.IdentityProperties[0];

                sb.Write("JOIN[");
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
        protected override void GenerateEnding(QueryOperation select, Graph graph, ModelDetail modelDetail, ref CharWriteBuffer sb)
        {

        }

        protected override void AppendLineBreak(ref CharWriteBuffer sb)
        {
            sb.Write(Environment.NewLine);
        }

        protected override string OperatorToString(Operator operation)
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
    }
}
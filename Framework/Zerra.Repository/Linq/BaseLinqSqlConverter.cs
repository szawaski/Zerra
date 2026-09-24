using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zerra.Reflection;
using Zerra.Repository.IO;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// Base class for converting LINQ expressions into SQL query strings.
    /// </summary>
    public abstract partial class BaseLinqSqlConverter
    {
        /// <summary>
        /// Converts the specified query parameters into a SQL string.
        /// </summary>
        /// <param name="select">The query operation type (e.g., Single, Many, First).</param>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of records to skip.</param>
        /// <param name="take">An optional number of records to take.</param>
        /// <param name="graph">An optional graph describing which properties to include.</param>
        /// <param name="modelDetail">Metadata about the model being queried.</param>
        /// <returns>The generated SQL query string.</returns>
        public string ConvertInternal(QueryOperation select, LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail)
        {
            var operationContext = new MemberContext();
            var sb = new CharWriter();
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

        /// <summary>
        /// Builds the SQL query into the provided <see cref="CharWriter"/>.
        /// </summary>
        /// <param name="sb">The writer to append the SQL into.</param>
        /// <param name="select">The query operation type.</param>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of records to skip.</param>
        /// <param name="take">An optional number of records to take.</param>
        /// <param name="graph">An optional graph describing which properties to include.</param>
        /// <param name="modelDetail">Metadata about the model being queried.</param>
        /// <param name="operationContext">The current member context for the operation.</param>
        protected void Convert(ref CharWriter sb, QueryOperation select, LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail, MemberContext operationContext)
        {
            var hasWhere = where is not null;
            var hasOrderSkipTake = (select == QueryOperation.Many || select == QueryOperation.First) && (order?.OrderExpressions.Length > 0 || skip > 0 || take > 0);

            GenerateSelect(select, graph, modelDetail, ref sb);

            GenerateFrom(modelDetail, ref sb);

            if (hasWhere || hasOrderSkipTake)
            {
                var rootDependant = new ParameterDependant(modelDetail, null);
                var sbWhereOrder = new CharWriter();
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

        /// <summary>
        /// Converts a LINQ <see cref="Expression"/> node into its SQL representation.
        /// </summary>
        /// <param name="exp">The expression to convert.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        protected void ConvertToSql(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            switch (exp.NodeType)
            {
                case ExpressionType.Add:
                    if (exp.Type == typeof(string))
                        ConvertToSqlStringConcat(exp, ref sb, context);
                    else
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
                    if (exp.Type == typeof(bool) || exp.Type == typeof(bool?))
                        ConvertToSqlBinary(context.Inverted ? Operator.Or : Operator.And, exp, ref sb, context);
                    else
                        ConvertToSqlBinary(Operator.BitwiseAnd, exp, ref sb, context);
                    break;
                case ExpressionType.AndAlso:
                    ConvertToSqlBinary(context.Inverted ? Operator.Or : Operator.And, exp, ref sb, context);
                    break;
                case ExpressionType.AndAssign:
                    throw new NotImplementedException();
                case ExpressionType.ArrayIndex:
                    ConvertToSqlArrayIndex(exp, ref sb, context);
                    break;
                case ExpressionType.ArrayLength:
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
                case ExpressionType.Assign:
                    throw new NotImplementedException();
                case ExpressionType.Block:
                    throw new NotImplementedException();
                case ExpressionType.Call:
                    ConvertToSqlCall(exp, ref sb, context);
                    break;
                case ExpressionType.Coalesce:
                    if (IsEvaluatable(exp))
                        ConvertToSqlEvaluate(exp, ref sb, context);
                    else
                        ConvertToSqlCoalesce(exp, ref sb, context);
                    break;
                case ExpressionType.Conditional:
                    if (IsEvaluatable(exp))
                        ConvertToSqlEvaluate(exp, ref sb, context);
                    else
                        ConvertToSqlConditional(exp, ref sb, context);
                    break;
                case ExpressionType.Constant:
                    ConvertToSqlConstant(exp, ref sb, context);
                    break;
                case ExpressionType.Convert:
                    ConvertToSqlUnary(Operator.Null, exp, ref sb, context);
                    break;
                case ExpressionType.ConvertChecked:
                    ConvertToSqlUnary(Operator.Null, exp, ref sb, context);
                    break;
                case ExpressionType.DebugInfo:
                    throw new NotImplementedException();
                case ExpressionType.Decrement:
                    throw new NotImplementedException();
                case ExpressionType.Default:
                    ConvertToSqlEvaluate(exp, ref sb, context);
                    break;
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
                    if (exp.Type == typeof(bool) || exp.Type == typeof(bool?))
                        throw new NotSupportedException("Exclusive or of conditions is not supported");
                    ConvertToSqlBinary(Operator.BitwiseXor, exp, ref sb, context);
                    break;
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
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
                case ExpressionType.Invoke:
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
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
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
                case ExpressionType.Loop:
                    throw new NotImplementedException();
                case ExpressionType.MemberAccess:
                    ConvertToSqlMember(exp, ref sb, context);
                    break;
                case ExpressionType.MemberInit:
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
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
                    ConvertToSqlUnary(Operator.Negative, exp, ref sb, context);
                    break;
                case ExpressionType.NegateChecked:
                    ConvertToSqlUnary(Operator.Negative, exp, ref sb, context);
                    break;
                case ExpressionType.New:
                    ConvertToSqlNew(exp, ref sb, context);
                    break;
                case ExpressionType.NewArrayBounds:
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
                case ExpressionType.NewArrayInit:
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
                case ExpressionType.Not:
                    if (exp.Type == typeof(bool) || exp.Type == typeof(bool?))
                    {
                        context.InvertStack++;
                        ConvertToSqlUnary(Operator.Not, exp, ref sb, context);
                        context.InvertStack--;
                    }
                    else
                    {
                        //C# writes ~ on an integer as Not
                        ConvertToSqlUnary(Operator.BitwiseNot, exp, ref sb, context);
                    }
                    break;
                case ExpressionType.NotEqual:
                    ConvertToSqlBinary(context.Inverted ? Operator.Equals : Operator.NotEquals, exp, ref sb, context);
                    break;
                case ExpressionType.OnesComplement:
                    ConvertToSqlUnary(Operator.BitwiseNot, exp, ref sb, context);
                    break;
                case ExpressionType.Or:
                    if (exp.Type == typeof(bool) || exp.Type == typeof(bool?))
                        ConvertToSqlBinary(context.Inverted ? Operator.And : Operator.Or, exp, ref sb, context);
                    else
                        ConvertToSqlBinary(Operator.BitwiseOr, exp, ref sb, context);
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
                    {
                        //the Power node comes from VB, C# calls Math.Pow which the dialects convert
                        context.MemberContext.OperatorStack.Push(Operator.Call);
                        var invertStack = context.InvertStack;
                        context.InvertStack = 0;
                        var power = (BinaryExpression)exp;
                        sb.Write("POWER((");
                        ConvertToSql(power.Left, ref sb, context);
                        sb.Write("),(");
                        ConvertToSql(power.Right, ref sb, context);
                        sb.Write("))");
                        context.InvertStack = invertStack;
                        _ = context.MemberContext.OperatorStack.Pop();
                        break;
                    }
                case ExpressionType.PowerAssign:
                    throw new NotImplementedException();
                case ExpressionType.PreDecrementAssign:
                    throw new NotImplementedException();
                case ExpressionType.PreIncrementAssign:
                    throw new NotImplementedException();
                case ExpressionType.Quote:
                    ConvertToSql(((UnaryExpression)exp).Operand, ref sb, context);
                    break;
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
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
                case ExpressionType.TypeEqual:
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
                case ExpressionType.TypeIs:
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
                case ExpressionType.UnaryPlus:
                    ConvertToSqlUnary(Operator.Null, exp, ref sb, context);
                    break;
                case ExpressionType.Unbox:
                    ConvertToSqlUnary(Operator.Null, exp, ref sb, context);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Converts a lambda expression into its SQL representation.
        /// </summary>
        /// <param name="exp">The lambda expression to convert.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        protected abstract void ConvertToSqlLambda(Expression exp, ref CharWriter sb, BuilderContext context);
        private void ConvertToSqlUnary(Operator prefixOperation, Expression exp, ref CharWriter sb, BuilderContext context)
        {
            context.MemberContext.OperatorStack.Push(prefixOperation);

            var unary = (UnaryExpression)exp;
            var prefix = OperatorToString(prefixOperation);

            if (prefix is null)
            {
                ConvertToSql(unary.Operand, ref sb, context);
            }
            else
            {
                var castSigned = prefixOperation == Operator.BitwiseNot && BitwiseResultUnsigned;
                if (castSigned)
                    sb.Write("CAST(");

                //the operand may be an expression like a+b so it needs its own brackets
                sb.Write(prefix);
                sb.Write('(');
                ConvertToSql(unary.Operand, ref sb, context);
                sb.Write(')');

                if (castSigned)
                    sb.Write(" AS SIGNED)");
            }

            _ = context.MemberContext.OperatorStack.Pop();
        }
        private void ConvertToSqlBinary(Operator operation, Expression exp, ref CharWriter sb, BuilderContext context)
        {
            context.MemberContext.OperatorStack.Push(operation);

            var binary = (BinaryExpression)exp;

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

            var castSigned = BitwiseResultUnsigned && (operation == Operator.BitwiseAnd || operation == Operator.BitwiseOr || operation == Operator.BitwiseXor);
            if (castSigned)
                sb.Write("CAST(");

            sb.Write('(');
            ConvertToSql(binaryLeft, ref sb, context);
            sb.Write(')');

            sb.Write(OperatorToString(operation));

            if (binaryRight is not null)
            {
                sb.Write('(');
                ConvertToSql(binaryRight, ref sb, context);
                sb.Write(')');
            }
            else
            {
                sb.Write(" NULL");
            }

            if (castSigned)
                sb.Write(" AS SIGNED)");

            _ = context.MemberContext.OperatorStack.Pop();
        }
        /// <summary>
        /// Whether bitwise operators give an unsigned 64 bit result, such as in MySQL, so the result is cast back to a signed number to match .NET.
        /// </summary>
        protected virtual bool BitwiseResultUnsigned => false;
        private void ConvertToSqlArrayIndex(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            var array = (BinaryExpression)exp;
            var member = (Array)EvaluateMemberAccess(array.Left)!;
            object? value;
            if (array.Right.Type == typeof(long))
            {
                var index = (long)Evaluate(array.Right)!;
                value = member.GetValue(index);
            }
            else
            {
                var index = (int)Evaluate(array.Right)!;
                value = member.GetValue(index);
            }

            ConvertToSqlValue(array.Left.Type.GetElementType()!, value, ref sb, context);
        }
        /// <summary>
        /// Converts a member access expression into its SQL representation.
        /// </summary>
        /// <param name="exp">The member access expression to convert.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        protected void ConvertToSqlMember(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            var member = (MemberExpression)exp;

            if (member.Expression is null || IsEvaluatable(member.Expression))
            {
                ConvertToSqlEvaluate(member, ref sb, context);
            }
            else if (!ConvertToSqlMemberFunction(member, ref sb, context))
            {
                context.MemberContext.MemberAccessStack.Push(member);
                ConvertToSql(member.Expression, ref sb, context);
                _ = context.MemberContext.MemberAccessStack.Pop();
            }
        }
        /// <summary>
        /// Converts a member that is a SQL function of the expression it is read from, such as <c>string.Length</c>, <c>DateTime.Year</c>, or <c>Nullable.HasValue</c>.
        /// Only called when the expression the member is read from can't be evaluated, so it involves the model.
        /// </summary>
        /// <param name="member">The member access expression to convert.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        /// <returns><see langword="true"/> if the member was written; <see langword="false"/> if it is not a function member.</returns>
        protected abstract bool ConvertToSqlMemberFunction(MemberExpression member, ref CharWriter sb, BuilderContext context);
        /// <summary>
        /// Converts a null coalescing (<c>??</c>) expression into its SQL representation.
        /// </summary>
        /// <param name="exp">The coalesce expression to convert.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        protected abstract void ConvertToSqlCoalesce(Expression exp, ref CharWriter sb, BuilderContext context);
        /// <summary>
        /// Converts a string addition (<c>a + b</c>) into its SQL representation.
        /// </summary>
        /// <param name="exp">The add expression with a string result to convert.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        protected abstract void ConvertToSqlStringConcat(Expression exp, ref CharWriter sb, BuilderContext context);
        private void ConvertToSqlConstant(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            var constant = (ConstantExpression)exp;

            ConvertToSqlConstantStack(constant.Type, constant.Value, ref sb, context);
        }
        private void ConvertToSqlConstantStack(Type type, object? value, ref CharWriter sb, BuilderContext context)
        {
            if (context.MemberContext.MemberAccessStack.Count > 0)
            {
                var memberProperty = context.MemberContext.MemberAccessStack.Pop();
                if (value is null)
                {
                    sb.Write("NULL");
                }
                else
                {
                    switch (memberProperty.Member.MemberType)
                    {
                        case MemberTypes.Field:
                            {
                                var field = (FieldInfo)memberProperty.Member;
                                var fieldValue = field.GetValue(value);
                                ConvertToSqlConstantStack(field.FieldType, fieldValue, ref sb, context);
                                break;
                            }
                        case MemberTypes.Property:
                            {
                                var property = (PropertyInfo)memberProperty.Member;
                                if (property.GetMethod is not null)
                                {
                                    var propertyValue = property.GetValue(value);
                                    ConvertToSqlConstantStack(property.PropertyType, propertyValue, ref sb, context);
                                }
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
        /// <summary>
        /// Converts a method call expression into its SQL representation.
        /// </summary>
        /// <param name="exp">The method call expression to convert.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        protected abstract void ConvertToSqlCall(Expression exp, ref CharWriter sb, BuilderContext context);
        private void ConvertToSqlNew(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            context.MemberContext.OperatorStack.Push(Operator.New);

            var newExp = (NewExpression)exp;

            var argumentTypes = newExp.Arguments.Select(x => x.Type).ToArray();

            //constructor should not be trimmed since the expression should be evaluatable
            var typeDetail = newExp.Type.GetTypeDetail();
            var constructor = typeDetail.GetConstructor(argumentTypes);
            //var constructor = newExp.Type.GetConstructor(argumentTypes)!;

            var parameters = new object?[newExp.Arguments.Count];
            var i = 0;
            foreach (var argument in newExp.Arguments)
            {
                var argumentValue = Evaluate(argument);
                parameters[i++] = argumentValue;
            }

            //var value = constructor.Invoke(parameters);
            var value = constructor.CreatorBoxed(parameters);
            ConvertToSqlValue(newExp.Type, value, ref sb, context);

            _ = context.MemberContext.OperatorStack.Pop();
        }
        private void ConvertToSqlParameter(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            var parameter = (ParameterExpression)exp;

            if (parameter.Name is null)
                throw new Exception($"Parameter has no name {parameter.Type.Name}");

            var modelDetail = context.MemberContext.ModelContexts[parameter];

            var parameterInContext = modelDetail == context.MemberContext.ModelStack.Peek() || modelDetail.Type == typeof(object);

            ConvertToSqlParameterModel(modelDetail, ref sb, context, parameterInContext);
        }
        /// <summary>
        /// Converts a model parameter reference into its SQL representation.
        /// </summary>
        /// <param name="modelDetail">Metadata about the model.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        /// <param name="parameterInContext">Indicates whether the parameter is the current model in context.</param>
        protected abstract void ConvertToSqlParameterModel(ModelDetail modelDetail, ref CharWriter sb, BuilderContext context, bool parameterInContext);
        /// <summary>
        /// Converts a conditional (ternary) expression into its SQL representation.
        /// </summary>
        /// <param name="exp">The conditional expression to convert.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        protected abstract void ConvertToSqlConditional(Expression exp, ref CharWriter sb, BuilderContext context);
        /// <summary>
        /// Evaluates an expression to a value and converts it into its SQL representation.
        /// </summary>
        /// <param name="exp">The expression to evaluate and convert.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        protected void ConvertToSqlEvaluate(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            //no operator is pushed, a boolean value needs to see the operator it is used in to know if it is a condition
            var value = Evaluate(exp);
            ConvertToSqlValue(exp.Type, value, ref sb, context);
        }
        private void ConvertToSqlEvaluateOnly(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            if (!IsEvaluatable(exp))
                throw new NotSupportedException($"Cannot convert {exp.NodeType} expression that uses the model");
            ConvertToSqlEvaluate(exp, ref sb, context);
        }

        /// <summary>
        /// Converts a runtime value of the given type into its SQL representation.
        /// </summary>
        /// <param name="type">The CLR type of the value.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        protected void ConvertToSqlValue(Type type, object? value, ref CharWriter sb, BuilderContext context)
        {
            MemberExpression? memberProperty = null;

            if (context.MemberContext.MemberAccessStack.Count > 0)
                memberProperty = context.MemberContext.MemberAccessStack.Pop();

            var memberPropertyHandled = ConvertToSqlValueRender(memberProperty, type, value, ref sb, context);

            if (memberProperty is not null)
            {
                if (!memberPropertyHandled)
                    throw new NotSupportedException($"{type.FullName}.{memberProperty.Member.Name} not supported");
                context.MemberContext.MemberAccessStack.Push(memberProperty);
            }
        }

        /// <summary>The type name of <see cref="Nullable{T}"/>, used for nullable type detection.</summary>
        protected static readonly string nullableTypeName = typeof(Nullable<>).Name;

        /// <summary>
        /// Renders a value into SQL, optionally using a member property context.
        /// </summary>
        /// <param name="memberProperty">An optional member property that provides additional context.</param>
        /// <param name="type">The CLR type of the value.</param>
        /// <param name="value">The value to render.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        /// <returns><see langword="true"/> if the member property was handled; otherwise, <see langword="false"/>.</returns>
        protected abstract bool ConvertToSqlValueRender(MemberExpression? memberProperty, Type type, object? value, ref CharWriter sb, BuilderContext context);

        /// <summary>
        /// Determines whether the given expression can be fully evaluated to a constant value
        /// without requiring model/parameter context.
        /// </summary>
        /// <param name="exp">The expression to check.</param>
        /// <returns><see langword="true"/> if the expression is evaluatable; otherwise, <see langword="false"/>.</returns>
        protected bool IsEvaluatable(Expression exp)
        {
            return IsEvaluatable(exp, null);
        }
        //scope holds the parameters of lambdas and blocks inside the expression, those have values when it's evaluated
        private bool IsEvaluatable(Expression exp, List<ParameterExpression>? scope)
        {
            switch (exp.NodeType)
            {
                case ExpressionType.Constant:
                case ExpressionType.Default:
                    return true;

                case ExpressionType.Parameter:
                    return scope is not null && scope.Contains((ParameterExpression)exp);

                case ExpressionType.MemberAccess:
                    {
                        var member = (MemberExpression)exp;
                        return member.Expression is null || IsEvaluatable(member.Expression, scope);
                    }

                case ExpressionType.Call:
                    {
                        var call = (MethodCallExpression)exp;
                        foreach (var arg in call.Arguments)
                        {
                            if (!IsEvaluatable(arg, scope))
                                return false;
                        }
                        return call.Object is null || IsEvaluatable(call.Object, scope);
                    }

                case ExpressionType.Lambda:
                    {
                        var lambda = (LambdaExpression)exp;
                        scope ??= new();
                        foreach (var parameter in lambda.Parameters)
                            scope.Add(parameter);
                        var result = IsEvaluatable(lambda.Body, scope);
                        foreach (var parameter in lambda.Parameters)
                            _ = scope.Remove(parameter);
                        return result;
                    }

                case ExpressionType.Block:
                    {
                        var block = (BlockExpression)exp;
                        scope ??= new();
                        foreach (var variable in block.Variables)
                            scope.Add(variable);
                        var result = true;
                        foreach (var expression in block.Expressions)
                        {
                            if (!IsEvaluatable(expression, scope))
                            {
                                result = false;
                                break;
                            }
                        }
                        foreach (var variable in block.Variables)
                            _ = scope.Remove(variable);
                        return result;
                    }

                case ExpressionType.Conditional:
                    {
                        var conditional = (ConditionalExpression)exp;
                        return IsEvaluatable(conditional.Test, scope) && IsEvaluatable(conditional.IfTrue, scope) && IsEvaluatable(conditional.IfFalse, scope);
                    }

                case ExpressionType.New:
                    {
                        var newExp = (NewExpression)exp;
                        foreach (var arg in newExp.Arguments)
                        {
                            if (!IsEvaluatable(arg, scope))
                                return false;
                        }
                        return true;
                    }

                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    {
                        var newArray = (NewArrayExpression)exp;
                        foreach (var expression in newArray.Expressions)
                        {
                            if (!IsEvaluatable(expression, scope))
                                return false;
                        }
                        return true;
                    }

                case ExpressionType.ListInit:
                    {
                        var listInit = (ListInitExpression)exp;
                        if (!IsEvaluatable(listInit.NewExpression, scope))
                            return false;
                        foreach (var initializer in listInit.Initializers)
                        {
                            foreach (var arg in initializer.Arguments)
                            {
                                if (!IsEvaluatable(arg, scope))
                                    return false;
                            }
                        }
                        return true;
                    }

                case ExpressionType.MemberInit:
                    {
                        var memberInit = (MemberInitExpression)exp;
                        return IsEvaluatable(memberInit.NewExpression, scope) && IsEvaluatableBindings(memberInit.Bindings, scope);
                    }

                case ExpressionType.Invoke:
                    {
                        var invoke = (InvocationExpression)exp;
                        foreach (var arg in invoke.Arguments)
                        {
                            if (!IsEvaluatable(arg, scope))
                                return false;
                        }
                        return IsEvaluatable(invoke.Expression, scope);
                    }

                case ExpressionType.Index:
                    {
                        var index = (IndexExpression)exp;
                        foreach (var arg in index.Arguments)
                        {
                            if (!IsEvaluatable(arg, scope))
                                return false;
                        }
                        return index.Object is null || IsEvaluatable(index.Object, scope);
                    }

                case ExpressionType.TypeIs:
                case ExpressionType.TypeEqual:
                    return IsEvaluatable(((TypeBinaryExpression)exp).Expression, scope);

                case ExpressionType.ArrayLength:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.Decrement:
                case ExpressionType.Increment:
                case ExpressionType.IsFalse:
                case ExpressionType.IsTrue:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.OnesComplement:
                case ExpressionType.PostDecrementAssign:
                case ExpressionType.PostIncrementAssign:
                case ExpressionType.PreDecrementAssign:
                case ExpressionType.PreIncrementAssign:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                case ExpressionType.UnaryPlus:
                case ExpressionType.Unbox:
                    return IsEvaluatable(((UnaryExpression)exp).Operand, scope);

                case ExpressionType.Add:
                case ExpressionType.AddAssign:
                case ExpressionType.AddAssignChecked:
                case ExpressionType.AddChecked:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.AndAssign:
                case ExpressionType.ArrayIndex:
                case ExpressionType.Assign:
                case ExpressionType.Coalesce:
                case ExpressionType.Divide:
                case ExpressionType.DivideAssign:
                case ExpressionType.Equal:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.ExclusiveOrAssign:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LeftShift:
                case ExpressionType.LeftShiftAssign:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.Modulo:
                case ExpressionType.ModuloAssign:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.MultiplyAssignChecked:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.NotEqual:
                case ExpressionType.Or:
                case ExpressionType.OrAssign:
                case ExpressionType.OrElse:
                case ExpressionType.Power:
                case ExpressionType.PowerAssign:
                case ExpressionType.RightShift:
                case ExpressionType.RightShiftAssign:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractAssign:
                case ExpressionType.SubtractAssignChecked:
                case ExpressionType.SubtractChecked:
                    {
                        var binary = (BinaryExpression)exp;
                        return IsEvaluatable(binary.Left, scope) && IsEvaluatable(binary.Right, scope);
                    }

                default:
                    //control flow such as Goto, Loop, Switch, Throw, and Try isn't written by C# in an expression tree
                    return false;
            }
        }
        private bool IsEvaluatableBindings(IEnumerable<MemberBinding> bindings, List<ParameterExpression>? scope)
        {
            foreach (var binding in bindings)
            {
                switch (binding.BindingType)
                {
                    case MemberBindingType.Assignment:
                        if (!IsEvaluatable(((MemberAssignment)binding).Expression, scope))
                            return false;
                        break;
                    case MemberBindingType.MemberBinding:
                        if (!IsEvaluatableBindings(((MemberMemberBinding)binding).Bindings, scope))
                            return false;
                        break;
                    case MemberBindingType.ListBinding:
                        foreach (var initializer in ((MemberListBinding)binding).Initializers)
                        {
                            foreach (var arg in initializer.Arguments)
                            {
                                if (!IsEvaluatable(arg, scope))
                                    return false;
                            }
                        }
                        break;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines whether the given expression evaluates to <see langword="null"/>.
        /// </summary>
        /// <param name="exp">The expression to check.</param>
        /// <returns><see langword="true"/> if the expression evaluates to <see langword="null"/>; otherwise, <see langword="false"/>.</returns>
        protected bool IsNull(Expression exp)
        {
            return exp.NodeType switch
            {
                ExpressionType.Add => false,
                ExpressionType.AddAssign => false,
                ExpressionType.AddAssignChecked => false,
                ExpressionType.AddChecked => false,
                ExpressionType.And => false,
                ExpressionType.AndAlso => false,
                ExpressionType.AndAssign => false,
                ExpressionType.ArrayIndex => IsNullArrayIndex(exp),
                ExpressionType.ArrayLength => false,
                ExpressionType.Assign => false,
                ExpressionType.Block => false,
                ExpressionType.Call => IsNullCall(exp),
                ExpressionType.Coalesce => IsNullEvaluated(exp),
                ExpressionType.Conditional => IsNullEvaluated(exp),
                ExpressionType.Constant => IsNullConstant(exp),
                ExpressionType.Convert => IsNullUnary(exp),
                ExpressionType.ConvertChecked => false,
                ExpressionType.DebugInfo => false,
                ExpressionType.Decrement => false,
                ExpressionType.Default => !exp.Type.IsValueType || exp.Type.Name == nullableTypeName,
                ExpressionType.Divide => false,
                ExpressionType.DivideAssign => false,
                ExpressionType.Dynamic => false,
                ExpressionType.Equal => false,
                ExpressionType.ExclusiveOr => false,
                ExpressionType.ExclusiveOrAssign => false,
                ExpressionType.Extension => false,
                ExpressionType.Goto => false,
                ExpressionType.GreaterThan => false,
                ExpressionType.GreaterThanOrEqual => false,
                ExpressionType.Increment => false,
                ExpressionType.Index => false,
                ExpressionType.Invoke => false,
                ExpressionType.IsFalse => false,
                ExpressionType.IsTrue => false,
                ExpressionType.Label => false,
                ExpressionType.Lambda => false,
                ExpressionType.LeftShift => false,
                ExpressionType.LeftShiftAssign => false,
                ExpressionType.LessThan => false,
                ExpressionType.LessThanOrEqual => false,
                ExpressionType.ListInit => false,
                ExpressionType.Loop => false,
                ExpressionType.MemberAccess => IsNullMemberAccess(exp),
                ExpressionType.MemberInit => false,
                ExpressionType.Modulo => false,
                ExpressionType.ModuloAssign => false,
                ExpressionType.Multiply => false,
                ExpressionType.MultiplyAssign => false,
                ExpressionType.MultiplyAssignChecked => false,
                ExpressionType.MultiplyChecked => false,
                ExpressionType.Negate => false,
                ExpressionType.NegateChecked => false,
                ExpressionType.New => false,
                ExpressionType.NewArrayBounds => false,
                ExpressionType.NewArrayInit => false,
                ExpressionType.Not => false,
                ExpressionType.NotEqual => false,
                ExpressionType.OnesComplement => false,
                ExpressionType.Or => false,
                ExpressionType.OrAssign => false,
                ExpressionType.OrElse => false,
                ExpressionType.Parameter => false,
                ExpressionType.PostDecrementAssign => false,
                ExpressionType.PostIncrementAssign => false,
                ExpressionType.Power => false,
                ExpressionType.PowerAssign => false,
                ExpressionType.PreDecrementAssign => false,
                ExpressionType.PreIncrementAssign => false,
                ExpressionType.Quote => false,
                ExpressionType.RightShift => false,
                ExpressionType.RightShiftAssign => false,
                ExpressionType.RuntimeVariables => false,
                ExpressionType.Subtract => false,
                ExpressionType.SubtractAssign => false,
                ExpressionType.SubtractAssignChecked => false,
                ExpressionType.SubtractChecked => false,
                ExpressionType.Switch => false,
                ExpressionType.Throw => false,
                ExpressionType.Try => false,
                ExpressionType.TypeAs => false,
                ExpressionType.TypeEqual => false,
                ExpressionType.TypeIs => false,
                ExpressionType.UnaryPlus => false,
                ExpressionType.Unbox => false,
                _ => throw new NotImplementedException(),
            };
            ;
        }
        private bool IsNullUnary(Expression exp)
        {
            var unary = (UnaryExpression)exp;
            return IsNull(unary.Operand);
        }
        private bool IsNullConstant(Expression exp)
        {
            var constant = (ConstantExpression)exp;
            return constant.Value is null;
        }
        private bool IsNullCall(Expression exp)
        {
            //a call that uses the model is written as SQL so it isn't a null literal
            return IsNullEvaluated(exp);
        }
        private bool IsNullEvaluated(Expression exp)
        {
            //only a type that can hold null is evaluated, so a call like Guid.NewGuid() isn't run an extra time
            if (exp.Type.IsValueType && exp.Type.Name != nullableTypeName)
                return false;
            if (!IsEvaluatable(exp))
                return false;
            return Evaluate(exp) is null;
        }
        private bool IsNullArrayIndex(Expression exp)
        {
            var array = (BinaryExpression)exp;
            var member = (Array)EvaluateMemberAccess(array.Left)!;
            object? value;
            if (array.Right.Type == typeof(long))
            {
                var index = (long)Evaluate(array.Right)!;
                value = member.GetValue(index);
            }
            else
            {
                var index = (int)Evaluate(array.Right)!;
                value = member.GetValue(index);
            }

            return value is null;
        }
        private bool IsNullMemberAccess(Expression exp)
        {
            var member = (MemberExpression)exp;

            object? value;
            if (member.Expression is null)
            {
                value = Evaluate(member);
            }
            else
            {
                var isEvaluatable = IsEvaluatable(member.Expression);
                if (!isEvaluatable)
                    return false;

                var expressionValue = member.Expression is null ? null : Evaluate(member.Expression);
                switch (member.Member.MemberType)
                {
                    case MemberTypes.Field:
                        var fieldInfo = (FieldInfo)member.Member;
                        if (expressionValue is null && !fieldInfo.IsStatic)
                            return true;
                        value = fieldInfo.GetValue(expressionValue);
                        break;
                    case MemberTypes.Property:
                        var propertyInfo = (PropertyInfo)member.Member;
                        if (propertyInfo.GetMethod is null)
                            return true;
                        if (expressionValue is null && !propertyInfo.GetMethod.IsStatic)
                            return true;
                        value = propertyInfo.GetValue(expressionValue);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            return value is null;
        }

        /// <summary>
        /// Evaluates an expression that doesn't use the model to its value.
        /// </summary>
        /// <param name="exp">The expression to evaluate.</param>
        /// <returns>The value of the expression.</returns>
        protected static object? Evaluate(Expression exp)
        {
            return exp.NodeType switch
            {
                ExpressionType.Constant => EvaluateConstant(exp),
                ExpressionType.MemberAccess => EvaluateMemberAccess(exp),
                ExpressionType.Lambda => EvaludateLambda(exp),
                ExpressionType.Default => EvaluateDefault(exp),
                ExpressionType.Coalesce => EvaluateCoalesce(exp),
                ExpressionType.Conditional => EvaluateConditional(exp),
                _ => EvaluateInvoke(exp),
            };
        }
        [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method.", Justification = "Only value types are created here and their default value doesn't use a constructor.")]
        private static object? EvaluateDefault(Expression exp)
        {
            if (!exp.Type.IsValueType || exp.Type.Name == nullableTypeName)
                return null;
            return RuntimeHelpers.GetUninitializedObject(exp.Type);
        }
        private static object? EvaluateCoalesce(Expression exp)
        {
            var coalesce = (BinaryExpression)exp;
            if (coalesce.Conversion is not null)
                return EvaluateInvoke(exp);
            return Evaluate(coalesce.Left) ?? Evaluate(coalesce.Right);
        }
        private static object? EvaluateConditional(Expression exp)
        {
            var conditional = (ConditionalExpression)exp;
            return (bool)Evaluate(conditional.Test)! ? Evaluate(conditional.IfTrue) : Evaluate(conditional.IfFalse);
        }
        private static object? EvaluateConstant(Expression exp)
        {
            var constant = (ConstantExpression)exp;
            return constant.Value;
        }
        private static object? EvaluateMemberAccess(Expression exp)
        {
            var member = (MemberExpression)exp;
            var expressionValue = member.Expression is null ? null : Evaluate(member.Expression);

            object? value;
            switch (member.Member.MemberType)
            {
                case MemberTypes.Field:
                    var fieldInfo = (FieldInfo)member.Member;
                    if (expressionValue is null && !fieldInfo.IsStatic)
                        return null;
                    value = fieldInfo.GetValue(expressionValue);
                    break;
                case MemberTypes.Property:
                    var propertyInfo = (PropertyInfo)member.Member;
                    if (propertyInfo.GetMethod is null)
                        return null;
                    if (expressionValue is null && !propertyInfo.GetMethod.IsStatic)
                        return null;
                    value = propertyInfo.GetValue(expressionValue);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return value;
        }
        private static object? EvaludateLambda(Expression exp)
        {
            var lambda = (LambdaExpression)exp;
            return lambda.Compile().DynamicInvoke();
        }
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Falls back to InvalidOperationException at runtime when dynamic code is unsupported.")]
        private static object? EvaluateInvoke(Expression exp)
        {
            if (exp.NodeType == ExpressionType.Call && (exp.Type.Name == "ReadOnlySpan`1" || exp.Type.Name == "Span`1"))
            {
                var call = (MethodCallExpression)exp;
                if (call.Method.Name == "op_Implicit")
                {
                    var valueInner = Evaluate(call.Arguments[0]);
                    return valueInner;
                }
            }

            try
            {
                var value = Expression.Lambda(exp).Compile().DynamicInvoke();
                return value;
            }
            catch (Exception ex)
            {
                if (!RuntimeFeature.IsDynamicCodeSupported)
                    throw new InvalidOperationException($"Dynamic code execution is not supported in this runtime environment.", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates the WHERE clause SQL into the provided writer.
        /// </summary>
        /// <param name="where">The filter expression.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="rootDependant">The root parameter dependant used for join tracking.</param>
        /// <param name="operationContext">The current member context for the operation.</param>
        protected abstract void GenerateWhere(LambdaExpression? where, ref CharWriter sb, ParameterDependant rootDependant, MemberContext operationContext);
        /// <summary>
        /// Generates the ORDER BY, OFFSET, and FETCH/LIMIT clauses into the provided writer.
        /// </summary>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of records to skip.</param>
        /// <param name="take">An optional number of records to take.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="rootDependant">The root parameter dependant used for join tracking.</param>
        /// <param name="operationContext">The current member context for the operation.</param>
        protected abstract void GenerateOrderSkipTake(QueryOrder? order, int? skip, int? take, ref CharWriter sb, ParameterDependant rootDependant, MemberContext operationContext);
        /// <summary>
        /// Generates the SELECT clause into the provided writer.
        /// </summary>
        /// <param name="select">The query operation type.</param>
        /// <param name="graph">An optional graph describing which properties to include.</param>
        /// <param name="modelDetail">Metadata about the model being queried.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        protected abstract void GenerateSelect(QueryOperation select, Graph? graph, ModelDetail modelDetail, ref CharWriter sb);
        /// <summary>
        /// Generates the column list for the SELECT clause into the provided writer.
        /// </summary>
        /// <param name="graph">An optional graph describing which properties to include.</param>
        /// <param name="modelDetail">Metadata about the model being queried.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        protected abstract void GenerateSelectProperties(Graph? graph, ModelDetail modelDetail, ref CharWriter sb);
        /// <summary>
        /// Generates the FROM clause into the provided writer.
        /// </summary>
        /// <param name="modelDetail">Metadata about the model being queried.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        protected abstract void GenerateFrom(ModelDetail modelDetail, ref CharWriter sb);
        /// <summary>
        /// Generates any required JOIN clauses into the provided writer based on parameter dependants.
        /// </summary>
        /// <param name="dependant">The root parameter dependant describing the join graph.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        protected abstract void GenerateJoin(ParameterDependant dependant, ref CharWriter sb);
        /// <summary>
        /// Generates any trailing SQL (e.g., TOP/LIMIT wrappers, semicolons) into the provided writer.
        /// </summary>
        /// <param name="select">The query operation type.</param>
        /// <param name="graph">An optional graph describing which properties to include.</param>
        /// <param name="modelDetail">Metadata about the model being queried.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        protected abstract void GenerateEnding(QueryOperation select, Graph? graph, ModelDetail modelDetail, ref CharWriter sb);

        /// <summary>
        /// Appends a line break into the provided writer.
        /// </summary>
        /// <param name="sb">The writer to append into.</param>
        protected abstract void AppendLineBreak(ref CharWriter sb);

        /// <summary>
        /// Returns the SQL string representation of the given <see cref="Operator"/>.
        /// </summary>
        /// <param name="operation">The operator to convert.</param>
        /// <returns>The SQL operator string, or <see langword="null"/> if not applicable.</returns>
        protected abstract string? OperatorToString(Operator operation);
    }
}

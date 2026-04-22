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
        public string ConvertInternal(QueryOperation select, Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail)
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
        protected void Convert(ref CharWriter sb, QueryOperation select, Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail, MemberContext operationContext)
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
                    ConvertToSqlArrayIndex(exp, ref sb, context);
                    break;
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
                    ConvertToSqlUnary(Operator.Negative, exp, ref sb, context);
                    break;
                case ExpressionType.NegateChecked:
                    ConvertToSqlUnary(Operator.Negative, exp, ref sb, context);
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
                    ConvertToSqlUnary(Operator.Null, exp, ref sb, context);
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
            sb.Write(OperatorToString(prefixOperation));

            ConvertToSql(unary.Operand, ref sb, context);

            //if (suffixOperation is not null)
            //    sb.Write(suffixOperation);

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

            _ = context.MemberContext.OperatorStack.Pop();
        }
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

            if (member.Expression is null)
            {
                ConvertToSqlEvaluate(member, ref sb, context);
            }
            else
            {
                context.MemberContext.MemberAccessStack.Push(member);
                ConvertToSql(member.Expression, ref sb, context);
                _ = context.MemberContext.MemberAccessStack.Pop();
            }
        }
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

            var modelDetail = context.MemberContext.ModelContexts[parameter.Name];

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
            context.MemberContext.OperatorStack.Push(Operator.Evaluate);

            var value = Evaluate(exp);
            ConvertToSqlValue(exp.Type, value, ref sb, context);

            _ = context.MemberContext.OperatorStack.Pop();
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
        private bool IsEvaluatableUnary(Expression exp)
        {
            var unary = (UnaryExpression)exp;
            return IsEvaluatable(unary.Operand);
        }
        private bool IsEvaluatableBinary(Expression exp)
        {
            var binary = (BinaryExpression)exp;
            return IsEvaluatable(binary.Left) && IsEvaluatable(binary.Right);
        }
        private bool IsEvaluatableConstant(Expression exp)
        {
            return true;
        }
        private bool IsEvaluatableBlock(Expression exp)
        {
            var block = (BlockExpression)exp;
            foreach (var variable in block.Variables)
            {
                if (!IsEvaluatable(variable))
                    return false;
            }

            foreach (var expression in block.Expressions)
            {
                if (!IsEvaluatable(expression))
                    return false;
            }

            return true;
        }
        private bool IsEvaluatableCall(Expression exp)
        {
            var call = (MethodCallExpression)exp;

            foreach (var arg in call.Arguments)
            {
                if (!IsEvaluatable(arg))
                    return false;
            }

            if (call.Object is not null)
                return IsEvaluatable(call.Object);

            return true;
        }
        private bool IsEvaluatableMemberAccess(Expression exp)
        {
            var member = (MemberExpression)exp;
            if (member.Expression is null)
            {
                return true;
            }
            return IsEvaluatable(member.Expression);
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
                ExpressionType.Add => throw new NotImplementedException(),
                ExpressionType.AddAssign => throw new NotImplementedException(),
                ExpressionType.AddAssignChecked => throw new NotImplementedException(),
                ExpressionType.AddChecked => throw new NotImplementedException(),
                ExpressionType.And => throw new NotImplementedException(),
                ExpressionType.AndAlso => throw new NotImplementedException(),
                ExpressionType.AndAssign => throw new NotImplementedException(),
                ExpressionType.ArrayIndex => IsNullArrayIndex(exp),
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
            var call = (MethodCallExpression)exp;
            if (call.Object is null)
            {
                var result = true;
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

        private static object? Evaluate(Expression exp)
        {
            return exp.NodeType switch
            {
                ExpressionType.Constant => EvaluateConstant(exp),
                ExpressionType.MemberAccess => EvaluateMemberAccess(exp),
                ExpressionType.Lambda => EvaludateLambda(exp),
                _ => EvaluateInvoke(exp),
            };
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
        private static object? EvaluateInvoke(Expression exp)
        {
            if (exp.NodeType == ExpressionType.Call && exp.Type.Name == "ReadOnlySpan`1" || exp.Type.Name == "Span`1")
            {
                var call = (MethodCallExpression)exp;
                if (call.Method.Name == "op_Implicit")
                {
                    var valueInner = Evaluate(call.Arguments[0]);
                    return valueInner;
                }
            }

            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                try
                {
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
                    var value = Expression.Lambda(exp).Compile().DynamicInvoke();
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
                    return value;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Dynamic code execution is not supported in this runtime environment.", ex);
                }
            }
            else
            {
                var value = Expression.Lambda(exp).Compile().DynamicInvoke();
                return value;
            }
        }

        /// <summary>
        /// Generates the WHERE clause SQL into the provided writer.
        /// </summary>
        /// <param name="where">The filter expression.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="rootDependant">The root parameter dependant used for join tracking.</param>
        /// <param name="operationContext">The current member context for the operation.</param>
        protected abstract void GenerateWhere(Expression? where, ref CharWriter sb, ParameterDependant rootDependant, MemberContext operationContext);
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

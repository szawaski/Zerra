using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Zerra.IO;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract partial class BaseLinqSqlConverter
    {
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

        protected void Convert(ref CharWriter sb, QueryOperation select, Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail, MemberContext operationContext)
        {
            var hasWhere = where != null;
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
        protected abstract void ConvertToSqlLambda(Expression exp, ref CharWriter sb, BuilderContext context);
        private void ConvertToSqlUnary(Operator prefixOperation, Expression exp, ref CharWriter sb, BuilderContext context)
        {
            context.MemberContext.OperatorStack.Push(prefixOperation);

            var unary = (UnaryExpression)exp;
            sb.Write(OperatorToString(prefixOperation));

            ConvertToSql(unary.Operand, ref sb, context);

            //if (suffixOperation != null)
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
        protected void ConvertToSqlMember(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            var member = (MemberExpression)exp;

            if (member.Expression == null)
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
                                var field = (FieldInfo)memberProperty.Member;
                                var fieldValue = field.GetValue(value);
                                ConvertToSqlConstantStack(field.FieldType, fieldValue, ref sb, context);
                                break;
                            }
                        case MemberTypes.Property:
                            {
                                var property = (PropertyInfo)memberProperty.Member;
                                if (property.GetMethod != null)
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
        protected abstract void ConvertToSqlCall(Expression exp, ref CharWriter sb, BuilderContext context);
        private void ConvertToSqlNew(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            context.MemberContext.OperatorStack.Push(Operator.New);

            var newExp = (NewExpression)exp;

            var argumentTypes = newExp.Arguments.Select(x => x.Type).ToArray();
            var constructor = newExp.Type.GetConstructor(argumentTypes)!;

            var parameters = new object?[newExp.Arguments.Count];
            var i = 0;
            foreach (var argument in newExp.Arguments)
            {
                var argumentValue = Expression.Lambda(argument).Compile().DynamicInvoke();
                parameters[i++] = argumentValue;
            }

            var value = constructor.Invoke(parameters.ToArray());
            ConvertToSqlValue(newExp.Type, value, ref sb, context);

            _ = context.MemberContext.OperatorStack.Pop();
        }
        private void ConvertToSqlParameter(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            var parameter = (ParameterExpression)exp;

            if (parameter.Name == null)
                throw new Exception($"Parameter has no name {parameter.Type.GetNiceName()}");

            var modelDetail = context.MemberContext.ModelContexts[parameter.Name];

            var parameterInContext = modelDetail == context.MemberContext.ModelStack.Peek();

            ConvertToSqlParameterModel(modelDetail, ref sb, context, parameterInContext);
        }
        protected abstract void ConvertToSqlParameterModel(ModelDetail modelDetail, ref CharWriter sb, BuilderContext context, bool parameterInContext);
        protected abstract void ConvertToSqlConditional(Expression exp, ref CharWriter sb, BuilderContext context);
        protected void ConvertToSqlEvaluate(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            context.MemberContext.OperatorStack.Push(Operator.Evaluate);

            var value = Evaluate(exp);
            ConvertToSqlValue(exp.Type, value, ref sb, context);

            _ = context.MemberContext.OperatorStack.Pop();
        }

        protected void ConvertToSqlValue(Type type, object? value, ref CharWriter sb, BuilderContext context)
        {
            MemberExpression? memberProperty = null;

            if (context.MemberContext.MemberAccessStack.Count > 0)
                memberProperty = context.MemberContext.MemberAccessStack.Pop();

            var memberPropertyHandled = ConvertToSqlValueRender(memberProperty, type, value, ref sb, context);

            if (memberProperty != null)
            {
                if (!memberPropertyHandled)
                    throw new NotSupportedException($"{type.FullName}.{memberProperty.Member.Name} not supported");
                context.MemberContext.MemberAccessStack.Push(memberProperty);
            }
        }
        protected abstract bool ConvertToSqlValueRender(MemberExpression? memberProperty, Type type, object? value, ref CharWriter sb, BuilderContext context);

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

            if (call.Object != null)
                return IsEvaluatable(call.Object);

            return true;
        }
        private bool IsEvaluatableMemberAccess(Expression exp)
        {
            var member = (MemberExpression)exp;
            if (member.Expression == null)
            {
                return true;
            }
            return IsEvaluatable(member.Expression);
        }

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
            return constant.Value == null;
        }
        private bool IsNullCall(Expression exp)
        {
            var call = (MethodCallExpression)exp;
            if (call.Object == null)
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

            return value == null;
        }
        private bool IsNullMemberAccess(Expression exp)
        {
            var member = (MemberExpression)exp;

            object? value;
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
                        if (propertyInfo.GetMethod == null)
                            return true;
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

        private object? Evaluate(Expression exp)
        {
            return exp.NodeType switch
            {
                ExpressionType.Constant => EvaluateConstant(exp),
                ExpressionType.MemberAccess => EvaluateMemberAccess(exp),
                _ => EvaluateInvoke(exp),
            };
            ;
        }
        private object? EvaluateConstant(Expression exp)
        {
            var constant = (ConstantExpression)exp;
            return constant.Value;
        }
        private object? EvaluateMemberAccess(Expression exp)
        {
            var member = (MemberExpression)exp;
            var expressionValue = member.Expression == null ? null : Evaluate(member.Expression);

            object? value;
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
                    if (propertyInfo.GetMethod == null)
                        return null;
                    if (expressionValue == null && !propertyInfo.GetMethod.IsStatic)
                        return null;
                    value = propertyInfo.GetValue(expressionValue);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return value;
        }
        private object? EvaluateInvoke(Expression exp)
        {
            var value = Expression.Lambda(exp).Compile().DynamicInvoke();
            return value;
        }

        protected abstract void GenerateWhere(Expression? where, ref CharWriter sb, ParameterDependant rootDependant, MemberContext operationContext);
        protected abstract void GenerateOrderSkipTake(QueryOrder? order, int? skip, int? take, ref CharWriter sb, ParameterDependant rootDependant, MemberContext operationContext);
        protected abstract void GenerateSelect(QueryOperation select, Graph? graph, ModelDetail modelDetail, ref CharWriter sb);
        protected abstract void GenerateSelectProperties(Graph? graph, ModelDetail modelDetail, ref CharWriter sb);
        protected abstract void GenerateFrom(ModelDetail modelDetail, ref CharWriter sb);
        protected abstract void GenerateJoin(ParameterDependant dependant, ref CharWriter sb);
        protected abstract void GenerateEnding(QueryOperation select, Graph? graph, ModelDetail modelDetail, ref CharWriter sb);

        protected abstract void AppendLineBreak(ref CharWriter sb);

        protected abstract string? OperatorToString(Operator operation);
    }
}

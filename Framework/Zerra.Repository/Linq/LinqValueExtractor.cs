// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// Extracts values for specified model properties from a LINQ filter expression.
    /// </summary>
    internal static partial class LinqValueExtractor
    {
        /// <summary>
        /// Extracts constant values compared against the named properties within the given filter expression.
        /// </summary>
        /// <param name="where">The filter expression to analyse.</param>
        /// <param name="propertyModelType">The model type whose properties are being targeted.</param>
        /// <param name="propertyNames">The names of the properties whose compared values should be extracted.</param>
        /// <returns>
        /// A dictionary mapping each property name to the list of values compared against it in the expression.
        /// </returns>
        public static IDictionary<string, List<object?>> Extract(Expression where, Type propertyModelType, params string[] propertyNames)
        {
            var context = new Context(propertyModelType, propertyNames);

            _ = Extract(where, context);

            return context.Values;
        }

        private static Return? Extract(Expression exp, Context context)
        {
            //a value such as (a ? b : c) or a + 1 is a value however it's written
            if (exp.NodeType != ExpressionType.Constant && exp.NodeType != ExpressionType.Lambda && IsEvaluatable(exp))
                return ExtractEvaluate(exp, context);

            switch (exp.NodeType)
            {
                case ExpressionType.Add:
                    return ExtractBinary("+", exp, context);
                case ExpressionType.AddChecked:
                    return ExtractBinary("+", exp, context);
                case ExpressionType.And:
                    return ExtractBinary(context.Inverted ? "OR" : "AND", exp, context);
                case ExpressionType.AndAlso:
                    return ExtractBinary(context.Inverted ? "OR" : "AND", exp, context);
                case ExpressionType.Call:
                    return ExtractCall(exp, context);
                case ExpressionType.Conditional:
                    return ExtractConditional(exp, context);
                case ExpressionType.Constant:
                    return ExtractConstant(exp, context);
                case ExpressionType.Convert:
                    return ExtractUnary(exp, context);
                case ExpressionType.ConvertChecked:
                    return ExtractUnary(exp, context);
                case ExpressionType.Divide:
                    return ExtractBinary("/", exp, context);
                case ExpressionType.Equal:
                    return ExtractBinary(context.Inverted ? "!=" : "=", exp, context);
                case ExpressionType.GreaterThan:
                    return ExtractBinary(context.Inverted ? "<=" : ">", exp, context);
                case ExpressionType.GreaterThanOrEqual:
                    return ExtractBinary(context.Inverted ? "<" : ">=", exp, context);
                case ExpressionType.Lambda:
                    return ExtractLambda(exp, context);
                case ExpressionType.LessThan:
                    return ExtractBinary("<", exp, context);
                case ExpressionType.LessThanOrEqual:
                    return ExtractBinary("<=", exp, context);
                case ExpressionType.MemberAccess:
                    return ExtractMember(exp, context);
                case ExpressionType.Modulo:
                    return ExtractBinary("%", exp, context);
                case ExpressionType.Multiply:
                    return ExtractBinary("*", exp, context);
                case ExpressionType.MultiplyChecked:
                    return ExtractBinary("*", exp, context);
                case ExpressionType.Negate:
                    return ExtractUnary(exp, context);
                case ExpressionType.NegateChecked:
                    return ExtractUnary(exp, context);
                case ExpressionType.Not:
                    context.InvertStack++;
                    var ret = ExtractUnary(exp, context);
                    context.InvertStack--;
                    return ret;
                case ExpressionType.NotEqual:
                    return ExtractBinary(context.Inverted ? "=" : "!=", exp, context);
                case ExpressionType.Or:
                    return ExtractBinary(context.Inverted ? "AND" : "OR", exp, context);
                case ExpressionType.OrElse:
                    return ExtractBinary(context.Inverted ? "AND" : "OR", exp, context);
                case ExpressionType.Parameter:
                    return ExtractParameter(context);
                case ExpressionType.Subtract:
                    return ExtractBinary("-", exp, context);
                case ExpressionType.SubtractChecked:
                    return ExtractBinary("-", exp, context);
                default:
                    //anything else is part of the filter applied to the models once they are read
                    return null;
            }
        }
        private static Return? ExtractLambda(Expression exp, Context context)
        {
            var lambda = (LambdaExpression)exp;
            if (lambda.Parameters.Count != 1)
                throw new NotSupportedException("Can only parse a lambda with one parameter.");

            var modelType = lambda.Parameters[0].Type;
            var modelDetail = ModelAnalyzer.GetModel(modelType);
            context.ModelStack.Push(modelDetail);

            var ret = Extract(lambda.Body, context);

            _ = context.ModelStack.Pop();

            return ret;
        }
        private static Return? ExtractUnary(Expression exp, Context context)
        {
            var unary = (UnaryExpression)exp;

            var ret = Extract(unary.Operand, context);

            return ret;
        }
        private static Return? ExtractBinary(string operation, Expression exp, Context context)
        {
            var binary = (BinaryExpression)exp;

            var leftRet = Extract(binary.Left, context);

            var rightRet = Extract(binary.Right, context);

            if (operation == "=")
            {
                if (leftRet is not null && rightRet is not null)
                {
                    if (leftRet.Values is null && rightRet.Values is not null && leftRet.PropertyModelType == context.PropertyModelType && context.PropertyNames.Contains(leftRet.PropertyName))
                    {
                        context.Values[leftRet.PropertyName!].AddRange(rightRet.Values);
                    }
                    else if (rightRet.Values is null && leftRet.Values is not null && rightRet.PropertyModelType == context.PropertyModelType && context.PropertyNames.Contains(rightRet.PropertyName))
                    {
                        context.Values[rightRet.PropertyName!].AddRange(leftRet.Values);
                    }
                }
            }

            return null;
        }
        private static Return? ExtractMember(Expression exp, Context context)
        {
            Return? ret;

            var member = (MemberExpression)exp;

            //a value is evaluated before this, so the member is on the model
            context.MemberAccessStack.Push(member);
            ret = Extract(member.Expression!, context);
            _ = context.MemberAccessStack.Pop();

            return ret;
        }
        private static Return? ExtractConstant(Expression exp, Context context)
        {
            var constant = (ConstantExpression)exp;

            var ret = ExtractValue(constant.Type, constant.Value, context);

            return ret;
        }
        private static Return? ExtractCall(Expression exp, Context context)
        {
            Return? ret = null;

            var call = (MethodCallExpression)exp;
            if (call.Method.DeclaringType is not null)
            {
                if (call.Method.DeclaringType == typeof(Enumerable) || call.Method.DeclaringType == typeof(MemoryExtensions) || call.Method.DeclaringType == typeof(Queryable))
                {
                    switch (call.Method.Name)
                    {
                        case "Contains":
                            {
                                if (call.Arguments.Count != 2)
                                    break;

                                var callingObject = call.Arguments[0];
                                var lambda = call.Arguments[1];

                                var retLambda = Extract(lambda, context);

                                var retCallingObject = Extract(callingObject, context);

                                if (!context.Inverted)
                                {
                                    if (retLambda is not null && retCallingObject is not null && retCallingObject.Values is not null && retLambda.PropertyModelType == context.PropertyModelType && context.PropertyNames.Contains(retLambda.PropertyName))
                                    {
                                        context.Values[retLambda.PropertyName!].AddRange(retCallingObject.Values);
                                    }
                                }

                                break;
                            }
                    }
                }
                else if (call.Object is not null && call.Method.DeclaringType != typeof(string))
                {
                    var typeDetails = TypeAnalyzer.GetTypeDetail(call.Method.DeclaringType);
                    if (typeDetails.HasIEnumerableGeneric && typeDetails.IEnumerableGenericInnerTypeDetail!.CoreType.HasValue)
                    {
                        switch (call.Method.Name)
                        {
                            case "Contains":
                                {
                                    if (call.Arguments.Count != 1)
                                        break;

                                    var callingObject = call.Object;
                                    var lambda = call.Arguments[0];

                                    var retLambda = Extract(lambda, context);

                                    var retCallingObject = Extract(callingObject, context);

                                    if (!context.Inverted)
                                    {
                                        if (retLambda is not null && retCallingObject is not null && retCallingObject.Values is not null && retLambda.PropertyModelType == context.PropertyModelType && context.PropertyNames.Contains(retLambda.PropertyName))
                                        {
                                            context.Values[retLambda.PropertyName!].AddRange(retCallingObject.Values);
                                        }
                                    }

                                    break;
                                }
                        }
                    }
                }
            }

            return ret;
        }
        private static Return? ExtractParameter(Context context)
        {
            Return? ret;

            var member = context.MemberAccessStack.Pop();

            var modelDetail = context.ModelStack.Peek();
            var modelProperty = modelDetail.GetMember(member.Member.Name);
            if (modelProperty.ForeignIdentity is not null)
            {
                var subModelInfo = ModelAnalyzer.GetModel(modelProperty.ActualType);
                context.ModelStack.Push(subModelInfo);
                ret = ExtractParameter(context);
                _ = context.ModelStack.Pop();
            }
            else if (member.Expression is not null)
            {
                //a member of the property such as Name.Length isn't the property's value, only a nullable's Value is
                if (context.MemberAccessStack.Count > 0 && (member.Type.Name != typeof(Nullable<>).Name || context.MemberAccessStack.Peek().Member.Name != "Value"))
                    ret = null;
                else
                    ret = new Return(member.Expression.Type, member.Member.Name);
            }
            else
            {
                ret = null;
            }

            context.MemberAccessStack.Push(member);

            return ret;
        }
        private static Return? ExtractConditional(Expression exp, Context context)
        {
            var conditional = (ConditionalExpression)exp;

            _ = Extract(conditional.Test, context);

            _ = Extract(conditional.IfTrue, context);

            _ = Extract(conditional.IfFalse, context);

            return null;
        }
        private static Return? ExtractEvaluate(Expression exp, Context context)
        {
            var value = Evaluate(exp);
            var ret = ExtractValue(exp.Type, value, context);

            return ret;
        }

        private static Return ExtractValue(Type type, object? value, Context context)
        {
            Return ret;

            if (value is null)
            {
                ret = new Return(value);
            }
            else if (type.IsArray)
            {
                var array = (Array)value;
                var values = new object?[array.Length];
                for (var i = 0; i < values.Length; i++)
                    values[i] = array.GetValue(i);
                ret = new Return(values);
            }
            else if (value is ICollection collection)
            {
                var values = new object?[collection.Count];
                var i = 0;
                foreach (var item in collection)
                    values[i++] = item;
                ret = new Return(values);
            }
            else if (value is IEnumerable enumerable)
            {
                var values = new List<object>();
                foreach (var item in enumerable)
                    values.Add(item);
                ret = new Return(values.ToArray());
            }
            else
            {
                ret = new Return(value);
            }

            return ret;
        }

        private static bool IsEvaluatable(Expression exp)
        {
            switch (exp.NodeType)
            {
                case ExpressionType.Constant:
                case ExpressionType.Default:
                    return true;
                case ExpressionType.Call:
                    {
                        var call = (MethodCallExpression)exp;
                        return (call.Object is null || IsEvaluatable(call.Object)) && IsEvaluatable(call.Arguments);
                    }
                case ExpressionType.MemberAccess:
                    {
                        var member = (MemberExpression)exp;
                        return member.Expression is null || IsEvaluatable(member.Expression);
                    }
                case ExpressionType.Conditional:
                    {
                        var conditional = (ConditionalExpression)exp;
                        return IsEvaluatable(conditional.Test) && IsEvaluatable(conditional.IfTrue) && IsEvaluatable(conditional.IfFalse);
                    }
                case ExpressionType.New:
                    return IsEvaluatable(((NewExpression)exp).Arguments);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return IsEvaluatable(((NewArrayExpression)exp).Expressions);
                case ExpressionType.ListInit:
                    {
                        var listInit = (ListInitExpression)exp;
                        if (!IsEvaluatable(listInit.NewExpression))
                            return false;
                        foreach (var initializer in listInit.Initializers)
                        {
                            if (!IsEvaluatable(initializer.Arguments))
                                return false;
                        }
                        return true;
                    }
                case ExpressionType.Index:
                    {
                        var index = (IndexExpression)exp;
                        return (index.Object is null || IsEvaluatable(index.Object)) && IsEvaluatable(index.Arguments);
                    }
                case ExpressionType.Invoke:
                    {
                        var invocation = (InvocationExpression)exp;
                        return IsEvaluatable(invocation.Expression) && IsEvaluatable(invocation.Arguments);
                    }
                case ExpressionType.TypeIs:
                case ExpressionType.TypeEqual:
                    return IsEvaluatable(((TypeBinaryExpression)exp).Expression);
                case ExpressionType.Parameter:
                case ExpressionType.Lambda:
                case ExpressionType.Quote:
                    return false;
            }

            if (exp is BinaryExpression binary)
                return exp.NodeType != ExpressionType.Assign && IsEvaluatable(binary.Left) && IsEvaluatable(binary.Right);
            if (exp is UnaryExpression unary && exp.NodeType != ExpressionType.Throw)
                return IsEvaluatable(unary.Operand);

            //blocks, loops, and the like aren't values
            return false;
        }
        private static bool IsEvaluatable(IReadOnlyList<Expression> expressions)
        {
            foreach (var expression in expressions)
            {
                if (!IsEvaluatable(expression))
                    return false;
            }
            return true;
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
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
                var value = Expression.Lambda(exp).Compile().DynamicInvoke();
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
                return value;
            }
            catch (Exception ex)
            {
                if (!RuntimeFeature.IsDynamicCodeSupported)
                    throw new InvalidOperationException($"Dynamic code execution is not supported in this runtime environment.", ex);
                throw;
            }
        }
    }
}
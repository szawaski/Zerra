﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    internal static partial class LinqValueExtractor
    {
        public static IDictionary<string, List<object?>> Extract(Expression where, Type propertyModelType, params string[] propertyNames)
        {
            var context = new Context(propertyModelType, propertyNames);

            _ = Extract(where, context);

            return context.Values;
        }

        private static Return? Extract(Expression exp, Context context)
        {
            switch (exp.NodeType)
            {
                case ExpressionType.Add:
                    return ExtractBinary("+", exp, context);
                case ExpressionType.AddAssign:
                    throw new NotImplementedException();
                case ExpressionType.AddAssignChecked:
                    throw new NotImplementedException();
                case ExpressionType.AddChecked:
                    return ExtractBinary("+", exp, context);
                case ExpressionType.And:
                    return ExtractBinary(context.Inverted ? "OR" : "AND", exp, context);
                case ExpressionType.AndAlso:
                    return ExtractBinary(context.Inverted ? "OR" : "AND", exp, context);
                case ExpressionType.AndAssign:
                    throw new NotImplementedException();
                case ExpressionType.ArrayLength:
                    throw new NotImplementedException();
                case ExpressionType.Assign:
                    throw new NotImplementedException();
                case ExpressionType.Block:
                    throw new NotImplementedException();
                case ExpressionType.Call:
                    return ExtractCall(exp, context);
                case ExpressionType.Coalesce:
                    throw new NotImplementedException();
                case ExpressionType.Conditional:
                    return ExtractConditional(exp, context);
                case ExpressionType.Constant:
                    return ExtractConstant(exp, context);
                case ExpressionType.Convert:
                    return ExtractUnary(exp, context);
                case ExpressionType.ConvertChecked:
                    return ExtractUnary(exp, context);
                case ExpressionType.DebugInfo:
                    throw new NotImplementedException();
                case ExpressionType.Decrement:
                    throw new NotImplementedException();
                case ExpressionType.Default:
                    throw new NotImplementedException();
                case ExpressionType.Divide:
                    return ExtractBinary("/", exp, context);
                case ExpressionType.DivideAssign:
                    throw new NotImplementedException();
                case ExpressionType.Dynamic:
                    throw new NotImplementedException();
                case ExpressionType.Equal:
                    return ExtractBinary(context.Inverted ? "!=" : "=", exp, context);
                case ExpressionType.ExclusiveOr:
                    throw new NotImplementedException();
                case ExpressionType.ExclusiveOrAssign:
                    throw new NotImplementedException();
                case ExpressionType.Extension:
                    throw new NotImplementedException();
                case ExpressionType.Goto:
                    throw new NotImplementedException();
                case ExpressionType.GreaterThan:
                    return ExtractBinary(context.Inverted ? "<=" : ">", exp, context);
                case ExpressionType.GreaterThanOrEqual:
                    return ExtractBinary(context.Inverted ? "<" : ">=", exp, context);
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
                    return ExtractLambda(exp, context);
                case ExpressionType.LeftShift:
                    throw new NotImplementedException();
                case ExpressionType.LeftShiftAssign:
                    throw new NotImplementedException();
                case ExpressionType.LessThan:
                    return ExtractBinary("<", exp, context);
                case ExpressionType.LessThanOrEqual:
                    return ExtractBinary("<=", exp, context);
                case ExpressionType.ListInit:
                    throw new NotImplementedException();
                case ExpressionType.Loop:
                    throw new NotImplementedException();
                case ExpressionType.MemberAccess:
                    return ExtractMember(exp, context);
                case ExpressionType.MemberInit:
                    throw new NotImplementedException();
                case ExpressionType.Modulo:
                    return ExtractBinary("%", exp, context);
                case ExpressionType.ModuloAssign:
                    throw new NotImplementedException();
                case ExpressionType.Multiply:
                    return ExtractBinary("*", exp, context);
                case ExpressionType.MultiplyAssign:
                    throw new NotImplementedException();
                case ExpressionType.MultiplyAssignChecked:
                    throw new NotImplementedException();
                case ExpressionType.MultiplyChecked:
                    return ExtractBinary("*", exp, context);
                case ExpressionType.Negate:
                    return ExtractUnary(exp, context);
                case ExpressionType.NegateChecked:
                    return ExtractUnary(exp, context);
                case ExpressionType.New:
                    return ExtractNew(exp, context);
                case ExpressionType.NewArrayBounds:
                    throw new NotImplementedException();
                case ExpressionType.NewArrayInit:
                    throw new NotImplementedException();
                case ExpressionType.Not:
                    context.InvertStack++;
                    var ret = ExtractUnary(exp, context);
                    context.InvertStack--;
                    return ret;
                case ExpressionType.NotEqual:
                    return ExtractBinary(context.Inverted ? "=" : "!=", exp, context);
                case ExpressionType.OnesComplement:
                    throw new NotImplementedException();
                case ExpressionType.Or:
                    return ExtractBinary(context.Inverted ? "AND" : "OR", exp, context);
                case ExpressionType.OrAssign:
                    throw new NotImplementedException();
                case ExpressionType.OrElse:
                    return ExtractBinary(context.Inverted ? "AND" : "OR", exp, context);
                case ExpressionType.Parameter:
                    return ExtractParameter(context);
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
                    return ExtractBinary("-", exp, context);
                case ExpressionType.SubtractAssign:
                    throw new NotImplementedException();
                case ExpressionType.SubtractAssignChecked:
                    throw new NotImplementedException();
                case ExpressionType.SubtractChecked:
                    return ExtractBinary("-", exp, context);
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
                if (leftRet != null && rightRet != null)
                {
                    if (leftRet.Values == null && rightRet.Values != null && leftRet.PropertyModelType == context.PropertyModelType && context.PropertyNames.Contains(leftRet.PropertyName))
                    {
                        context.Values[leftRet.PropertyName!].AddRange(rightRet.Values);
                    }
                    else if (rightRet.Values == null && leftRet.Values != null && rightRet.PropertyModelType == context.PropertyModelType && context.PropertyNames.Contains(rightRet.PropertyName))
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

            if (member.Expression == null)
            {
                ret = ExtractEvaluate(member, context);
            }
            else
            {
                context.MemberAccessStack.Push(member);
                ret = Extract(member.Expression, context);
                _ = context.MemberAccessStack.Pop();
            }

            return ret;
        }
        private static Return? ExtractConstant(Expression exp, Context context)
        {
            var constant = (ConstantExpression)exp;

            var ret = ExtractConstantStack(constant.Type, constant.Value, context);

            return ret;
        }
        private static Return? ExtractConstantStack(Type type, object? value, Context context)
        {
            Return? ret;

            if (context.MemberAccessStack.Count > 0)
            {
                var memberProperty = context.MemberAccessStack.Pop();
                if (value == null)
                {
                    ret = ExtractValue(type, value, context);
                }
                else
                {
                    switch (memberProperty.Member.MemberType)
                    {
                        case MemberTypes.Field:
                            {
                                var field = (FieldInfo)memberProperty.Member;
                                var fieldValue = field.GetValue(value);
                                ret = ExtractConstantStack(field.FieldType, fieldValue, context);
                                break;
                            }
                        case MemberTypes.Property:
                            {
                                var property = (PropertyInfo)memberProperty.Member;
                                if (property.GetMethod != null)
                                {
                                    var propertyValue = property.GetValue(value);
                                    ret = ExtractConstantStack(property.PropertyType, propertyValue, context);
                                }
                                else
                                {
                                    ret = null;
                                }
                                break;
                            }
                        default:
                            throw new NotImplementedException();
                    }
                }

                context.MemberAccessStack.Push(memberProperty);
            }
            else
            {
                ret = ExtractValue(type, value, context);
            }

            return ret;
        }
        private static Return? ExtractCall(Expression exp, Context context)
        {
            Return? ret = null;

            var call = (MethodCallExpression)exp;
            var isEvaluatable = IsEvaluatable(exp);
            if (isEvaluatable)
            {
                ret = ExtractEvaluate(exp, context);
            }
            else if (call.Method.DeclaringType != null)
            {
                if (call.Method.DeclaringType == typeof(Enumerable) || call.Method.DeclaringType == typeof(Queryable))
                {
                    switch (call.Method.Name)
                    {
                        case "All":
                            {
                                break;
                            }
                        case "Any":
                            {
                                break;
                            }
                        case "Count":
                            {
                                break;
                            }
                        case "Contains":
                            {
                                if (call.Arguments.Count != 2)
                                    throw new NotSupportedException(String.Format("Cannot extract from call expression {0}", call.Method.Name));

                                var callingObject = call.Arguments[0];
                                var lambda = call.Arguments[1];

                                var retLambda = Extract(lambda, context);

                                var retCallingObject = Extract(callingObject, context);

                                if (!context.Inverted)
                                {
                                    if (retLambda != null && retCallingObject != null && retCallingObject.Values != null && retLambda.PropertyModelType == context.PropertyModelType && context.PropertyNames.Contains(retLambda.PropertyName))
                                    {
                                        context.Values[retLambda.PropertyName!].AddRange(retCallingObject.Values);
                                    }
                                }

                                break;
                            }
                        default:
                            throw new NotSupportedException(String.Format("Cannot extract from call expression {0}", call.Method.Name));
                    }
                }
                else if (call.Method.DeclaringType == typeof(string))
                {
                    switch (call.Method.Name)
                    {
                        case "Contains":
                            {
                                break;
                            }
                        default:
                            throw new NotSupportedException(String.Format("Cannot extract from call expression {0}", call.Method.Name));
                    }
                }
                else if (call.Object != null)
                {
                    var typeDetails = TypeAnalyzer.GetTypeDetail(call.Method.DeclaringType);
                    if (typeDetails.IsIEnumerableGeneric && TypeLookup.CoreTypesWithNullables.Contains(typeDetails.IEnumerableGenericInnerType))
                    {
                        switch (call.Method.Name)
                        {
                            case "Contains":
                                {
                                    if (call.Arguments.Count != 1)
                                        throw new NotSupportedException(String.Format("Cannot extract from call expression {0}", call.Method.Name));

                                    var callingObject = call.Object;
                                    var lambda = call.Arguments[0];

                                    var retLambda = Extract(lambda, context);

                                    var retCallingObject = Extract(callingObject, context);

                                    if (!context.Inverted)
                                    {
                                        if (retLambda != null && retCallingObject != null && retCallingObject.Values != null && retLambda.PropertyModelType == context.PropertyModelType && context.PropertyNames.Contains(retLambda.PropertyName))
                                        {
                                            context.Values[retLambda.PropertyName!].AddRange(retCallingObject.Values);
                                        }
                                    }

                                    break;
                                }
                            default:
                                throw new NotSupportedException(String.Format("Cannot extract from call expression {0}", call.Method.Name));
                        }
                    }
                    else
                    {
                        throw new NotSupportedException(String.Format("Cannot extract from call expression {0}", call.Method.Name));
                    }
                }
            }

            return ret;
        }
        private static Return? ExtractNew(Expression exp, Context context)
        {
            var newExp = (NewExpression)exp;

            var argumentTypes = newExp.Arguments.Select(x => x.Type).ToArray();
            var constructor = newExp.Type.GetConstructor(argumentTypes)!;

            var parameters = new List<object?>();
            foreach (var argument in newExp.Arguments)
            {
                var argumentValue = Expression.Lambda(argument).Compile().DynamicInvoke();
                parameters.Add(argumentValue);
            }

            var value = constructor.Invoke(parameters.ToArray());
            var ret = ExtractValue(newExp.Type, value, context);

            return ret;
        }
        private static Return? ExtractParameter(Context context)
        {
            Return? ret;

            var member = context.MemberAccessStack.Pop();

            var modelDetail = context.ModelStack.Peek();
            var modelProperty = modelDetail.GetProperty(member.Member.Name);
            if (modelProperty.ForeignIdentity != null)
            {
                var subModelInfo = ModelAnalyzer.GetModel(modelProperty.InnerType);
                context.ModelStack.Push(subModelInfo);
                ret = ExtractParameter(context);
                _ = context.ModelStack.Pop();
            }
            else if (member.Expression != null)
            {
                if (context.MemberAccessStack.Count > 0)
                {
                    var memberPropertyHandled = false;
                    var memberProperty = context.MemberAccessStack.Pop();

                    if (member.Type.Name == typeof(Nullable<>).Name && memberProperty.Member.Name == "Value")
                    {
                        memberPropertyHandled = true;
                    }
                    else if (member.Type == typeof(DateTime))
                    {
                        memberPropertyHandled = true;
                    }

                    if (!memberPropertyHandled)
                        throw new NotSupportedException(String.Format("{0}.{1} not supported", member.Member.Name, memberProperty.Member.Name));
                    context.MemberAccessStack.Push(memberProperty);
                }

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
            var value = Expression.Lambda(exp).Compile().DynamicInvoke();
            var ret = ExtractValue(exp.Type, value, context);

            return ret;
        }

        private static Return ExtractValue(Type type, object? value, Context context)
        {
            Return ret;

            if (context.MemberAccessStack.Count > 0)
                _ = context.MemberAccessStack.Pop();

            if (value == null)
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
            return exp.NodeType switch
            {
                ExpressionType.Add => IsEvaluatableBinary(exp),
                ExpressionType.AddAssign => IsEvaluatableBinary(exp),
                ExpressionType.AddAssignChecked => IsEvaluatableBinary(exp),
                ExpressionType.AddChecked => IsEvaluatableBinary(exp),
                ExpressionType.And => IsEvaluatableBinary(exp),
                ExpressionType.AndAlso => IsEvaluatableBinary(exp),
                ExpressionType.AndAssign => IsEvaluatableBinary(exp),
                ExpressionType.ArrayLength => IsEvaluatableUnary(exp),
                ExpressionType.Assign => IsEvaluatableBinary(exp),
                ExpressionType.Block => IsEvaluatableBlock(exp),
                ExpressionType.Call => IsEvaluatableCall(exp),
                ExpressionType.Coalesce => throw new NotImplementedException(),
                ExpressionType.Conditional => throw new NotImplementedException(),
                ExpressionType.Constant => true,
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
            var unary = (UnaryExpression)exp;
            return IsEvaluatable(unary.Operand);
        }
        private static bool IsEvaluatableBinary(Expression exp)
        {
            var binary = (BinaryExpression)exp;
            return IsEvaluatable(binary.Left) && IsEvaluatable(binary.Right);
        }
        private static bool IsEvaluatableBlock(Expression exp)
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
        private static bool IsEvaluatableCall(Expression exp)
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
        private static bool IsEvaluatableMemberAccess(Expression exp)
        {
            var member = (MemberExpression)exp;
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
        }
        private static bool IsNullUnary(Expression exp)
        {
            var unary = (UnaryExpression)exp;
            return IsNull(unary.Operand);
        }
        private static bool IsNullConstant(Expression exp)
        {
            var constant = (ConstantExpression)exp;
            return constant.Value == null;
        }
        private static bool IsNullCall(Expression exp)
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
        private static bool IsNullMemberAccess(Expression exp)
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

        private static object? Evaluate(Expression exp)
        {
            return exp.NodeType switch
            {
                ExpressionType.Constant => EvaluateConstant(exp),
                ExpressionType.MemberAccess => EvaluateMemberAccess(exp),
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
        private static object? EvaluateInvoke(Expression exp)
        {
            var value = Expression.Lambda(exp).Compile().DynamicInvoke();
            return value;
        }
    }
}
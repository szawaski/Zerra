// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Zerra.Reflection;

namespace Zerra.Linq
{
    public partial class LinqRebinder
    {
        //public static Expression RebindType(Expression exp, Type current, Type replacement, bool evaluateCalls = false)
        //{
        //    var typeReplacements = new Dictionary<Type, Type>();
        //    typeReplacements.Add(current, replacement);

        //    var context = new RebinderContext(typeReplacements, null, evaluateCalls);
        //    var result = Rebind(exp, context);
        //    return result;
        //}

        public static Expression RebindExpression(Expression exp, Expression current, Expression replacement)
        {
            var expressionReplacements = new Dictionary<Expression, Expression>
            {
                { current, replacement }
            };

            var context = new RebinderContext(expressionReplacements, null);
            var result = Rebind(exp, context);
            return result;
        }

        public static Expression Rebind(Expression exp, IDictionary<Expression, Expression> expressionReplacements)
        {
            var context = new RebinderContext(expressionReplacements, null);
            var result = Rebind(exp, context);
            return result;
        }

        public static Expression Rebind(Expression exp, IDictionary<string, Expression> expressionStringReplacements)
        {
            var context = new RebinderContext(null, expressionStringReplacements);
            var result = Rebind(exp, context);
            return result;
        }

        private static Expression Rebind(Expression exp, RebinderContext context)
        {
            if (exp == null)
                return null;

            if (context.ExpressionReplacements != null && context.ExpressionReplacements.TryGetValue(exp, out Expression newExpression))
            {
                return newExpression;
            }
            if (context.ExpressionStringReplacements != null && context.ExpressionStringReplacements.TryGetValue(exp.ToString(), out newExpression))
            {
                return newExpression;
            }

            switch (exp.NodeType)
            {
                case ExpressionType.Add:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.Add(Rebind(cast.Left, context), Rebind(cast.Right, context), cast.Method);
                    }
                case ExpressionType.AddAssign:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.Add(Rebind(cast.Left, context), Rebind(cast.Right, context), cast.Method);
                    }
                case ExpressionType.AddAssignChecked:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.AddAssignChecked(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.AddChecked:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.AddChecked(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.And:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.And(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.AndAlso:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.AndAlso(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.AndAssign:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.AndAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.ArrayIndex:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.ArrayIndex(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.ArrayLength:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.ArrayLength(Rebind(cast.Operand, context));
                    }
                case ExpressionType.Assign:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.Assign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.Block:
                    {
                        var cast = exp as BlockExpression;

                        var replacementExpressions = new Expression[cast.Expressions.Count];
                        for (var i = 0; i < cast.Expressions.Count; i++)
                        {
                            var replacementExpression = Rebind(cast.Expressions[i], context);
                            replacementExpressions[i] = replacementExpression;
                        }

                        var replacementParameters = new List<ParameterExpression>();
                        for (var i = 0; i < cast.Variables.Count; i++)
                        {
                            Expression replacementExpression = Rebind(cast.Variables[i], context);
                            var replacementParameter = ExtractParametersExpression(replacementExpression);
                            replacementParameters.AddRange(replacementParameter);
                        }

                        //var rebindedVariables = new ParameterExpression[cast.Expressions.Count];
                        //i = 0;
                        //foreach (var variable in cast.Variables)
                        //{
                        //    if (context.TypeReplacements != null && context.TypeReplacements.TryGetValue(variable.Type, out Type replacementType))
                        //    {
                        //        if (!context.Parameters.TryGetValue(replacementType, out ParameterExpression replacementParameter))
                        //        {
                        //            replacementParameter = Expression.Parameter(replacementType, variable.Name);
                        //            context.Parameters.Add(replacementType, replacementParameter);
                        //        }

                        //        rebindedVariables[i] = replacementParameter;
                        //    }
                        //    else
                        //    {
                        //        rebindedVariables[i] = variable;
                        //    }
                        //    i++;
                        //}

                        //return Expression.Block(rebindedVariables, rebindedExpressions);

                        return Expression.Block(replacementParameters, replacementExpressions);
                    }
                case ExpressionType.Call:
                    {
                        var cast = exp as MethodCallExpression;

                        Expression obj = Rebind(cast.Object, context);

                        var replacementExpressions = new Expression[cast.Arguments.Count];
                        for (var i = 0; i < cast.Arguments.Count; i++)
                        {
                            var replacementExpression = Rebind(cast.Arguments[i], context);
                            replacementExpressions[i] = replacementExpression;
                        }

                        //var method = cast.Method;
                        //if (context.TypeReplacements != null)
                        //{
                        //    if (method.IsGenericMethod)
                        //    {
                        //        var genericArgumentTypes = cast.Method.GetGenericArguments();
                        //        for (var j = 0; j < genericArgumentTypes.Length; j++)
                        //        {
                        //            if (context.TypeReplacements.TryGetValue(genericArgumentTypes[j], out Type replacementType))
                        //                genericArgumentTypes[j] = replacementType;
                        //        }

                        //        var methodGeneric = TypeAnalyzer.GetGenericMethod(method.GetGenericMethodDefinition(), genericArgumentTypes);
                        //        method = methodGeneric.MethodInfo;
                        //    }
                        //}

                        //return Expression.Call(obj, method, replacementArgs);

                        return Expression.Call(obj, cast.Method, replacementExpressions);
                    }
                case ExpressionType.Coalesce:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.Coalesce(Rebind(cast.Left, context), Rebind(cast.Right, context), cast.Conversion);
                    }
                case ExpressionType.Conditional:
                    {
                        var cast = exp as ConditionalExpression;
                        return Expression.Condition(Rebind(cast.Test, context), Rebind(cast.IfTrue, context), Rebind(cast.IfFalse, context), cast.Type);
                    }
                case ExpressionType.Constant:
                    {
                        return exp;
                    }
                case ExpressionType.Convert:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.Convert(Rebind(cast.Operand, context), cast.Type);
                    }
                case ExpressionType.ConvertChecked:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.ConvertChecked(Rebind(cast.Operand, context), cast.Type);
                    }
                case ExpressionType.DebugInfo:
                    {
                        return exp;
                    }
                case ExpressionType.Decrement:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.Decrement(Rebind(cast.Operand, context));
                    }
                case ExpressionType.Default:
                    {
                        return exp;
                    }
                case ExpressionType.Divide:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.Divide(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.DivideAssign:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.DivideAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.Dynamic:
                    {
                        var cast = exp as DynamicExpression;

                        var replacementExpressions = new Expression[cast.Arguments.Count];
                        for (var i = 0; i < cast.Arguments.Count; i++)
                        {
                            var replacementExpression = Rebind(cast.Arguments[i], context);
                            replacementExpressions[i] = replacementExpression;
                        }

                        return Expression.MakeDynamic(cast.DelegateType, cast.Binder, replacementExpressions);
                    }
                case ExpressionType.Equal:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.Equal(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.ExclusiveOr:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.ExclusiveOr(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.ExclusiveOrAssign:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.ExclusiveOrAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.Extension:
                    {
                        return exp;
                    }
                case ExpressionType.Goto:
                    {
                        var cast = exp as GotoExpression;
                        return Expression.Goto(cast.Target, Rebind(cast.Value, context), cast.Type);
                    }
                case ExpressionType.GreaterThan:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.GreaterThan(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.GreaterThanOrEqual:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.GreaterThanOrEqual(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.Increment:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.Increment(Rebind(cast.Operand, context));
                    }
                case ExpressionType.Index:
                    {
                        var cast = exp as IndexExpression;

                        var replacementExpressions = new Expression[cast.Arguments.Count];
                        for (var i = 0; i < cast.Arguments.Count; i++)
                        {
                            var replacementExpression = Rebind(cast.Arguments[i], context);
                            replacementExpressions[i] = replacementExpression;
                        }

                        return Expression.MakeIndex(Rebind(cast.Object, context), cast.Indexer, replacementExpressions);
                    }
                case ExpressionType.Invoke:
                    {
                        var cast = exp as InvocationExpression;

                        var replacementExpressions = new Expression[cast.Arguments.Count];
                        for (var i = 0; i < cast.Arguments.Count; i++)
                        {
                            var replacementExpression = Rebind(cast.Arguments[i], context);
                            replacementExpressions[i] = replacementExpression;
                        }

                        return Expression.Invoke(Rebind(cast.Expression, context), replacementExpressions);
                    }
                case ExpressionType.IsFalse:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.IsFalse(Rebind(cast.Operand, context));
                    }
                case ExpressionType.IsTrue:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.IsTrue(Rebind(cast.Operand, context));
                    }
                case ExpressionType.Label:
                    {
                        var cast = exp as LabelExpression;
                        return Expression.Label(cast.Target, Rebind(cast.DefaultValue, context));
                    }
                case ExpressionType.Lambda:
                    {
                        var cast = exp as LambdaExpression;

                        var replacementParameters = new List<ParameterExpression>();
                        for (var i = 0; i < cast.Parameters.Count; i++)
                        {
                            Expression replacementExpression = Rebind(cast.Parameters[i], context);
                            var replacementParameter = ExtractParametersExpression(replacementExpression);
                            replacementParameters.AddRange(replacementParameter);
                        }

                        Expression body = Rebind(cast.Body, context);

                        return Expression.Lambda(body, replacementParameters);
                    }
                case ExpressionType.LeftShift:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.LeftShift(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.LeftShiftAssign:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.LeftShiftAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.LessThan:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.LessThan(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.LessThanOrEqual:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.LessThanOrEqual(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.ListInit:
                    {
                        var cast = exp as ListInitExpression;

                        var replacementInits = new ElementInit[cast.Initializers.Count];
                        for (var i = 0; i < cast.Initializers.Count; i++)
                        {
                            var replacementExpressions2 = new Expression[cast.Initializers[i].Arguments.Count];
                            for (var j = 0; j < cast.Initializers[i].Arguments.Count; j++)
                            {
                                var replacementExpression2 = Rebind(cast.Initializers[i].Arguments[j], context);
                                replacementExpressions2[j] = replacementExpression2;
                            }
                            var replacementInit = Expression.ElementInit(cast.Initializers[i].AddMethod, replacementExpressions2);
                            replacementInits[i] = replacementInit;
                        }

                        return Expression.ListInit((NewExpression)Rebind(cast.NewExpression, context), replacementInits);
                    }
                case ExpressionType.Loop:
                    {
                        var cast = exp as LoopExpression;
                        return Expression.Loop(Rebind(cast.Body, context), cast.BreakLabel, cast.ContinueLabel);
                    }
                case ExpressionType.MemberAccess:
                    {
                        var cast = exp as MemberExpression;
                        return Expression.MakeMemberAccess(Rebind(cast.Expression, context), cast.Member);
                        //if (cast.Expression != null)
                        //{
                        //    Expression newMemberExpression = Rebind(cast.Expression, context);
                        //    MemberInfo member = newMemberExpression.Type.GetMember(cast.Member.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).First();
                        //    return Expression.MakeMemberAccess(newMemberExpression, member);
                        //}
                        //else
                        //{
                        //    return exp;
                        //}
                    }
                case ExpressionType.MemberInit:
                    {
                        var cast = exp as MemberInitExpression;
                        return Expression.MemberInit((NewExpression)Rebind(cast.NewExpression, context), cast.Bindings);
                    }
                case ExpressionType.Modulo:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.ModuloAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.ModuloAssign:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.ModuloAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.Multiply:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.Multiply(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.MultiplyAssign:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.MultiplyAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.MultiplyAssignChecked:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.MultiplyAssignChecked(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.MultiplyChecked:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.MultiplyChecked(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.Negate:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.Negate(Rebind(cast.Operand, context));
                    }
                case ExpressionType.NegateChecked:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.NegateChecked(Rebind(cast.Operand, context));
                    }
                case ExpressionType.New:
                    {
                        var cast = exp as NewExpression;

                        var replacementExpressions = new Expression[cast.Arguments.Count];
                        for (var i = 0; i < cast.Arguments.Count; i++)
                        {
                            var replacementExpression = Rebind(cast.Arguments[i], context);
                            replacementExpressions[i] = replacementExpression;
                        }

                        return Expression.New(cast.Constructor, replacementExpressions, cast.Members);
                    }
                case ExpressionType.NewArrayBounds:
                    {
                        var cast = exp as NewArrayExpression;

                        var replacementExpressions = new Expression[cast.Expressions.Count];
                        for (var i = 0; i < cast.Expressions.Count; i++)
                        {
                            var replacementExpression = Rebind(cast.Expressions[i], context);
                            replacementExpressions[i] = replacementExpression;
                        }

                        return Expression.NewArrayBounds(cast.Type, replacementExpressions);
                    }
                case ExpressionType.NewArrayInit:
                    {
                        var cast = exp as NewArrayExpression;

                        var replacementExpressions = new Expression[cast.Expressions.Count];
                        for (var i = 0; i < cast.Expressions.Count; i++)
                        {
                            var replacementExpression = Rebind(cast.Expressions[i], context);
                            replacementExpressions[i] = replacementExpression;
                        }

                        return Expression.NewArrayInit(cast.Type, replacementExpressions);
                    }
                case ExpressionType.Not:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.Not(Rebind(cast.Operand, context));
                    }
                case ExpressionType.NotEqual:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.NotEqual(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.OnesComplement:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.OnesComplement(Rebind(cast.Operand, context));
                    }
                case ExpressionType.Or:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.Or(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.OrAssign:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.OrAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.OrElse:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.OrElse(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.Parameter:
                    {
                        return exp;
                        //var cast = exp as ParameterExpression;

                        //if (context.TypeReplacements != null && context.TypeReplacements.TryGetValue(cast.Type, out Type replacementType))
                        //{
                        //    if (!context.Parameters.TryGetValue(replacementType, out ParameterExpression replacementParameter))
                        //    {
                        //        replacementParameter = Expression.Parameter(replacementType, cast.Name);
                        //        context.Parameters.Add(replacementType, replacementParameter);
                        //    }

                        //    return replacementParameter;
                        //}
                        //else
                        //{
                        //    return cast;
                        //}
                    }
                case ExpressionType.PostDecrementAssign:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.PostDecrementAssign(Rebind(cast.Operand, context));
                    }
                case ExpressionType.PostIncrementAssign:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.PostDecrementAssign(Rebind(cast.Operand, context));
                    }
                case ExpressionType.Power:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.Power(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.PowerAssign:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.PowerAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.PreDecrementAssign:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.PostDecrementAssign(Rebind(cast.Operand, context));
                    }
                case ExpressionType.PreIncrementAssign:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.PreIncrementAssign(Rebind(cast.Operand, context));
                    }
                case ExpressionType.Quote:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.Quote(Rebind(cast.Operand, context));
                    }
                case ExpressionType.RightShift:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.RightShift(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.RightShiftAssign:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.RightShiftAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.RuntimeVariables:
                    {
                        var cast = exp as RuntimeVariablesExpression;

                        var replacementParameters = new List<ParameterExpression>();
                        for (var i = 0; i < cast.Variables.Count; i++)
                        {
                            Expression replacementExpression = Rebind(cast.Variables[i], context);
                            var replacementParameter = ExtractParametersExpression(replacementExpression);
                            replacementParameters.AddRange(replacementParameter);
                        }

                        return Expression.RuntimeVariables(replacementParameters);
                    }
                case ExpressionType.Subtract:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.Subtract(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.SubtractAssign:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.SubtractAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.SubtractAssignChecked:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.SubtractAssignChecked(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.SubtractChecked:
                    {
                        var cast = exp as BinaryExpression;
                        return Expression.SubtractChecked(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.Switch:
                    {
                        var cast = exp as SwitchExpression;

                        var replacementCases = new SwitchCase[cast.Cases.Count];
                        for (var i = 0; i < cast.Cases.Count; i++)
                        {
                            var replacementExpressions2 = new Expression[cast.Cases[i].TestValues.Count];
                            for (var j = 0; j < cast.Cases[i].TestValues.Count; j++)
                            {
                                var replacementExpression2 = Rebind(cast.Cases[i].TestValues[j], context);
                                replacementExpressions2[j] = replacementExpression2;
                            }
                            var replacementCase = Expression.SwitchCase(Rebind(cast.Cases[i].Body, context), replacementExpressions2);
                            replacementCases[i] = replacementCase;
                        }

                        return Expression.Switch(cast.Type, Rebind(cast.SwitchValue, context), Rebind(cast.DefaultBody, context), cast.Comparison, cast.Cases);
                    }
                case ExpressionType.Throw:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.Throw(Rebind(cast.Operand, context), cast.Type);
                    }
                case ExpressionType.Try:
                    {
                        var cast = exp as TryExpression;

                        var replacementBlocks = new CatchBlock[cast.Handlers.Count];
                        for (var i = 0; i < cast.Handlers.Count; i++)
                        {
                            var replacementExpression = Rebind(cast.Handlers[i].Variable, context);
                            var replacementParameters = ExtractParametersExpression(replacementExpression);
                            var replacementBlock = Expression.Catch(replacementParameters.Single(), Rebind(cast.Handlers[i].Body, context), Rebind(cast.Handlers[i].Filter, context));
                            replacementBlocks[i] = replacementBlock;
                        }

                        return Expression.MakeTry(cast.Type, Rebind(cast.Body, context), Rebind(cast.Finally, context), Rebind(cast.Fault, context), replacementBlocks);
                    }
                case ExpressionType.TypeAs:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.TypeAs(Rebind(cast.Operand, context), cast.Type);
                    }
                case ExpressionType.TypeEqual:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.TypeEqual(Rebind(cast.Operand, context), cast.Type);
                    }
                case ExpressionType.TypeIs:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.TypeIs(Rebind(cast.Operand, context), cast.Type);
                    }
                case ExpressionType.UnaryPlus:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.UnaryPlus(Rebind(cast.Operand, context));
                    }
                case ExpressionType.Unbox:
                    {
                        var cast = exp as UnaryExpression;
                        return Expression.Unbox(Rebind(cast.Operand, context), cast.Type);
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private static ParameterExpression[] ExtractParametersExpression(Expression exp)
        {
            var parameters = new List<ParameterExpression>();
            ExtractParametersExpressionInternal(exp, parameters);
            return parameters.Distinct().ToArray();
        }
        private static void ExtractParametersExpressionInternal(Expression exp, List<ParameterExpression> parameters)
        {
            if (exp == null)
                return;

            switch (exp.NodeType)
            {
                case ExpressionType.Add:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.AddAssign:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.AddAssignChecked:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.AddChecked:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.And:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.AndAlso:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.AndAssign:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.ArrayLength:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Assign:
                    throw new NotImplementedException();
                case ExpressionType.Block:
                    {
                        var cast = exp as BlockExpression;
                        foreach (var item in cast.Expressions)
                            ExtractParametersExpressionInternal(item, parameters);
                        foreach (var item in cast.Variables)
                            ExtractParametersExpressionInternal(item, parameters);
                        return;
                    }
                case ExpressionType.Call:
                    {
                        var cast = exp as MethodCallExpression;
                        ExtractParametersExpressionInternal(cast.Object, parameters);
                        foreach (Expression arg in cast.Arguments)
                            ExtractParametersExpressionInternal(arg, parameters);
                        return;
                    }
                case ExpressionType.Coalesce:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.Conditional:
                    {
                        var cast = exp as ConditionalExpression;
                        ExtractParametersExpressionInternal(cast.Test, parameters);
                        ExtractParametersExpressionInternal(cast.IfTrue, parameters);
                        ExtractParametersExpressionInternal(cast.IfFalse, parameters);
                        return;
                    }
                case ExpressionType.Constant:
                    {
                        return;
                    }
                case ExpressionType.Convert:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.ConvertChecked:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.DebugInfo:
                    {
                        return;
                    }
                case ExpressionType.Decrement:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Default:
                    {
                        return;
                    }
                case ExpressionType.Divide:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.DivideAssign:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.Dynamic:
                    {
                        var cast = exp as DynamicExpression;
                        foreach (var item in cast.Arguments)
                            ExtractParametersExpressionInternal(item, parameters);
                        return;
                    }
                case ExpressionType.Equal:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.ExclusiveOr:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.ExclusiveOrAssign:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.Extension:
                    {
                        return;
                    }
                case ExpressionType.Goto:
                    {
                        var cast = exp as GotoExpression;
                        ExtractParametersExpressionInternal(cast.Value, parameters);
                        return;
                    }
                case ExpressionType.GreaterThan:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.GreaterThanOrEqual:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.Increment:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Index:
                    {
                        var cast = exp as IndexExpression;
                        ExtractParametersExpressionInternal(cast.Object, parameters);
                        foreach (var arg in cast.Arguments)
                            ExtractParametersExpressionInternal(arg, parameters);
                        return;
                    }
                    throw new NotImplementedException();
                case ExpressionType.Invoke:
                    {
                        var cast = exp as InvocationExpression;
                        ExtractParametersExpressionInternal(cast.Expression, parameters);
                        foreach (var item in cast.Arguments)
                            ExtractParametersExpressionInternal(item, parameters);
                        return;
                    }
                case ExpressionType.IsFalse:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.IsTrue:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Label:
                    {
                        var cast = exp as LabelExpression;
                        ExtractParametersExpressionInternal(cast.DefaultValue, parameters);
                        return;
                    }
                case ExpressionType.Lambda:
                    {
                        var cast = exp as LambdaExpression;
                        ExtractParametersExpressionInternal(cast.Body, parameters);
                        return;
                    }
                case ExpressionType.LeftShift:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.LeftShiftAssign:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.LessThan:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.LessThanOrEqual:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.ListInit:
                    {
                        var cast = exp as ListInitExpression;
                        ExtractParametersExpressionInternal(cast.NewExpression, parameters);
                        foreach (var item in cast.Initializers)
                            foreach (var item2 in item.Arguments)
                                ExtractParametersExpressionInternal(item2, parameters);
                        return;
                    }
                case ExpressionType.Loop:
                    {
                        var cast = exp as LoopExpression;
                        ExtractParametersExpressionInternal(cast.Body, parameters);
                        return;
                    }
                case ExpressionType.MemberAccess:
                    {
                        var cast = exp as MemberExpression;
                        ExtractParametersExpressionInternal(cast.Expression, parameters);
                        return;
                    }
                case ExpressionType.MemberInit:
                    throw new NotImplementedException();
                case ExpressionType.Modulo:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.ModuloAssign:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.Multiply:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.MultiplyAssign:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.MultiplyAssignChecked:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.MultiplyChecked:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.Negate:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.NegateChecked:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.New:
                    {
                        var cast = exp as NewExpression;
                        foreach (var item in cast.Arguments)
                            ExtractParametersExpressionInternal(item, parameters);
                        return;
                    }
                case ExpressionType.NewArrayBounds:
                    {
                        var cast = exp as NewArrayExpression;
                        foreach (var item in cast.Expressions)
                            ExtractParametersExpressionInternal(item, parameters);
                        return;
                    }
                case ExpressionType.NewArrayInit:
                    {
                        var cast = exp as NewArrayExpression;
                        foreach (var item in cast.Expressions)
                            ExtractParametersExpressionInternal(item, parameters);
                        return;
                    }
                case ExpressionType.Not:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.NotEqual:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.OnesComplement:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Or:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.OrAssign:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.OrElse:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.Parameter:
                    {
                        var cast = exp as ParameterExpression;
                        parameters.Add(cast);
                        return;
                    }
                case ExpressionType.PostDecrementAssign:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.PostIncrementAssign:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Power:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.PowerAssign:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.PreDecrementAssign:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.PreIncrementAssign:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Quote:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.RightShift:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.RightShiftAssign:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.RuntimeVariables:
                    {
                        var cast = exp as RuntimeVariablesExpression;
                        foreach (var item in cast.Variables)
                            ExtractParametersExpressionInternal(item, parameters);
                        return;
                    }
                case ExpressionType.Subtract:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.SubtractAssign:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.SubtractAssignChecked:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.SubtractChecked:
                    {
                        var cast = exp as BinaryExpression;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.Switch:
                    {
                        var cast = exp as SwitchExpression;
                        ExtractParametersExpressionInternal(cast.SwitchValue, parameters);
                        ExtractParametersExpressionInternal(cast.DefaultBody, parameters);
                        foreach (var item in cast.Cases)
                        {
                            ExtractParametersExpressionInternal(item.Body, parameters);
                            foreach (var item2 in item.TestValues)
                                ExtractParametersExpressionInternal(item2, parameters);
                        }
                        return;
                    }
                case ExpressionType.Throw:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Try:
                    {
                        var cast = exp as TryExpression;
                        ExtractParametersExpressionInternal(cast.Body, parameters);
                        ExtractParametersExpressionInternal(cast.Fault, parameters);
                        ExtractParametersExpressionInternal(cast.Finally, parameters);
                        foreach (var item in cast.Handlers)
                        {
                            ExtractParametersExpressionInternal(item.Body, parameters);
                            ExtractParametersExpressionInternal(item.Variable, parameters);
                        }
                        return;
                    }
                case ExpressionType.TypeAs:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.TypeEqual:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.TypeIs:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.UnaryPlus:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Unbox:
                    {
                        var cast = exp as UnaryExpression;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
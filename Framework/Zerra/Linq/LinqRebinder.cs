// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Zerra.Linq
{
    public sealed partial class LinqRebinder
    {
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
            if (context.ExpressionReplacements != null && context.ExpressionReplacements.TryGetValue(exp, out var newExpression))
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
                        var cast = (BinaryExpression)exp;
                        return Expression.Add(Rebind(cast.Left, context), Rebind(cast.Right, context), cast.Method);
                    }
                case ExpressionType.AddAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.Add(Rebind(cast.Left, context), Rebind(cast.Right, context), cast.Method);
                    }
                case ExpressionType.AddAssignChecked:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.AddAssignChecked(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.AddChecked:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.AddChecked(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.And:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.And(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.AndAlso:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.AndAlso(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.AndAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.AndAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.ArrayIndex:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.ArrayIndex(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.ArrayLength:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.ArrayLength(Rebind(cast.Operand, context));
                    }
                case ExpressionType.Assign:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.Assign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.Block:
                    {
                        var cast = (BlockExpression)exp;

                        var replacementExpressions = new Expression[cast.Expressions.Count];
                        for (var i = 0; i < cast.Expressions.Count; i++)
                        {
                            var replacementExpression = Rebind(cast.Expressions[i], context);
                            replacementExpressions[i] = replacementExpression;
                        }

                        var replacementParameters = new List<ParameterExpression>();
                        for (var i = 0; i < cast.Variables.Count; i++)
                        {
                            var replacementExpression = Rebind(cast.Variables[i], context);
                            var replacementParameter = ExtractParametersExpression(replacementExpression);
                            replacementParameters.AddRange(replacementParameter);
                        }

                        return Expression.Block(replacementParameters, replacementExpressions);
                    }
                case ExpressionType.Call:
                    {
                        var cast = (MethodCallExpression)exp;

                        var replacementExpressions = new Expression[cast.Arguments.Count];
                        for (var i = 0; i < cast.Arguments.Count; i++)
                        {
                            var replacementExpression = Rebind(cast.Arguments[i], context);
                            replacementExpressions[i] = replacementExpression;
                        }

                        return Expression.Call(cast.Object != null ? Rebind(cast.Object, context) : null, cast.Method, replacementExpressions);
                    }
                case ExpressionType.Coalesce:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.Coalesce(Rebind(cast.Left, context), Rebind(cast.Right, context), cast.Conversion);
                    }
                case ExpressionType.Conditional:
                    {
                        var cast = (ConditionalExpression)exp;
                        return Expression.Condition(Rebind(cast.Test, context), Rebind(cast.IfTrue, context), Rebind(cast.IfFalse, context), cast.Type);
                    }
                case ExpressionType.Constant:
                    {
                        return exp;
                    }
                case ExpressionType.Convert:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.Convert(Rebind(cast.Operand, context), cast.Type);
                    }
                case ExpressionType.ConvertChecked:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.ConvertChecked(Rebind(cast.Operand, context), cast.Type);
                    }
                case ExpressionType.DebugInfo:
                    {
                        return exp;
                    }
                case ExpressionType.Decrement:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.Decrement(Rebind(cast.Operand, context));
                    }
                case ExpressionType.Default:
                    {
                        return exp;
                    }
                case ExpressionType.Divide:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.Divide(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.DivideAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.DivideAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.Dynamic:
                    {
                        var cast = (DynamicExpression)exp;

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
                        var cast = (BinaryExpression)exp;
                        return Expression.Equal(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.ExclusiveOr:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.ExclusiveOr(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.ExclusiveOrAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.ExclusiveOrAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.Extension:
                    {
                        return exp;
                    }
                case ExpressionType.Goto:
                    {
                        var cast = (GotoExpression)exp;
                        return Expression.Goto(cast.Target, cast.Value != null ? Rebind(cast.Value, context) : null, cast.Type);
                    }
                case ExpressionType.GreaterThan:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.GreaterThan(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.GreaterThanOrEqual:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.GreaterThanOrEqual(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.Increment:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.Increment(Rebind(cast.Operand, context));
                    }
                case ExpressionType.Index:
                    {
                        var cast = (IndexExpression)exp;


                        var replacementExpressions = new Expression[cast.Arguments.Count];
                        for (var i = 0; i < cast.Arguments.Count; i++)
                        {
                            var replacementExpression = Rebind(cast.Arguments[i], context);
                            replacementExpressions[i] = replacementExpression;
                        }

#pragma warning disable CS8604 // Possible null reference argument.
                        return Expression.MakeIndex(cast.Object != null ? Rebind(cast.Object, context) : null, cast.Indexer, replacementExpressions);
#pragma warning restore CS8604 // Possible null reference argument.

                    }
                case ExpressionType.Invoke:
                    {
                        var cast = (InvocationExpression)exp;

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
                        var cast = (UnaryExpression)exp;
                        return Expression.IsFalse(Rebind(cast.Operand, context));
                    }
                case ExpressionType.IsTrue:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.IsTrue(Rebind(cast.Operand, context));
                    }
                case ExpressionType.Label:
                    {
                        var cast = (LabelExpression)exp;
                        return Expression.Label(cast.Target, cast.DefaultValue != null ? Rebind(cast.DefaultValue, context) : null);
                    }
                case ExpressionType.Lambda:
                    {
                        var cast = (LambdaExpression)exp;

                        var replacementParameters = new List<ParameterExpression>();
                        for (var i = 0; i < cast.Parameters.Count; i++)
                        {
                            var replacementExpression = Rebind(cast.Parameters[i], context);
                            var replacementParameter = ExtractParametersExpression(replacementExpression);
                            replacementParameters.AddRange(replacementParameter);
                        }

                        var body = Rebind(cast.Body, context);

                        return Expression.Lambda(body, replacementParameters);
                    }
                case ExpressionType.LeftShift:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.LeftShift(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.LeftShiftAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.LeftShiftAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.LessThan:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.LessThan(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.LessThanOrEqual:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.LessThanOrEqual(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.ListInit:
                    {
                        var cast = (ListInitExpression)exp;

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
                        var cast = (LoopExpression)exp;
                        return Expression.Loop(Rebind(cast.Body, context), cast.BreakLabel, cast.ContinueLabel);
                    }
                case ExpressionType.MemberAccess:
                    {
                        var cast = (MemberExpression)exp;

                        if (cast.Expression != null)
                        {
                            return Expression.MakeMemberAccess(Rebind(cast.Expression, context), cast.Member);
                        }
                        else
                        {
                            return exp;
                        }
                    }
                case ExpressionType.MemberInit:
                    {
                        var cast = (MemberInitExpression)exp;
                        return Expression.MemberInit((NewExpression)Rebind(cast.NewExpression, context), cast.Bindings);
                    }
                case ExpressionType.Modulo:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.ModuloAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.ModuloAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.ModuloAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.Multiply:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.Multiply(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.MultiplyAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.MultiplyAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.MultiplyAssignChecked:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.MultiplyAssignChecked(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.MultiplyChecked:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.MultiplyChecked(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.Negate:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.Negate(Rebind(cast.Operand, context));
                    }
                case ExpressionType.NegateChecked:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.NegateChecked(Rebind(cast.Operand, context));
                    }
                case ExpressionType.New:
                    {
                        var cast = (NewExpression)exp;

                        if (cast.Constructor != null)
                        {
                            var replacementExpressions = new Expression[cast.Arguments.Count];
                            for (var i = 0; i < cast.Arguments.Count; i++)
                            {
                                var replacementExpression = Rebind(cast.Arguments[i], context);
                                replacementExpressions[i] = replacementExpression;
                            }

                            return Expression.New(cast.Constructor, replacementExpressions, cast.Members);
                        }
                        else
                        {
                            return exp;
                        }
                    }
                case ExpressionType.NewArrayBounds:
                    {
                        var cast = (NewArrayExpression)exp;

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
                        var cast = (NewArrayExpression)exp;

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
                        var cast = (UnaryExpression)exp;
                        return Expression.Not(Rebind(cast.Operand, context));
                    }
                case ExpressionType.NotEqual:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.NotEqual(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.OnesComplement:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.OnesComplement(Rebind(cast.Operand, context));
                    }
                case ExpressionType.Or:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.Or(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.OrAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.OrAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.OrElse:
                    {
                        var cast = (BinaryExpression)exp;
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
                        var cast = (UnaryExpression)exp;
                        return Expression.PostDecrementAssign(Rebind(cast.Operand, context));
                    }
                case ExpressionType.PostIncrementAssign:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.PostDecrementAssign(Rebind(cast.Operand, context));
                    }
                case ExpressionType.Power:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.Power(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.PowerAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.PowerAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.PreDecrementAssign:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.PostDecrementAssign(Rebind(cast.Operand, context));
                    }
                case ExpressionType.PreIncrementAssign:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.PreIncrementAssign(Rebind(cast.Operand, context));
                    }
                case ExpressionType.Quote:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.Quote(Rebind(cast.Operand, context));
                    }
                case ExpressionType.RightShift:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.RightShift(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.RightShiftAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.RightShiftAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.RuntimeVariables:
                    {
                        var cast = (RuntimeVariablesExpression)exp;

                        var replacementParameters = new List<ParameterExpression>();
                        for (var i = 0; i < cast.Variables.Count; i++)
                        {
                            var replacementExpression = Rebind(cast.Variables[i], context);
                            var replacementParameter = ExtractParametersExpression(replacementExpression);
                            replacementParameters.AddRange(replacementParameter);
                        }

                        return Expression.RuntimeVariables(replacementParameters);
                    }
                case ExpressionType.Subtract:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.Subtract(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.SubtractAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.SubtractAssign(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.SubtractAssignChecked:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.SubtractAssignChecked(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.SubtractChecked:
                    {
                        var cast = (BinaryExpression)exp;
                        return Expression.SubtractChecked(Rebind(cast.Left, context), Rebind(cast.Right, context));
                    }
                case ExpressionType.Switch:
                    {
                        var cast = (SwitchExpression)exp;

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

                        return Expression.Switch(cast.Type, Rebind(cast.SwitchValue, context), cast.DefaultBody != null ? Rebind(cast.DefaultBody, context) : null, cast.Comparison, cast.Cases);
                    }
                case ExpressionType.Throw:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.Throw(Rebind(cast.Operand, context), cast.Type);
                    }
                case ExpressionType.Try:
                    {
                        var cast = (TryExpression)exp;

                        var replacementBlocks = new CatchBlock[cast.Handlers.Count];
                        for (var i = 0; i < cast.Handlers.Count; i++)
                        {
                            var handler = cast.Handlers[i];
                            ParameterExpression? replacementParameter = null;
                            if (handler.Variable != null)
                            {
                                var replacementExpression = Rebind(handler.Variable, context);
                                replacementParameter = ExtractParametersExpression(replacementExpression).SingleOrDefault();
                            }
                            var replacementBlock = Expression.MakeCatchBlock(handler.Test, replacementParameter, Rebind(handler.Body, context), handler.Filter != null ? Rebind(handler.Filter, context) : null);
                            replacementBlocks[i] = replacementBlock;
                        }

                        return Expression.MakeTry(cast.Type, Rebind(cast.Body, context), cast.Finally != null ? Rebind(cast.Finally, context) : null, cast.Fault != null ? Rebind(cast.Fault, context) : null, replacementBlocks);
                    }
                case ExpressionType.TypeAs:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.TypeAs(Rebind(cast.Operand, context), cast.Type);
                    }
                case ExpressionType.TypeEqual:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.TypeEqual(Rebind(cast.Operand, context), cast.Type);
                    }
                case ExpressionType.TypeIs:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.TypeIs(Rebind(cast.Operand, context), cast.Type);
                    }
                case ExpressionType.UnaryPlus:
                    {
                        var cast = (UnaryExpression)exp;
                        return Expression.UnaryPlus(Rebind(cast.Operand, context));
                    }
                case ExpressionType.Unbox:
                    {
                        var cast = (UnaryExpression)exp;
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
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.AddAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.AddAssignChecked:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.AddChecked:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.And:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.AndAlso:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.AndAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.ArrayLength:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Assign:
                    throw new NotImplementedException();
                case ExpressionType.Block:
                    {
                        var cast = (BlockExpression)exp;
                        foreach (var item in cast.Expressions)
                            ExtractParametersExpressionInternal(item, parameters);
                        foreach (var item in cast.Variables)
                            ExtractParametersExpressionInternal(item, parameters);
                        return;
                    }
                case ExpressionType.Call:
                    {
                        var cast = (MethodCallExpression)exp;
                        if (cast.Object != null)
                            ExtractParametersExpressionInternal(cast.Object, parameters);
                        foreach (var arg in cast.Arguments)
                            ExtractParametersExpressionInternal(arg, parameters);
                        return;
                    }
                case ExpressionType.Coalesce:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.Conditional:
                    {
                        var cast = (ConditionalExpression)exp;
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
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.ConvertChecked:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.DebugInfo:
                    {
                        return;
                    }
                case ExpressionType.Decrement:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Default:
                    {
                        return;
                    }
                case ExpressionType.Divide:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.DivideAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.Dynamic:
                    {
                        var cast = (DynamicExpression)exp;
                        foreach (var item in cast.Arguments)
                            ExtractParametersExpressionInternal(item, parameters);
                        return;
                    }
                case ExpressionType.Equal:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.ExclusiveOr:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.ExclusiveOrAssign:
                    {
                        var cast = (BinaryExpression)exp;
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
                        var cast = (GotoExpression)exp;
                        if (cast.Value != null)
                            ExtractParametersExpressionInternal(cast.Value, parameters);
                        return;
                    }
                case ExpressionType.GreaterThan:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.GreaterThanOrEqual:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.Increment:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Index:
                    {
                        var cast = (IndexExpression)exp;
                        if (cast.Object != null)
                            ExtractParametersExpressionInternal(cast.Object, parameters);
                        foreach (var arg in cast.Arguments)
                            ExtractParametersExpressionInternal(arg, parameters);
                        return;
                    }
                    throw new NotImplementedException();
                case ExpressionType.Invoke:
                    {
                        var cast = (InvocationExpression)exp;
                        ExtractParametersExpressionInternal(cast.Expression, parameters);
                        foreach (var item in cast.Arguments)
                            ExtractParametersExpressionInternal(item, parameters);
                        return;
                    }
                case ExpressionType.IsFalse:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.IsTrue:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Label:
                    {
                        var cast = (LabelExpression)exp;
                        if (cast.DefaultValue != null)
                            ExtractParametersExpressionInternal(cast.DefaultValue, parameters);
                        return;
                    }
                case ExpressionType.Lambda:
                    {
                        var cast = (LambdaExpression)exp;
                        ExtractParametersExpressionInternal(cast.Body, parameters);
                        return;
                    }
                case ExpressionType.LeftShift:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.LeftShiftAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.LessThan:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.LessThanOrEqual:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.ListInit:
                    {
                        var cast = (ListInitExpression)exp;
                        ExtractParametersExpressionInternal(cast.NewExpression, parameters);
                        foreach (var item in cast.Initializers)
                        {
                            foreach (var item2 in item.Arguments)
                                ExtractParametersExpressionInternal(item2, parameters);
                        }

                        return;
                    }
                case ExpressionType.Loop:
                    {
                        var cast = (LoopExpression)exp;
                        ExtractParametersExpressionInternal(cast.Body, parameters);
                        return;
                    }
                case ExpressionType.MemberAccess:
                    {
                        var cast = (MemberExpression)exp;
                        if (cast.Expression != null)
                            ExtractParametersExpressionInternal(cast.Expression, parameters);
                        return;
                    }
                case ExpressionType.MemberInit:
                    throw new NotImplementedException();
                case ExpressionType.Modulo:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.ModuloAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.Multiply:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.MultiplyAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.MultiplyAssignChecked:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.MultiplyChecked:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.Negate:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.NegateChecked:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.New:
                    {
                        var cast = (NewExpression)exp;
                        foreach (var item in cast.Arguments)
                            ExtractParametersExpressionInternal(item, parameters);
                        return;
                    }
                case ExpressionType.NewArrayBounds:
                    {
                        var cast = (NewArrayExpression)exp;
                        foreach (var item in cast.Expressions)
                            ExtractParametersExpressionInternal(item, parameters);
                        return;
                    }
                case ExpressionType.NewArrayInit:
                    {
                        var cast = (NewArrayExpression)exp;
                        foreach (var item in cast.Expressions)
                            ExtractParametersExpressionInternal(item, parameters);
                        return;
                    }
                case ExpressionType.Not:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.NotEqual:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.OnesComplement:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Or:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.OrAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.OrElse:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.Parameter:
                    {
                        var cast = (ParameterExpression)exp;
                        parameters.Add(cast);
                        return;
                    }
                case ExpressionType.PostDecrementAssign:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.PostIncrementAssign:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Power:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.PowerAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.PreDecrementAssign:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.PreIncrementAssign:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Quote:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.RightShift:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.RightShiftAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.RuntimeVariables:
                    {
                        var cast = (RuntimeVariablesExpression)exp;
                        foreach (var item in cast.Variables)
                            ExtractParametersExpressionInternal(item, parameters);
                        return;
                    }
                case ExpressionType.Subtract:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.SubtractAssign:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.SubtractAssignChecked:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.SubtractChecked:
                    {
                        var cast = (BinaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Left, parameters);
                        ExtractParametersExpressionInternal(cast.Right, parameters);
                        return;
                    }
                case ExpressionType.Switch:
                    {
                        var cast = (SwitchExpression)exp;
                        ExtractParametersExpressionInternal(cast.SwitchValue, parameters);
                        if (cast.DefaultBody != null)
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
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Try:
                    {
                        var cast = (TryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Body, parameters);
                        if (cast.Fault != null)
                            ExtractParametersExpressionInternal(cast.Fault, parameters);
                        if (cast.Finally != null)
                            ExtractParametersExpressionInternal(cast.Finally, parameters);
                        foreach (var item in cast.Handlers)
                        {
                            ExtractParametersExpressionInternal(item.Body, parameters);
                            if (item.Variable != null)
                                ExtractParametersExpressionInternal(item.Variable, parameters);
                        }
                        return;
                    }
                case ExpressionType.TypeAs:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.TypeEqual:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.TypeIs:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.UnaryPlus:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                case ExpressionType.Unbox:
                    {
                        var cast = (UnaryExpression)exp;
                        ExtractParametersExpressionInternal(cast.Operand, parameters);
                        return;
                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
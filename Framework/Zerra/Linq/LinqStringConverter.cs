// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Zerra.Reflection;

namespace Zerra.Linq
{
    public static partial class LinqStringConverter
    {
        public static string Convert(Expression exp)
        {
            var sb = new StringBuilder();

            var context = new ConvertContext(sb);

            ConvertToString(exp, context);

            return sb.ToString();
        }

        private static void ConvertToString(Expression exp, ConvertContext context)
        {
            switch (exp.NodeType)
            {
                case ExpressionType.Add:
                    ConvertToStringBinary("+", exp, context);
                    break;
                case ExpressionType.AddAssign:
                    ConvertToStringBinary("+=", exp, context);
                    break;
                case ExpressionType.AddAssignChecked:
                    ConvertToStringBinary("+=", exp, context);
                    break;
                case ExpressionType.AddChecked:
                    ConvertToStringBinary("+", exp, context);
                    break;
                case ExpressionType.And:
                    ConvertToStringBinary("&", exp, context);
                    break;
                case ExpressionType.AndAlso:
                    ConvertToStringBinary("&&", exp, context);
                    break;
                case ExpressionType.AndAssign:
                    ConvertToStringBinary("&=", exp, context);
                    break;
                case ExpressionType.ArrayLength:
                    ConvertToStringUnary(null, ".Length", exp, context);
                    break;
                case ExpressionType.Assign:
                    ConvertToStringBinary("=", exp, context);
                    break;
                case ExpressionType.Block:
                    ConvertToStringBlock(exp, context);
                    break;
                case ExpressionType.Call:
                    ConvertToStringCall(exp, context);
                    break;
                case ExpressionType.Coalesce:
                    ConvertToStringBinary("??", exp, context);
                    break;
                case ExpressionType.Conditional:
                    ConvertToStringConditional(exp, context);
                    break;
                case ExpressionType.Constant:
                    ConvertToStringConstant(exp, context);
                    break;
                case ExpressionType.Convert:
                    ConvertToStringBox(exp, context);
                    break;
                case ExpressionType.ConvertChecked:
                    ConvertToStringUnary(null, null, exp, context);
                    break;
                case ExpressionType.DebugInfo:
                    ConvertToStringDebugInfo(exp, context);
                    break;
                case ExpressionType.Decrement:
                    ConvertToStringUnary(null, "--", exp, context);
                    break;
                case ExpressionType.Default:
                    ConvertToStringDefault(exp, context);
                    break;
                case ExpressionType.Divide:
                    ConvertToStringBinary("/", exp, context);
                    break;
                case ExpressionType.DivideAssign:
                    ConvertToStringBinary("/=", exp, context);
                    break;
                case ExpressionType.Dynamic:
                    ConvertToStringDynamic(exp, context);
                    break;
                case ExpressionType.Equal:
                    ConvertToStringBinary("==", exp, context);
                    break;
                case ExpressionType.ExclusiveOr:
                    ConvertToStringBinary("^", exp, context);
                    break;
                case ExpressionType.ExclusiveOrAssign:
                    ConvertToStringBinary("^=", exp, context);
                    break;
                case ExpressionType.Extension:
                    throw new NotImplementedException();
                case ExpressionType.Goto:
                    ConvertToStringGoto(exp, context);
                    break;
                case ExpressionType.GreaterThan:
                    ConvertToStringBinary(">", exp, context);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    ConvertToStringBinary(">=", exp, context);
                    break;
                case ExpressionType.Increment:
                    ConvertToStringUnary(null, "++", exp, context);
                    break;
                case ExpressionType.Index:
                    ConvertToStringIndex(exp, context);
                    break;
                case ExpressionType.Invoke:
                    ConvertToStringInvoke(exp, context);
                    break;
                case ExpressionType.IsFalse:
                    ConvertToStringUnary(null, "==false", exp, context);
                    break;
                case ExpressionType.IsTrue:
                    ConvertToStringUnary(null, "==true", exp, context);
                    break;
                case ExpressionType.Label:
                    ConvertToStringLabel(exp, context);
                    break;
                case ExpressionType.Lambda:
                    ConvertToStringLambda(exp, context);
                    break;
                case ExpressionType.LeftShift:
                    ConvertToStringBinary("<<", exp, context);
                    break;
                case ExpressionType.LeftShiftAssign:
                    ConvertToStringBinary("<<=", exp, context);
                    break;
                case ExpressionType.LessThan:
                    ConvertToStringBinary("<", exp, context);
                    break;
                case ExpressionType.LessThanOrEqual:
                    ConvertToStringBinary("<=", exp, context);
                    break;
                case ExpressionType.ListInit:
                    ConvertToStringListInit(exp, context);
                    break;
                case ExpressionType.Loop:
                    ConvertToStringLoop(exp, context);
                    break;
                case ExpressionType.MemberAccess:
                    ConvertToStringMember(exp, context);
                    break;
                case ExpressionType.MemberInit:
                    ConvertToStringMemberInit(exp, context);
                    break;
                case ExpressionType.Modulo:
                    ConvertToStringBinary("%", exp, context);
                    break;
                case ExpressionType.ModuloAssign:
                    ConvertToStringBinary("%=", exp, context);
                    break;
                case ExpressionType.Multiply:
                    ConvertToStringBinary("*", exp, context);
                    break;
                case ExpressionType.MultiplyAssign:
                    ConvertToStringBinary("*=", exp, context);
                    break;
                case ExpressionType.MultiplyAssignChecked:
                    ConvertToStringBinary("*=", exp, context);
                    break;
                case ExpressionType.MultiplyChecked:
                    ConvertToStringBinary("*", exp, context);
                    break;
                case ExpressionType.Negate:
                    ConvertToStringUnary("-", null, exp, context);
                    break;
                case ExpressionType.NegateChecked:
                    ConvertToStringUnary("-", null, exp, context);
                    break;
                case ExpressionType.New:
                    ConvertToStringNew(exp, context);
                    break;
                case ExpressionType.NewArrayBounds:
                    ConvertToStringNewArray(exp, context);
                    break;
                case ExpressionType.NewArrayInit:
                    ConvertToStringNewArray(exp, context);
                    break;
                case ExpressionType.Not:
                    ConvertToStringUnary("!", null, exp, context);
                    break;
                case ExpressionType.NotEqual:
                    ConvertToStringBinary("!=", exp, context);
                    break;
                case ExpressionType.OnesComplement:
                    ConvertToStringUnary("~", null, exp, context);
                    break;
                case ExpressionType.Or:
                    ConvertToStringBinary("|", exp, context);
                    break;
                case ExpressionType.OrAssign:
                    ConvertToStringBinary("|=", exp, context);
                    break;
                case ExpressionType.OrElse:
                    ConvertToStringBinary("||", exp, context);
                    break;
                case ExpressionType.Parameter:
                    ConvertToStringParameter(exp, context);
                    break;
                case ExpressionType.PostDecrementAssign:
                    ConvertToStringUnary(null, "--", exp, context);
                    break;
                case ExpressionType.PostIncrementAssign:
                    ConvertToStringUnary(null, "++", exp, context);
                    break;
                case ExpressionType.Power:
                    ConvertToStringBinary("^", exp, context);
                    break;
                case ExpressionType.PowerAssign:
                    ConvertToStringBinary("^=", exp, context);
                    break;
                case ExpressionType.PreDecrementAssign:
                    ConvertToStringUnary("--", null, exp, context);
                    break;
                case ExpressionType.PreIncrementAssign:
                    ConvertToStringUnary("++", null, exp, context);
                    break;
                case ExpressionType.Quote:
                    ConvertToStringUnary(null, null, exp, context);
                    break;
                case ExpressionType.RightShift:
                    ConvertToStringBinary(">>", exp, context);
                    break;
                case ExpressionType.RightShiftAssign:
                    ConvertToStringBinary(">>=", exp, context);
                    break;
                case ExpressionType.RuntimeVariables:
                    throw new NotImplementedException();
                case ExpressionType.Subtract:
                    ConvertToStringBinary("-", exp, context);
                    break;
                case ExpressionType.SubtractAssign:
                    ConvertToStringBinary("-=", exp, context);
                    break;
                case ExpressionType.SubtractAssignChecked:
                    ConvertToStringBinary("-=", exp, context);
                    break;
                case ExpressionType.SubtractChecked:
                    ConvertToStringBinary("-", exp, context);
                    break;
                case ExpressionType.Switch:
                    ConvertToStringSwitch(exp, context);
                    break;
                case ExpressionType.Throw:
                    ConvertToStringUnary("throw ", null, exp, context);
                    break;
                case ExpressionType.Try:
                    ConvertToStringTry(exp, context);
                    break;
                case ExpressionType.TypeAs:
                    ConvertToStringTypeBinaryExpression(" as ", exp, context);
                    break;
                case ExpressionType.TypeEqual:
                    ConvertToStringTypeBinaryExpression("==", exp, context);
                    break;
                case ExpressionType.TypeIs:
                    ConvertToStringTypeBinaryExpression(" is ", exp, context);
                    break;
                case ExpressionType.UnaryPlus:
                    ConvertToStringUnary("+", null, exp, context);
                    break;
                case ExpressionType.Unbox:
                    ConvertToStringBox(exp, context);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        private static void ConvertToStringLambda(Expression exp, ConvertContext context)
        {
            var lambda = exp as LambdaExpression;
            context.Builder.Append('(');
            for (var i = 0; i < lambda.Parameters.Count; i++)
            {
                if (i > 0)
                    context.Builder.Append(',');
                var parameter = lambda.Parameters[i];
                context.Builder.Append(parameter.Type.Name);
                if (!String.IsNullOrWhiteSpace(parameter.Name))
                    context.Builder.Append(' ').Append(parameter.Name);
            }
            context.Builder.Append(")=>");
            ConvertToString(lambda.Body, context);
        }
        private static void ConvertToStringUnary(string prefixOperation, string suffixOperation, Expression exp, ConvertContext context)
        {
            var unary = exp as UnaryExpression;
            context.Builder.Append(prefixOperation);
            ConvertToString(unary.Operand, context);
            context.Builder.Append(suffixOperation);
        }
        private static void ConvertToStringBox(Expression exp, ConvertContext context)
        {
            var unary = exp as UnaryExpression;
            context.Builder.Append('(').Append(unary.Type.GetNiceName()).Append(')');
            ConvertToString(unary.Operand, context);
        }
        private static void ConvertToStringBinary(string operation, Expression exp, ConvertContext context)
        {
            var binary = exp as BinaryExpression;
            //context.Builder.Append('(');
            ConvertToString(binary.Left, context);
            //context.Builder.Append(')');
            context.Builder.Append(operation);
            //context.Builder.Append('(');
            ConvertToString(binary.Right, context);
            //context.Builder.Append(')');
        }
        private static void ConvertToStringMember(Expression exp, ConvertContext context)
        {
            var member = exp as MemberExpression;

            if (member.Expression == null)
            {
                context.Builder.Append(member.Member.DeclaringType.GetNiceName()).Append('.').Append(member.Member.Name);
                if (context.MemberAccessStack.Count > 0)
                {
                    context.Builder.Append('.');
                    ConvertToStringMemberStack(context);
                }
            }
            else
            {
                context.MemberAccessStack.Push(member);
                ConvertToString(member.Expression, context);
                context.MemberAccessStack.Pop();
            }
        }
        private static void ConvertToStringConstant(Expression exp, ConvertContext context)
        {
            var constant = exp as ConstantExpression;

            ConvertToStringConstantStack(constant.Type, constant.Value, context);
        }
        private static void ConvertToStringConstantStack(Type type, object value, ConvertContext context)
        {
            if (context.MemberAccessStack.Count > 0)
            {
                var memberProperty = context.MemberAccessStack.Pop();

                switch (memberProperty.Member.MemberType)
                {
                    case MemberTypes.Field:
                        {
                            var field = memberProperty.Member as FieldInfo;
                            object fieldValue = field.GetValue(value);
                            ConvertToStringConstantStack(field.FieldType, fieldValue, context);
                            break;
                        }
                    case MemberTypes.Property:
                        {
                            var property = memberProperty.Member as PropertyInfo;
                            object propertyValue = property.GetValue(value);
                            ConvertToStringConstantStack(property.PropertyType, propertyValue, context);
                            break;
                        }
                    default:
                        throw new NotImplementedException();
                }

                context.MemberAccessStack.Push(memberProperty);
            }
            else
            {
                ConvertToStringValue(type, value, context);
            }
        }
        private static void ConvertToStringCall(Expression exp, ConvertContext context)
        {
            var call = exp as MethodCallExpression;

            if (call.Object == null)
            {
                var callingObject = call.Arguments[0];
                ConvertToString(callingObject, context);
                context.Builder.Append('.');
                context.Builder.Append(call.Method.Name);
                context.Builder.Append('(');
                for (int i = 1; i < call.Arguments.Count; i++)
                {
                    if (i > 1)
                        context.Builder.Append(',');
                    var arg = call.Arguments[i];
                    ConvertToString(arg, context);
                }
                context.Builder.Append(')');
            }
            else
            {
                ConvertToString(call.Object, context);
                context.Builder.Append('.');
                context.Builder.Append(call.Method.Name);
                context.Builder.Append('(');
                for (int i = 0; i < call.Arguments.Count; i++)
                {
                    if (i > 1)
                        context.Builder.Append(',');
                    var arg = call.Arguments[i];
                    ConvertToString(arg, context);
                }
                context.Builder.Append(')');
            }
        }
        private static void ConvertToStringNew(Expression exp, ConvertContext context)
        {
            var newExp = exp as NewExpression;

            context.Builder.Append("new ").Append(newExp.Type.GetNiceName()).Append('(');
            for (var i = 0; i < newExp.Arguments.Count; i++)
            {
                if (i > 0)
                    context.Builder.Append(", ");
                ConvertToString(newExp.Arguments[i], context);
            }
            context.Builder.Append(')');
        }
        private static void ConvertToStringNewArray(Expression exp, ConvertContext context)
        {
            var newExp = exp as NewArrayExpression;

            context.Builder.Append("new ").Append(newExp.Type.GetNiceName()).Append("[] {");
            for (var i = 0; i < newExp.Expressions.Count; i++)
            {
                if (i > 0)
                    context.Builder.Append(", ");
                ConvertToString(newExp.Expressions[i], context);
            }
            context.Builder.Append('}');
        }
        private static void ConvertToStringLabel(Expression exp, ConvertContext context)
        {
            var label = exp as LabelExpression;
            context.Builder.Append(label.Target.Name).Append(':');
        }
        private static void ConvertToStringDebugInfo(Expression exp, ConvertContext context)
        {
            var debugInfo = exp as DebugInfoExpression;
        }
        private static void ConvertToStringMemberInit(Expression exp, ConvertContext context)
        {
            var memberInti = exp as MemberInitExpression;
            ConvertToString(memberInti.NewExpression, context);
            context.Builder.Append('{');
            for (var i = 0; i < memberInti.Bindings.Count; i++)
            {
                var binding = memberInti.Bindings[i];
                if (i > 0)
                    context.Builder.Append(", ");

                if (binding.Member.MemberType == MemberTypes.Property)
                    context.Builder.Append(((PropertyInfo)binding.Member).PropertyType.GetNiceName()).Append(' ');
                else if (binding.Member.MemberType == MemberTypes.Field)
                    context.Builder.Append(((FieldInfo)binding.Member).FieldType.GetNiceName()).Append(' ');
                context.Builder.Append(binding.Member.Name);
            }
            context.Builder.Append('}');
        }
        private static void ConvertToStringListInit(Expression exp, ConvertContext context)
        {
            var listInti = exp as ListInitExpression;
            ConvertToString(listInti.NewExpression, context);
            context.Builder.Append('{');
            for (var i = 0; i < listInti.Initializers.Count; i++)
            {
                var initializers = listInti.Initializers[i];
                if (i > 0)
                    context.Builder.Append(", ");

                context.Builder.Append("new (");
                for (var j = 0; j < initializers.Arguments.Count; j++)
                {
                    if (j > 0)
                        context.Builder.Append(", ");
                    ConvertToString(initializers.Arguments[j], context);
                }
                context.Builder.Append(')');
            }
            context.Builder.Append('}');
        }
        private static void ConvertToStringLoop(Expression exp, ConvertContext context)
        {
            var loop = exp as LoopExpression;
            context.Builder.Append("while {");
            ConvertToString(loop.Body, context);
            context.Builder.Append('}');
        }
        private static void ConvertToStringIndex(Expression exp, ConvertContext context)
        {
            var index = exp as IndexExpression;
            context.Builder.Append(index.Type.GetNiceName());
            context.Builder.Append('[');
            for (var i = 0; i < index.Arguments.Count; i++)
            {
                if (i > 0)
                    context.Builder.Append(',');
                ConvertToString(index.Arguments[i], context);
            }
            context.Builder.Append(']');
        }
        private static void ConvertToStringTypeBinaryExpression(string operation, Expression exp, ConvertContext context)
        {
            var typeBinary = exp as TypeBinaryExpression;
            ConvertToString(typeBinary.Expression, context);
            context.Builder.Append(operation);
            context.Builder.Append(typeBinary.TypeOperand.GetNiceName());
        }
        private static void ConvertToStringBlock(Expression exp, ConvertContext context)
        {
            var block = exp as BlockExpression;
            context.Builder.Append('{');
            foreach (var item in block.Expressions)
            {
                ConvertToString(item, context);
                context.Builder.Append(';');
            }
            context.Builder.Append('}');
        }
        private static void ConvertToStringDefault(Expression exp, ConvertContext context)
        {
            var @default = exp as DefaultExpression;
            context.Builder.Append("default(");
            context.Builder.Append(@default.Type.GetNiceName());
            context.Builder.Append(')');
        }
        private static void ConvertToStringDynamic(Expression exp, ConvertContext context)
        {
            var dynamic = exp as DynamicExpression;
            context.Builder.Append(dynamic.DelegateType.GetNiceName());
            context.Builder.Append('(');
            for (var i = 0; i < dynamic.Arguments.Count; i++)
            {
                if (i > 0)
                    context.Builder.Append(',');
                ConvertToString(dynamic.Arguments[i], context);
            }
            context.Builder.Append(')');
        }
        private static void ConvertToStringTry(Expression exp, ConvertContext context)
        {
            var @try = exp as TryExpression;
            context.Builder.Append("try {");
            ConvertToString(@try.Body, context);
            context.Builder.Append('}');
            if (@try.Fault != null)
            {
                context.Builder.Append("catch {");
                ConvertToString(@try.Fault, context);
                context.Builder.Append('}');
            }
            if (@try.Finally != null)
            {
                context.Builder.Append("finally {");
                ConvertToString(@try.Finally, context);
                context.Builder.Append('}');
            }
        }
        private static void ConvertToStringSwitch(Expression exp, ConvertContext context)
        {
            var @switch = exp as SwitchExpression;
            context.Builder.Append("switch(");
            ConvertToString(@switch.SwitchValue, context);
            context.Builder.Append(") {");
            foreach (var @case in @switch.Cases)
            {
                foreach (var testValue in @case.TestValues)
                {
                    context.Builder.Append("case ");
                    ConvertToString(testValue, context);
                    context.Builder.Append(':');
                }
                ConvertToString(@switch.DefaultBody, context);
            }
            if (@switch.DefaultBody != null)
            {
                context.Builder.Append("default: ");
                ConvertToString(@switch.DefaultBody, context);
            }
            context.Builder.Append('}');
        }
        private static void ConvertToStringGoto(Expression exp, ConvertContext context)
        {
            var @goto = exp as GotoExpression;
            context.Builder.Append("goto ");
            context.Builder.Append(@goto.Target.Name);
        }
        private static void ConvertToStringInvoke(Expression exp, ConvertContext context)
        {
            var invocation = exp as InvocationExpression;
            ConvertToString(invocation, context);
            context.Builder.Append('(');
            for (var i = 0; i < invocation.Arguments.Count; i++)
            {
                if (i > 0)
                    context.Builder.Append(',');
                ConvertToString(invocation.Arguments[i], context);
            }
            context.Builder.Append(')');
        }
        private static void ConvertToStringParameter(Expression exp, ConvertContext context)
        {
            var parameterExpression = exp as ParameterExpression;

            //if (!String.IsNullOrWhiteSpace(parameterExpression.Name))
            //{
            //    context.Builder.Append('(').Append(parameterExpression.Type.Name).Append(')');
            //    context.Builder.Append(parameterExpression.Name);
            //}
            //else
            //{
            context.Builder.Append(parameterExpression.Type.GetNiceName());
            //}
            if (context.MemberAccessStack.Count > 0)
            {
                context.Builder.Append('.');
                ConvertToStringMemberStack(context);
            }
        }

        private static void ConvertToStringMemberStack(ConvertContext context)
        {
            var member = context.MemberAccessStack.Pop();

            context.Builder.Append(member.Member.Name);
            if (context.MemberAccessStack.Count > 0)
            {
                context.Builder.Append('.');
                ConvertToStringMemberStack(context);
            }

            context.MemberAccessStack.Push(member);
        }
        private static void ConvertToStringConditional(Expression exp, ConvertContext context)
        {
            var conditional = exp as ConditionalExpression;
            ConvertToString(conditional.Test, context);
            context.Builder.Append('?');
            ConvertToString(conditional.IfTrue, context);
            context.Builder.Append(':');
            ConvertToString(conditional.IfFalse, context);
        }

        private static void ConvertToStringValue(Type type, object value, ConvertContext context)
        {
            ConvertToStringValueRender(type, value, context);
            ConvertToStringValueStack(context);
        }
        private static void ConvertToStringValueRender(Type type, object value, ConvertContext context)
        {
            var typeDetails = TypeAnalyzer.GetTypeDetail(type);

            if (value == null)
            {
                context.Builder.Append("null");
                ConvertToStringValueStack(context);
                return;
            }

            if (typeDetails.IsNullable)
            {
                type = typeDetails.InnerTypes[0];
                typeDetails = typeDetails.InnerTypeDetails[0];
            }

            if (type.IsEnum)
            {
                context.Builder.Append(type.GetNiceName()).Append('.').Append(value);
                return;
            }

            if (type.IsArray)
            {
                Type arrayType = typeDetails.InnerTypes[0];
                context.Builder.Append('[');
                bool first = true;
                foreach (object item in (IEnumerable)value)
                {
                    if (!first)
                        context.Builder.Append(',');
                    ConvertToStringValue(arrayType, item, context);
                    first = false;
                }
                context.Builder.Append(']');
                return;
            }

            if (typeDetails.IsIEnumerableGeneric)
            {
                context.Builder.Append('[');
                bool first = true;
                foreach (object item in (IEnumerable)value)
                {
                    if (!first)
                        context.Builder.Append(',');
                    ConvertToStringValue(typeDetails.IEnumerableGenericInnerType, item, context);
                    first = false;
                }
                context.Builder.Append(']');
                return;
            }

            if (TypeLookup.CoreTypeLookup(type, out CoreType coreType))
            {
                switch (coreType)
                {
                    case CoreType.Boolean: context.Builder.Append((bool)value); return;
                    case CoreType.Byte: context.Builder.Append((byte)value); return;
                    case CoreType.SByte: context.Builder.Append((sbyte)value); return;
                    case CoreType.Int16: context.Builder.Append((short)value); return;
                    case CoreType.UInt16: context.Builder.Append((ushort)value); return;
                    case CoreType.Int32: context.Builder.Append((int)value); return;
                    case CoreType.UInt32: context.Builder.Append((uint)value); return;
                    case CoreType.Int64: context.Builder.Append((long)value); return;
                    case CoreType.UInt64: context.Builder.Append((ulong)value); return;
                    case CoreType.Single: context.Builder.Append((float)value); return;
                    case CoreType.Double: context.Builder.Append((double)value); return;
                    case CoreType.Decimal: context.Builder.Append((decimal)value); return;
                    case CoreType.Char: context.Builder.Append('\'').Append(value).Append('\''); return;
                    case CoreType.DateTime: context.Builder.Append("DateTime.Parse(\"").Append(value).Append("\")"); return;
                    case CoreType.DateTimeOffset: context.Builder.Append("DateTimeOffset.Parse(\"").Append(value).Append("\")"); return;
                    case CoreType.TimeSpan: context.Builder.Append("TimeSpan.Parse(\"").Append(value).Append("\")"); return;
                    case CoreType.Guid: context.Builder.Append('\'').Append(value.ToString()).Append('\''); return;
                    case CoreType.String: context.Builder.Append('\'').Append(((string)value).Replace("'", "''")).Append('\''); return;
                }
            }

            if (type == typeof(object))
            {
                context.Builder.Append('\'').Append(value.ToString().Replace("'", "''")).Append('\'');
                return;
            }

            throw new NotImplementedException($"{type.GetNiceName()} value {value?.ToString()} not converted");
        }
        private static void ConvertToStringValueStack(ConvertContext context)
        {
            if (context.MemberAccessStack.Count > 0)
            {
                var member = context.MemberAccessStack.Pop();

                context.Builder.Append('.');
                context.Builder.Append(member.Member.Name);

                if (context.MemberAccessStack.Count > 0)
                {
                    context.Builder.Append('.');
                    ConvertToStringMemberStack(context);
                }

                context.MemberAccessStack.Push(member);
            }
        }
    }
}

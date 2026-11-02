// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using Zerra.Repository.IO;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract partial class BaseLinqSqlConverter
    {
        private static Type? EnumOf(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            return underlying.IsEnum ? underlying : null;
        }
        private static bool IsEnumNumber(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            return IsInteger(underlying) || IsFraction(underlying);
        }
        private static Expression WithoutConvert(Expression exp)
        {
            while (exp is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
                exp = convert.Operand;
            return exp;
        }

        //an enum computed from a number, such as (Kind)x.Number or a date's DayOfWeek, is a number in SQL, a member of a model is its stored text
        private bool IsStoredEnum(Expression exp, MemberContext memberContext)
        {
            if (EnumOf(exp.Type) is null)
                return false;
            switch (exp)
            {
                case MemberExpression { Expression: not null } member:
                    if (memberContext.ResultColumns is not null)
                    {
                        if (member.Expression is ParameterExpression parameter && memberContext.ResultColumns.ContainsKey(parameter))
                            return memberContext.ResultEnums?.Contains(member.Member.Name) == true;
                        if (member.Expression is MemberExpression { Expression: ParameterExpression partParameter } whole && memberContext.ResultColumns.ContainsKey(partParameter))
                            return memberContext.ResultEnums?.Contains($"{whole.Member.Name}.{member.Member.Name}") == true;
                    }
                    if (Nullable.GetUnderlyingType(member.Expression.Type) is not null)
                        return IsStoredEnum(member.Expression, memberContext);
                    return !TypeLookup.GetCoreType(member.Expression.Type, out _) && !IsEvaluatable(member);
                case ConditionalExpression conditional:
                    return IsStoredEnum(WithoutConvert(conditional.IfTrue), memberContext) || IsStoredEnum(WithoutConvert(conditional.IfFalse), memberContext);
                case BinaryExpression { NodeType: ExpressionType.Coalesce } coalesce:
                    return IsStoredEnum(WithoutConvert(coalesce.Left), memberContext) || IsStoredEnum(WithoutConvert(coalesce.Right), memberContext);
                default:
                    return false;
            }
        }

        private static string? EnumLiteral(object? value, Type enumType)
        {
            if (value is null)
                return null;
            var item = value.GetType() == enumType ? value : Enum.ToObject(enumType, value);
            //a value the enum doesn't name can't be stored, its number matches nothing as C# would
            if (enumType.IsDefined(typeof(FlagsAttribute), false) || Enum.IsDefined(enumType, item))
                return EnumName.GetName(enumType, item);
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
        private bool TryEnumLiteral(Expression exp, Type enumType, out Expression literal)
        {
            literal = exp;
            if (!IsEvaluatable(exp))
                return false;
            var type = Nullable.GetUnderlyingType(exp.Type) ?? exp.Type;
            if (type != enumType && !IsInteger(type))
                return false;
            var value = Evaluate(exp);
            if (value is null)
                return false;
            //a number beyond the enum's range is compared as a number
            if (type != enumType && Convert.ToDecimal(Enum.ToObject(enumType, value), CultureInfo.InvariantCulture) != Convert.ToDecimal(value, CultureInfo.InvariantCulture))
                return false;
            literal = Expression.Constant(EnumLiteral(value, enumType), typeof(string));
            return true;
        }

        //C# compiles a comparison of enums to one of numbers, text is compared only for equality so an index is used and order is by the numbers
        private void ConvertEnumComparison(Operator operation, ref Expression left, ref Expression right, MemberContext memberContext)
        {
            var bareLeft = WithoutConvert(left);
            var bareRight = WithoutConvert(right);
            var leftStored = IsStoredEnum(bareLeft, memberContext);
            var rightStored = IsStoredEnum(bareRight, memberContext);
            if (!leftStored && !rightStored)
                return;
            if (operation is Operator.Equals or Operator.NotEquals)
            {
                var enumType = EnumOf((leftStored ? bareLeft : bareRight).Type)!;
                if (leftStored && rightStored)
                {
                    if (EnumOf(bareRight.Type) == enumType)
                    {
                        left = bareLeft;
                        right = bareRight;
                        return;
                    }
                }
                else if (leftStored && TryEnumLiteral(bareRight, enumType, out var rightLiteral))
                {
                    left = bareLeft;
                    right = rightLiteral;
                    return;
                }
                else if (rightStored && TryEnumLiteral(bareLeft, enumType, out var leftLiteral))
                {
                    left = leftLiteral;
                    right = bareRight;
                    return;
                }
            }
            left = EnumAsNumber(left);
            right = EnumAsNumber(right);
        }

        private static Expression EnumAsNumber(Expression exp)
            => EnumOf(exp.Type) is { } enumType ? Expression.Convert(exp, Enum.GetUnderlyingType(enumType)) : exp;

        private void ConvertToSqlEnumNumber(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            var enumType = EnumOf(exp.Type)!;
            if (enumType.IsDefined(typeof(FlagsAttribute), false))
                throw new NotSupportedException($"{enumType.Name} has flags, stored as text it can't be used as a number or sorted");
            var values = Enum.GetValuesAsUnderlyingType(enumType);
            sb.Write("(CASE ");
            ConvertToSql(exp, ref sb, context);
            foreach (var value in values)
            {
                sb.Write(" WHEN ");
                ConvertToSqlValue(typeof(string), EnumName.GetName(enumType, value), ref sb, context);
                sb.Write(" THEN ");
                sb.Write(Convert.ToString(value, CultureInfo.InvariantCulture)!);
            }
            sb.Write(" END)");
        }

        //the text of an enum expression, typed object as C# has no conversion of an enum to text to build
        private Expression EnumAsText(Expression exp, Type enumType, MemberContext memberContext)
        {
            var bare = WithoutConvert(exp);
            if (IsEvaluatable(bare))
            {
                var literal = EnumLiteral(Evaluate(bare), enumType);
                return literal is null ? Expression.Constant(null, typeof(object)) : Expression.Convert(Expression.Constant(literal, typeof(string)), typeof(object));
            }
            switch (bare)
            {
                case ConditionalExpression conditional:
                    return Expression.Condition(conditional.Test, EnumAsText(conditional.IfTrue, enumType, memberContext), EnumAsText(conditional.IfFalse, enumType, memberContext), typeof(object));
                case BinaryExpression { NodeType: ExpressionType.Coalesce } coalesce:
                    return Expression.Coalesce(EnumAsText(coalesce.Left, enumType, memberContext), EnumAsText(coalesce.Right, enumType, memberContext));
            }
            if (IsStoredEnum(bare, memberContext))
                return Expression.Convert(bare, typeof(object));

            if (enumType.IsDefined(typeof(FlagsAttribute), false))
                throw new NotSupportedException($"{enumType.Name} has flags, a value computed from a number can't be written as its text");
            var underlying = Enum.GetUnderlyingType(enumType);
            var number = Nullable.GetUnderlyingType(bare.Type) == underlying || bare.Type == underlying ? bare : Expression.Convert(bare, underlying);
            var values = Enum.GetValuesAsUnderlyingType(enumType);
            Expression text = Expression.Constant(null, typeof(object));
            for (var i = values.Length - 1; i >= 0; i--)
            {
                var value = values.GetValue(i)!;
                text = Expression.Condition(Expression.Equal(number, Expression.Constant(value, number.Type)), Expression.Convert(Expression.Constant(EnumName.GetName(enumType, value), typeof(string)), typeof(object)), text, typeof(object));
            }
            return text;
        }

        /// <summary>
        /// Gets the list a stored enum is looked for in, with its values as the text that is stored.
        /// </summary>
        /// <param name="list">The expression of the list.</param>
        /// <param name="value">The expression looked for in the list.</param>
        /// <param name="context">The current builder context.</param>
        /// <returns>The expression of the list to write.</returns>
        protected Expression EnumList(Expression list, Expression value, BuilderContext context)
        {
            if (EnumOf(value.Type) is not { } enumType || !IsStoredEnum(WithoutConvert(value), context.MemberContext) || !IsEvaluatable(list))
                return list;
            var names = new List<string?>();
            foreach (var item in (IEnumerable)Evaluate(list)!)
                names.Add(EnumLiteral(item, enumType));
            return Expression.Constant(names.ToArray(), typeof(string[]));
        }

        /// <summary>
        /// Gets the expression to assign to a member, which is the text that is stored for an enum.
        /// </summary>
        /// <param name="value">The expression assigned.</param>
        /// <param name="member">The member assigned to.</param>
        /// <param name="memberContext">The current member context.</param>
        /// <returns>The expression to write.</returns>
        protected Expression EnumAssigned(Expression value, ModelMemberDetail member, MemberContext memberContext)
            => member.EnumType is null ? value : EnumAsText(value, member.EnumType, memberContext);

        /// <summary>
        /// Notes when a member of a grouped result is an enum's stored text.
        /// </summary>
        /// <param name="name">The name of the member in the result.</param>
        /// <param name="column">The column computing the member.</param>
        /// <param name="plan">The plan of the statement.</param>
        /// <param name="memberContext">The current member context.</param>
        protected void AddResultEnum(string name, LinqColumn column, LinqPlan plan, MemberContext memberContext)
        {
            if (column.Kind is LinqColumnKind.Min or LinqColumnKind.Max)
                return;
            var expression = column.Kind == LinqColumnKind.KeyPart ? plan.KeyParts![column.KeyPart] : column.Expression;
            if (column.Member is { EnumType: not null } || (expression is not null && IsStoredEnum(WithoutConvert(expression), memberContext)))
                (memberContext.ResultEnums ??= new()).Add(name);
        }

        //C# gives the field's name where the column holds the name from EnumName
        private string StoredEnumToString(string value, Type enumType, BuilderContext context)
        {
            if (enumType.IsDefined(typeof(FlagsAttribute), false))
                throw new NotSupportedException($"{enumType.Name} has flags, the store can't write them the way ToString does");
            var names = Enum.GetNames(enumType);
            var values = Enum.GetValuesAsUnderlyingType(enumType);
            var cases = new System.Text.StringBuilder("CASE ").Append(value);
            var differs = false;
            for (var i = 0; i < names.Length; i++)
            {
                var stored = EnumName.GetName(enumType, values.GetValue(i)!);
                differs |= stored != names[i];
                _ = cases.Append(" WHEN ").Append(Argument(Expression.Constant(stored, typeof(string)), context)).Append(" THEN ").Append(Argument(Expression.Constant(names[i], typeof(string)), context));
            }
            return differs ? cases.Append(" END").ToString() : value;
        }
    }
}

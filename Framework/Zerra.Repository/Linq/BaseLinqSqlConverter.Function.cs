// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Repository.Reflection;
using Zerra.Repository.IO;

namespace Zerra.Repository
{
    public abstract partial class BaseLinqSqlConverter
    {
        /// <summary>
        /// A function the store computes, whose SQL each dialect gives as a template with its arguments as {0}, {1}, and so on.
        /// </summary>
        protected enum Function : byte
        {
            /// <summary>A number as text.</summary>
            NumberText,
            /// <summary>A bool member as True or False.</summary>
            BoolText,
            /// <summary>A Guid as text in lower case.</summary>
            GuidText,
            /// <summary>Text as a Guid.</summary>
            GuidParse,
            /// <summary>Pads {0} on the left to {1} characters with {2}, leaving longer text as it is.</summary>
            PadLeft,
            /// <summary>Pads {0} on the right to {1} characters with {2}, leaving longer text as it is.</summary>
            PadRight,
            /// <summary>Concatenates {0} and {1}.</summary>
            Concat,
            /// <summary>Adds {1} microseconds to the date and time {0}.</summary>
            DateAddMicroseconds,
            /// <summary>Adds {1} months to the date and time {0}.</summary>
            DateAddMonths,
            /// <summary>Adds {1} days to the date {0}.</summary>
            DateOnlyAddDays,
            /// <summary>Adds {1} months to the date {0}.</summary>
            DateOnlyAddMonths,
            /// <summary>Adds {1} microseconds to the time {0}, wrapping around midnight.</summary>
            TimeAddMicroseconds,
            /// <summary>The microseconds from the date and time {1} to {0}.</summary>
            DateDiffMicroseconds,
            /// <summary>The microseconds from the time {1} to {0}.</summary>
            TimeDiffMicroseconds,
            /// <summary>The microseconds in a stored time span {0}.</summary>
            SpanMicroseconds,
            /// <summary>{0} divided by {1}, dropping the fraction toward zero.</summary>
            IntegerDivide,
            /// <summary>The remainder of {0} divided by {1}.</summary>
            Modulo,
            /// <summary>{0} rounded to the whole number nearest.</summary>
            RoundWhole,
            /// <summary>{0} rounded to {1} decimal places.</summary>
            RoundPlaces,
            /// <summary>The date and time {0} as text in the dialect's format {1}.</summary>
            DateText,
            /// <summary>Ten to the power {0}, of a type that keeps a decimal's digits when it scales one.</summary>
            PowerOfTen
        }

        /// <summary>Gets a dialect's SQL for a function.</summary>
        /// <param name="function">The function.</param>
        /// <returns>The SQL with its arguments as {0}, {1}, and so on.</returns>
        protected abstract string FunctionTemplate(Function function);
        /// <summary>Gets the type a dialect casts a value to, for a CAST expression.</summary>
        /// <param name="type">The CLR type: an integer, a floating point, a decimal, or a string.</param>
        /// <returns>The SQL type.</returns>
        protected abstract string CastType(Type type);
        /// <summary>Gets the SQL type for a number with a fixed count of decimal places.</summary>
        /// <param name="places">The decimal places.</param>
        /// <returns>The SQL type.</returns>
        protected virtual string FixedType(int places) => $"DECIMAL(38,{places})";
        /// <summary>Converts a custom .NET date format to the dialect's, refusing what it can't say.</summary>
        /// <param name="format">The .NET format.</param>
        /// <returns>The format as a SQL literal.</returns>
        protected abstract string DateFormat(string format);
        /// <summary>Writes a dialect's aggregate joining text.</summary>
        /// <param name="value">The SQL of the value, null safe.</param>
        /// <param name="separator">The SQL of the separator.</param>
        /// <param name="constantSeparator">Whether the separator is a literal rather than read from a row.</param>
        /// <param name="order">The SQL of the order, or <see langword="null"/>.</param>
        /// <returns>The SQL of the aggregate.</returns>
        protected abstract string StringAggregate(string value, string separator, bool constantSeparator, string? order);

        private static string Fill(string template, params string[] arguments)
        {
            for (var i = 0; i < arguments.Length; i++)
                template = template.Replace($"{{{i}}}", arguments[i]);
            return template;
        }
        private string Fn(Function function, params string[] arguments) => Fill(FunctionTemplate(function), arguments);
        private string Cast(string value, Type type) => $"CAST({value} AS {CastType(type)})";

        //each mode from FLOOR and CEILING, which every store does exactly, where a store's ROUND of a double can round a midpoint to even and of a number away from zero
        /// <summary>
        /// Converts <c>Math.Round</c> with its digits and <see cref="MidpointRounding"/>, to even when none is given as in C#.
        /// </summary>
        /// <param name="call">The call of <c>Math.Round</c>.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        protected void ConvertToSqlRound(MethodCallExpression call, ref CharWriter sb, BuilderContext context)
        {
            Expression? digits = null;
            Expression? modeArgument = null;
            if (call.Arguments.Count == 2)
            {
                if (call.Arguments[1].Type == typeof(MidpointRounding))
                    modeArgument = call.Arguments[1];
                else
                    digits = call.Arguments[1];
            }
            else if (call.Arguments.Count == 3)
            {
                digits = call.Arguments[1];
                modeArgument = call.Arguments[2];
            }
            else if (call.Arguments.Count != 1)
            {
                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
            }

            if (modeArgument is not null && !IsEvaluatable(modeArgument))
                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}, the {nameof(MidpointRounding)} must be known when the query is written");
            var mode = modeArgument is null ? MidpointRounding.ToEven : (MidpointRounding)Evaluate(modeArgument)!;
            string? scale = null;
            if (digits is not null)
            {
                if (IsEvaluatable(digits))
                {
                    var places = (int)Evaluate(digits)!;
                    if (places < 0 || places > 28)
                        throw new ArgumentOutOfRangeException(nameof(digits), places, "Rounding digits must be between 0 and 28");
                    if (places > 0)
                        scale = "1" + new string('0', places);
                }
                else
                {
                    scale = Fn(Function.PowerOfTen, Argument(digits, context));
                }
            }

            var value = Argument(call.Arguments[0], context);
            var scaled = scale is null ? $"({value})" : $"(({value})*{scale})";
            //the midpoint is found by doubling, a fraction taken by subtracting FLOOR loses its digits on SQL Server where the value has the most precision
            var rounded = mode switch
            {
                MidpointRounding.ToEven => $"(FLOOR({scaled})+CASE WHEN 2*{scaled}>2*FLOOR({scaled})+1 THEN 1 WHEN 2*{scaled}<2*FLOOR({scaled})+1 THEN 0 ELSE FLOOR({scaled})-2*FLOOR(FLOOR({scaled})/2) END)",
                MidpointRounding.AwayFromZero => $"(FLOOR({scaled})+CASE WHEN 2*{scaled}>2*FLOOR({scaled})+1 THEN 1 WHEN 2*{scaled}<2*FLOOR({scaled})+1 THEN 0 WHEN {scaled}<0 THEN 0 ELSE 1 END)",
                MidpointRounding.ToZero => $"(CASE WHEN {scaled}<0 THEN CEILING({scaled}) ELSE FLOOR({scaled}) END)",
                MidpointRounding.ToNegativeInfinity => $"FLOOR({scaled})",
                MidpointRounding.ToPositiveInfinity => $"CEILING({scaled})",
                _ => throw new NotSupportedException($"Cannot convert call expression {call.Method.Name} with {mode}"),
            };
            sb.Write(scale is null ? rounded : $"({rounded}/{scale})");
        }

        //an argument of a function, read as a value whatever is around the call
        private string Argument(Expression expression, BuilderContext context)
        {
            var sb = new CharWriter();
            try
            {
                var invertStack = context.InvertStack;
                context.InvertStack = 0;
                context.MemberContext.OperatorStack.Push(Operator.Call);
                ConvertToSql(expression, ref sb, context);
                _ = context.MemberContext.OperatorStack.Pop();
                context.InvertStack = invertStack;
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }
        }

        /// <summary>Gets whether a call looks in another query, which is a subquery of the statement.</summary>
        /// <param name="call">The call.</param>
        /// <returns>Whether the call looks in a query.</returns>
        protected static bool LooksInQuery(MethodCallExpression call) => call.Object is ConstantExpression { Value: LinqSubquery };
        private static Type Underlying(Type type) => Nullable.GetUnderlyingType(type) ?? type;
        private static bool IsInteger(Type type) => type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort);
        private static bool IsFraction(Type type) => type == typeof(decimal) || type == typeof(double) || type == typeof(float);
        private static bool IsDate(Type type) => type == typeof(DateTime) || type == typeof(DateTimeOffset);
        private static Expression Unconverted(Expression expression)
        {
            while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion)
                expression = conversion.Operand;
            return expression;
        }

        /// <summary>
        /// Writes a call the store computes the same way in every dialect, from each dialect's SQL for it: text, numbers, dates, and times.
        /// </summary>
        /// <param name="call">The call.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        /// <returns>Whether the call was written.</returns>
        protected bool TryConvertToSqlFunction(MethodCallExpression call, ref CharWriter sb, BuilderContext context)
        {
            var sql = FunctionSql(call, context);
            if (sql is null)
                return false;
            sb.Write(sql);
            return true;
        }

        private string? FunctionSql(MethodCallExpression call, BuilderContext context)
        {
            var declaring = call.Method.DeclaringType;
            if (call.Method.Name == "ToString" && call.Object is not null)
                return ToStringSql(call, context);

            if (declaring == typeof(string))
            {
                switch (call.Method.Name)
                {
                    case "PadLeft":
                    case "PadRight":
                        {
                            var fill = call.Arguments.Count == 2 ? (char)Evaluate(call.Arguments[1])! : ' ';
                            return Fn(call.Method.Name == "PadLeft" ? Function.PadLeft : Function.PadRight, Argument(call.Object!, context), Argument(call.Arguments[0], context), $"'{fill.ToString().Replace("'", "''")}'");
                        }
                    case "Join":
                        return StringJoinSql(call, context);
                }
                return null;
            }
            if (declaring == typeof(Convert))
                return ConvertSql(call, context);
            if (call.Method.Name == "Parse" && call.Method.IsStatic && call.Arguments.Count == 1 && call.Arguments[0].Type == typeof(string))
            {
                if (declaring == typeof(Guid))
                    return Fn(Function.GuidParse, Argument(call.Arguments[0], context));
                if (declaring is not null && (IsInteger(declaring) || IsFraction(declaring)))
                    return Cast(Argument(call.Arguments[0], context), declaring);
                return null;
            }
            if (declaring is not null && (IsDate(declaring) || declaring == typeof(DateOnly) || declaring == typeof(TimeOnly)) && call.Object is not null)
                return DateCallSql(call, declaring, context);
            if (declaring == typeof(TimeSpan) && call.Method.IsStatic && call.Method.Name.StartsWith("From", StringComparison.Ordinal))
                throw new NotSupportedException($"TimeSpan.{call.Method.Name} of a value of the model is read through a total, such as (x.End - x.Start).TotalHours");
            return null;
        }

        private string ToStringSql(MethodCallExpression call, BuilderContext context)
        {
            var type = Underlying(call.Object!.Type);
            var nullable = Nullable.GetUnderlyingType(call.Object.Type) is not null;
            var value = Argument(call.Object, context);
            string text;
            if (call.Arguments.Count == 1 && call.Arguments[0].Type == typeof(string))
            {
                var format = (string?)Evaluate(call.Arguments[0]);
                if (String.IsNullOrEmpty(format))
                    throw new NotSupportedException("ToString with a format takes a constant format");
                text = FormattedSql(value, type, format);
            }
            else if (call.Arguments.Count != 0)
            {
                throw new NotSupportedException($"ToString on {type.Name} with those arguments isn't supported, the store has no culture to format with");
            }
            else if (type == typeof(string))
            {
                return value;
            }
            else if (type == typeof(bool))
            {
                text = Fn(Function.BoolText, value);
            }
            else if (type == typeof(Guid))
            {
                text = Fn(Function.GuidText, value);
            }
            else if (type.IsEnum && IsStoredEnum(WithoutConvert(call.Object), context.MemberContext))
            {
                text = StoredEnumToString(value, type, context);
            }
            else if (type.IsEnum)
            {
                var names = Enum.GetNames(type);
                var values = Enum.GetValuesAsUnderlyingType(type);
                var cases = new System.Text.StringBuilder("CASE ").Append(value);
                for (var i = 0; i < names.Length; i++)
                    _ = cases.Append(" WHEN ").Append(values.GetValue(i)).Append(" THEN '").Append(names[i]).Append('\'');
                text = cases.Append(" END").ToString();
            }
            else if (IsInteger(type) || IsFraction(type))
            {
                text = Fn(Function.NumberText, value);
            }
            else
            {
                throw new NotSupportedException($"ToString on {type.Name} depends on a culture the store doesn't have, so give a format such as ToString(\"yyyy-MM-dd\")");
            }
            //a nullable with no value is empty text in C#
            return nullable ? $"COALESCE({text},'')" : text;
        }

        private string FormattedSql(string value, Type type, string format)
        {
            if (IsDate(type) || type == typeof(DateOnly))
            {
                if (format.Length == 1)
                    throw new NotSupportedException($"The date format {format} depends on a culture the store doesn't have, so spell it out such as yyyy-MM-dd");
                return Fn(Function.DateText, value, DateFormat(format));
            }
            if ((IsInteger(type) || IsFraction(type)) && format.Length >= 2 && Int32.TryParse(format.AsSpan(1), out var digits))
            {
                switch (Char.ToUpperInvariant(format[0]))
                {
                    case 'F':
                        return Fn(Function.NumberText, $"CAST({Fn(Function.RoundPlaces, value, digits.ToString())} AS {FixedType(digits)})");
                    case 'D' when IsInteger(type):
                        return Fn(Function.PadLeft, Fn(Function.NumberText, value), digits.ToString(), "'0'");
                }
            }
            throw new NotSupportedException($"The format {format} on {type.Name} isn't supported, numbers take F2 or D4 and dates a custom format such as yyyy-MM-dd");
        }

        private string? ConvertSql(MethodCallExpression call, BuilderContext context)
        {
            if (call.Arguments.Count != 1)
                throw new NotSupportedException($"Convert.{call.Method.Name} with those arguments isn't supported");
            var source = Underlying(Unconverted(call.Arguments[0]).Type);
            var value = Argument(call.Arguments[0], context);
            var target = call.Method.ReturnType;
            if (target == typeof(string))
            {
                if (source == typeof(string))
                    return value;
                if (IsInteger(source) || IsFraction(source))
                    return Fn(Function.NumberText, value);
                if (source == typeof(bool))
                    return Fn(Function.BoolText, value);
                if (source == typeof(Guid))
                    return Fn(Function.GuidText, value);
                throw new NotSupportedException($"Convert.ToString of {source.Name} depends on a culture the store doesn't have");
            }
            if (target == typeof(bool))
                return $"({value})<>0";
            if (IsInteger(target))
            {
                //C# rounds to the whole number where a cast drops the fraction, and a midpoint to the even number where SQL rounds it away from zero
                if (IsFraction(source))
                    return Cast($"CASE WHEN ({value})-FLOOR({value})=0.5 THEN 2*{Fn(Function.RoundWhole, $"({value})/2")} ELSE {Fn(Function.RoundWhole, value)} END", target);
                return Cast(value, target);
            }
            if (IsFraction(target))
                return Cast(value, target);
            return null;
        }

        private string? DateCallSql(MethodCallExpression call, Type declaring, BuilderContext context)
        {
            var value = Argument(call.Object!, context);
            string Micros(Expression amount, long scale) => Cast(Fn(Function.RoundWhole, $"({Argument(amount, context)})*{scale}.0"), typeof(long));
            if (declaring == typeof(DateOnly))
            {
                switch (call.Method.Name)
                {
                    case "AddDays": return Fn(Function.DateOnlyAddDays, value, Argument(call.Arguments[0], context));
                    case "AddMonths": return Fn(Function.DateOnlyAddMonths, value, Argument(call.Arguments[0], context));
                    case "AddYears": return Fn(Function.DateOnlyAddMonths, value, $"({Argument(call.Arguments[0], context)})*12");
                }
                return null;
            }
            var add = declaring == typeof(TimeOnly) ? Function.TimeAddMicroseconds : Function.DateAddMicroseconds;
            switch (call.Method.Name)
            {
                case "AddHours": return Fn(add, value, Micros(call.Arguments[0], 3_600_000_000));
                case "AddMinutes": return Fn(add, value, Micros(call.Arguments[0], 60_000_000));
                case "Add": return Fn(add, value, SpanMicroseconds(call.Arguments[0], context));
            }
            if (declaring == typeof(TimeOnly))
                return null;
            switch (call.Method.Name)
            {
                case "AddDays": return Fn(add, value, Micros(call.Arguments[0], 86_400_000_000));
                case "AddSeconds": return Fn(add, value, Micros(call.Arguments[0], 1_000_000));
                case "AddMilliseconds": return Fn(add, value, Micros(call.Arguments[0], 1_000));
                case "AddMonths": return Fn(Function.DateAddMonths, value, Argument(call.Arguments[0], context));
                case "AddYears": return Fn(Function.DateAddMonths, value, $"({Argument(call.Arguments[0], context)})*12");
                case "Subtract":
                    return call.Arguments[0].Type == typeof(TimeSpan)
                        ? Fn(add, value, $"-({SpanMicroseconds(call.Arguments[0], context)})")
                        : throw new NotSupportedException("The time between two dates is read through a total, such as (x.End - x.Start).TotalHours");
            }
            return null;
        }

        //whether a time span is computed, rather than a stored or constant value the rest of the conversion already reads
        private bool IsComputedSpan(Expression expression)
        {
            expression = Unconverted(expression);
            if (Underlying(expression.Type) != typeof(TimeSpan) || IsEvaluatable(expression))
                return false;
            if (expression is BinaryExpression { NodeType: ExpressionType.Add or ExpressionType.AddChecked or ExpressionType.Subtract or ExpressionType.SubtractChecked })
                return true;
            return expression is MethodCallExpression { Method.Name: "Add" or "Subtract" };
        }

        //a time span as a count of microseconds, so sums, differences, totals, and comparisons are plain arithmetic in every dialect
        private string SpanMicroseconds(Expression expression, BuilderContext context)
        {
            expression = Unconverted(expression);
            if (IsEvaluatable(expression))
                return (((TimeSpan?)Evaluate(expression))?.Ticks / 10 ?? 0).ToString();
            switch (expression)
            {
                case BinaryExpression { NodeType: ExpressionType.Subtract or ExpressionType.SubtractChecked } difference:
                    return BetweenOrDifference(difference.Left, difference.Right, context);
                case BinaryExpression { NodeType: ExpressionType.Add or ExpressionType.AddChecked } sum:
                    return $"({SpanMicroseconds(sum.Left, context)}+{SpanMicroseconds(sum.Right, context)})";
                case MethodCallExpression { Method.Name: "Subtract", Object: not null } call when call.Arguments.Count == 1:
                    return BetweenOrDifference(call.Object, call.Arguments[0], context);
                case MethodCallExpression { Method.Name: "Add", Object: not null } call when call.Arguments.Count == 1 && call.Object.Type == typeof(TimeSpan):
                    return $"({SpanMicroseconds(call.Object, context)}+{SpanMicroseconds(call.Arguments[0], context)})";
            }
            return Fn(Function.SpanMicroseconds, Argument(expression, context));
        }
        private string BetweenOrDifference(Expression left, Expression right, BuilderContext context)
        {
            var type = Underlying(Unconverted(left).Type);
            if (IsDate(type))
                return Fn(Function.DateDiffMicroseconds, Argument(left, context), Argument(right, context));
            if (type == typeof(TimeOnly))
                return Fn(Function.TimeDiffMicroseconds, Argument(left, context), Argument(right, context));
            return $"({SpanMicroseconds(left, context)}-{SpanMicroseconds(right, context)})";
        }

        //a date or time plus or minus a time span
        private bool TryConvertToSqlDateArithmetic(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            var binary = (BinaryExpression)exp;
            if (IsEvaluatable(exp) || binary.Right.Type != typeof(TimeSpan))
                return false;
            var left = Underlying(binary.Left.Type);
            if (!IsDate(left))
            {
                if (IsComputedSpan(exp))
                    throw new NotSupportedException("A computed time span is read through a total, such as (x.End - x.Start).TotalHours, or compared to another time span");
                return false;
            }
            var amount = SpanMicroseconds(binary.Right, context);
            if (binary.NodeType is ExpressionType.Subtract or ExpressionType.SubtractChecked)
                amount = $"-({amount})";
            sb.Write(Fn(Function.DateAddMicroseconds, Argument(binary.Left, context), amount));
            return true;
        }
        private bool IsDateDifference(Expression exp)
            => exp is BinaryExpression { NodeType: ExpressionType.Subtract or ExpressionType.SubtractChecked } binary && binary.Right.Type != typeof(TimeSpan) && exp.Type == typeof(TimeSpan) && !IsEvaluatable(exp);

        //a comparison where a side is a computed time span compares the counts of microseconds
        private bool TryConvertToSqlSpanComparison(Operator operation, Expression exp, ref CharWriter sb, BuilderContext context)
        {
            var binary = (BinaryExpression)exp;
            if (!IsComputedSpan(binary.Left) && !IsComputedSpan(binary.Right))
                return false;
            sb.Write('(');
            sb.Write(SpanMicroseconds(binary.Left, context));
            sb.Write(')');
            sb.Write(OperatorToString(operation, typeof(long)));
            sb.Write('(');
            sb.Write(SpanMicroseconds(binary.Right, context));
            sb.Write(')');
            return true;
        }

        //a total or a part of a computed time span
        private bool TryConvertToSqlSpanMember(MemberExpression member, ref CharWriter sb, BuilderContext context)
        {
            if (member.Member.DeclaringType != typeof(TimeSpan) || member.Expression is null || !IsComputedSpan(member.Expression))
                return false;
            var micros = SpanMicroseconds(member.Expression, context);
            string Total(long scale) => $"(CAST({micros} AS {CastType(typeof(double))})/{scale}.0)";
            string Part(long scale, long within) => Fn(Function.Modulo, Fn(Function.IntegerDivide, micros, scale.ToString()), within.ToString());
            sb.Write(member.Member.Name switch
            {
                "TotalDays" => Total(86_400_000_000),
                "TotalHours" => Total(3_600_000_000),
                "TotalMinutes" => Total(60_000_000),
                "TotalSeconds" => Total(1_000_000),
                "TotalMilliseconds" => Total(1_000),
                "Days" => Fn(Function.IntegerDivide, micros, "86400000000"),
                "Hours" => Part(3_600_000_000, 24),
                "Minutes" => Part(60_000_000, 60),
                "Seconds" => Part(1_000_000, 60),
                "Milliseconds" => Part(1_000, 1000),
                "Ticks" => $"({micros}*10)",
                _ => throw new NotSupportedException($"{member.Member.Name} of a computed time span isn't supported"),
            });
            return true;
        }

        private string StringJoinSql(MethodCallExpression call, BuilderContext context)
        {
            if (call.Arguments.Count != 2)
                throw new NotSupportedException("string.Join takes a separator and values, as new[] { .. } or the rows of a related collection");
            var separator = Unconverted(call.Arguments[0]);
            var values = Unconverted(call.Arguments[1]);
            if (values is NewArrayExpression array)
            {
                var separatorSql = Argument(separator, context);
                var joined = "''";
                for (var i = 0; i < array.Expressions.Count; i++)
                {
                    var item = $"COALESCE({Argument(array.Expressions[i], context)},'')";
                    joined = i == 0 ? item : Fn(Function.Concat, Fn(Function.Concat, joined, separatorSql), item);
                }
                return joined;
            }
            return RelatedStringJoinSql(separator, values, context);
        }

        //the rows of a related collection joined into one text by a subquery matching them to the row
        private string RelatedStringJoinSql(Expression separator, Expression values, BuilderContext context)
        {
            LambdaExpression? selector = null;
            LambdaExpression? where = null;
            var orders = new List<(LambdaExpression Key, bool Descending)>();
            var source = values;
            if (source is MethodCallExpression { Method.Name: "Select" } select && select.Method.DeclaringType == typeof(Enumerable) && select.Arguments.Count == 2)
            {
                selector = Lambda(select.Arguments[1]);
                source = select.Arguments[0];
            }
            while (source is MethodCallExpression chain && chain.Method.DeclaringType == typeof(Enumerable) && chain.Arguments.Count == 2)
            {
                if (chain.Method.Name == "Where")
                {
                    var condition = Lambda(chain.Arguments[1]);
                    where = where is null ? condition : LinqQuery.AppendAnd(condition, where);
                }
                else if (chain.Method.Name is "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending")
                {
                    orders.Insert(0, (Lambda(chain.Arguments[1]), chain.Method.Name.EndsWith("Descending", StringComparison.Ordinal)));
                }
                else
                {
                    break;
                }
                source = chain.Arguments[0];
            }
            if (selector is null || source is not MemberExpression { Expression: not null } member)
                throw new NotSupportedException("string.Join takes values as new[] { .. } or the rows of a related collection selected to text, such as x.Lines.Select(l => l.Name)");

            var parentModel = ModelAnalyzer.GetModel(member.Expression.Type);
            var relation = parentModel.GetMember(member.Member.Name);
            var relationKey = RelationKey(relation, parentModel);
            var relatedModel = ModelAnalyzer.GetModel(relation.ActualType);

            var sbNone = new CharWriter();
            try
            {
                context.MemberContext.InCallNoRender++;
                ConvertToSqlMember(member, ref sbNone, context);
                context.MemberContext.InCallNoRender--;
            }
            finally
            {
                sbNone.Dispose();
            }
            var parentTable = RelationTable(member.Expression, context);

            //a separator a row gives is read from the row outside the subquery
            var constantSeparator = IsEvaluatable(separator);
            var separatorSql = Argument(separator, context);

            var dependant = new ParameterDependant(relatedModel, null);
            var inner = new BuilderContext(dependant, context.MemberContext);
            string value;
            string? order = null;
            string? whereSql = null;
            context.MemberContext.DependantStack.Push(dependant);
            context.MemberContext.ModelStack.Push(relatedModel);
            context.MemberContext.ModelContexts.Add(selector.Parameters[0], relatedModel);
            try
            {
                value = $"COALESCE({Argument(selector.Body, inner)},'')";
                _ = context.MemberContext.ModelContexts.Remove(selector.Parameters[0]);
                if (orders.Count > 0)
                {
                    var parts = new List<string>();
                    foreach (var (key, descending) in orders)
                    {
                        context.MemberContext.ModelContexts.Add(key.Parameters[0], relatedModel);
                        parts.Add(descending ? $"{Argument(key.Body, inner)} DESC" : Argument(key.Body, inner));
                        _ = context.MemberContext.ModelContexts.Remove(key.Parameters[0]);
                    }
                    order = String.Join(",", parts);
                }
                if (where is not null)
                {
                    var sbWhere = new CharWriter();
                    try
                    {
                        context.MemberContext.OperatorStack.Push(Operator.Lambda);
                        context.MemberContext.ModelContexts.Add(where.Parameters[0], relatedModel);
                        ConvertToSql(where.Body, ref sbWhere, inner);
                        _ = context.MemberContext.ModelContexts.Remove(where.Parameters[0]);
                        _ = context.MemberContext.OperatorStack.Pop();
                        whereSql = sbWhere.ToString();
                    }
                    finally
                    {
                        sbWhere.Dispose();
                    }
                }
            }
            finally
            {
                _ = context.MemberContext.ModelStack.Pop();
                _ = context.MemberContext.DependantStack.Pop();
            }

            var sb = new CharWriter();
            try
            {
                sb.Write("(SELECT COALESCE(");
                sb.Write(StringAggregate(value, separatorSql, constantSeparator, order));
                sb.Write(",'')");
                GenerateFrom(relatedModel, ref sb);
                GenerateJoin(dependant, ref sb);
                sb.Write(" WHERE ");
                for (var i = 0; i < relationKey.Length; i++)
                {
                    if (i > 0)
                        sb.Write(" AND ");
                    sb.Write(QuoteColumn(parentTable, relationKey[i].Identity.StoreName));
                    sb.Write('=');
                    sb.Write(QuoteColumn(dependant.TableName, relationKey[i].Key.StoreName));
                }
                if (whereSql is not null)
                {
                    sb.Write(" AND(");
                    sb.Write(whereSql);
                    sb.Write(')');
                }
                sb.Write(')');
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }
        }

        /// <summary>Gets a column of a table named as the dialect quotes it.</summary>
        /// <param name="table">The name of the table in the statement.</param>
        /// <param name="column">The name of the column.</param>
        /// <returns>The SQL of the column.</returns>
        protected abstract string QuoteColumn(string table, string column);

        private static LambdaExpression Lambda(Expression argument)
            => (LambdaExpression)(argument.NodeType == ExpressionType.Quote ? ((UnaryExpression)argument).Operand : argument);
    }
}

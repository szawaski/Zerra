using System.Linq.Expressions;
using Zerra.Reflection;
using Zerra.Repository.IO;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Linq
{
    public abstract partial class BaseLinqSqlConverter
    {
        /// <summary>
        /// A GroupJoin's group, read by an aggregate of it in a subquery of the second query, whose rows are those with the key of the first query's row.
        /// </summary>
        protected sealed class JoinGroup
        {
            /// <summary>The statement of the second query.</summary>
            public string Sql { get; }
            /// <summary>The columns of the second query's elements, by the members naming them.</summary>
            public Dictionary<string, string> Columns { get; }
            /// <summary>The members of <see cref="Columns"/> that are an enum's stored text.</summary>
            public HashSet<string> Enums { get; }
            /// <summary>The join.</summary>
            public LinqJoin Join { get; }

            internal JoinGroup(string sql, Dictionary<string, string> columns, HashSet<string> enums, LinqJoin join)
            {
                this.Sql = sql;
                this.Columns = columns;
                this.Enums = enums;
                this.Join = join;
            }
        }

        /// <summary>
        /// Writes the place of a row in an order, from 1.
        /// </summary>
        /// <param name="orders">The order, or <see langword="null"/> for the order the store reads the rows in.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="rootDependant">The root parameter dependant used for join tracking.</param>
        /// <param name="operationContext">The current member context for the operation.</param>
        protected void ConvertRowNumber(LinqOrder[]? orders, ref CharWriter sb, ParameterDependant rootDependant, MemberContext operationContext)
        {
            if (orders is null)
            {
                sb.Write(UnorderedRowNumber);
                return;
            }
            sb.Write("ROW_NUMBER()OVER(ORDER BY");
            for (var i = 0; i < orders.Length; i++)
            {
                if (i > 0)
                    sb.Write(',');
                ConvertToSql(orders[i].Expression, ref sb, new BuilderContext(rootDependant, operationContext) { IsOrderBy = true, IsSort = true });
                if (orders[i].Descending)
                    sb.Write("DESC");
            }
            sb.Write(')');
        }
        /// <summary>The place of a row in no order, as the store reads the rows.</summary>
        protected virtual string UnorderedRowNumber => "ROW_NUMBER()OVER()";

        //each query of a join is a derived table, the first L and the second R, so a model joins itself as two tables
        private string ConvertJoinSql(LinqJoin join, ref List<object>? parameters, out LinqPlan plan)
        {
            var left = ConvertSetPart(join.Left, ref parameters, out var leftPlan);
            LinqPlan? rightPlan = null;
            var right = join.Right is null ? null : ConvertSetPart(join.Right, ref parameters, out rightPlan);
            var modelDetail = ModelAnalyzer.GetModel(join.Left.ModelType);
            plan = LinqPlan.ReadJoin(join, leftPlan.Shape!, rightPlan?.Shape, leftPlan.NumberColumn, modelDetail);
            if (join.Kind is not (LinqJoinKind.Inner or LinqJoinKind.Left or LinqJoinKind.Right or LinqJoinKind.Group))
                return ConvertPositionSql(join, left, leftPlan, right, rightPlan, plan, modelDetail, parameters);
            if (right is null || rightPlan is null)
                throw new InvalidOperationException("A join reads two queries");

            var group = join.Kind == LinqJoinKind.Group;
            var leftColumns = new Dictionary<string, string>();
            var leftEnums = new HashSet<string>();
            AddSetColumns(leftColumns, leftEnums, "L", String.Empty, leftPlan.Shape!);
            var rightColumns = new Dictionary<string, string>();
            var rightEnums = new HashSet<string>();
            AddSetColumns(rightColumns, rightEnums, group ? "G" : "R", String.Empty, rightPlan.Shape!);
            //a query the join may have no row of says it had one in a column of its own, which the element compared to null is
            if (join.Kind == LinqJoinKind.Left)
            {
                right = $"SELECT R.*,1 AS M FROM({right})AS R";
                if (rightPlan.Shape!.Kind != LinqShapeKind.Column)
                    rightColumns[String.Empty] = QuoteColumn("R", "M");
            }
            else if (join.Kind == LinqJoinKind.Right)
            {
                left = $"SELECT L.*,1 AS M FROM({left})AS L";
                if (leftPlan.Shape!.Kind != LinqShapeKind.Column)
                    leftColumns[String.Empty] = QuoteColumn("L", "M");
            }

            var operationContext = new MemberContext() { Parameters = parameters, ResultColumns = new(), ResultParameterEnums = new() };
            operationContext.ResultColumns[join.LeftElement] = leftColumns;
            operationContext.ResultParameterEnums[join.LeftElement] = leftEnums;
            if (join.LeftKey is not null)
            {
                operationContext.ResultColumns[join.LeftKey.Parameters[0]] = leftColumns;
                operationContext.ResultParameterEnums[join.LeftKey.Parameters[0]] = leftEnums;
            }
            if (join.RightKey is not null)
            {
                operationContext.ResultColumns[join.RightKey.Parameters[0]] = rightColumns;
                operationContext.ResultParameterEnums[join.RightKey.Parameters[0]] = rightEnums;
            }
            if (group)
            {
                operationContext.JoinGroups = new() { [join.RightElement] = new JoinGroup(right, rightColumns, rightEnums, join) };
            }
            else
            {
                operationContext.ResultColumns[join.RightElement] = rightColumns;
                operationContext.ResultParameterEnums[join.RightElement] = rightEnums;
            }
            var rootDependant = new ParameterDependant(modelDetail, null);

            var sb = new CharWriter();
            try
            {
                sb.Write("SELECT ");
                for (var i = 0; i < plan.Columns.Count; i++)
                {
                    if (i > 0)
                        sb.Write(',');
                    ConvertJoinColumn(plan.Columns[i], ref sb, rootDependant, operationContext);
                    sb.Write(" AS C");
                    sb.Write(i);
                }
                sb.Write(" FROM(");
                sb.Write(left);
                sb.Write(")AS L");
                if (!group)
                {
                    //every pair, when there is nothing to match them by
                    var cross = join.LeftKey is null && join.Condition is null;
                    sb.Write(join.Kind switch
                    {
                        LinqJoinKind.Left => " LEFT JOIN(",
                        LinqJoinKind.Right => " RIGHT JOIN(",
                        _ => cross ? " CROSS JOIN(" : " INNER JOIN(",
                    });
                    sb.Write(right);
                    sb.Write(")AS R");
                    if (!cross || join.Kind != LinqJoinKind.Inner)
                    {
                        sb.Write(" ON");
                        if (join.LeftKey is not null)
                            ConvertJoinMatch(join, ref sb, rootDependant, operationContext);
                        if (join.Condition is not null)
                        {
                            if (join.LeftKey is not null)
                                sb.Write(" AND");
                            ConvertJoinExpression(join.Condition, false, ref sb, rootDependant, operationContext);
                        }
                        if (cross)
                            sb.Write(" 1=1");
                    }
                }
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }
        }

        //a column of a join, one of either query's or computed from them
        private void ConvertJoinColumn(LinqColumn column, ref CharWriter sb, ParameterDependant rootDependant, MemberContext operationContext)
        {
            if (column.From.HasValue)
                sb.Write(QuoteColumn(column.FromRight ? "R" : "L", column.From.Value switch { -1 => "M", -2 => "P", _ => $"C{column.From.Value}" }));
            else
                ConvertJoinExpression(column.Expression!, true, ref sb, rootDependant, operationContext);
        }

        //an operation by position reads the place of each row of a query, numbered in its order; what it keeps is found in a derived table W of the elements with what it counts
        private string ConvertPositionSql(LinqJoin join, string left, LinqPlan leftPlan, string? right, LinqPlan? rightPlan, LinqPlan plan, ModelDetail modelDetail, List<object>? parameters)
        {
            var place = QuoteColumn("L", $"C{leftPlan.NumberColumn}");
            var leftColumns = new Dictionary<string, string>();
            var leftEnums = new HashSet<string>();
            AddSetColumns(leftColumns, leftEnums, "L", String.Empty, leftPlan.Shape!);
            var operationContext = new MemberContext() { Parameters = parameters, ResultColumns = new(), ResultParameterEnums = new() };
            operationContext.ResultColumns[join.LeftElement] = leftColumns;
            operationContext.ResultParameterEnums[join.LeftElement] = leftEnums;
            if (join.LeftKey is not null)
            {
                operationContext.ResultColumns[join.LeftKey.Parameters[0]] = leftColumns;
                operationContext.ResultParameterEnums[join.LeftKey.Parameters[0]] = leftEnums;
            }
            if (rightPlan is not null && join.Kind != LinqJoinKind.UnionBy)
            {
                var rightColumns = new Dictionary<string, string>();
                var rightEnums = new HashSet<string>();
                AddSetColumns(rightColumns, rightEnums, "R", String.Empty, rightPlan.Shape!);
                operationContext.ResultColumns[join.RightElement] = rightColumns;
                operationContext.ResultParameterEnums[join.RightElement] = rightEnums;
            }
            else
            {
                //the place an element is at counts from 0
                operationContext.ResultColumns[join.RightElement] = new() { [String.Empty] = $"({place}-1)" };
                operationContext.ResultParameterEnums[join.RightElement] = new();
            }
            var rootDependant = new ParameterDependant(modelDetail, null);

            if (join.Kind == LinqJoinKind.SequenceEqual)
            {
                if (!SameShape(leftPlan.Shape!, rightPlan!.Shape!))
                    throw new NotSupportedException("The queries SequenceEqual compares build their elements from the same values in the same order");
                //a pair differs when one is missing or a value differs, null equal to null
                var rightPlace = QuoteColumn("R", $"C{rightPlan.NumberColumn}");
                var differs = new CharWriter();
                try
                {
                    var first = true;
                    for (var i = 0; i < leftPlan.Columns.Count; i++)
                    {
                        if (i == leftPlan.NumberColumn)
                            continue;
                        if (!first)
                            differs.Write(" OR ");
                        first = false;
                        var l = QuoteColumn("L", $"C{i}");
                        var r = QuoteColumn("R", $"C{i}");
                        differs.Write($"NOT({l}={r} OR ({l} IS NULL AND {r} IS NULL))");
                    }
                    if (first)
                        differs.Write("1=0");
                    return $"SELECT 1 AS C0 FROM({left})AS L LEFT JOIN({right})AS R ON {place}={rightPlace} WHERE {rightPlace} IS NULL OR {differs.ToString()} UNION ALL SELECT 1 AS C0 FROM({right})AS R LEFT JOIN({left})AS L ON {place}={rightPlace} WHERE {place} IS NULL";
                }
                finally
                {
                    differs.Dispose();
                }
            }

            var sb = new CharWriter();
            try
            {
                if (join.Kind == LinqJoinKind.Append)
                    left = AppendSql(join, left, leftPlan, rootDependant, operationContext);
                else if (join.Kind == LinqJoinKind.UnionBy)
                    left = $"SELECT L.*,1 AS P FROM({left})AS L UNION ALL SELECT R.*,2 AS P FROM({right})AS R";

                sb.Write("SELECT ");
                for (var i = 0; i < plan.Columns.Count; i++)
                {
                    if (i > 0)
                        sb.Write(',');
                    ConvertJoinColumn(plan.Columns[i], ref sb, rootDependant, operationContext);
                    sb.Write(" AS C");
                    sb.Write(i);
                }
                switch (join.Kind)
                {
                    case LinqJoinKind.While:
                        //the elements before the first that fails are those with no failure up to them
                        sb.Write(",SUM(CASE WHEN");
                        ConvertJoinExpression(join.Condition!, false, ref sb, rootDependant, operationContext);
                        sb.Write($" THEN 0 ELSE 1 END)OVER(ORDER BY {place} ROWS UNBOUNDED PRECEDING)AS F");
                        break;
                    case LinqJoinKind.Last:
                        sb.Write(",COUNT(1)OVER()AS T");
                        break;
                    case LinqJoinKind.DistinctBy:
                    case LinqJoinKind.UnionBy:
                        {
                            sb.Write(",ROW_NUMBER()OVER(PARTITION BY ");
                            var parts = AggregateCommon.GetKeyParts(join.LeftKey!);
                            for (var i = 0; i < parts.Length; i++)
                            {
                                if (i > 0)
                                    sb.Write(',');
                                ConvertJoinExpression(parts[i], true, ref sb, rootDependant, operationContext);
                            }
                            sb.Write(" ORDER BY ");
                            if (join.Kind == LinqJoinKind.UnionBy)
                            {
                                sb.Write(QuoteColumn("L", "P"));
                                sb.Write(',');
                            }
                            sb.Write(place);
                            sb.Write(")AS F");
                            break;
                        }
                }
                sb.Write(" FROM(");
                sb.Write(left);
                sb.Write(")AS L");
                switch (join.Kind)
                {
                    case LinqJoinKind.Zip:
                        sb.Write($" INNER JOIN({right})AS R ON {place}={QuoteColumn("R", $"C{rightPlan!.NumberColumn}")}");
                        return sb.ToString();
                    case LinqJoinKind.Index:
                        if (join.Condition is not null)
                        {
                            sb.Write(" WHERE");
                            ConvertJoinExpression(join.Condition, false, ref sb, rootDependant, operationContext);
                        }
                        return sb.ToString();
                    case LinqJoinKind.Append:
                        return sb.ToString();
                    case LinqJoinKind.While:
                        return $"SELECT * FROM({sb.ToString()})AS W WHERE {QuoteColumn("W", "F")}{(join.Skips ? ">0" : "=0")}";
                    case LinqJoinKind.Last:
                        {
                            var number = QuoteColumn("W", $"C{plan.PositionColumns![^1]}");
                            var total = QuoteColumn("W", "T");
                            var condition = join.At ? $"{number}={total}-{join.Count}+1" : join.Skips ? $"{number}<={total}-{join.Count}" : $"{number}>{total}-{join.Count}";
                            return $"SELECT * FROM({sb.ToString()})AS W WHERE {condition}";
                        }
                    default:
                        return $"SELECT * FROM({sb.ToString()})AS W WHERE {QuoteColumn("W", "F")}=1";
                }
            }
            finally
            {
                sb.Dispose();
            }
        }
        //the rows of the query and one more of the element added, first or last by its place
        private string AppendSql(LinqJoin join, string left, LinqPlan leftPlan, ParameterDependant rootDependant, MemberContext operationContext)
        {
            var values = new object?[leftPlan.Columns.Count];
            var written = new bool[values.Length];
            ElementValues(leftPlan.Shape!, join.Value, values, written);
            var sb = new CharWriter();
            try
            {
                sb.Write("SELECT ");
                for (var i = 0; i < values.Length; i++)
                {
                    if (i > 0)
                        sb.Write(',');
                    sb.Write(i == leftPlan.NumberColumn ? QuoteColumn("L", $"C{i}") : AppendedColumn(leftPlan.Columns[i], QuoteColumn("L", $"C{i}")));
                    sb.Write(" AS C");
                    sb.Write(i);
                }
                sb.Write(" FROM(");
                sb.Write(left);
                sb.Write(")AS L UNION ALL SELECT ");
                //each value is written as a value, as an argument is
                var context = new BuilderContext(rootDependant, operationContext) { IsOrderBy = true };
                operationContext.OperatorStack.Push(Operator.Call);
                for (var i = 0; i < values.Length; i++)
                {
                    if (i > 0)
                        sb.Write(',');
                    var column = leftPlan.Columns[i];
                    if (i == leftPlan.NumberColumn)
                    {
                        sb.Write(join.Prepends ? "0" : "2147483647");
                    }
                    else if (!written[i] || values[i] is null)
                    {
                        sb.Write("NULL");
                    }
                    else if (values[i] is bool flag)
                    {
                        sb.Write(BooleanValue(flag));
                    }
                    else if (column.Member is { EnumType: { } enumType })
                    {
                        //an enum member holds its name
                        ConvertToSqlValue(typeof(string), EnumName.GetName(enumType, values[i]!), ref sb, context);
                    }
                    else
                    {
                        var value = new CharWriter();
                        try
                        {
                            ConvertToSqlValue(Nullable.GetUnderlyingType(column.Type) ?? column.Type, values[i], ref value, context);
                            sb.Write(AppendedValue(column, value.ToString()));
                        }
                        finally
                        {
                            value.Dispose();
                        }
                    }
                    sb.Write(" AS C");
                    sb.Write(i);
                }
                _ = operationContext.OperatorStack.Pop();
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }
        }
        /// <summary>
        /// Writes a value added as a row so the rows combined with it keep the type of its column, for a store that would otherwise take the column as the value's type.
        /// </summary>
        /// <param name="column">The column of the query the value is added to.</param>
        /// <param name="value">The SQL of the value.</param>
        /// <returns>The SQL.</returns>
        protected virtual string AppendedValue(LinqColumn column, string value) => value;
        /// <summary>
        /// Writes a column of the rows a value is added to, for a store whose type for the column and the value would otherwise differ.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <param name="sql">The SQL of the column.</param>
        /// <returns>The SQL.</returns>
        protected virtual string AppendedColumn(LinqColumn column, string sql) => sql;
        /// <summary>
        /// Writes a bool as a value of the store's bool type, so rows combined with a bool column keep its type.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The SQL.</returns>
        protected virtual string BooleanValue(bool value) => value ? "1" : "0";
        //the value of each column an element is built from
        private static void ElementValues(LinqShape shape, object? value, object?[] values, bool[] written)
        {
            switch (shape.Kind)
            {
                case LinqShapeKind.Column:
                    values[shape.Column!.Index] = value;
                    written[shape.Column.Index] = true;
                    break;
                case LinqShapeKind.Model:
                    if (value is null)
                        return;
                    if (shape.JoinedKey is not null)
                    {
                        values[shape.JoinedKey.Index] = shape.Model!.IdentityMembers[0].GetterBoxed(value);
                        written[shape.JoinedKey.Index] = true;
                    }
                    for (var i = 0; i < shape.ModelMembers!.Length; i++)
                    {
                        values[shape.ModelColumns![i].Index] = shape.ModelMembers[i].GetterBoxed(value);
                        written[shape.ModelColumns[i].Index] = true;
                    }
                    break;
                case LinqShapeKind.Object:
                    {
                        if (value is null)
                            return;
                        if (shape.Arguments is { Length: > 0 } && shape.ArgumentNames is null)
                            throw new NotSupportedException($"{shape.Type.Name} built through its constructor can't be added as a row, select its members by name");
                        var type = TypeAnalyzer.GetTypeDetail(shape.Type);
                        for (var i = 0; i < (shape.ArgumentNames?.Length ?? 0); i++)
                            ElementValues(shape.Arguments![i], type.GetMember(shape.ArgumentNames![i]).GetterBoxed!(value), values, written);
                        for (var i = 0; i < (shape.Members?.Length ?? 0); i++)
                            ElementValues(shape.Values![i], shape.Members![i].GetterBoxed!(value), values, written);
                        break;
                    }
                default:
                    throw new NotSupportedException("An element added as a row is built from its own values, not a relation read after them");
            }
        }

        //the keys of a pair are equal; a key of one part that is null matches none as in LINQ, where parts of a key of several compare as their object does, null equal to null
        private void ConvertJoinMatch(LinqJoin join, ref CharWriter sb, ParameterDependant rootDependant, MemberContext operationContext)
        {
            var leftParts = AggregateCommon.GetKeyParts(join.LeftKey!);
            var rightParts = AggregateCommon.GetKeyParts(join.RightKey!);
            if (leftParts.Length != rightParts.Length)
                throw new NotSupportedException("The keys of a join have the same parts");
            if (leftParts.Length == 1 && IsStoredEnum(WithoutConvert(leftParts[0]), operationContext) == IsStoredEnum(WithoutConvert(rightParts[0]), operationContext))
            {
                ConvertJoinExpression(leftParts[0], true, ref sb, rootDependant, operationContext);
                sb.Write('=');
                ConvertJoinExpression(rightParts[0], true, ref sb, rootDependant, operationContext);
                return;
            }
            //both rows are there, so a part that can't be null is compared as it is
            for (var i = 0; i < leftParts.Length; i++)
            {
                if (i > 0)
                    sb.Write(" AND");
                var leftPart = leftParts[i];
                var rightPart = rightParts[i].Type == leftPart.Type ? rightParts[i] : Expression.Convert(rightParts[i], leftPart.Type);
                var nullable = !leftPart.Type.IsValueType || Nullable.GetUnderlyingType(leftPart.Type) is not null;
                if (!nullable && IsStoredEnum(WithoutConvert(leftPart), operationContext) == IsStoredEnum(WithoutConvert(rightPart), operationContext))
                {
                    sb.Write('(');
                    ConvertJoinExpression(leftPart, true, ref sb, rootDependant, operationContext);
                    sb.Write('=');
                    ConvertJoinExpression(rightPart, true, ref sb, rootDependant, operationContext);
                    sb.Write(')');
                    continue;
                }
                Expression equal = Expression.Equal(leftPart, rightPart);
                if (leftParts.Length == 1 && nullable)
                    equal = Expression.AndAlso(Expression.NotEqual(leftPart, Expression.Constant(null, leftPart.Type)), equal);
                ConvertJoinExpression(equal, false, ref sb, rootDependant, operationContext);
            }
        }

        //an expression over the elements of a join's queries, a value or a condition
        private void ConvertJoinExpression(Expression expression, bool value, ref CharWriter sb, ParameterDependant rootDependant, MemberContext operationContext)
        {
            operationContext.OperatorStack.Push(Operator.Lambda);
            sb.Write('(');
            ConvertToSql(expression, ref sb, new BuilderContext(rootDependant, operationContext) { IsOrderBy = value });
            sb.Write(')');
            _ = operationContext.OperatorStack.Pop();
        }

        //the columns of the elements of a statement, each named by the members from the element down, an object or model by its match column when it has one
        private void AddSetColumns(Dictionary<string, string> columns, HashSet<string> enums, string table, string name, LinqShape shape)
        {
            switch (shape.Kind)
            {
                case LinqShapeKind.Column:
                    columns[name] = QuoteColumn(table, $"C{shape.Column!.Index}");
                    if (shape.Column.Member is { EnumType: not null })
                        _ = enums.Add(name);
                    break;
                case LinqShapeKind.Model:
                    if (shape.JoinedKey is not null)
                        columns[name] = QuoteColumn(table, $"C{shape.JoinedKey.Index}");
                    for (var i = 0; i < shape.ModelMembers!.Length; i++)
                    {
                        var member = name.Length == 0 ? shape.ModelMembers[i].Name : $"{name}.{shape.ModelMembers[i].Name}";
                        columns[member] = QuoteColumn(table, $"C{shape.ModelColumns![i].Index}");
                        if (shape.ModelMembers[i].EnumType is not null)
                            _ = enums.Add(member);
                    }
                    break;
                case LinqShapeKind.Object:
                    if (shape.JoinedKey is not null)
                        columns[name] = QuoteColumn(table, $"C{shape.JoinedKey.Index}");
                    for (var i = 0; i < (shape.ArgumentNames?.Length ?? 0); i++)
                        AddSetColumns(columns, enums, table, name.Length == 0 ? shape.ArgumentNames![i] : $"{name}.{shape.ArgumentNames![i]}", shape.Arguments![i]);
                    for (var i = 0; i < (shape.Members?.Length ?? 0); i++)
                        AddSetColumns(columns, enums, table, name.Length == 0 ? shape.Members![i].Name : $"{name}.{shape.Members![i].Name}", shape.Values![i]);
                    break;
            }
        }

        //an aggregate of a GroupJoin's group, as a subquery of the second query's rows with the key of the first query's row
        private string? JoinGroupSql(MethodCallExpression call, BuilderContext context)
        {
            var groups = context.MemberContext.JoinGroups;
            if (groups is null || call.Method.DeclaringType != typeof(Enumerable) || call.Arguments.Count == 0)
                return null;
            var source = call.Arguments[0];
            LambdaExpression? select = null;
            if (source is MethodCallExpression { Method.Name: "Select" } selectCall && selectCall.Method.DeclaringType == typeof(Enumerable) && selectCall.Arguments.Count == 2 && Lambda(selectCall.Arguments[1]).Parameters.Count == 1)
            {
                select = Lambda(selectCall.Arguments[1]);
                source = selectCall.Arguments[0];
            }
            var conditions = new List<LambdaExpression>();
            while (source is MethodCallExpression { Method.Name: "Where" } whereCall && whereCall.Method.DeclaringType == typeof(Enumerable) && whereCall.Arguments.Count == 2 && Lambda(whereCall.Arguments[1]).Parameters.Count == 1)
            {
                conditions.Add(Lambda(whereCall.Arguments[1]));
                source = whereCall.Arguments[0];
            }
            if (source is not ParameterExpression parameter || !groups.TryGetValue(parameter, out var group))
                return null;

            var argument = call.Arguments.Count == 2 ? Lambda(call.Arguments[1]) : null;
            if (call.Arguments.Count > 2 || (argument is not null && argument.Parameters.Count != 1))
                throw new NotSupportedException($"{call.Method.Name} of a GroupJoin's group with more than the element isn't supported");
            string? value = null;
            string? function = null;
            var exists = false;
            var negated = false;
            switch (call.Method.Name)
            {
                case "Count":
                case "LongCount":
                    if (select is not null)
                        throw new NotSupportedException($"{call.Method.Name} of a GroupJoin's group counts its elements, filter them with Where");
                    if (argument is not null)
                        conditions.Add(argument);
                    function = "COUNT";
                    value = "1";
                    break;
                case "Any":
                    if (select is not null)
                        throw new NotSupportedException("Any of a GroupJoin's group looks at its elements, filter them with Where");
                    if (argument is not null)
                        conditions.Add(argument);
                    exists = true;
                    break;
                case "All":
                    if (select is not null || argument is null)
                        throw new NotSupportedException("All of a GroupJoin's group takes a condition on its elements");
                    //every element meets the condition when none fails it
                    conditions.Add(Expression.Lambda(argument.Type, Expression.Not(argument.Body), argument.Parameters));
                    exists = true;
                    negated = true;
                    break;
                case "Sum":
                case "Min":
                case "Max":
                case "Average":
                    {
                        if ((argument is null) == (select is null))
                            throw new NotSupportedException($"{call.Method.Name} of a GroupJoin's group takes the value to aggregate once, such as g.{call.Method.Name}(x => x.Price)");
                        var selector = argument ?? select!;
                        value = GroupValue(selector, group, context);
                        var type = Nullable.GetUnderlyingType(selector.ReturnType) ?? selector.ReturnType;
                        if (call.Method.Name == "Average" && IsInteger(type))
                            value = Cast(value, typeof(double));
                        function = call.Method.Name == "Average" ? "AVG" : call.Method.Name.ToUpperInvariant();
                        break;
                    }
                default:
                    throw new NotSupportedException($"{call.Method.Name} of a GroupJoin's group isn't supported, read it with Count, Any, All, Sum, Min, Max, or Average");
            }

            var sb = new CharWriter();
            try
            {
                if (exists)
                    sb.Write(negated ? "NOT EXISTS(SELECT 1" : "EXISTS(SELECT 1");
                else if (call.Method.Name == "Sum")
                    sb.Write($"COALESCE((SELECT SUM({value})");
                else
                    sb.Write($"(SELECT {function}({value})");
                sb.Write(" FROM(");
                sb.Write(group.Sql);
                sb.Write(")AS G WHERE");
                ConvertJoinMatch(group.Join, ref sb, context.RootDependant, context.MemberContext);
                foreach (var condition in conditions)
                {
                    sb.Write(" AND");
                    context.MemberContext.ResultColumns![condition.Parameters[0]] = group.Columns;
                    context.MemberContext.ResultParameterEnums![condition.Parameters[0]] = group.Enums;
                    ConvertJoinExpression(condition.Body, false, ref sb, context.RootDependant, context.MemberContext);
                    _ = context.MemberContext.ResultColumns.Remove(condition.Parameters[0]);
                    _ = context.MemberContext.ResultParameterEnums.Remove(condition.Parameters[0]);
                }
                sb.Write(')');
                if (!exists && call.Method.Name == "Sum")
                    sb.Write(",0)");
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }
        }
        private string GroupValue(LambdaExpression selector, JoinGroup group, BuilderContext context)
        {
            context.MemberContext.ResultColumns![selector.Parameters[0]] = group.Columns;
            context.MemberContext.ResultParameterEnums![selector.Parameters[0]] = group.Enums;
            if (IsStoredEnum(WithoutConvert(selector.Body), context.MemberContext))
                throw new NotSupportedException("An aggregate of a GroupJoin's group of an enum stored as its name isn't supported");
            var sb = new CharWriter();
            try
            {
                ConvertJoinExpression(selector.Body, true, ref sb, context.RootDependant, context.MemberContext);
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
                _ = context.MemberContext.ResultColumns.Remove(selector.Parameters[0]);
                _ = context.MemberContext.ResultParameterEnums.Remove(selector.Parameters[0]);
            }
        }
    }
}

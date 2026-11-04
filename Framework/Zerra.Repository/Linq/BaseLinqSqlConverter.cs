using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zerra.Reflection;
using Zerra.Repository.IO;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Linq
{
    /// <summary>
    /// Base class for converting LINQ expressions into SQL query strings.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract partial class BaseLinqSqlConverter
    {

        /// <summary>
        /// Converts an expression over the model, a group key part or a projected value, as the body of a lambda on the model, in parentheses, so related models in it join or subquery as they do in a where.
        /// </summary>
        /// <param name="part">The expression.</param>
        /// <param name="parameter">The model parameter the expression uses.</param>
        /// <param name="sb">The writer to append the SQL into.</param>
        /// <param name="rootDependant">The root parameter dependant used for join tracking.</param>
        /// <param name="modelDetail">Metadata about the model being queried.</param>
        /// <param name="operationContext">The current member context for the operation.</param>
        protected void ConvertModelExpression(Expression part, ParameterExpression parameter, ref CharWriter sb, ParameterDependant rootDependant, ModelDetail modelDetail, MemberContext operationContext)
        {
            //the same setup as a lambda's, without making a lambda, which needs a delegate type the AOT compiler may not have
            var context = new BuilderContext(rootDependant, operationContext) { IsOrderBy = true };
            operationContext.OperatorStack.Push(Operator.Lambda);
            operationContext.DependantStack.Push(rootDependant);
            operationContext.ModelStack.Push(modelDetail);
            operationContext.ModelContexts.Add(parameter, modelDetail);

            sb.Write('(');
            ConvertToSql(part, ref sb, context);
            sb.Write(')');

            _ = operationContext.ModelContexts.Remove(parameter);
            _ = operationContext.ModelStack.Pop();
            _ = operationContext.DependantStack.Pop();
            _ = operationContext.OperatorStack.Pop();
        }

        /// <summary>
        /// Converts a LINQ query to its statements in one pass: the statement over its model, and nested within it one per relation the plan reads after the rows, each selecting the rows of every parent at once, those whose key the parent's statement selects.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="modelDetail">Metadata about the model being queried.</param>
        /// <returns>The statement, the plan of its rows, and the statements of the relations.</returns>
        protected LinqStatement ConvertLinqInternal(LinqQuery query, ModelDetail modelDetail)
        {
            if (query.Combines)
                return ConvertLinqCombined(query);
            return ConvertLinq(query, LinqPlan.Read(query, modelDetail), modelDetail, null);
        }

        //each query of a set or a join is a derived table and the set or join is one too before it's combined again, so they combine in the order written where SQL's INTERSECT would bind tighter than UNION
        private LinqStatement ConvertLinqCombined(LinqQuery query)
        {
            List<object>? parameters = null;
            var sql = ConvertCombinedSql(query, ref parameters, out var plan, true);
            sql = query.Terminal switch
            {
                LinqTerminal.Count or LinqTerminal.LongCount => $"SELECT COUNT(1) FROM({sql})AS Z",
                LinqTerminal.Any => AnySql(sql),
                LinqTerminal.Sum or LinqTerminal.Min or LinqTerminal.Max or LinqTerminal.Average => ConvertCombinedAggregate(query, sql, plan, parameters),
                _ => sql,
            };
            return new LinqStatement(sql, plan, null, parameters);
        }
        //an aggregate of the combined elements, its value read from the columns of each
        private string ConvertCombinedAggregate(LinqQuery query, string sql, LinqPlan plan, List<object>? parameters)
        {
            var columns = new Dictionary<string, string>();
            var enums = new HashSet<string>();
            AddSetColumns(columns, enums, "Z", String.Empty, plan.Shape!);
            var operationContext = new MemberContext() { Parameters = parameters, ResultEnums = enums };
            var rootDependant = new ParameterDependant(ModelAnalyzer.GetModel(query.ModelType), null);
            var value = query.Value!;
            var body = value.Body is UnaryExpression { NodeType: ExpressionType.Convert } boxed && boxed.Type == typeof(object) ? boxed.Operand : value.Body;
            //an enum's smallest and largest are by its number, whatever the store holds
            var type = Nullable.GetUnderlyingType(body.Type) ?? body.Type;
            if (query.Terminal is LinqTerminal.Min or LinqTerminal.Max && type.IsEnum)
                body = Expression.Convert(body, Enum.GetUnderlyingType(type));
            var sb = new CharWriter();
            try
            {
                sb.Write(query.Terminal switch
                {
                    //SUM of no rows is NULL where the LINQ sum is 0
                    LinqTerminal.Sum => "SELECT COALESCE(SUM(",
                    LinqTerminal.Min => "SELECT MIN(",
                    LinqTerminal.Max => "SELECT MAX(",
                    _ => "SELECT AVG(",
                });
                var sbValue = new CharWriter();
                try
                {
                    ConvertSetValue(body, value.Parameters[0], columns, ref sbValue, rootDependant, operationContext);
                    //an average of integers is a fraction in LINQ
                    sb.Write(query.Terminal == LinqTerminal.Average && IsInteger(type) ? Cast(sbValue.ToString(), typeof(double)) : sbValue.ToString());
                }
                finally
                {
                    sbValue.Dispose();
                }
                sb.Write(query.Terminal == LinqTerminal.Sum ? "),0) FROM(" : ") FROM(");
                sb.Write(sql);
                sb.Write(")AS Z");
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }
        }
        //the elements of a set or a join and what follows them
        //the statement read on its own keeps the elements of an operation by position in their places, a derived table has no order
        private string ConvertCombinedSql(LinqQuery query, ref List<object>? parameters, out LinqPlan plan, bool top = false)
        {
            var sql = query.Set is not null ? ConvertSetSql(query.Set, ref parameters, out plan) : ConvertJoinSql(query.Join!, ref parameters, out plan);
            var placed = plan.PositionColumns is not null && (top || query.Skip.HasValue || query.Take.HasValue);
            if (query.Having is not null || query.ResultOrder is not null || query.Skip.HasValue || query.Take.HasValue || query.Select is not null || query.SelectedHaving is not null || query.SelectedOrder is not null || query.Distinct || placed)
                sql = ConvertSetResults(query, sql, ref plan, parameters, top);
            return sql;
        }
        //the condition, order, and page of the elements on the combined rows, each member of an element the column holding it
        private string ConvertSetResults(LinqQuery query, string sql, ref LinqPlan plan, List<object>? parameters, bool top = false)
        {
            var columns = new Dictionary<string, string>();
            var enums = new HashSet<string>();
            AddSetColumns(columns, enums, "Z", String.Empty, plan.Shape!);
            var positions = plan.PositionColumns;

            var operationContext = new MemberContext() { Parameters = parameters, ResultEnums = enums };
            var rootDependant = new ParameterDependant(ModelAnalyzer.GetModel(query.ModelType), null);
            var sb = new CharWriter();
            try
            {
                var select = query.Select is not null && (query.ReadsElements || query.SelectedHaving is not null || query.Distinct) ? LinqPlan.ReadSetSelect(query.Select, ModelAnalyzer.GetModel(query.ModelType)) : null;
                Dictionary<string, string>? selectedColumns = null;
                HashSet<string>? selectedEnums = null;
                if (select is null)
                {
                    sb.Write(query.Distinct ? "SELECT DISTINCT * FROM(" : "SELECT * FROM(");
                }
                else
                {
                    var values = new string[select.Columns.Count];
                    var valueEnums = new bool[values.Length];
                    for (var i = 0; i < values.Length; i++)
                    {
                        var column = select.Columns[i];
                        var value = new CharWriter();
                        try
                        {
                            ConvertSetValue(column.Expression!, column.Parameter!, columns, ref value, rootDependant, operationContext);
                            values[i] = value.ToString();
                        }
                        finally
                        {
                            value.Dispose();
                        }
                        valueEnums[i] = column.Expression == column.Parameter ? enums.Contains(String.Empty) : column.Expression is MemberExpression { Expression: ParameterExpression owner } member && owner == column.Parameter && enums.Contains(member.Member.Name);
                    }
                    selectedColumns = new();
                    selectedEnums = new();
                    AddSelectedColumn(selectedColumns, selectedEnums, String.Empty, select.Shape!, values, valueEnums);

                    sb.Write(query.Distinct ? "SELECT DISTINCT " : "SELECT ");
                    for (var i = 0; i < values.Length; i++)
                    {
                        if (i > 0)
                            sb.Write(',');
                        sb.Write(values[i]);
                        sb.Write(" AS C");
                        sb.Write(i);
                    }
                    sb.Write(" FROM(");
                    if (query.ReadsElements || query.Distinct)
                        plan = select;
                }
                sb.Write(sql);
                sb.Write(")AS Z");
                var none = query.Take == 0;
                var selectedHaving = selectedColumns is null ? null : query.SelectedHaving;
                if (query.Having is not null || selectedHaving is not null || none)
                {
                    sb.Write(" WHERE");
                    if (query.Having is not null)
                        ConvertSetExpression(query.Having, columns, false, ref sb, rootDependant, operationContext);
                    if (selectedHaving is not null)
                    {
                        if (query.Having is not null)
                            sb.Write(" AND");
                        operationContext.ResultEnums = selectedEnums;
                        ConvertSetExpression(selectedHaving, selectedColumns!, false, ref sb, rootDependant, operationContext);
                        operationContext.ResultEnums = enums;
                    }
                    //a page of no rows is a condition of none, as SQL Server refuses to fetch none
                    if (none)
                        sb.Write(query.Having is null && selectedHaving is null ? " 1=0" : " AND 1=0");
                }
                var paged = !none && (query.Skip.HasValue || query.Take.HasValue);
                //an order only matters to the elements read or to a page of them
                var selectedOrder = selectedColumns is null ? null : query.SelectedOrder;
                var ordered = (query.ResultOrder is not null || selectedOrder is not null) && (query.ReadsElements || paged);
                //elements of an operation by position are in their places unless ordered otherwise, which the Distinct of what a Select builds loses
                var byPosition = !ordered && positions is not null && ((query.ReadsElements && top) || paged) && !(query.Distinct && select is not null);
                //the different elements are ordered and paged once they are found, each member the column of the elements holding it
                if (query.Distinct && (ordered || paged || byPosition))
                {
                    var distinct = sb.ToString();
                    sb.Dispose();
                    sb = new CharWriter();
                    sb.Write("SELECT * FROM(");
                    sb.Write(distinct);
                    sb.Write(")AS D");
                    columns = new();
                    enums = new();
                    AddSetColumns(columns, enums, "D", String.Empty, (select ?? plan).Shape!);
                    operationContext.ResultEnums = enums;
                    if (selectedColumns is not null)
                    {
                        selectedColumns = columns;
                        selectedEnums = enums;
                    }
                }
                if (byPosition)
                {
                    sb.Write(" ORDER BY ");
                    var table = query.Distinct ? "D" : "Z";
                    for (var i = 0; i < positions!.Length; i++)
                    {
                        if (i > 0)
                            sb.Write(',');
                        sb.Write(QuoteColumn(table, $"C{positions[i]}"));
                    }
                    ordered = true;
                }
                else if (ordered)
                {
                    sb.Write(" ORDER BY");
                    var first = true;
                    foreach (var order in query.ResultOrder ?? [])
                    {
                        if (!first)
                            sb.Write(',');
                        first = false;
                        ConvertSetExpression(order.Expression, columns, true, ref sb, rootDependant, operationContext);
                        if (order.Descending)
                            sb.Write("DESC");
                    }
                    operationContext.ResultEnums = selectedEnums;
                    foreach (var order in selectedOrder ?? [])
                    {
                        if (!first)
                            sb.Write(',');
                        first = false;
                        ConvertSetExpression(order.Expression, selectedColumns!, true, ref sb, rootDependant, operationContext);
                        if (order.Descending)
                            sb.Write("DESC");
                    }
                    operationContext.ResultEnums = enums;
                }
                if (paged)
                    GeneratePage(query.Skip, query.Take, ordered, ref sb, operationContext);
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }
        }
        private static void AddSelectedColumn(Dictionary<string, string> columns, HashSet<string> enums, string name, LinqShape value, string[] values, bool[] valueEnums)
        {
            if (value.Kind == LinqShapeKind.Object)
            {
                for (var i = 0; i < (value.ArgumentNames?.Length ?? 0); i++)
                    AddSelectedColumn(columns, enums, name.Length == 0 ? value.ArgumentNames![i] : $"{name}.{value.ArgumentNames![i]}", value.Arguments![i], values, valueEnums);
                if (name.Length == 0)
                {
                    for (var i = 0; i < (value.Members?.Length ?? 0); i++)
                        AddSelectedColumn(columns, enums, value.Members![i].Name, value.Values![i], values, valueEnums);
                }
                return;
            }
            if (value.Kind != LinqShapeKind.Column)
                return;
            columns[name] = values[value.Column!.Index];
            if (valueEnums[value.Column.Index])
                _ = enums.Add(name);
        }
        private void ConvertSetValue(Expression value, ParameterExpression parameter, Dictionary<string, string> columns, ref CharWriter sb, ParameterDependant rootDependant, MemberContext operationContext)
        {
            operationContext.ResultColumns ??= new();
            operationContext.ResultColumns[parameter] = columns;
            operationContext.OperatorStack.Push(Operator.Lambda);
            sb.Write('(');
            //a condition is a value here, as it is in the columns of a query
            ConvertToSql(value, ref sb, new BuilderContext(rootDependant, operationContext) { IsOrderBy = true });
            sb.Write(')');
            _ = operationContext.OperatorStack.Pop();
            _ = operationContext.ResultColumns.Remove(parameter);
        }
        private void ConvertSetExpression(LambdaExpression lambda, Dictionary<string, string> columns, bool isOrder, ref CharWriter sb, ParameterDependant rootDependant, MemberContext operationContext)
        {
            operationContext.ResultColumns ??= new();
            operationContext.ResultColumns[lambda.Parameters[0]] = columns;
            operationContext.OperatorStack.Push(Operator.Lambda);
            sb.Write('(');
            //a value boxed for a lambda over objects is ordered as the value, an enum by its number
            var body = lambda.Body is UnaryExpression { NodeType: ExpressionType.Convert } boxed && boxed.Type == typeof(object) ? boxed.Operand : lambda.Body;
            ConvertToSql(body, ref sb, new BuilderContext(rootDependant, operationContext) { IsOrderBy = isOrder, IsSort = isOrder });
            sb.Write(')');
            _ = operationContext.OperatorStack.Pop();
            _ = operationContext.ResultColumns.Remove(lambda.Parameters[0]);
        }
        /// <summary>
        /// Writes a page of a statement's rows after its order, which is none when <paramref name="ordered"/> is false.
        /// </summary>
        /// <param name="skip">The rows to skip.</param>
        /// <param name="take">The rows to read.</param>
        /// <param name="ordered">Whether the statement has an order.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="operationContext">The current member context for the operation.</param>
        protected abstract void GeneratePage(int? skip, int? take, bool ordered, ref CharWriter sb, MemberContext operationContext);
        private string ConvertSetSql(LinqSet set, ref List<object>? parameters, out LinqPlan plan)
        {
            var left = ConvertSetPart(set.Left, ref parameters, out plan);
            var right = ConvertSetPart(set.Right, ref parameters, out var rightPlan);
            if (!SameShape(plan.Shape!, rightPlan.Shape!))
                throw new NotSupportedException($"The queries {set.Operation} combines build their elements from the same values in the same order, as rows combine by position");
            var operation = set.Operation switch
            {
                LinqSetOperation.Union => "UNION",
                LinqSetOperation.Concat => "UNION ALL",
                LinqSetOperation.Intersect => "INTERSECT",
                LinqSetOperation.Except => "EXCEPT",
                _ => throw new NotSupportedException($"{set.Operation} isn't a set operation"),
            };
            return $"SELECT * FROM({left})AS Z {operation} SELECT * FROM({right})AS Z";
        }
        private string ConvertSetPart(LinqQuery part, ref List<object>? parameters, out LinqPlan plan)
        {
            if (part.Combines)
                return ConvertCombinedSql(part, ref parameters, out plan);
            var modelDetail = ModelAnalyzer.GetModel(part.ModelType);
            var statement = ConvertLinq(part, LinqPlan.Read(part, modelDetail), modelDetail, parameters);
            if (statement.Pieces is not null)
                throw new NotSupportedException("A query combined with a set operation reads its own rows, not a relation read after them");
            parameters ??= statement.Parameters;
            plan = statement.Plan;
            return statement.Sql;
        }
        private static bool SameShape(LinqShape left, LinqShape right)
        {
            if (left.Kind != right.Kind || left.Type != right.Type)
                return false;
            switch (left.Kind)
            {
                case LinqShapeKind.Column:
                    return left.Column!.Index == right.Column!.Index;
                case LinqShapeKind.Model:
                    if (left.Model != right.Model || left.ModelRelations is not null || right.ModelRelations is not null)
                        return false;
                    for (var i = 0; i < left.ModelColumns!.Length; i++)
                    {
                        if (left.ModelColumns[i].Index != right.ModelColumns![i].Index)
                            return false;
                    }
                    return true;
                case LinqShapeKind.Object:
                    if ((left.Arguments?.Length ?? 0) != (right.Arguments?.Length ?? 0) || (left.Members?.Length ?? 0) != (right.Members?.Length ?? 0))
                        return false;
                    for (var i = 0; i < (left.Arguments?.Length ?? 0); i++)
                    {
                        if (!SameShape(left.Arguments![i], right.Arguments![i]))
                            return false;
                    }
                    for (var i = 0; i < (left.Members?.Length ?? 0); i++)
                    {
                        if (left.Members![i].Name != right.Members![i].Name || !SameShape(left.Values![i], right.Values![i]))
                            return false;
                    }
                    return true;
                default:
                    return false;
            }
        }
        /// <summary>
        /// Writes whether a statement has any rows as a statement giving a row when it does and none when it doesn't.
        /// </summary>
        /// <param name="sql">The statement.</param>
        /// <returns>The SQL.</returns>
        protected virtual string AnySql(string sql) => $"SELECT 1 WHERE EXISTS({sql})";

        /// <summary>
        /// Builds the statements of a LINQ query. The statement over the model: the plan's columns, or the count, the one row of an any, or the aggregate, then the from, joins, where, and for a grouped query the group by, having, and the order and paging of the results, otherwise the order and paging of the models. Then for each piece of the plan, nested: its plan's columns, the key matching each row to its parent among them, the from, joins, a where of the parents' keys as the parent's statement selects them and the relation's own condition, and the relation's order.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="plan">The plan of the rows, from <see cref="LinqPlan.Read"/>.</param>
        /// <param name="modelDetail">Metadata about the model being queried.</param>
        /// <param name="parameters">The values taken as parameters of the statement this query is written in, or <see langword="null"/> for a statement of its own.</param>
        /// <returns>The statement, the plan of its rows, and the statements of the relations.</returns>
        protected abstract LinqStatement ConvertLinq(LinqQuery query, LinqPlan plan, ModelDetail modelDetail, List<object>? parameters);

        /// <summary>
        /// Converts the condition of an update or delete to its SQL, starting with WHERE, for the engine to append to its UPDATE or DELETE statement.
        /// </summary>
        /// <param name="where">The condition selecting the rows.</param>
        /// <param name="modelDetail">Metadata about the model being changed.</param>
        /// <param name="parameters">The values taken as parameters, which the condition adds to, or <see langword="null"/> to write values into the statement.</param>
        /// <returns>The SQL from WHERE on.</returns>
        protected string ConvertPersistWhereInternal(LambdaExpression where, ModelDetail modelDetail, List<object>? parameters = null)
        {
            var operationContext = new MemberContext() { Parameters = parameters };
            var sb = new CharWriter();
            try
            {
                ConvertPersistWhere(ref sb, where, modelDetail, operationContext);
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }
        }

        /// <summary>
        /// Builds the WHERE of an update or delete into the provided <see cref="CharWriter"/>. A condition through a relation selects the matching identities in a subquery, since each database writes joins differently in UPDATE and DELETE.
        /// </summary>
        /// <param name="sb">The writer to append the SQL into.</param>
        /// <param name="where">The condition selecting the rows.</param>
        /// <param name="modelDetail">Metadata about the model being changed.</param>
        /// <param name="operationContext">The current member context for the operation.</param>
        protected abstract void ConvertPersistWhere(ref CharWriter sb, LambdaExpression where, ModelDetail modelDetail, MemberContext operationContext);

        /// <summary>
        /// Converts an update of every row matching a condition to values computed from the row into its SQL statement.
        /// </summary>
        /// <param name="sets">The members to set from <see cref="ProjectionMember.FromSet"/>, each with its value in terms of the row.</param>
        /// <param name="where">The condition selecting the rows.</param>
        /// <param name="modelDetail">Metadata about the model being changed.</param>
        /// <param name="parameters">The values taken as parameters, which the statement adds to, or <see langword="null"/> to write values into the statement.</param>
        /// <returns>The SQL.</returns>
        protected string ConvertPersistUpdateInternal(ProjectionMember[] sets, LambdaExpression where, ModelDetail modelDetail, List<object>? parameters = null)
        {
            var operationContext = new MemberContext() { Parameters = parameters };
            var sb = new CharWriter();
            try
            {
                ConvertPersistUpdate(ref sb, sets, where, modelDetail, operationContext);
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }
        }

        /// <summary>
        /// Builds the UPDATE of every row matching a condition into the provided <see cref="CharWriter"/>: each member set to its value converted in terms of the row, which reads only the model's own columns, then the condition as <see cref="ConvertPersistWhere"/> writes it.
        /// </summary>
        /// <param name="sb">The writer to append the SQL into.</param>
        /// <param name="sets">The members to set from <see cref="ProjectionMember.FromSet"/>, each with its value in terms of the row.</param>
        /// <param name="where">The condition selecting the rows.</param>
        /// <param name="modelDetail">Metadata about the model being changed.</param>
        /// <param name="operationContext">The current member context for the operation.</param>
        protected abstract void ConvertPersistUpdate(ref CharWriter sb, ProjectionMember[] sets, LambdaExpression where, ModelDetail modelDetail, MemberContext operationContext);

        /// <summary>
        /// Builds the SQL query into the provided <see cref="CharWriter"/>.
        /// </summary>
        /// <param name="sb">The writer to append the SQL into.</param>
        /// <param name="count">Whether the statement counts the rows rather than reading them.</param>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="graph">An optional graph describing which properties to include.</param>
        /// <param name="modelDetail">Metadata about the model being queried.</param>
        /// <param name="operationContext">The current member context for the operation.</param>
        protected void ConvertSubquery(ref CharWriter sb, bool count, LambdaExpression? where, Graph? graph, ModelDetail modelDetail, MemberContext operationContext)
        {
            GenerateSelect(count, graph, modelDetail, ref sb);
            GenerateFrom(modelDetail, ref sb);
            if (where is null)
                return;
            var rootDependant = new ParameterDependant(modelDetail, null);
            var sbWhere = new CharWriter();
            try
            {
                GenerateWhere(where, ref sbWhere, rootDependant, operationContext);
                GenerateJoin(rootDependant, ref sb);
                sb.Write(sbWhere);
            }
            finally
            {
                sbWhere.Dispose();
            }
        }
        /// <summary>
        /// Converts a LINQ <see cref="Expression"/> node into its SQL representation.
        /// </summary>
        /// <param name="exp">The expression to convert.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        protected void ConvertToSql(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            if (context.IsSort && exp.NodeType != ExpressionType.Lambda)
            {
                context.IsSort = false;
                exp = EnumAsNumber(exp);
            }
            switch (exp.NodeType)
            {
                case ExpressionType.Add:
                    if (TryConvertToSqlDateArithmetic(exp, ref sb, context))
                        break;
                    if (exp.Type == typeof(string))
                        ConvertToSqlStringConcat(exp, ref sb, context);
                    else
                        ConvertToSqlBinary(Operator.Add, exp, ref sb, context);
                    break;
                case ExpressionType.AddAssign:
                    throw new NotImplementedException();
                case ExpressionType.AddAssignChecked:
                    throw new NotImplementedException();
                case ExpressionType.AddChecked:
                    if (TryConvertToSqlDateArithmetic(exp, ref sb, context))
                        break;
                    ConvertToSqlBinary(Operator.Add, exp, ref sb, context);
                    break;
                case ExpressionType.And:
                    if (exp.Type == typeof(bool) || exp.Type == typeof(bool?))
                        ConvertToSqlBinary(context.Inverted ? Operator.Or : Operator.And, exp, ref sb, context);
                    else
                        ConvertToSqlBinary(Operator.BitwiseAnd, exp, ref sb, context);
                    break;
                case ExpressionType.AndAlso:
                    if (!ConvertToSqlConditionAsValue(exp, ref sb, context))
                        ConvertToSqlBinary(context.Inverted ? Operator.Or : Operator.And, exp, ref sb, context);
                    break;
                case ExpressionType.AndAssign:
                    throw new NotImplementedException();
                case ExpressionType.ArrayIndex:
                    ConvertToSqlArrayIndex(exp, ref sb, context);
                    break;
                case ExpressionType.ArrayLength:
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
                case ExpressionType.Assign:
                    throw new NotImplementedException();
                case ExpressionType.Block:
                    throw new NotImplementedException();
                case ExpressionType.Call:
                    if (exp is MethodCallExpression { Object: ConstantExpression { Value: LinqSubquery subquery } } subqueryCall)
                    {
                        if (!ConvertToSqlConditionAsValue(exp, ref sb, context))
                            ConvertToSqlSubquery(subquery, subqueryCall.Arguments[0], ref sb, context);
                    }
                    else if (exp is MethodCallExpression { Method.Name: nameof(Enum.HasFlag) } hasFlag && hasFlag.Method.DeclaringType == typeof(Enum))
                        ConvertToSql(HasFlagAsNumbers(hasFlag), ref sb, context);
                    else
                        ConvertToSqlCall(exp, ref sb, context);
                    break;
                case ExpressionType.Coalesce:
                    if (IsEvaluatable(exp))
                        ConvertToSqlEvaluate(exp, ref sb, context);
                    else if (EnumOf(exp.Type) is { } coalesceEnum && IsStoredEnum(exp, context.MemberContext))
                        ConvertToSql(EnumAsText(exp, coalesceEnum, context.MemberContext), ref sb, context);
                    else
                        ConvertToSqlCoalesce(exp, ref sb, context);
                    break;
                case ExpressionType.Conditional:
                    if (IsEvaluatable(exp))
                        ConvertToSqlEvaluate(exp, ref sb, context);
                    else if (EnumOf(exp.Type) is { } conditionalEnum && IsStoredEnum(exp, context.MemberContext))
                        ConvertToSql(EnumAsText(exp, conditionalEnum, context.MemberContext), ref sb, context);
                    else
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
                    ConvertToSqlEvaluate(exp, ref sb, context);
                    break;
                case ExpressionType.Divide:
                    ConvertToSqlBinary(Operator.Divide, exp, ref sb, context);
                    break;
                case ExpressionType.DivideAssign:
                    throw new NotImplementedException();
                case ExpressionType.Dynamic:
                    throw new NotImplementedException();
                case ExpressionType.Equal:
                    if (!ConvertToSqlConditionAsValue(exp, ref sb, context))
                        ConvertToSqlComparison(context.Inverted ? Operator.NotEquals : Operator.Equals, exp, ref sb, context);
                    break;
                case ExpressionType.ExclusiveOr:
                    if (exp.Type == typeof(bool) || exp.Type == typeof(bool?))
                        throw new NotSupportedException("Exclusive or of conditions is not supported");
                    ConvertToSqlBinary(Operator.BitwiseXor, exp, ref sb, context);
                    break;
                case ExpressionType.ExclusiveOrAssign:
                    throw new NotImplementedException();
                case ExpressionType.Extension:
                    throw new NotImplementedException();
                case ExpressionType.Goto:
                    throw new NotImplementedException();
                case ExpressionType.GreaterThan:
                    if (!ConvertToSqlConditionAsValue(exp, ref sb, context))
                        ConvertToSqlComparison(context.Inverted ? Operator.LessThanOrEquals : Operator.GreaterThan, exp, ref sb, context);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    if (!ConvertToSqlConditionAsValue(exp, ref sb, context))
                        ConvertToSqlComparison(context.Inverted ? Operator.LessThan : Operator.GreaterThanOrEquals, exp, ref sb, context);
                    break;
                case ExpressionType.Increment:
                    throw new NotImplementedException();
                case ExpressionType.Index:
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
                case ExpressionType.Invoke:
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
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
                    if (!ConvertToSqlConditionAsValue(exp, ref sb, context))
                        ConvertToSqlComparison(context.Inverted ? Operator.GreaterThanOrEquals : Operator.LessThan, exp, ref sb, context);
                    break;
                case ExpressionType.LessThanOrEqual:
                    if (!ConvertToSqlConditionAsValue(exp, ref sb, context))
                        ConvertToSqlComparison(context.Inverted ? Operator.GreaterThan : Operator.LessThanOrEquals, exp, ref sb, context);
                    break;
                case ExpressionType.ListInit:
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
                case ExpressionType.Loop:
                    throw new NotImplementedException();
                case ExpressionType.MemberAccess:
                    ConvertToSqlMember(exp, ref sb, context);
                    break;
                case ExpressionType.MemberInit:
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
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
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
                case ExpressionType.NewArrayInit:
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
                case ExpressionType.Not:
                    if (exp.Type == typeof(bool) || exp.Type == typeof(bool?))
                    {
                        if (!ConvertToSqlConditionAsValue(exp, ref sb, context))
                        {
                            context.InvertStack++;
                            ConvertToSqlUnary(Operator.Not, exp, ref sb, context);
                            context.InvertStack--;
                        }
                    }
                    else
                    {
                        //C# writes ~ on an integer as Not
                        ConvertToSqlUnary(Operator.BitwiseNot, exp, ref sb, context);
                    }
                    break;
                case ExpressionType.NotEqual:
                    if (!ConvertToSqlConditionAsValue(exp, ref sb, context))
                        ConvertToSqlComparison(context.Inverted ? Operator.Equals : Operator.NotEquals, exp, ref sb, context);
                    break;
                case ExpressionType.OnesComplement:
                    ConvertToSqlUnary(Operator.BitwiseNot, exp, ref sb, context);
                    break;
                case ExpressionType.Or:
                    if (exp.Type == typeof(bool) || exp.Type == typeof(bool?))
                        ConvertToSqlBinary(context.Inverted ? Operator.And : Operator.Or, exp, ref sb, context);
                    else
                        ConvertToSqlBinary(Operator.BitwiseOr, exp, ref sb, context);
                    break;
                case ExpressionType.OrAssign:
                    throw new NotImplementedException();
                case ExpressionType.OrElse:
                    if (!ConvertToSqlConditionAsValue(exp, ref sb, context))
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
                    {
                        //the Power node comes from VB, C# calls Math.Pow which the dialects convert
                        context.MemberContext.OperatorStack.Push(Operator.Call);
                        var invertStack = context.InvertStack;
                        context.InvertStack = 0;
                        var power = (BinaryExpression)exp;
                        sb.Write("POWER((");
                        ConvertToSql(power.Left, ref sb, context);
                        sb.Write("),(");
                        ConvertToSql(power.Right, ref sb, context);
                        sb.Write("))");
                        context.InvertStack = invertStack;
                        _ = context.MemberContext.OperatorStack.Pop();
                        break;
                    }
                case ExpressionType.PowerAssign:
                    throw new NotImplementedException();
                case ExpressionType.PreDecrementAssign:
                    throw new NotImplementedException();
                case ExpressionType.PreIncrementAssign:
                    throw new NotImplementedException();
                case ExpressionType.Quote:
                    ConvertToSql(((UnaryExpression)exp).Operand, ref sb, context);
                    break;
                case ExpressionType.RightShift:
                    throw new NotImplementedException();
                case ExpressionType.RightShiftAssign:
                    throw new NotImplementedException();
                case ExpressionType.RuntimeVariables:
                    throw new NotImplementedException();
                case ExpressionType.Subtract:
                    if (IsDateDifference(exp))
                        throw new NotSupportedException("The time between two values is read through a total, such as (x.End - x.Start).TotalHours, or compared to a time span");
                    if (TryConvertToSqlDateArithmetic(exp, ref sb, context))
                        break;
                    ConvertToSqlBinary(Operator.Subtract, exp, ref sb, context);
                    break;
                case ExpressionType.SubtractAssign:
                    throw new NotImplementedException();
                case ExpressionType.SubtractAssignChecked:
                    throw new NotImplementedException();
                case ExpressionType.SubtractChecked:
                    if (TryConvertToSqlDateArithmetic(exp, ref sb, context))
                        break;
                    ConvertToSqlBinary(Operator.Subtract, exp, ref sb, context);
                    break;
                case ExpressionType.Switch:
                    throw new NotImplementedException();
                case ExpressionType.Throw:
                    throw new NotImplementedException();
                case ExpressionType.Try:
                    throw new NotImplementedException();
                case ExpressionType.TypeAs:
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
                case ExpressionType.TypeEqual:
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
                case ExpressionType.TypeIs:
                    ConvertToSqlEvaluateOnly(exp, ref sb, context);
                    break;
                case ExpressionType.UnaryPlus:
                    ConvertToSqlUnary(Operator.Null, exp, ref sb, context);
                    break;
                case ExpressionType.Unbox:
                    ConvertToSqlUnary(Operator.Null, exp, ref sb, context);
                    break;
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
            var prefix = OperatorToString(prefixOperation, unary.Type);

            if (prefix is null)
            {
                var enumOperand = WithoutConvert(unary.Operand);
                if (IsEnumNumber(unary.Type) && IsStoredEnum(enumOperand, context.MemberContext))
                    ConvertToSqlEnumNumber(enumOperand, ref sb, context);
                else
                    ConvertToSql(unary.Operand, ref sb, context);
            }
            else
            {
                var castSigned = prefixOperation == Operator.BitwiseNot && BitwiseResultUnsigned;
                if (castSigned)
                    sb.Write("CAST(");

                //the operand may be an expression like a+b so it needs its own brackets
                sb.Write(prefix);
                sb.Write('(');
                ConvertToSql(unary.Operand, ref sb, context);
                sb.Write(')');

                if (castSigned)
                    sb.Write(" AS SIGNED)");
            }

            _ = context.MemberContext.OperatorStack.Pop();
        }
        private bool ConvertToSqlConditionAsValue(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            var lastOperator = context.MemberContext.OperatorStack.Peek();
            if (lastOperator == Operator.And || lastOperator == Operator.Or || lastOperator == Operator.Not || (lastOperator == Operator.Lambda && !context.IsOrderBy))
                return false;

            //a condition where a value is expected, such as a projected member IsActive = x.Status == "Active"
            context.MemberContext.OperatorStack.Push(Operator.And);
            ConvertToSqlBooleanValue(exp, ref sb, context);
            _ = context.MemberContext.OperatorStack.Pop();
            return true;
        }

        private void ConvertToSqlComparison(Operator operation, Expression exp, ref CharWriter sb, BuilderContext context)
        {
            if (!TryConvertToSqlSpanComparison(operation, exp, ref sb, context))
                ConvertToSqlBinary(operation, exp, ref sb, context);
        }
        //C# compares chars as their numbers, so a char literal arrives as an int where the column holds the character
        private static void ConvertCharComparison(ref Expression left, ref Expression right)
        {
            if (left is UnaryExpression { NodeType: ExpressionType.Convert, Operand: var leftChar } && (leftChar.Type == typeof(char) || leftChar.Type == typeof(char?)) && right is ConstantExpression { Value: int rightNumber })
            {
                left = leftChar;
                right = Expression.Constant((char)rightNumber, leftChar.Type);
            }
            else if (right is UnaryExpression { NodeType: ExpressionType.Convert, Operand: var rightChar } && (rightChar.Type == typeof(char) || rightChar.Type == typeof(char?)) && left is ConstantExpression { Value: int leftNumber })
            {
                right = rightChar;
                left = Expression.Constant((char)leftNumber, rightChar.Type);
            }
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

            if (binaryRight is not null && operation is Operator.Equals or Operator.NotEquals or Operator.LessThan or Operator.LessThanOrEquals or Operator.GreaterThan or Operator.GreaterThanOrEquals)
            {
                ConvertEnumComparison(operation, ref binaryLeft, ref binaryRight!, context.MemberContext);
                ConvertCharComparison(ref binaryLeft, ref binaryRight);
            }

            //SQL comparisons with NULL are unknown, C# gives true or false, the null cases C# counts as true are added
            if (binaryRight is not null && (operation == Operator.Equals || operation == Operator.NotEquals || operation == Operator.LessThan || operation == Operator.LessThanOrEquals || operation == Operator.GreaterThan || operation == Operator.GreaterThanOrEquals))
            {
                var leftCanBeNull = CanBeNull(binaryLeft);
                var rightCanBeNull = CanBeNull(binaryRight);
                var addNulls = operation switch
                {
                    Operator.Equals => leftCanBeNull && rightCanBeNull,
                    Operator.NotEquals => leftCanBeNull || rightCanBeNull,
                    _ => context.Inverted && (leftCanBeNull || rightCanBeNull),
                };
                if (addNulls)
                {
                    sb.Write("((");
                    ConvertToSql(binaryLeft, ref sb, context);
                    sb.Write(')');
                    sb.Write(OperatorToString(operation, binary.Type));
                    sb.Write('(');
                    ConvertToSql(binaryRight, ref sb, context);
                    sb.Write(')');

                    if (operation == Operator.Equals)
                    {
                        //both null are equal
                        sb.Write(" OR ((");
                        ConvertToSql(binaryLeft, ref sb, context);
                        sb.Write(") IS NULL AND (");
                        ConvertToSql(binaryRight, ref sb, context);
                        sb.Write(") IS NULL)");
                    }
                    else if (operation == Operator.NotEquals)
                    {
                        //one null is not equal, both null is equal
                        if (leftCanBeNull)
                        {
                            sb.Write(" OR ((");
                            ConvertToSql(binaryLeft, ref sb, context);
                            sb.Write(") IS NULL");
                            if (rightCanBeNull)
                            {
                                sb.Write(" AND (");
                                ConvertToSql(binaryRight, ref sb, context);
                                sb.Write(") IS NOT NULL");
                            }
                            sb.Write(')');
                        }
                        if (rightCanBeNull)
                        {
                            sb.Write(" OR ((");
                            ConvertToSql(binaryRight, ref sb, context);
                            sb.Write(") IS NULL");
                            if (leftCanBeNull)
                            {
                                sb.Write(" AND (");
                                ConvertToSql(binaryLeft, ref sb, context);
                                sb.Write(") IS NOT NULL");
                            }
                            sb.Write(')');
                        }
                    }
                    else
                    {
                        //a negated comparison is true when a side is null
                        if (leftCanBeNull)
                        {
                            sb.Write(" OR (");
                            ConvertToSql(binaryLeft, ref sb, context);
                            sb.Write(") IS NULL");
                        }
                        if (rightCanBeNull)
                        {
                            sb.Write(" OR (");
                            ConvertToSql(binaryRight, ref sb, context);
                            sb.Write(") IS NULL");
                        }
                    }

                    sb.Write(')');
                    _ = context.MemberContext.OperatorStack.Pop();
                    return;
                }
            }

            var castSigned = BitwiseResultUnsigned && (operation == Operator.BitwiseAnd || operation == Operator.BitwiseOr || operation == Operator.BitwiseXor);
            if (castSigned)
                sb.Write("CAST(");

            sb.Write('(');
            ConvertToSql(binaryLeft, ref sb, context);
            sb.Write(')');

            sb.Write(OperatorToString(operation, binary.Type));

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

            if (castSigned)
                sb.Write(" AS SIGNED)");

            _ = context.MemberContext.OperatorStack.Pop();
        }
        /// <summary>
        /// Whether bitwise operators give an unsigned 64 bit result, such as in MySQL, so the result is cast back to a signed number to match .NET.
        /// </summary>
        protected virtual bool BitwiseResultUnsigned => false;
        private bool CanBeNull(Expression exp)
        {
            //a value read from one row of a related collection is null when there is no row
            if (exp.Type.IsValueType && Nullable.GetUnderlyingType(exp.Type) is null)
                return !IsEvaluatable(exp) && RelatedElementFinder.Reads(exp);
            //a value is known, a null value is written as IS NULL instead
            return !IsEvaluatable(exp);
        }
        //whether an expression reads one row of a related collection, such as x.Lines.OrderBy(l => l.Price).First().Quantity
        private sealed class RelatedElementFinder : ExpressionVisitor
        {
            private bool found;
            public static bool Reads(Expression exp)
            {
                var finder = new RelatedElementFinder();
                _ = finder.Visit(exp);
                return finder.found;
            }
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.DeclaringType == typeof(Enumerable) && node.Method.Name is "First" or "FirstOrDefault" or "Single" or "SingleOrDefault" or "Last" or "LastOrDefault" or "ElementAt" or "ElementAtOrDefault")
                {
                    found = true;
                    return node;
                }
                return base.VisitMethodCall(node);
            }
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

            if (context.MemberContext.ResultColumns is not null)
            {
                //a member of a result in its having or order, at any depth, is the column that computes it, named by the members from the result down
                var owner = member.Expression;
                var path = member.Member.Name;
                while (owner is MemberExpression parent)
                {
                    path = $"{parent.Member.Name}.{path}";
                    owner = parent.Expression;
                }
                if (owner is ParameterExpression resultParameter && context.MemberContext.ResultColumns.TryGetValue(resultParameter, out var resultColumns))
                {
                    if (resultColumns.TryGetValue(path, out var column))
                    {
                        sb.Write(column);
                        ConvertToSqlBooleanColumn(member.Type, ref sb, context);
                        return;
                    }
                    //a member of a value, such as a string's Length, reads the column of the value
                    var ownerPath = path.Length == member.Member.Name.Length ? String.Empty : path.Substring(0, path.Length - member.Member.Name.Length - 1);
                    if (!resultColumns.ContainsKey(ownerPath))
                        throw new NotSupportedException($"{path} isn't a value of the result, so its results can't be filtered or ordered by it");
                }
            }

            if (TryConvertToSqlSpanMember(member, ref sb, context))
                return;

            if (member.Expression is null || IsEvaluatable(member.Expression))
            {
                ConvertToSqlEvaluate(member, ref sb, context);
            }
            else if (!ConvertToSqlMemberFunction(member, ref sb, context))
            {
                context.MemberContext.MemberAccessStack.Push(member);
                ConvertToSql(member.Expression, ref sb, context);
                _ = context.MemberContext.MemberAccessStack.Pop();
            }
        }
        //a key of several members isn't one value to look for, so a row of the query with each of them exists
        private void ConvertToSqlSubqueryKeyRow(LinqSubquery subquery, MemberInitExpression keyRow, ref CharWriter sb, BuilderContext context)
        {
            var query = subquery.Query;
            var modelDetail = ModelAnalyzer.GetModel(query.ModelType);
            var statement = ConvertLinq(query, LinqPlan.Read(query, modelDetail), modelDetail, context.MemberContext.Parameters);
            if (statement.Pieces is not null)
                throw new NotSupportedException("A query a condition looks in selects values of its own row, not a relation");
            var values = statement.Plan.Shape!.Values!;
            var inverted = context.Inverted;
            var invertStack = context.InvertStack;
            context.InvertStack = 0;
            sb.Write(inverted ? "NOT EXISTS(SELECT 1 FROM(" : "EXISTS(SELECT 1 FROM(");
            sb.Write(statement.Sql);
            sb.Write(")AS Z WHERE ");
            context.MemberContext.OperatorStack.Push(Operator.Call);
            for (var i = 0; i < keyRow.Bindings.Count; i++)
            {
                if (i > 0)
                    sb.Write(" AND ");
                sb.Write(QuoteColumn("Z", $"C{values[i].Column!.Index}"));
                sb.Write('=');
                ConvertToSql(WithoutConvert(((MemberAssignment)keyRow.Bindings[i]).Expression), ref sb, context);
            }
            _ = context.MemberContext.OperatorStack.Pop();
            sb.Write(')');
            context.InvertStack = invertStack;
        }
        /// <summary>
        /// Converts looking for a key of several members, a <c>LinqKeyRow</c> built from the model, in a collection of keys known when the query is written: each key's members matched together, as SQL has no row of values to look for in a list.
        /// </summary>
        /// <param name="collection">The keys.</param>
        /// <param name="value">The key looked for.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        /// <returns>Whether it was a key of several members and is written.</returns>
        protected bool TryConvertToSqlKeyRows(Expression collection, Expression value, ref CharWriter sb, BuilderContext context)
        {
            if (WithoutConvert(value) is not MemberInitExpression keyRow || keyRow.Type != typeof(LinqKeyRow) || !IsEvaluatable(collection))
                return false;
            var inverted = context.Inverted;
            var invertStack = context.InvertStack;
            context.InvertStack = 0;
            var columns = new string[keyRow.Bindings.Count];
            for (var i = 0; i < columns.Length; i++)
                columns[i] = Argument(WithoutConvert(((MemberAssignment)keyRow.Bindings[i]).Expression), context);
            context.MemberContext.OperatorStack.Push(Operator.Call);
            sb.Write(inverted ? "NOT(" : "(");
            var any = false;
            foreach (var item in (System.Collections.IEnumerable)Evaluate(collection)!)
            {
                var key = (LinqKeyRow)item;
                if (any)
                    sb.Write(" OR ");
                any = true;
                sb.Write('(');
                for (var i = 0; i < columns.Length; i++)
                {
                    if (i > 0)
                        sb.Write(" AND ");
                    sb.Write(columns[i]);
                    sb.Write('=');
                    var part = key[i];
                    ConvertToSqlValue(part?.GetType() ?? typeof(object), part, ref sb, context);
                }
                sb.Write(')');
            }
            if (!any)
                sb.Write("1=0");
            sb.Write(')');
            _ = context.MemberContext.OperatorStack.Pop();
            context.InvertStack = invertStack;
            return true;
        }
        /// <summary>
        /// Gets the columns of a relation's key in pairs, in the order of the identity referred to: the member holding the foreign key, on the related model for a collection and on the model for a single relation, and the identity member it refers to.
        /// </summary>
        /// <param name="relation">The relation member.</param>
        /// <param name="model">The model the relation is a member of.</param>
        /// <returns>The pairs of columns.</returns>
        protected static (ModelMemberDetail Key, ModelMemberDetail Identity)[] RelationKey(ModelMemberDetail relation, ModelDetail model)
        {
            var related = ModelAnalyzer.GetModel(relation.ActualType);
            var holder = relation.IsEnumerable ? related : model;
            var referred = relation.IsEnumerable ? model : related;
            var names = relation.ForeignIdentities ?? throw new Exception($"{relation.Type.Name} missing Foreign Identity");
            if (names.Length != referred.IdentityMembers.Count)
                throw new NotSupportedException($"{relation.Name} names {names.Length} foreign identities where {referred.Type.Name} has {referred.IdentityMembers.Count} identities");
            var pairs = new (ModelMemberDetail Key, ModelMemberDetail Identity)[names.Length];
            for (var i = 0; i < names.Length; i++)
                pairs[i] = (holder.GetMember(names[i]), referred.IdentityMembers[i]);
            return pairs;
        }
        /// <summary>
        /// Gets the name in the statement of the table an expression reads a related collection from: an alias when a join needed one.
        /// </summary>
        /// <param name="owner">The expression the collection is a member of.</param>
        /// <param name="context">The current builder context.</param>
        /// <returns>The name of the table.</returns>
        protected static string RelationTable(Expression owner, BuilderContext context)
            => FindDependant(owner, context)?.TableName ?? ModelAnalyzer.GetModel(owner.Type).DataSourceEntityName;
        private static ParameterDependant? FindDependant(Expression owner, BuilderContext context)
        {
            switch (owner)
            {
                case ParameterExpression:
                    if (context.MemberContext.DependantStack.Count == 0)
                        return null;
                    var current = context.MemberContext.DependantStack.Peek();
                    return current.ModelDetail.Type == owner.Type ? current : null;
                case MemberExpression { Expression: not null } member:
                    return FindDependant(member.Expression, context) is { } parent && parent.Dependants.TryGetValue(member.Member.Name, out var dependant) ? dependant : null;
                case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion:
                    return FindDependant(conversion.Operand, context);
                default:
                    return null;
            }
        }
        /// <summary>
        /// Gets the name in the statement of the table a lambda over a related collection matches its rows to.
        /// </summary>
        /// <param name="property">The member reading the collection.</param>
        /// <param name="callingModel">The model the collection belongs to.</param>
        /// <param name="context">The current builder context.</param>
        /// <returns>The name of the table.</returns>
        protected static string CallingTable(MemberExpression property, ModelDetail callingModel, BuilderContext context)
            => context.MemberContext.CallingTables is not null && context.MemberContext.CallingTables.TryGetValue(property, out var table) ? table : callingModel.DataSourceEntityName;

        //a query a condition looks in is written in the statement, a page of it in a derived table as MySQL and MariaDB refuse a LIMIT in an IN
        private void ConvertToSqlSubquery(LinqSubquery subquery, Expression value, ref CharWriter sb, BuilderContext context)
        {
            if (value is UnaryExpression { NodeType: ExpressionType.Convert } boxed)
                value = boxed.Operand;
            if (value is MemberInitExpression keyRow && keyRow.Type == typeof(LinqKeyRow))
            {
                ConvertToSqlSubqueryKeyRow(subquery, keyRow, ref sb, context);
                return;
            }
            var inverted = context.Inverted;
            var invertStack = context.InvertStack;
            context.InvertStack = 0;
            context.MemberContext.OperatorStack.Push(Operator.Call);
            ConvertToSql(value, ref sb, context);
            _ = context.MemberContext.OperatorStack.Pop();
            context.InvertStack = invertStack;
            sb.Write(inverted ? " NOT IN(" : " IN(");
            var query = subquery.Query;
            var modelDetail = ModelAnalyzer.GetModel(query.ModelType);
            var statement = ConvertLinq(query, LinqPlan.Read(query, modelDetail), modelDetail, context.MemberContext.Parameters);
            if (statement.Pieces is not null)
                throw new NotSupportedException("A query a condition looks in selects one value of its own row, not a relation");
            //MySQL and MariaDB also refuse to read the table being changed unless it is a derived table
            if (query.Skip.HasValue || query.Take.HasValue || context.RootDependant.ModelDetail.Type == query.ModelType)
            {
                sb.Write("SELECT * FROM(");
                sb.Write(statement.Sql);
                sb.Write(")AS Z");
            }
            else
            {
                sb.Write(statement.Sql);
            }
            sb.Write(')');
        }

        /// <summary>
        /// Converts a member that is a SQL function of the expression it is read from, such as <c>string.Length</c>, <c>DateTime.Year</c>, or <c>Nullable.HasValue</c>.
        /// Only called when the expression the member is read from can't be evaluated, so it involves the model.
        /// </summary>
        /// <param name="member">The member access expression to convert.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        /// <returns><see langword="true"/> if the member was written; <see langword="false"/> if it is not a function member.</returns>
        protected abstract bool ConvertToSqlMemberFunction(MemberExpression member, ref CharWriter sb, BuilderContext context);
        /// <summary>
        /// Converts a null coalescing (<c>??</c>) expression into its SQL representation.
        /// </summary>
        /// <param name="exp">The coalesce expression to convert.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        protected abstract void ConvertToSqlCoalesce(Expression exp, ref CharWriter sb, BuilderContext context);
        /// <summary>
        /// Converts a string addition (<c>a + b</c>) into its SQL representation.
        /// </summary>
        /// <param name="exp">The add expression with a string result to convert.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        protected abstract void ConvertToSqlStringConcat(Expression exp, ref CharWriter sb, BuilderContext context);
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
        //a column of a result that is a bool used as a condition is compared, which also carries any NOT around it, as a model's bool member is
        private void ConvertToSqlBooleanColumn(Type type, ref CharWriter sb, BuilderContext context)
        {
            if (type != typeof(bool) && type != typeof(bool?))
                return;
            var lastOperator = context.MemberContext.OperatorStack.Peek();
            if (lastOperator == Operator.And || lastOperator == Operator.Or || lastOperator == Operator.Not || (lastOperator == Operator.Lambda && !context.IsOrderBy))
                sb.Write(BooleanCondition(context.Inverted));
        }
        /// <summary>
        /// Writes the comparison making a bool column a condition.
        /// </summary>
        /// <param name="inverted">Whether the condition is that the column is false.</param>
        /// <returns>The comparison.</returns>
        protected virtual string BooleanCondition(bool inverted) => inverted ? "=0" : "=1";
        private void ConvertToSqlParameter(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            var parameter = (ParameterExpression)exp;

            //an element of a set that is one value is the column holding it
            if (context.MemberContext.ResultColumns is not null && context.MemberContext.ResultColumns.TryGetValue(parameter, out var resultColumns) && resultColumns.TryGetValue(String.Empty, out var resultColumn))
            {
                sb.Write(resultColumn);
                ConvertToSqlBooleanColumn(parameter.Type, ref sb, context);
                return;
            }

            if (parameter.Name is null)
                throw new Exception($"Parameter has no name {parameter.Type.Name}");

            var modelDetail = context.MemberContext.ModelContexts[parameter];

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
        /// Converts a condition used where a value is expected, such as a projected member set to a comparison, into its SQL representation.
        /// The condition is a boolean value on most databases; one without a boolean type writes it as a CASE giving 1 or 0.
        /// </summary>
        /// <param name="exp">The condition to convert.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        protected virtual void ConvertToSqlBooleanValue(Expression exp, ref CharWriter sb, BuilderContext context) => ConvertToSql(exp, ref sb, context);
        /// <summary>
        /// Evaluates an expression to a value and converts it into its SQL representation.
        /// </summary>
        /// <param name="exp">The expression to evaluate and convert.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        protected void ConvertToSqlEvaluate(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            //no operator is pushed, a boolean value needs to see the operator it is used in to know if it is a condition
            var value = Evaluate(exp);
            ConvertToSqlValue(exp.Type, value, ref sb, context);
        }
        private void ConvertToSqlEvaluateOnly(Expression exp, ref CharWriter sb, BuilderContext context)
        {
            if (!IsEvaluatable(exp))
                throw new NotSupportedException($"Cannot convert {exp.NodeType} expression that uses the model");
            ConvertToSqlEvaluate(exp, ref sb, context);
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
            if (context.MemberContext.MemberAccessStack.Count > 0)
                throw new NotSupportedException($"{type.FullName}.{context.MemberContext.MemberAccessStack.Peek().Member.Name} not supported");

            ConvertToSqlValueRender(type, value, ref sb, context);
        }

        /// <summary>The type name of <see cref="Nullable{T}"/>, used for nullable type detection.</summary>
        protected static readonly string nullableTypeName = typeof(Nullable<>).Name;

        /// <summary>
        /// Renders a value into SQL.
        /// </summary>
        /// <param name="type">The CLR type of the value.</param>
        /// <param name="value">The value to render.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="context">The current builder context.</param>
        protected abstract void ConvertToSqlValueRender(Type type, object? value, ref CharWriter sb, BuilderContext context);

        /// <summary>
        /// Determines whether the given expression can be fully evaluated to a constant value
        /// without requiring model/parameter context.
        /// </summary>
        /// <param name="exp">The expression to check.</param>
        /// <returns><see langword="true"/> if the expression is evaluatable; otherwise, <see langword="false"/>.</returns>
        protected bool IsEvaluatable(Expression exp)
        {
            return IsEvaluatable(exp, null);
        }
        //scope holds the parameters of lambdas and blocks inside the expression, those have values when it's evaluated
        private bool IsEvaluatable(Expression exp, List<ParameterExpression>? scope)
        {
            switch (exp.NodeType)
            {
                case ExpressionType.Constant:
                case ExpressionType.Default:
                    return true;

                case ExpressionType.Parameter:
                    return scope is not null && scope.Contains((ParameterExpression)exp);

                case ExpressionType.MemberAccess:
                    {
                        var member = (MemberExpression)exp;
                        return member.Expression is null || IsEvaluatable(member.Expression, scope);
                    }

                case ExpressionType.Call:
                    {
                        var call = (MethodCallExpression)exp;
                        foreach (var arg in call.Arguments)
                        {
                            if (!IsEvaluatable(arg, scope))
                                return false;
                        }
                        return call.Object is null || IsEvaluatable(call.Object, scope);
                    }

                case ExpressionType.Lambda:
                    {
                        var lambda = (LambdaExpression)exp;
                        scope ??= new();
                        foreach (var parameter in lambda.Parameters)
                            scope.Add(parameter);
                        var result = IsEvaluatable(lambda.Body, scope);
                        foreach (var parameter in lambda.Parameters)
                            _ = scope.Remove(parameter);
                        return result;
                    }

                case ExpressionType.Block:
                    {
                        var block = (BlockExpression)exp;
                        scope ??= new();
                        foreach (var variable in block.Variables)
                            scope.Add(variable);
                        var result = true;
                        foreach (var expression in block.Expressions)
                        {
                            if (!IsEvaluatable(expression, scope))
                            {
                                result = false;
                                break;
                            }
                        }
                        foreach (var variable in block.Variables)
                            _ = scope.Remove(variable);
                        return result;
                    }

                case ExpressionType.Conditional:
                    {
                        var conditional = (ConditionalExpression)exp;
                        return IsEvaluatable(conditional.Test, scope) && IsEvaluatable(conditional.IfTrue, scope) && IsEvaluatable(conditional.IfFalse, scope);
                    }

                case ExpressionType.New:
                    {
                        var newExp = (NewExpression)exp;
                        foreach (var arg in newExp.Arguments)
                        {
                            if (!IsEvaluatable(arg, scope))
                                return false;
                        }
                        return true;
                    }

                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    {
                        var newArray = (NewArrayExpression)exp;
                        foreach (var expression in newArray.Expressions)
                        {
                            if (!IsEvaluatable(expression, scope))
                                return false;
                        }
                        return true;
                    }

                case ExpressionType.ListInit:
                    {
                        var listInit = (ListInitExpression)exp;
                        if (!IsEvaluatable(listInit.NewExpression, scope))
                            return false;
                        foreach (var initializer in listInit.Initializers)
                        {
                            foreach (var arg in initializer.Arguments)
                            {
                                if (!IsEvaluatable(arg, scope))
                                    return false;
                            }
                        }
                        return true;
                    }

                case ExpressionType.MemberInit:
                    {
                        var memberInit = (MemberInitExpression)exp;
                        return IsEvaluatable(memberInit.NewExpression, scope) && IsEvaluatableBindings(memberInit.Bindings, scope);
                    }

                case ExpressionType.Invoke:
                    {
                        var invoke = (InvocationExpression)exp;
                        foreach (var arg in invoke.Arguments)
                        {
                            if (!IsEvaluatable(arg, scope))
                                return false;
                        }
                        return IsEvaluatable(invoke.Expression, scope);
                    }

                case ExpressionType.Index:
                    {
                        var index = (IndexExpression)exp;
                        foreach (var arg in index.Arguments)
                        {
                            if (!IsEvaluatable(arg, scope))
                                return false;
                        }
                        return index.Object is null || IsEvaluatable(index.Object, scope);
                    }

                case ExpressionType.TypeIs:
                case ExpressionType.TypeEqual:
                    return IsEvaluatable(((TypeBinaryExpression)exp).Expression, scope);

                case ExpressionType.ArrayLength:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.Decrement:
                case ExpressionType.Increment:
                case ExpressionType.IsFalse:
                case ExpressionType.IsTrue:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.OnesComplement:
                case ExpressionType.PostDecrementAssign:
                case ExpressionType.PostIncrementAssign:
                case ExpressionType.PreDecrementAssign:
                case ExpressionType.PreIncrementAssign:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                case ExpressionType.UnaryPlus:
                case ExpressionType.Unbox:
                    return IsEvaluatable(((UnaryExpression)exp).Operand, scope);

                case ExpressionType.Add:
                case ExpressionType.AddAssign:
                case ExpressionType.AddAssignChecked:
                case ExpressionType.AddChecked:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.AndAssign:
                case ExpressionType.ArrayIndex:
                case ExpressionType.Assign:
                case ExpressionType.Coalesce:
                case ExpressionType.Divide:
                case ExpressionType.DivideAssign:
                case ExpressionType.Equal:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.ExclusiveOrAssign:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LeftShift:
                case ExpressionType.LeftShiftAssign:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.Modulo:
                case ExpressionType.ModuloAssign:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.MultiplyAssignChecked:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.NotEqual:
                case ExpressionType.Or:
                case ExpressionType.OrAssign:
                case ExpressionType.OrElse:
                case ExpressionType.Power:
                case ExpressionType.PowerAssign:
                case ExpressionType.RightShift:
                case ExpressionType.RightShiftAssign:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractAssign:
                case ExpressionType.SubtractAssignChecked:
                case ExpressionType.SubtractChecked:
                    {
                        var binary = (BinaryExpression)exp;
                        return IsEvaluatable(binary.Left, scope) && IsEvaluatable(binary.Right, scope);
                    }

                default:
                    //control flow such as Goto, Loop, Switch, Throw, and Try isn't written by C# in an expression tree
                    return false;
            }
        }
        private bool IsEvaluatableBindings(IEnumerable<MemberBinding> bindings, List<ParameterExpression>? scope)
        {
            foreach (var binding in bindings)
            {
                switch (binding.BindingType)
                {
                    case MemberBindingType.Assignment:
                        if (!IsEvaluatable(((MemberAssignment)binding).Expression, scope))
                            return false;
                        break;
                    case MemberBindingType.MemberBinding:
                        if (!IsEvaluatableBindings(((MemberMemberBinding)binding).Bindings, scope))
                            return false;
                        break;
                    case MemberBindingType.ListBinding:
                        foreach (var initializer in ((MemberListBinding)binding).Initializers)
                        {
                            foreach (var arg in initializer.Arguments)
                            {
                                if (!IsEvaluatable(arg, scope))
                                    return false;
                            }
                        }
                        break;
                }
            }
            return true;
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
                ExpressionType.Add => false,
                ExpressionType.AddAssign => false,
                ExpressionType.AddAssignChecked => false,
                ExpressionType.AddChecked => false,
                ExpressionType.And => false,
                ExpressionType.AndAlso => false,
                ExpressionType.AndAssign => false,
                ExpressionType.ArrayIndex => IsNullArrayIndex(exp),
                ExpressionType.ArrayLength => false,
                ExpressionType.Assign => false,
                ExpressionType.Block => false,
                ExpressionType.Call => IsNullCall(exp),
                ExpressionType.Coalesce => IsNullEvaluated(exp),
                ExpressionType.Conditional => IsNullEvaluated(exp),
                ExpressionType.Constant => IsNullConstant(exp),
                ExpressionType.Convert => IsNullUnary(exp),
                ExpressionType.ConvertChecked => false,
                ExpressionType.DebugInfo => false,
                ExpressionType.Decrement => false,
                ExpressionType.Default => !exp.Type.IsValueType || exp.Type.Name == nullableTypeName,
                ExpressionType.Divide => false,
                ExpressionType.DivideAssign => false,
                ExpressionType.Dynamic => false,
                ExpressionType.Equal => false,
                ExpressionType.ExclusiveOr => false,
                ExpressionType.ExclusiveOrAssign => false,
                ExpressionType.Extension => false,
                ExpressionType.Goto => false,
                ExpressionType.GreaterThan => false,
                ExpressionType.GreaterThanOrEqual => false,
                ExpressionType.Increment => false,
                ExpressionType.Index => false,
                ExpressionType.Invoke => false,
                ExpressionType.IsFalse => false,
                ExpressionType.IsTrue => false,
                ExpressionType.Label => false,
                ExpressionType.Lambda => false,
                ExpressionType.LeftShift => false,
                ExpressionType.LeftShiftAssign => false,
                ExpressionType.LessThan => false,
                ExpressionType.LessThanOrEqual => false,
                ExpressionType.ListInit => false,
                ExpressionType.Loop => false,
                ExpressionType.MemberAccess => IsNullMemberAccess(exp),
                ExpressionType.MemberInit => false,
                ExpressionType.Modulo => false,
                ExpressionType.ModuloAssign => false,
                ExpressionType.Multiply => false,
                ExpressionType.MultiplyAssign => false,
                ExpressionType.MultiplyAssignChecked => false,
                ExpressionType.MultiplyChecked => false,
                ExpressionType.Negate => false,
                ExpressionType.NegateChecked => false,
                ExpressionType.New => false,
                ExpressionType.NewArrayBounds => false,
                ExpressionType.NewArrayInit => false,
                ExpressionType.Not => false,
                ExpressionType.NotEqual => false,
                ExpressionType.OnesComplement => false,
                ExpressionType.Or => false,
                ExpressionType.OrAssign => false,
                ExpressionType.OrElse => false,
                ExpressionType.Parameter => false,
                ExpressionType.PostDecrementAssign => false,
                ExpressionType.PostIncrementAssign => false,
                ExpressionType.Power => false,
                ExpressionType.PowerAssign => false,
                ExpressionType.PreDecrementAssign => false,
                ExpressionType.PreIncrementAssign => false,
                ExpressionType.Quote => false,
                ExpressionType.RightShift => false,
                ExpressionType.RightShiftAssign => false,
                ExpressionType.RuntimeVariables => false,
                ExpressionType.Subtract => false,
                ExpressionType.SubtractAssign => false,
                ExpressionType.SubtractAssignChecked => false,
                ExpressionType.SubtractChecked => false,
                ExpressionType.Switch => false,
                ExpressionType.Throw => false,
                ExpressionType.Try => false,
                ExpressionType.TypeAs => false,
                ExpressionType.TypeEqual => false,
                ExpressionType.TypeIs => false,
                ExpressionType.UnaryPlus => false,
                ExpressionType.Unbox => false,
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
            //a call that uses the model is written as SQL so it isn't a null literal
            return IsNullEvaluated(exp);
        }
        private bool IsNullEvaluated(Expression exp)
        {
            //only a type that can hold null is evaluated, so a call like Guid.NewGuid() isn't run an extra time
            if (exp.Type.IsValueType && exp.Type.Name != nullableTypeName)
                return false;
            if (!IsEvaluatable(exp))
                return false;
            return Evaluate(exp) is null;
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

        /// <summary>
        /// Evaluates an expression that doesn't use the model to its value.
        /// </summary>
        /// <param name="exp">The expression to evaluate.</param>
        /// <returns>The value of the expression.</returns>
        protected static object? Evaluate(Expression exp)
        {
            return exp.NodeType switch
            {
                ExpressionType.Constant => EvaluateConstant(exp),
                ExpressionType.MemberAccess => EvaluateMemberAccess(exp),
                ExpressionType.Lambda => EvaludateLambda(exp),
                ExpressionType.Default => EvaluateDefault(exp),
                ExpressionType.Coalesce => EvaluateCoalesce(exp),
                ExpressionType.Conditional => EvaluateConditional(exp),
                _ => EvaluateInvoke(exp),
            };
        }
        [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method.", Justification = "Only value types are created here and their default value doesn't use a constructor.")]
        private static object? EvaluateDefault(Expression exp)
        {
            if (!exp.Type.IsValueType || exp.Type.Name == nullableTypeName)
                return null;
            return RuntimeHelpers.GetUninitializedObject(exp.Type);
        }
        private static object? EvaluateCoalesce(Expression exp)
        {
            var coalesce = (BinaryExpression)exp;
            if (coalesce.Conversion is not null)
                return EvaluateInvoke(exp);
            return Evaluate(coalesce.Left) ?? Evaluate(coalesce.Right);
        }
        private static object? EvaluateConditional(Expression exp)
        {
            var conditional = (ConditionalExpression)exp;
            return (bool)Evaluate(conditional.Test)! ? Evaluate(conditional.IfTrue) : Evaluate(conditional.IfFalse);
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
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Falls back to InvalidOperationException at runtime when dynamic code is unsupported.")]
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
                var value = Expression.Lambda(exp).Compile().DynamicInvoke();
                return value;
            }
            catch (Exception ex)
            {
                if (!RuntimeFeature.IsDynamicCodeSupported)
                    throw new InvalidOperationException($"Dynamic code execution is not supported in this runtime environment.", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates the WHERE clause SQL into the provided writer.
        /// </summary>
        /// <param name="where">The filter expression.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="rootDependant">The root parameter dependant used for join tracking.</param>
        /// <param name="operationContext">The current member context for the operation.</param>
        protected abstract void GenerateWhere(LambdaExpression? where, ref CharWriter sb, ParameterDependant rootDependant, MemberContext operationContext);
        /// <summary>
        /// Generates the ORDER BY, OFFSET, and FETCH/LIMIT clauses into the provided writer.
        /// </summary>
        /// <param name="order">An optional ordering specification.</param>
        /// <param name="skip">An optional number of records to skip.</param>
        /// <param name="take">An optional number of records to take.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        /// <param name="rootDependant">The root parameter dependant used for join tracking.</param>
        /// <param name="operationContext">The current member context for the operation.</param>
        protected abstract void GenerateOrderSkipTake(LinqOrder[]? order, int? skip, int? take, ref CharWriter sb, ParameterDependant rootDependant, MemberContext operationContext);
        /// <summary>
        /// Generates the SELECT clause into the provided writer.
        /// </summary>
        /// <param name="count">Whether the statement counts the rows rather than reading them.</param>
        /// <param name="graph">An optional graph describing which properties to include.</param>
        /// <param name="modelDetail">Metadata about the model being queried.</param>
        /// <param name="sb">The writer to append SQL into.</param>
        protected abstract void GenerateSelect(bool count, Graph? graph, ModelDetail modelDetail, ref CharWriter sb);
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
        /// Appends a line break into the provided writer.
        /// </summary>
        /// <param name="sb">The writer to append into.</param>
        protected abstract void AppendLineBreak(ref CharWriter sb);

        /// <summary>
        /// Returns the SQL string representation of the given <see cref="Operator"/>.
        /// </summary>
        /// <param name="operation">The operator to convert.</param>
        /// <param name="type">The type the operation results in.</param>
        /// <returns>The SQL operator string, or <see langword="null"/> if not applicable.</returns>
        protected abstract string? OperatorToString(Operator operation, Type type);
    }
}

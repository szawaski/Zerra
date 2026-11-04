// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Linq
{
    /// <summary>
    /// The plan of one statement of a LINQ query, read in one pass over its expressions: the columns the statement selects, how each row builds an element, and the relations read by statements of their own. Building an element reads each column once in index order, and a relation's key column after its element, so a store can read its rows sequentially.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class LinqPlan
    {
        //an enum's smallest and largest are by its number, whatever the store holds
        private static Expression AggregateValue(LinqColumnKind kind, Expression value)
        {
            if (kind is not (LinqColumnKind.Min or LinqColumnKind.Max))
                return value;
            var type = Nullable.GetUnderlyingType(value.Type) ?? value.Type;
            return type.IsEnum ? Expression.Convert(value, Enum.GetUnderlyingType(type)) : value;
        }
        /// <summary>The model the rows come from.</summary>
        public ModelDetail Model { get; }
        /// <summary>How each row builds an element, or <see langword="null"/> when the statement gives a count or whether there is any.</summary>
        public LinqShape? Shape { get; private set; }
        /// <summary>The columns in order.</summary>
        public List<LinqColumn> Columns { get; } = new();
        /// <summary>Whether every column is a member of the model, which writing them can't add a join for.</summary>
        public bool ReadsOnlyMembers
        {
            get
            {
                foreach (var column in Columns)
                {
                    if (column.Kind != LinqColumnKind.Value || column.Member is null || column.Expression is not null)
                        return false;
                }
                return true;
            }
        }
        /// <summary>The relation shapes, whose values follow the columns in a row built in memory.</summary>
        public List<LinqShape> Relations { get; } = new();
        /// <summary>The relations read after the rows.</summary>
        public List<LinqPiece> Pieces { get; } = new();
        /// <summary>The parts of the group key, for a grouped query.</summary>
        public Expression[]? KeyParts { get; private set; }
        /// <summary>The parameter of the key parts.</summary>
        public ParameterExpression? KeyParameter { get; private set; }
        /// <summary>The names of the key parts when the key is an anonymous object of them.</summary>
        public string[]? KeyNames { get; private set; }
        /// <summary>For the rows of a relation, the column holding the key matching each row to its parent, the first of <see cref="ParentKeyColumns"/>.</summary>
        public int? ParentKeyColumn { get; private set; }
        /// <summary>For the rows of a relation, the columns holding the key matching each row to its parent, one for each member of a key of several.</summary>
        public int[]? ParentKeyColumns { get; private set; }
        /// <summary>For a query an operation by position reads, the column of the place of each row.</summary>
        public int? NumberColumn { get; private set; }
        /// <summary>For an operation by position, the columns the elements are in order by.</summary>
        public int[]? PositionColumns { get; internal set; }

        private LinqPlan? parent;
        private NewExpression? keyConstructed;
        //what each element of a group is when GroupBy selects it, and the key of a result selector of the key and the group
        private LambdaExpression? groupElement;
        private ParameterExpression? groupKey;

        private LinqPlan(ModelDetail model)
        {
            this.Model = model;
        }

        /// <summary>
        /// Reads the plan of a query's statement.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="modelDetail">The model's detail.</param>
        /// <returns>The plan.</returns>
        public static LinqPlan Read(LinqQuery query, ModelDetail modelDetail)
        {
            //reading a whole model is most queries and its plan never changes, it has only the columns of the model
            if (query.Select is null && query.Graph is null && query.GroupBy is null && query.KeyMember is null && !query.Numbered
                && query.Terminal is not (LinqTerminal.Count or LinqTerminal.LongCount or LinqTerminal.Any or LinqTerminal.Sum or LinqTerminal.Min or LinqTerminal.Max or LinqTerminal.Average))
            {
                var whole = modelDetail.WholeModelPlan;
                if (whole is null)
                {
                    whole = new LinqPlan(modelDetail);
                    whole.Shape = whole.ReadModel(modelDetail, null, false);
                    modelDetail.WholeModelPlan = whole;
                }
                return whole;
            }

            var plan = new LinqPlan(modelDetail);
            switch (query.Terminal)
            {
                case LinqTerminal.Count:
                case LinqTerminal.LongCount:
                    //the different elements are counted, so they are read
                    if (query.Distinct)
                        break;
                    return plan;
                case LinqTerminal.Any:
                    return plan;
                case LinqTerminal.Sum:
                case LinqTerminal.Min:
                case LinqTerminal.Max:
                case LinqTerminal.Average:
                    {
                        var kind = query.Terminal switch
                        {
                            LinqTerminal.Sum => LinqColumnKind.Sum,
                            LinqTerminal.Min => LinqColumnKind.Min,
                            LinqTerminal.Max => LinqColumnKind.Max,
                            _ => LinqColumnKind.Average,
                        };
                        var type = AggregateCommon.GetResultType(kind, query.Value);
                        plan.Shape = LinqShape.OfColumn(type, plan.AddColumn(kind, AggregateValue(kind, query.Value!.Body), query.Value.Parameters[0], 0, type));
                        return plan;
                    }
            }
            if (query.GroupBy is not null)
            {
                plan.KeyParts = AggregateCommon.GetKeyParts(query.GroupBy);
                plan.KeyParameter = query.GroupBy.Parameters[0];
                plan.keyConstructed = query.GroupBy.Body as NewExpression;
                if (query.GroupBy.Body is NewExpression { Members: not null } keyType)
                {
                    plan.KeyNames = new string[keyType.Members.Count];
                    for (var i = 0; i < plan.KeyNames.Length; i++)
                        plan.KeyNames[i] = keyType.Members[i].Name;
                }
                if (query.CountsByKey)
                {
                    //each key with how many it has, as a KeyValuePair
                    if (!query.ResultType.IsGenericType || query.ResultType.GetGenericTypeDefinition() != typeof(KeyValuePair<,>))
                        throw new NotSupportedException("CountBy gives each key and its count, read them as they are");
                    var key = plan.ReadKey(query.ResultType.GetGenericArguments()[0]);
                    var count = LinqShape.OfColumn(typeof(int), plan.AddColumn(LinqColumnKind.Count, null, null, 0, typeof(long)));
                    plan.Shape = LinqShape.OfCreated(query.ResultType, TypeAnalyzer.GetTypeDetail(query.ResultType).GetConstructor(2).CreatorBoxed, [key, count], ["Key", "Value"]);
                    return plan;
                }
                if (query.Select is null)
                    throw new NotSupportedException("GroupBy needs a Select building a result from each group, such as g => new { g.Key, Rows = g.Count() }");
                plan.groupElement = query.GroupElement;
                plan.groupKey = query.GroupKey;
                plan.Shape = plan.ReadGroupElement(query.Select.Body, query.Select.Parameters[^1]);
                return plan;
            }
            if (query.KeyMember is not null)
            {
                plan.Shape = LinqShape.OfColumn(query.KeyMember.Type, plan.AddMemberColumn(query.KeyMember));
                return plan;
            }
            plan.Shape = query.Select is null ? plan.ReadModel(modelDetail, query.Graph, query.GraphRelations && query.ReturnsModels) : plan.ReadElement(query.Select.ReturnType == typeof(object) && query.Select.Body is UnaryExpression { NodeType: ExpressionType.Convert } boxed ? boxed.Operand : query.Select.Body, query.Select.Parameters[0], modelDetail);
            //the place of each row, after the columns of the element
            if (query.Numbered)
            {
                if (plan.Relations.Count > 0)
                    throw new NotSupportedException("A query an operation by position reads gives its own values, not a relation read after them");
                var number = new LinqColumn(plan.Columns.Count, LinqColumnKind.RowNumber, null, null, 0, typeof(long), null) { Orders = query.NumberOrder };
                plan.Columns.Add(number);
                plan.NumberColumn = number.Index;
            }
            plan.PlaceRelations();
            return plan;
        }

        /// <summary>
        /// Reads the plan of a Select of a set's elements, each value a column computed from the element's columns.
        /// </summary>
        /// <param name="select">The Select.</param>
        /// <param name="modelDetail">The model of the set's first query.</param>
        /// <returns>The plan.</returns>
        public static LinqPlan ReadSetSelect(LambdaExpression select, ModelDetail modelDetail)
        {
            var plan = new LinqPlan(modelDetail);
            plan.Shape = plan.ReadSetElement(select.Body, select.Parameters[0]);
            return plan;
        }
        private LinqShape ReadSetElement(Expression expression, ParameterExpression parameter)
        {
            switch (expression)
            {
                case NewExpression { Members: not null } anonymous:
                    {
                        var arguments = new LinqShape[anonymous.Arguments.Count];
                        var names = new string[arguments.Length];
                        for (var i = 0; i < arguments.Length; i++)
                        {
                            arguments[i] = ReadSetElement(anonymous.Arguments[i], parameter);
                            names[i] = anonymous.Members[i].Name;
                        }
                        return LinqShape.OfObject(anonymous.Type, anonymous.Constructor, arguments, names, null, null);
                    }
                case NewExpression constructed:
                    {
                        var arguments = new LinqShape[constructed.Arguments.Count];
                        for (var i = 0; i < arguments.Length; i++)
                            arguments[i] = ReadSetElement(constructed.Arguments[i], parameter);
                        return LinqShape.OfObject(constructed.Type, constructed.Constructor, arguments, null, null, null);
                    }
                case MemberInitExpression init:
                    {
                        var arguments = new LinqShape[init.NewExpression.Arguments.Count];
                        for (var i = 0; i < arguments.Length; i++)
                            arguments[i] = ReadSetElement(init.NewExpression.Arguments[i], parameter);
                        var typeDetail = TypeAnalyzer.GetTypeDetail(init.Type);
                        var members = new MemberDetail[init.Bindings.Count];
                        var values = new LinqShape[members.Length];
                        for (var i = 0; i < members.Length; i++)
                        {
                            if (init.Bindings[i] is not MemberAssignment assignment)
                                throw new NotSupportedException($"A repository query sets each member of {init.Type.Name} directly");
                            members[i] = typeDetail.GetMember(assignment.Member.Name);
                            if (members[i].SetterBoxed is null)
                                throw new NotSupportedException($"{init.Type.Name}.{members[i].Name} has no setter");
                            values[i] = ReadSetElement(assignment.Expression, parameter);
                        }
                        return LinqShape.OfObject(init.Type, init.NewExpression.Constructor, arguments, null, members, values);
                    }
                default:
                    return LinqShape.OfColumn(expression.Type, AddColumn(LinqColumnKind.Value, expression, parameter, 0, expression.Type));
            }
        }

        /// <summary>
        /// Reads the plan of a join's statement: each element of a query, whole or a member of it, read from that query's columns, anything else a column computed from them.
        /// </summary>
        /// <param name="join">The join.</param>
        /// <param name="left">How the first query builds its elements.</param>
        /// <param name="right">How the second query builds its elements, or <see langword="null"/> for an operation by position on one query.</param>
        /// <param name="leftNumber">For an operation by position, the column of the place of each row of the first query.</param>
        /// <param name="modelDetail">The model of the first query.</param>
        /// <returns>The plan.</returns>
        public static LinqPlan ReadJoin(LinqJoin join, LinqShape left, LinqShape? right, int? leftNumber, ModelDetail modelDetail)
        {
            var plan = new LinqPlan(modelDetail);
            if (join.Kind == LinqJoinKind.SequenceEqual)
            {
                //the rows are the pairs that differ, which only say whether there are any
                plan.Shape = LinqShape.OfColumn(typeof(int), plan.AddColumn(LinqColumnKind.Value, Expression.Constant(1), null, 0, typeof(int)));
                return plan;
            }
            if (join.Result is not null)
            {
                plan.Shape = plan.ReadJoinElement(join.Result, join, left, right);
            }
            else
            {
                //a ValueTuple of the place and the element, or of the pair
                var creator = TypeAnalyzer.GetTypeDetail(join.TupleType!).GetConstructor(2).CreatorBoxed;
                var first = join.Kind == LinqJoinKind.Index ? LinqShape.OfColumn(typeof(int), plan.AddColumn(LinqColumnKind.Value, join.RightElement, null, 0, typeof(int))) : plan.ReadJoinPart(left, false, false, true);
                var second = join.Kind == LinqJoinKind.Index ? plan.ReadJoinPart(left, false, false, true) : plan.ReadJoinPart(right!, true, false, true);
                plan.Shape = LinqShape.OfCreated(join.TupleType!, creator, [first, second], ["Item1", "Item2"]);
            }
            //an operation by position keeps the elements in their places, those of the first query of a UnionBy before the second's
            if (join.Kind is not (LinqJoinKind.Inner or LinqJoinKind.Left or LinqJoinKind.Right or LinqJoinKind.Group))
            {
                var positions = new List<int>();
                if (join.Kind == LinqJoinKind.UnionBy)
                {
                    var part = new LinqColumn(plan.Columns.Count, LinqColumnKind.Value, null, null, 0, typeof(int), null) { From = -2 };
                    plan.Columns.Add(part);
                    positions.Add(part.Index);
                }
                var place = new LinqColumn(plan.Columns.Count, LinqColumnKind.Value, null, null, 0, typeof(long), null) { From = leftNumber!.Value };
                plan.Columns.Add(place);
                positions.Add(place.Index);
                plan.PositionColumns = positions.ToArray();
            }
            return plan;
        }
        private LinqShape ReadJoinElement(Expression expression, LinqJoin join, LinqShape left, LinqShape? right)
        {
            if (TryJoinPart(expression, join, left, right, out var part, out var fromRight))
                return ReadJoinPart(part, fromRight, fromRight ? join.Kind == LinqJoinKind.Left : join.Kind == LinqJoinKind.Right, true);
            switch (expression)
            {
                case NewExpression { Members: not null } anonymous:
                    {
                        var arguments = new LinqShape[anonymous.Arguments.Count];
                        var names = new string[arguments.Length];
                        for (var i = 0; i < arguments.Length; i++)
                        {
                            arguments[i] = ReadJoinElement(anonymous.Arguments[i], join, left, right);
                            names[i] = anonymous.Members[i].Name;
                        }
                        return LinqShape.OfObject(anonymous.Type, anonymous.Constructor, arguments, names, null, null);
                    }
                case NewExpression constructed:
                    {
                        var arguments = new LinqShape[constructed.Arguments.Count];
                        for (var i = 0; i < arguments.Length; i++)
                            arguments[i] = ReadJoinElement(constructed.Arguments[i], join, left, right);
                        return LinqShape.OfObject(constructed.Type, constructed.Constructor, arguments, null, null, null);
                    }
                case MemberInitExpression init:
                    {
                        var arguments = new LinqShape[init.NewExpression.Arguments.Count];
                        for (var i = 0; i < arguments.Length; i++)
                            arguments[i] = ReadJoinElement(init.NewExpression.Arguments[i], join, left, right);
                        var typeDetail = TypeAnalyzer.GetTypeDetail(init.Type);
                        var members = new MemberDetail[init.Bindings.Count];
                        var values = new LinqShape[members.Length];
                        for (var i = 0; i < members.Length; i++)
                        {
                            if (init.Bindings[i] is not MemberAssignment assignment)
                                throw new NotSupportedException($"A repository query sets each member of {init.Type.Name} directly");
                            members[i] = typeDetail.GetMember(assignment.Member.Name);
                            if (members[i].SetterBoxed is null)
                                throw new NotSupportedException($"{init.Type.Name}.{members[i].Name} has no setter");
                            values[i] = ReadJoinElement(assignment.Expression, join, left, right);
                        }
                        return LinqShape.OfObject(init.Type, init.NewExpression.Constructor, arguments, null, members, values);
                    }
                default:
                    if (join.Kind == LinqJoinKind.Group && expression == join.RightElement)
                        throw new NotSupportedException("A GroupJoin's group is read by an aggregate of it, such as g.Count() or g.Sum(x => x.Price), or flattened with SelectMany");
                    return LinqShape.OfColumn(expression.Type, AddColumn(LinqColumnKind.Value, expression, null, 0, expression.Type));
            }
        }
        //an element of either query, or a member of one, found in the shape the query builds it with
        private static bool TryJoinPart(Expression expression, LinqJoin join, LinqShape left, LinqShape? right, [MaybeNullWhen(false)] out LinqShape part, out bool fromRight)
        {
            part = null;
            fromRight = false;
            var names = new List<string>();
            var owner = expression;
            while (owner is MemberExpression member)
            {
                names.Add(member.Member.Name);
                owner = member.Expression;
            }
            if (owner == join.LeftElement)
            {
                part = left;
            }
            else if (owner == join.RightElement && right is not null && join.Kind != LinqJoinKind.Group)
            {
                part = right;
                fromRight = true;
            }
            else
            {
                return false;
            }
            for (var i = names.Count - 1; i >= 0; i--)
            {
                part = Member(part, names[i]);
                if (part is null)
                    return false;
            }
            return true;
        }
        private static LinqShape? Member(LinqShape shape, string name)
        {
            switch (shape.Kind)
            {
                case LinqShapeKind.Model:
                    for (var i = 0; i < shape.ModelMembers!.Length; i++)
                    {
                        if (shape.ModelMembers[i].Name == name)
                            return LinqShape.OfColumn(shape.ModelMembers[i].Type, shape.ModelColumns![i]);
                    }
                    return null;
                case LinqShapeKind.Object:
                    for (var i = 0; i < (shape.ArgumentNames?.Length ?? 0); i++)
                    {
                        if (shape.ArgumentNames![i] == name)
                            return shape.Arguments![i];
                    }
                    for (var i = 0; i < (shape.Members?.Length ?? 0); i++)
                    {
                        if (shape.Members![i].Name == name)
                            return shape.Values![i];
                    }
                    return null;
                default:
                    return null;
            }
        }
        //the shape of an element of a query built again from the columns of that query; one the join may have no row of is null when its match column is
        private LinqShape ReadJoinPart(LinqShape part, bool fromRight, bool optional, bool top)
        {
            LinqColumn From(LinqColumn column, ModelMemberDetail? member, Type type)
            {
                var from = new LinqColumn(Columns.Count, LinqColumnKind.Value, null, null, 0, type, member) { From = column.Index, FromRight = fromRight, Optional = optional };
                Columns.Add(from);
                return from;
            }
            LinqColumn? Key()
            {
                if (!optional || !top)
                    return null;
                var key = new LinqColumn(Columns.Count, LinqColumnKind.Value, null, null, 0, typeof(int), null) { From = -1, FromRight = fromRight, Optional = true };
                Columns.Add(key);
                return key;
            }
            switch (part.Kind)
            {
                case LinqShapeKind.Column:
                    return LinqShape.OfColumn(part.Type, From(part.Column!, part.Column!.Member, part.Column.Type));
                case LinqShapeKind.Model:
                    {
                        if (part.ModelRelations is not null)
                            throw new NotSupportedException("A query paired by a join reads its own rows, not a relation read after them");
                        //the key comes first as it says whether the rest are there to read
                        var key = Key() ?? (part.JoinedKey is null ? null : From(part.JoinedKey, null, part.JoinedKey.Type));
                        var columns = new LinqColumn[part.ModelColumns!.Length];
                        for (var i = 0; i < columns.Length; i++)
                            columns[i] = From(part.ModelColumns[i], part.ModelMembers![i], part.ModelMembers[i].Type);
                        return LinqShape.OfOptionalModel(part.Model!, part.ModelMembers!, columns, key);
                    }
                case LinqShapeKind.Object:
                    {
                        var key = Key();
                        LinqShape[]? arguments = null;
                        if (part.Arguments is not null)
                        {
                            arguments = new LinqShape[part.Arguments.Length];
                            for (var i = 0; i < arguments.Length; i++)
                                arguments[i] = ReadJoinPart(part.Arguments[i], fromRight, optional, false);
                        }
                        LinqShape[]? values = null;
                        if (part.Values is not null)
                        {
                            values = new LinqShape[part.Values.Length];
                            for (var i = 0; i < values.Length; i++)
                                values[i] = ReadJoinPart(part.Values[i], fromRight, optional, false);
                        }
                        return LinqShape.OfOptionalObject(part, arguments, values, key);
                    }
                default:
                    throw new NotSupportedException("A query paired by a join reads its own rows, not a relation read after them");
            }
        }

        private void PlaceRelations()
        {
            for (var r = 0; r < Relations.Count; r++)
                Relations[r].Column!.Index = Columns.Count + r;
        }

        private LinqColumn AddColumn(LinqColumnKind kind, Expression? expression, ParameterExpression? parameter, int keyPart, Type type)
        {
            var column = new LinqColumn(Columns.Count, kind, expression, parameter, keyPart, type, null);
            Columns.Add(column);
            return column;
        }
        //a member read twice, by the element and as a relation's key, is two columns: each column is read once, in order, so a store reads rows sequentially
        private LinqColumn AddMemberColumn(ModelMemberDetail member)
        {
            var column = new LinqColumn(Columns.Count, LinqColumnKind.Value, null, null, 0, member.Type, member);
            Columns.Add(column);
            return column;
        }

        private LinqShape ReadModel(ModelDetail model, Graph? graph, bool relations)
        {
            var members = new List<ModelMemberDetail>();
            var columns = new List<LinqColumn>();
            foreach (var member in model.Members)
            {
                if (member.StoreName is null || member.ForeignIdentities is not null || (graph is not null && !graph.HasMember(member.Name)))
                    continue;
                members.Add(member);
                columns.Add(AddMemberColumn(member));
            }
            //a statement needs a column, so a graph of relations alone reads the identity
            if (members.Count == 0)
            {
                foreach (var identity in model.IdentityMembers)
                {
                    members.Add(identity);
                    columns.Add(AddMemberColumn(identity));
                }
            }
            List<LinqShape>? modelRelations = null;
            if (relations)
            {
                foreach (var relation in model.RelatedMembers)
                {
                    if (!graph!.HasMemberExplicitly(relation.Name))
                        continue;
                    var column = new LinqColumn(0, LinqColumnKind.Value, null, null, 0, relation.Type, relation);
                    (modelRelations ??= new()).Add(AddRelation(relation, model, relation.Type, column, null, null, null, null, null, graph.GetChildGraph(relation.Name)));
                }
            }
            return LinqShape.OfModel(model, members.ToArray(), columns.ToArray(), modelRelations?.ToArray());
        }

        private LinqShape ReadElement(Expression expression, ParameterExpression parameter, ModelDetail model)
        {
            if (TryReadRelation(expression, parameter, model, out var relation))
                return relation;
            if (expression is MemberExpression { Expression: not null } member && member.Expression == parameter && model.TryGetProperty(member.Member.Name, out var memberDetail) && memberDetail.StoreName is not null && !memberDetail.IsRelated)
                return LinqShape.OfColumn(expression.Type, AddMemberColumn(memberDetail));
            switch (expression)
            {
                case NewExpression { Members: not null } anonymous:
                    {
                        //an anonymous type, whose constructor takes its members in order
                        var arguments = new LinqShape[anonymous.Arguments.Count];
                        var names = new string[arguments.Length];
                        for (var i = 0; i < arguments.Length; i++)
                        {
                            arguments[i] = ReadElement(anonymous.Arguments[i], parameter, model);
                            names[i] = anonymous.Members[i].Name;
                        }
                        return LinqShape.OfObject(anonymous.Type, anonymous.Constructor, arguments, names, null, null);
                    }
                case NewExpression constructed:
                    {
                        var arguments = new LinqShape[constructed.Arguments.Count];
                        for (var i = 0; i < arguments.Length; i++)
                            arguments[i] = ReadElement(constructed.Arguments[i], parameter, model);
                        return LinqShape.OfObject(constructed.Type, constructed.Constructor, arguments, null, null, null);
                    }
                case MemberInitExpression init:
                    {
                        var arguments = new LinqShape[init.NewExpression.Arguments.Count];
                        for (var i = 0; i < arguments.Length; i++)
                            arguments[i] = ReadElement(init.NewExpression.Arguments[i], parameter, model);
                        var typeDetail = TypeAnalyzer.GetTypeDetail(init.Type);
                        var members = new MemberDetail[init.Bindings.Count];
                        var values = new LinqShape[members.Length];
                        for (var i = 0; i < members.Length; i++)
                        {
                            if (init.Bindings[i] is not MemberAssignment assignment)
                                throw new NotSupportedException($"A repository query sets each member of {init.Type.Name} directly");
                            members[i] = typeDetail.GetMember(assignment.Member.Name);
                            if (members[i].SetterBoxed is null)
                                throw new NotSupportedException($"{init.Type.Name}.{members[i].Name} has no setter");
                            values[i] = ReadElement(assignment.Expression, parameter, model);
                        }
                        return LinqShape.OfObject(init.Type, init.NewExpression.Constructor, arguments, null, members, values);
                    }
                default:
                    return LinqShape.OfColumn(expression.Type, AddColumn(LinqColumnKind.Value, expression, parameter, 0, expression.Type));
            }
        }

        //a relation of the model, whole or with its rows filtered, ordered, and selected from, read by a statement of its own after the rows
        private bool TryReadRelation(Expression expression, ParameterExpression parameter, ModelDetail model, [MaybeNullWhen(false)] out LinqShape shape)
        {
            shape = null;
            var source = expression;
            MethodInfo? toList = null;
            if (source is MethodCallExpression { Method.Name: "ToArray" or "ToList" } wrap && wrap.Method.DeclaringType == typeof(Enumerable) && wrap.Arguments.Count == 1)
            {
                source = wrap.Arguments[0];
                if (wrap.Method.Name == "ToList")
                    toList = wrap.Method;
            }
            LambdaExpression? select = null;
            LambdaExpression? where = null;
            var orders = new List<LinqOrder>();
            int? skip = null;
            int? take = null;
            var chained = false;
            while (source is MethodCallExpression chain && chain.Method.DeclaringType == typeof(Enumerable) && chain.Arguments.Count == 2 && chain.Method.Name is "Select" or "Where" or "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending" or "Skip" or "Take")
            {
                if (chain.Method.Name is "Skip" or "Take")
                {
                    if (where is not null || orders.Count > 0)
                        throw new NotSupportedException("The rows of a related collection are filtered and ordered before they are paged");
                    if (chain.Method.Name == "Take")
                    {
                        if (take.HasValue || skip.HasValue)
                            throw new NotSupportedException("The rows of a related collection skip before they take, once each");
                        take = LinqQuery.Count(chain.Arguments[1]);
                    }
                    else
                    {
                        if (skip.HasValue)
                            throw new NotSupportedException("The rows of a related collection skip before they take, once each");
                        skip = LinqQuery.Count(chain.Arguments[1]);
                    }
                    source = chain.Arguments[0];
                    chained = true;
                    continue;
                }
                var argument = chain.Arguments[1];
                if (argument.NodeType == ExpressionType.Quote)
                    argument = ((UnaryExpression)argument).Operand;
                if (argument is not LambdaExpression lambda || lambda.Parameters.Count != 1)
                    break;
                switch (chain.Method.Name)
                {
                    case "Select":
                        if (select is not null || where is not null || orders.Count > 0 || skip.HasValue || take.HasValue)
                            throw new NotSupportedException("The rows of a related collection are filtered, ordered, and paged, then selected from once");
                        select = lambda;
                        break;
                    case "Where":
                        where = where is null ? lambda : LinqQuery.AppendAnd(lambda, where);
                        break;
                    default:
                        orders.Insert(0, new LinqOrder(lambda, chain.Method.Name is "OrderByDescending" or "ThenByDescending"));
                        break;
                }
                source = chain.Arguments[0];
                chained = true;
            }
            if (source is not MemberExpression member || member.Expression != parameter || !model.TryGetProperty(member.Member.Name, out var relation) || !relation.IsRelated)
            {
                if (chained)
                    throw new NotSupportedException("Where, OrderBy, and Select before ToList or ToArray read the rows of a related collection of the model, such as x.Lines.Where(l => l.Quantity > 0).Select(l => new { l.Name })");
                return false;
            }
            if (!relation.IsEnumerable && (chained || source != expression))
                throw new NotSupportedException($"{relation.Name} is one related model, read whole or by its members");
            if (where is not null && ParameterFinder.Uses(where.Body, parameter) || select is not null && ParameterFinder.Uses(select.Body, parameter))
                throw new NotSupportedException($"The rows of {relation.Name} are filtered and built from themselves alone, not from the model");

            shape = AddRelation(relation, model, expression.Type, new LinqColumn(0, LinqColumnKind.Value, expression, parameter, 0, expression.Type, null), where, orders.Count == 0 ? null : orders.ToArray(), skip, take, select, null);
            if (toList is not null)
                shape.ReadWith(toList);
            return true;
        }

        //one related row reads with the row in its join, unless its rows read relations of their own
        private static bool CanJoin(ModelMemberDetail relation, ModelDetail related, LambdaExpression? select, Graph? graph)
        {
            if (relation.IsEnumerable || select is not null)
                return false;
            if (graph is not null)
            {
                foreach (var other in related.RelatedMembers)
                {
                    if (graph.HasMemberExplicitly(other.Name))
                        return false;
                }
            }
            return true;
        }

        private LinqShape AddJoined(ModelMemberDetail relation, ModelDetail model, ModelDetail related, LinqColumn column, Graph? graph)
        {
            var parameter = column.Parameter ?? Expression.Parameter(model.Type, "x");
            var source = column.Expression ?? Expression.MakeMemberAccess(parameter, relation.MemberInfo);
            LinqColumn Joined(ModelMemberDetail member)
            {
                var joined = new LinqColumn(Columns.Count, LinqColumnKind.Value, Expression.MakeMemberAccess(source, member.MemberInfo), parameter, 0, member.Type, null) { Joined = true };
                Columns.Add(joined);
                return joined;
            }
            //the identity comes first as it says whether the rest are there to read
            var key = Joined(related.IdentityMembers[0]);
            var members = new List<ModelMemberDetail>();
            var columns = new List<LinqColumn>();
            foreach (var member in related.Members)
            {
                if (member.StoreName is null || member.ForeignIdentities is not null || (graph is not null && !graph.HasMember(member.Name)))
                    continue;
                members.Add(member);
                columns.Add(Joined(member));
            }
            var shape = LinqShape.OfJoined(related, members.ToArray(), columns.ToArray(), key, column, relation);
            Relations.Add(shape);
            return shape;
        }

        private LinqShape AddRelation(ModelMemberDetail relation, ModelDetail model, Type type, LinqColumn column, LambdaExpression? where, LinqOrder[]? order, int? skip, int? take, LambdaExpression? select, Graph? graph)
        {
            var related = ModelAnalyzer.GetModel(relation.ActualType);
            //the rows can't be a row model above them, so a query never follows a relation round and round; a model with a collection of itself nests one level
            for (var above = this; above.parent is not null; above = above.parent)
            {
                if (above.Model == related)
                    throw new NotSupportedException($"The rows of {relation.Name} are {related.Type.Name}, which is already projected above them");
            }
            if (CanJoin(relation, related, select, graph))
                return AddJoined(relation, model, related, column, graph);
            var plan = new LinqPlan(related) { parent = this };
            plan.Shape = select is null ? plan.ReadModel(related, graph, graph is not null) : plan.ReadElement(select.Body, select.Parameters[0], related);
            //the key in pairs of the parent's member and the related model's, the identity of one and the foreign key of the other
            var names = relation.ForeignIdentities ?? throw new NotSupportedException($"Reading {relation.Name} needs a foreign identity");
            var referred = relation.IsEnumerable ? model : related;
            if (names.Length != referred.IdentityMembers.Count)
                throw new NotSupportedException($"{relation.Name} names {names.Length} foreign identities where {referred.Type.Name} has {referred.IdentityMembers.Count} identities");
            var parentKeys = new ModelMemberDetail[names.Length];
            var relatedKeys = new ModelMemberDetail[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                parentKeys[i] = relation.IsEnumerable ? model.IdentityMembers[i] : model.GetMember(names[i]);
                relatedKeys[i] = relation.IsEnumerable ? related.GetMember(names[i]) : related.IdentityMembers[i];
            }
            var relatedColumns = new int[relatedKeys.Length];
            for (var i = 0; i < relatedKeys.Length; i++)
                relatedColumns[i] = plan.AddMemberColumn(relatedKeys[i]).Index;
            plan.ParentKeyColumns = relatedColumns;
            plan.ParentKeyColumn = relatedColumns[0];
            plan.PlaceRelations();

            var parentColumns = new int[parentKeys.Length];
            for (var i = 0; i < parentKeys.Length; i++)
                parentColumns[i] = AddMemberColumn(parentKeys[i]).Index;
            var piece = new LinqPiece(relation, model, related, !relation.IsEnumerable, parentKeys, relatedKeys, where, order, skip, take, plan)
            {
                ParentColumn = parentColumns[0],
                ParentColumns = parentColumns,
            };
            Pieces.Add(piece);
            var shape = LinqShape.OfRelation(type, column, piece, Pieces.Count - 1);
            Relations.Add(shape);
            return shape;
        }

        //the key of a group, a column or an object of its parts
        private LinqShape ReadKey(Type type)
        {
            if (KeyParts!.Length != 1)
            {
                var keyArguments = new LinqShape[KeyParts.Length];
                for (var i = 0; i < keyArguments.Length; i++)
                    keyArguments[i] = LinqShape.OfColumn(KeyParts[i].Type, AddColumn(LinqColumnKind.KeyPart, null, null, i, KeyParts[i].Type));
                //a ValueTuple's parts have no names in its constructor, they are its fields
                var keyMembers = KeyNames;
                if (keyMembers is null)
                {
                    keyMembers = new string[keyArguments.Length];
                    for (var i = 0; i < keyMembers.Length; i++)
                        keyMembers[i] = $"Item{i + 1}";
                }
                return LinqShape.OfObject(keyConstructed!.Type, keyConstructed.Constructor, keyArguments, keyMembers, null, null);
            }
            return LinqShape.OfColumn(type, AddColumn(LinqColumnKind.KeyPart, null, null, 0, KeyParts[0].Type));
        }
        private LinqShape ReadGroupElement(Expression expression, ParameterExpression group)
        {
            var type = expression.Type;
            //a member wider than the aggregate, such as a long set to Count(), wraps it in a conversion which building the element does anyway
            while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion)
                expression = conversion.Operand;
            switch (expression)
            {
                case NewExpression { Members: not null } anonymous:
                    {
                        var arguments = new LinqShape[anonymous.Arguments.Count];
                        var names = new string[arguments.Length];
                        for (var i = 0; i < arguments.Length; i++)
                        {
                            arguments[i] = ReadGroupElement(anonymous.Arguments[i], group);
                            names[i] = anonymous.Members[i].Name;
                        }
                        return LinqShape.OfObject(anonymous.Type, anonymous.Constructor, arguments, names, null, null);
                    }
                case MemberInitExpression init:
                    {
                        var arguments = new LinqShape[init.NewExpression.Arguments.Count];
                        for (var i = 0; i < arguments.Length; i++)
                            arguments[i] = ReadGroupElement(init.NewExpression.Arguments[i], group);
                        var typeDetail = TypeAnalyzer.GetTypeDetail(init.Type);
                        var members = new MemberDetail[init.Bindings.Count];
                        var values = new LinqShape[members.Length];
                        for (var i = 0; i < members.Length; i++)
                        {
                            if (init.Bindings[i] is not MemberAssignment assignment)
                                throw new NotSupportedException($"A repository query sets each member of {init.Type.Name} directly");
                            members[i] = typeDetail.GetMember(assignment.Member.Name);
                            if (members[i].SetterBoxed is null)
                                throw new NotSupportedException($"{init.Type.Name}.{members[i].Name} has no setter");
                            values[i] = ReadGroupElement(assignment.Expression, group);
                        }
                        return LinqShape.OfObject(init.Type, init.NewExpression.Constructor, arguments, null, members, values);
                    }
                case MemberExpression { Member.Name: "Key" } key when key.Expression == group:
                    return ReadKey(type);
                case ParameterExpression keyParameter when keyParameter == groupKey:
                    return ReadKey(type);
                case MemberExpression part when (part.Expression is MemberExpression { Member.Name: "Key" } key && key.Expression == group) || (groupKey is not null && part.Expression == groupKey):
                    {
                        var index = KeyNames is null ? -1 : Array.IndexOf(KeyNames, part.Member.Name);
                        if (index < 0 && part.Member.Name.StartsWith("Item", StringComparison.Ordinal) && Int32.TryParse(part.Member.Name.AsSpan(4), out var item) && item >= 1 && item <= KeyParts!.Length)
                            index = item - 1;
                        if (index < 0)
                            throw new NotSupportedException($"{part.Member.Name} is not a part of the group key");
                        return LinqShape.OfColumn(type, AddColumn(LinqColumnKind.KeyPart, null, null, index, KeyParts![index].Type));
                    }
                case MethodCallExpression { Method.Name: "Count" or "LongCount" } count when count.Method.DeclaringType == typeof(Enumerable) && count.Arguments.Count == 1
                    && count.Arguments[0] is MethodCallExpression { Method.Name: "Distinct" } distinct && distinct.Method.DeclaringType == typeof(Enumerable) && distinct.Arguments.Count == 1
                    && distinct.Arguments[0] is MethodCallExpression { Method.Name: "Select" } selectCall && selectCall.Method.DeclaringType == typeof(Enumerable) && selectCall.Arguments.Count == 2 && selectCall.Arguments[0] == group:
                    {
                        var (value, parameter) = OfElement(Lambda(selectCall.Arguments[1]));
                        return LinqShape.OfColumn(type, AddColumn(LinqColumnKind.CountDistinct, value, parameter, 0, typeof(long)));
                    }
                case MethodCallExpression { Method.Name: "Join" } join when join.Method.DeclaringType == typeof(string) && join.Arguments.Count == 2
                    && join.Arguments[1] is MethodCallExpression { Method.Name: "Select" } joined && joined.Method.DeclaringType == typeof(Enumerable) && joined.Arguments.Count == 2 && joined.Arguments[0] == group:
                    {
                        if (ParameterFinder.Uses(join.Arguments[0], group))
                            throw new NotSupportedException("string.Join of a group takes a separator from outside the group");
                        var (value, parameter) = OfElement(Lambda(joined.Arguments[1]));
                        var separator = Expression.Lambda<Func<string?>>(join.Arguments[0]).Compile()() ?? "";
                        var column = new LinqColumn(Columns.Count, LinqColumnKind.Join, value, parameter, 0, typeof(string), null) { Separator = separator };
                        Columns.Add(column);
                        return LinqShape.OfColumn(type, column);
                    }
                case MethodCallExpression call when call.Method.DeclaringType == typeof(Enumerable) && call.Arguments.Count > 0 && call.Arguments[0] == group:
                    switch (call.Method.Name)
                    {
                        case "Count":
                        case "LongCount":
                            {
                                if (call.Arguments.Count == 1)
                                    return LinqShape.OfColumn(type, AddColumn(LinqColumnKind.Count, null, null, 0, typeof(long)));
                                var (predicate, parameter) = OfElement(Lambda(call.Arguments[1]));
                                return LinqShape.OfColumn(type, AddColumn(LinqColumnKind.CountIf, predicate, parameter, 0, typeof(long)));
                            }
                        case "Sum":
                        case "Average":
                        case "Min":
                        case "Max":
                            {
                                //a group of values GroupBy selects is aggregated as it is
                                if (call.Arguments.Count != 2 && (call.Arguments.Count != 1 || groupElement is null))
                                    throw new NotSupportedException($"A group's {call.Method.Name} takes the value to aggregate");
                                var kind = call.Method.Name switch
                                {
                                    "Sum" => LinqColumnKind.Sum,
                                    "Average" => LinqColumnKind.Average,
                                    "Min" => LinqColumnKind.Min,
                                    _ => LinqColumnKind.Max,
                                };
                                var selector = call.Arguments.Count == 2 ? Lambda(call.Arguments[1]) : groupElement!;
                                var (value, parameter) = call.Arguments.Count == 2 ? OfElement(selector) : (groupElement!.Body, groupElement.Parameters[0]);
                                return LinqShape.OfColumn(type, AddColumn(kind, AggregateValue(kind, value), parameter, 0, AggregateCommon.GetResultType(kind, selector)));
                            }
                        default:
                            throw new NotSupportedException($"{call.Method.Name} is not a group aggregate; use Count, Sum, Average, Min, or Max");
                    }
                default:
                    throw new NotSupportedException("A group's Select builds from the group's Key, a part of it, Count() with or without a condition, Select(..).Distinct().Count(), or Sum, Average, Min, or Max of a value");
            }
        }

        private static LambdaExpression Lambda(Expression argument)
        {
            if (argument.NodeType == ExpressionType.Quote)
                argument = ((UnaryExpression)argument).Operand;
            return (LambdaExpression)argument;
        }
        //a lambda over an element of a group as an expression over the model, through the element GroupBy selects
        private (Expression Body, ParameterExpression Parameter) OfElement(LambdaExpression lambda)
        {
            if (groupElement is null)
                return (lambda.Body, lambda.Parameters[0]);
            return (Zerra.Linq.LinqRebinder.RebindExpression(lambda.Body, lambda.Parameters[0], groupElement.Body), groupElement.Parameters[0]);
        }

        internal static bool Uses(Expression expression, ParameterExpression parameter) => ParameterFinder.Uses(expression, parameter);

        //whether an expression reads a parameter
        private sealed class ParameterFinder(ParameterExpression parameter) : ExpressionVisitor
        {
            private bool found;
            public static bool Uses(Expression expression, ParameterExpression parameter)
            {
                var finder = new ParameterFinder(parameter);
                _ = finder.Visit(expression);
                return finder.found;
            }
            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node == parameter)
                    found = true;
                return node;
            }
        }
    }
}

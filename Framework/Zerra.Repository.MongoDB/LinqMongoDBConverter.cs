// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using MongoDB.Bson;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zerra.Reflection;
using Zerra.Repository.Linq;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.MongoDB
{
    /// <summary>
    /// Converts a LINQ query to an aggregation pipeline. Conditions and values are aggregation expressions, a member through a single relation reads the related document with a $lookup,
    /// and a collection relation in a condition is the related documents looked up as an array.
    /// </summary>
    internal sealed partial class LinqMongoDBConverter
    {
        //a single identity is the document's _id, several are a document of them in _id and each is also a field of its own
        internal const string IdField = "_id";
        private const string LookupPrefix = "__r_";
        private const string ManyPrefix = "__m_";

        private readonly Dictionary<ParameterExpression, Scope> scopes = new();
        private readonly List<BsonDocument> pendingLookups = new();
        private readonly HashSet<string> lookups = new();
        private int variables;
        private int subqueries;

        //where a parameter's members are read from: the document, a looked up document, a variable of an array operator, or elements built from columns, such as the results of a group or a query a join pairs;
        //for elements the prefix is that of their columns, such as $c, so column 2 is $c2
        private sealed class Scope
        {
            public readonly string Prefix;
            public readonly string? FieldPath;
            public readonly ModelDetail? Model;
            public readonly LinqShape? Result;
            //the plan the columns of Result are of
            public LinqPlan? Plan { get; init; }
            //for elements a join may have no row of, the field that is missing when there's none
            public string? NullField { get; init; }
            //a parameter that is a value, such as the place of an element
            public BsonValue? Value { get; init; }
            //a GroupJoin's group, an array at the prefix of elements of Result
            public bool Group { get; init; }
            public Scope(string prefix, string? fieldPath, ModelDetail? model, LinqShape? result)
            {
                this.Prefix = prefix;
                this.FieldPath = fieldPath;
                this.Model = model;
                this.Result = result;
            }
        }
        //what an element of an array is: a model's document, or an element built from columns
        private readonly record struct Element(ModelDetail? Model, LinqShape? Shape, LinqPlan? Plan);
        private static Scope ElementScope(string variable, Element element)
            => element.Shape is not null ? new Scope("$$" + variable + ".c", null, null, element.Shape) { Plan = element.Plan } : new Scope("$$" + variable + ".", null, element.Model, null);

        public static MongoDBStatement ConvertLinq(LinqQuery query, ModelDetail modelDetail)
        {
            if (query.Temporal is not null)
                throw new NotSupportedException("MongoDB is not an event store");
            if (query.Combines)
                return ConvertCombinedTop(query, modelDetail);

            var plan = LinqPlan.Read(query, modelDetail);

            var converter = new LinqMongoDBConverter();
            var stages = new List<BsonDocument>();
            converter.WriteQuery(query, plan, modelDetail, stages);
            return new MongoDBStatement(modelDetail.DataSourceEntityName, stages, plan, null, ConvertPieces(plan));
        }

        //each relation read after the rows is a statement of the related collection, matched at run time by the keys of the parents read
        private static MongoDBStatement[]? ConvertPieces(LinqPlan plan)
        {
            if (plan.Pieces.Count == 0)
                return null;
            var statements = new MongoDBStatement[plan.Pieces.Count];
            for (var p = 0; p < statements.Length; p++)
            {
                var piece = plan.Pieces[p];
                var related = piece.RelatedModel;
                var converter = new LinqMongoDBConverter();
                var root = new Scope("$", String.Empty, related, null);
                var stages = new List<BsonDocument>();

                converter.WriteWhere(piece.Where, root, stages);

                var keyFields = new string[piece.RelatedKeys.Length];
                for (var k = 0; k < keyFields.Length; k++)
                    keyFields[k] = FieldName(piece.RelatedKeys[k], related);

                var sort = converter.WriteSortKeys(piece.Order, root, stages, "__o");
                if (piece.Skip.HasValue || piece.Take.HasValue)
                {
                    //each parent's rows are paged on their own, numbered within the parent's key
                    if (piece.Take == 0)
                    {
                        stages.Add(new BsonDocument("$match", new BsonDocument("$expr", false)));
                    }
                    else
                    {
                        var partition = new BsonDocument();
                        foreach (var keyField in keyFields)
                            partition[keyField.Replace('.', '_')] = "$" + keyField;
                        if (sort is null)
                            sort = new BsonDocument(IdField, 1);
                        stages.Add(new BsonDocument("$setWindowFields", new BsonDocument
                        {
                            { "partitionBy", partition },
                            { "sortBy", sort },
                            { "output", new BsonDocument("__n", new BsonDocument("$documentNumber", new BsonDocument())) }
                        }));
                        var range = new BsonDocument("$gt", piece.Skip ?? 0);
                        if (piece.Take.HasValue)
                            range["$lte"] = (piece.Skip ?? 0) + piece.Take.Value;
                        stages.Add(new BsonDocument("$match", new BsonDocument("__n", range)));
                    }
                }
                else if (sort is not null)
                {
                    stages.Add(new BsonDocument("$sort", sort));
                }

                var project = converter.ProjectColumns(piece.Plan, related, null);
                converter.FlushLookups(stages);
                stages.Add(new BsonDocument("$project", project));

                statements[p] = new MongoDBStatement(related.DataSourceEntityName, stages, piece.Plan, keyFields, ConvertPieces(piece.Plan));
            }
            return statements;
        }

        private void WriteQuery(LinqQuery query, LinqPlan plan, ModelDetail modelDetail, List<BsonDocument> stages)
        {
            var root = new Scope("$", String.Empty, modelDetail, null);

            WriteWhere(query.Where, root, stages);

            if (plan.KeyParts is not null)
            {
                WriteGroup(query, plan, modelDetail, root, stages);
                return;
            }

            var paged = query.Skip.HasValue || query.Take.HasValue;
            switch (query.Terminal)
            {
                case LinqTerminal.Count:
                case LinqTerminal.LongCount:
                    if (query.Distinct)
                    {
                        WriteDistinct(query, plan, modelDetail, root, stages, false);
                    }
                    else if (paged)
                    {
                        WriteOrder(query.Order, root, stages);
                        WritePage(query.Skip, query.Take, stages);
                    }
                    stages.Add(new BsonDocument("$count", "n"));
                    return;
                case LinqTerminal.Any:
                    if (paged)
                    {
                        WriteOrder(query.Order, root, stages);
                        WritePage(query.Skip, query.Take, stages);
                    }
                    stages.Add(new BsonDocument("$limit", 1));
                    stages.Add(new BsonDocument("$project", new BsonDocument(IdField, 1)));
                    return;
                case LinqTerminal.Sum:
                case LinqTerminal.Min:
                case LinqTerminal.Max:
                case LinqTerminal.Average:
                    {
                        if (paged)
                        {
                            WriteOrder(query.Order, root, stages);
                            WritePage(query.Skip, query.Take, stages);
                        }
                        var column = plan.Columns[0];
                        var value = ColumnValue(column, root);
                        FlushLookups(stages);
                        var accumulator = query.Terminal switch
                        {
                            LinqTerminal.Sum => "$sum",
                            LinqTerminal.Min => "$min",
                            LinqTerminal.Max => "$max",
                            _ => "$avg",
                        };
                        stages.Add(new BsonDocument("$group", new BsonDocument
                        {
                            { IdField, BsonNull.Value },
                            { "v", new BsonDocument(accumulator, value) }
                        }));
                        return;
                    }
                default:
                    if (query.Distinct)
                    {
                        if (plan.Pieces.Count > 0)
                            throw new NotSupportedException("Distinct with a relation read after the rows isn't supported");
                        WriteDistinct(query, plan, modelDetail, root, stages, true);
                        return;
                    }
                    WriteNumbering(plan, root, stages);
                    WriteOrder(query.Order, root, stages);
                    WritePage(query.Skip, query.Take, stages);
                    var project = ProjectColumns(plan, modelDetail, root);
                    FlushLookups(stages);
                    stages.Add(new BsonDocument("$project", project));
                    return;
            }
        }

        //the different elements are the groups of the projected documents, ordered and paged after
        private void WriteDistinct(LinqQuery query, LinqPlan plan, ModelDetail modelDetail, Scope root, List<BsonDocument> stages, bool elements)
        {
            var project = ProjectColumns(plan, modelDetail, root);
            BsonDocument? sort = null;
            if (elements && query.Order is not null)
            {
                sort = new BsonDocument();
                for (var i = 0; i < query.Order.Length; i++)
                {
                    var order = query.Order[i];
                    scopes[order.Expression.Parameters[0]] = root;
                    project["o" + i.ToString(CultureInfo.InvariantCulture)] = SortValue(order.Expression.Body);
                    sort["o" + i.ToString(CultureInfo.InvariantCulture)] = order.Descending ? -1 : 1;
                }
            }
            FlushLookups(stages);
            stages.Add(new BsonDocument("$project", project));
            stages.Add(new BsonDocument("$group", new BsonDocument(IdField, "$$ROOT")));
            stages.Add(new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$" + IdField)));
            if (sort is not null)
                stages.Add(new BsonDocument("$sort", sort));
            WritePage(query.Skip, query.Take, stages);
        }

        private void WriteGroup(LinqQuery query, LinqPlan plan, ModelDetail modelDetail, Scope root, List<BsonDocument> stages)
        {
            var id = new BsonDocument();
            scopes[plan.KeyParameter!] = root;
            for (var i = 0; i < plan.KeyParts!.Length; i++)
                id["k" + i.ToString(CultureInfo.InvariantCulture)] = Convert(plan.KeyParts[i]);

            var group = new BsonDocument(IdField, id);
            var project = new BsonDocument(IdField, 0);
            for (var i = 0; i < plan.Columns.Count; i++)
            {
                var column = plan.Columns[i];
                var name = "a" + i.ToString(CultureInfo.InvariantCulture);
                var columnName = "c" + i.ToString(CultureInfo.InvariantCulture);
                if (column.Parameter is not null)
                    scopes[column.Parameter] = root;
                switch (column.Kind)
                {
                    case LinqColumnKind.KeyPart:
                        project[columnName] = "$" + IdField + ".k" + column.KeyPart.ToString(CultureInfo.InvariantCulture);
                        continue;
                    case LinqColumnKind.Count:
                        group[name] = new BsonDocument("$sum", 1);
                        break;
                    case LinqColumnKind.CountIf:
                        group[name] = new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray { Convert(column.Expression!), 1, 0 }));
                        break;
                    case LinqColumnKind.CountDistinct:
                        group[name] = new BsonDocument("$addToSet", Convert(column.Expression!));
                        //as COUNT(DISTINCT ...) a null isn't counted
                        project[columnName] = new BsonDocument("$size", new BsonDocument("$filter", new BsonDocument
                        {
                            { "input", "$" + name },
                            { "cond", new BsonDocument("$ne", new BsonArray { "$$this", BsonNull.Value }) }
                        }));
                        continue;
                    case LinqColumnKind.Sum:
                        group[name] = new BsonDocument("$sum", Convert(column.Expression!));
                        break;
                    case LinqColumnKind.Average:
                        group[name] = new BsonDocument("$avg", Convert(column.Expression!));
                        break;
                    case LinqColumnKind.Min:
                        group[name] = new BsonDocument("$min", Convert(column.Expression!));
                        break;
                    case LinqColumnKind.Max:
                        group[name] = new BsonDocument("$max", Convert(column.Expression!));
                        break;
                    case LinqColumnKind.Join:
                        {
                            group[name] = new BsonDocument("$push", Convert(column.Expression!));
                            var separator = column.Separator ?? String.Empty;
                            //every value follows a separator, then the first separator is cut off
                            var joined = new BsonDocument("$reduce", new BsonDocument
                            {
                                { "input", "$" + name },
                                { "initialValue", String.Empty },
                                { "in", new BsonDocument("$concat", new BsonArray { "$$value", new BsonDocument("$literal", separator), new BsonDocument("$ifNull", new BsonArray { "$$this", String.Empty }) }) }
                            });
                            project[columnName] = new BsonDocument("$substrCP", new BsonArray { joined, separator.Length, Int32.MaxValue });
                            continue;
                        }
                    default:
                        throw new NotSupportedException($"A group's {column.Kind} isn't supported by MongoDB");
                }
                project[columnName] = "$" + name;
            }
            FlushLookups(stages);
            stages.Add(new BsonDocument("$group", group));
            stages.Add(new BsonDocument("$project", project));

            var results = new Scope("$c", null, null, plan.Shape) { Plan = plan };
            if (query.Having is not null)
            {
                scopes[query.Having.Parameters[0]] = results;
                stages.Add(new BsonDocument("$match", new BsonDocument("$expr", Convert(query.Having.Body))));
            }
            if (query.ResultOrder is not null)
                WriteOrder(query.ResultOrder, results, stages);
            WritePage(query.Skip, query.Take, stages);

            switch (query.Terminal)
            {
                case LinqTerminal.Count:
                case LinqTerminal.LongCount:
                    stages.Add(new BsonDocument("$count", "n"));
                    break;
                case LinqTerminal.Any:
                    stages.Add(new BsonDocument("$limit", 1));
                    break;
                case LinqTerminal.Sum:
                case LinqTerminal.Min:
                case LinqTerminal.Max:
                case LinqTerminal.Average:
                    throw new NotSupportedException("An aggregate of the results of groups isn't supported by MongoDB yet");
            }
        }

        private void WriteOrder(LinqOrder[]? order, Scope scope, List<BsonDocument> stages)
        {
            var sort = WriteSortKeys(order, scope, stages, "__o");
            if (sort is not null)
                stages.Add(new BsonDocument("$sort", sort));
        }
        //a member is sorted by its field, so an index can be used, anything else is computed into a field first
        private BsonDocument? WriteSortKeys(LinqOrder[]? order, Scope scope, List<BsonDocument> stages, string prefix)
        {
            if (order is null || order.Length == 0)
                return null;
            var sort = new BsonDocument();
            BsonDocument? computed = null;
            for (var i = 0; i < order.Length; i++)
            {
                scopes[order[i].Expression.Parameters[0]] = scope;
                var value = SortValue(order[i].Expression.Body);
                string field;
                if (value.IsString && value.AsString.StartsWith('$') && !value.AsString.StartsWith("$$", StringComparison.Ordinal))
                {
                    field = value.AsString.Substring(1);
                }
                else
                {
                    field = prefix + i.ToString(CultureInfo.InvariantCulture);
                    (computed ??= new BsonDocument())[field] = value;
                }
                //a later order by the same key adds nothing
                if (!sort.Contains(field))
                    sort[field] = order[i].Descending ? -1 : 1;
            }
            FlushLookups(stages);
            if (computed is not null)
                stages.Add(new BsonDocument("$addFields", computed));
            return sort;
        }

        //a stored enum is in order by its number, its name would sort by text
        private BsonValue SortValue(Expression key)
        {
            var bare = StripConvert(key);
            var stored = StoredEnum(bare);
            return stored is not null ? EnumNumber(Convert(bare), stored) : Convert(key);
        }

        private static void WritePage(int? skip, int? take, List<BsonDocument> stages)
        {
            if (skip.HasValue && skip.Value > 0)
                stages.Add(new BsonDocument("$skip", skip.Value));
            if (take.HasValue)
            {
                if (take.Value <= 0)
                    stages.Add(new BsonDocument("$match", new BsonDocument("$expr", false)));
                else
                    stages.Add(new BsonDocument("$limit", take.Value));
            }
        }

        //each column is a field named by its index
        private BsonDocument ProjectColumns(LinqPlan plan, ModelDetail modelDetail, Scope? root)
        {
            root ??= new Scope("$", String.Empty, modelDetail, null);
            var project = new BsonDocument(IdField, 0);
            for (var i = 0; i < plan.Columns.Count; i++)
                project["c" + i.ToString(CultureInfo.InvariantCulture)] = ColumnValue(plan.Columns[i], root);
            return project;
        }
        private BsonValue ColumnValue(LinqColumn column, Scope root)
        {
            if (column.From is not null)
                throw new NotSupportedException("Joins of queries aren't supported by MongoDB yet");
            switch (column.Kind)
            {
                case LinqColumnKind.Value:
                case LinqColumnKind.Sum:
                case LinqColumnKind.Min:
                case LinqColumnKind.Max:
                case LinqColumnKind.Average:
                    if (column.Expression is null)
                    {
                        if (column.Member is null)
                            throw new NotSupportedException("A column with no value isn't supported by MongoDB");
                        return "$" + root.FieldPath + FieldName(column.Member, root.Model!);
                    }
                    if (column.Parameter is not null)
                        scopes[column.Parameter] = root;
                    //as in SQL, a TimeSpan computed in the store isn't read, its total is
                    if (Underlying(column.Expression.Type) == typeof(TimeSpan) && StripConvert(column.Expression) is not MemberExpression)
                        throw new NotSupportedException("A TimeSpan computed in the store isn't read, read a total of it such as TotalMinutes");
                    return Convert(column.Expression);
                case LinqColumnKind.RowNumber:
                    return "$" + NumberField;
                default:
                    throw new NotSupportedException($"A column of {column.Kind} isn't supported by MongoDB yet");
            }
        }

        //a condition's parts of the document's own fields compared with values are a plain filter, which an index answers on its own and a count can take from the index;
        //the rest are an expression, after the lookups it reads
        private void WriteWhere(LambdaExpression? where, Scope root, List<BsonDocument> stages)
        {
            if (where is null)
            {
                FlushLookups(stages);
                return;
            }
            scopes[where.Parameters[0]] = root;
            var plain = new BsonArray();
            Expression? rest = null;
            foreach (var part in AndParts(where.Body))
            {
                var filter = PlainFilter(part);
                if (filter is not null)
                    plain.Add(filter);
                else
                    rest = rest is null ? part : Expression.AndAlso(rest, part);
            }
            if (plain.Count > 0)
                stages.Add(new BsonDocument("$match", plain.Count == 1 ? plain[0] : new BsonDocument("$and", plain)));
            var expression = rest is null ? null : Convert(rest);
            FlushLookups(stages);
            if (expression is not null)
                stages.Add(new BsonDocument("$match", new BsonDocument("$expr", expression)));
        }
        private static List<Expression> AndParts(Expression body)
        {
            var parts = new List<Expression>();
            var pending = new Stack<Expression>();
            pending.Push(body);
            while (pending.Count > 0)
            {
                var part = pending.Pop();
                if (part is BinaryExpression { NodeType: ExpressionType.AndAlso } and)
                {
                    pending.Push(and.Right);
                    pending.Push(and.Left);
                }
                else
                {
                    parts.Add(part);
                }
            }
            return parts;
        }

        //a filter in the query language when it means the same: a stored member of the document compared with a value, of the same type or a wider number,
        //and those joined by && || !; C# and the query language agree here that null equals null and only null, and that null orders with nothing
        private BsonDocument? PlainFilter(Expression exp)
        {
            switch (exp.NodeType)
            {
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                    {
                        var binary = (BinaryExpression)exp;
                        var left = PlainFilter(binary.Left);
                        if (left is null)
                            return null;
                        var right = PlainFilter(binary.Right);
                        if (right is null)
                            return null;
                        return new BsonDocument(exp.NodeType == ExpressionType.AndAlso ? "$and" : "$or", new BsonArray { left, right });
                    }
                case ExpressionType.Not:
                    {
                        var operand = PlainFilter(((UnaryExpression)exp).Operand);
                        return operand is null ? null : new BsonDocument("$nor", new BsonArray { operand });
                    }
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                    {
                        var binary = (BinaryExpression)exp;
                        var type = exp.NodeType;
                        string? field;
                        ModelMemberDetail? member;
                        Expression value;
                        if (PlainField(binary.Left, out field, out member) && IsEvaluatable(binary.Right))
                        {
                            value = binary.Right;
                        }
                        else if (PlainField(binary.Right, out field, out member) && IsEvaluatable(binary.Left))
                        {
                            value = binary.Left;
                            type = type switch
                            {
                                ExpressionType.LessThan => ExpressionType.GreaterThan,
                                ExpressionType.LessThanOrEqual => ExpressionType.GreaterThanOrEqual,
                                ExpressionType.GreaterThan => ExpressionType.LessThan,
                                ExpressionType.GreaterThanOrEqual => ExpressionType.LessThanOrEqual,
                                _ => type,
                            };
                        }
                        else
                        {
                            return null;
                        }
                        var equality = type is ExpressionType.Equal or ExpressionType.NotEqual;
                        var constant = Evaluate(value);
                        BsonValue bson;
                        if (constant is null)
                        {
                            //an order with null is false in C#, which the expression gives
                            if (!equality)
                                return null;
                            bson = BsonNull.Value;
                        }
                        else if (member!.EnumType is not null)
                        {
                            //a stored enum is its name, compared for equality only, its order is by its number
                            if (!equality || !TryEnumItem(constant, Underlying(constant.GetType()), member.EnumType, out var item))
                                return null;
                            bson = EnumItemText(item, member.EnumType);
                        }
                        else if (Underlying(member.Type) == typeof(char))
                        {
                            //C# compares a char by its number, the stored char is its text
                            bson = constant is char character ? character.ToString() : ((char)System.Convert.ToInt32(constant, CultureInfo.InvariantCulture)).ToString();
                        }
                        else
                        {
                            bson = ToBson(constant);
                        }
                        var op = type switch
                        {
                            ExpressionType.Equal => "$eq",
                            ExpressionType.NotEqual => "$ne",
                            ExpressionType.LessThan => "$lt",
                            ExpressionType.LessThanOrEqual => "$lte",
                            ExpressionType.GreaterThan => "$gt",
                            _ => "$gte",
                        };
                        return new BsonDocument(field!, new BsonDocument(op, bson));
                    }
                case ExpressionType.MemberAccess:
                    //a bool member as the condition
                    if (Underlying(exp.Type) == typeof(bool) && PlainField(exp, out var boolField, out _))
                        return new BsonDocument(boolField!, new BsonDocument("$eq", true));
                    return null;
                case ExpressionType.Call:
                    {
                        var call = (MethodCallExpression)exp;
                        Expression? collection = null;
                        Expression? item = null;
                        if (call.Method.Name == "Contains" && call.Method.DeclaringType == typeof(Enumerable) && call.Arguments.Count == 2)
                        {
                            collection = call.Arguments[0];
                            item = call.Arguments[1];
                        }
                        else if (call.Method.Name == "Contains" && call.Method.DeclaringType == typeof(MemoryExtensions) && call.Arguments.Count == 2)
                        {
                            collection = call.Arguments[0] is MethodCallExpression { Method.Name: "op_Implicit", Arguments.Count: 1 } span ? span.Arguments[0] : call.Arguments[0];
                            item = call.Arguments[1];
                        }
                        else if (call.Method.Name == "Contains" && call.Object is not null && call.Object.Type != typeof(string) && call.Arguments.Count == 1)
                        {
                            collection = call.Object;
                            item = call.Arguments[0];
                        }
                        else if (call.Method.Name == "StartsWith" && call.Method.DeclaringType == typeof(string) && call.Object is not null && call.Arguments.Count == 1 && call.Arguments[0].Type == typeof(string) && IsEvaluatable(call.Arguments[0]) && PlainField(call.Object, out var textField, out _))
                        {
                            //a prefix, which an index on the field answers
                            if (Evaluate(call.Arguments[0]) is not string prefix)
                                return null;
                            return new BsonDocument(textField!, new BsonDocument("$regex", new BsonRegularExpression("^" + System.Text.RegularExpressions.Regex.Escape(prefix))));
                        }
                        if (collection is null || item is null || !IsEvaluatable(collection) || !PlainField(item, out var inField, out var inMember))
                            return null;
                        var values = new BsonArray();
                        if (Evaluate(collection) is System.Collections.IEnumerable items)
                        {
                            foreach (var value in items)
                            {
                                if (value is null)
                                {
                                    values.Add(BsonNull.Value);
                                }
                                else if (inMember!.EnumType is not null)
                                {
                                    if (!TryEnumItem(value, Underlying(value.GetType()), inMember.EnumType, out var enumItem))
                                        return null;
                                    values.Add(EnumItemText(enumItem, inMember.EnumType));
                                }
                                else if (Underlying(inMember.Type) == typeof(char))
                                {
                                    values.Add(value is char character ? character.ToString() : ((char)System.Convert.ToInt32(value, CultureInfo.InvariantCulture)).ToString());
                                }
                                else
                                {
                                    values.Add(ToBson(value));
                                }
                            }
                        }
                        return new BsonDocument(inField!, new BsonDocument("$in", values));
                    }
                default:
                    return null;
            }
        }
        //the field of a stored member of the document itself, through the conversions that keep its value: a nullable, a wider number, an enum's or a char's number
        private bool PlainField(Expression exp, [MaybeNullWhen(false)] out string field, [MaybeNullWhen(false)] out ModelMemberDetail member)
        {
            field = null;
            member = null;
            while (exp is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
            {
                var from = Underlying(convert.Operand.Type);
                var to = Underlying(convert.Type);
                if (from != to && !(from.IsEnum && to == Enum.GetUnderlyingType(from)) && !(from == typeof(char) && IsInteger(to)) && !Widens(from, to))
                    return false;
                exp = convert.Operand;
            }
            if (exp is MemberExpression { Member.Name: "Value", Expression: { } nullable } && Nullable.GetUnderlyingType(nullable.Type) is not null)
                exp = nullable;
            if (exp is not MemberExpression { Expression: ParameterExpression parameter } owned || !scopes.TryGetValue(parameter, out var scope))
                return false;
            if (scope.Model is null || scope.Result is not null || scope.Value is not null || scope.Prefix != "$" || scope.FieldPath != String.Empty)
                return false;
            if (!scope.Model.TryGetProperty(owned.Member.Name, out var modelMember) || modelMember.StoreName is null || modelMember.IsRelated)
                return false;
            field = FieldName(modelMember, scope.Model);
            member = modelMember;
            return true;
        }
        //a conversion every value of the first type survives, which compares as BSON compares numbers
        private static bool Widens(Type from, Type to)
        {
            if (!IsInteger(from) && from != typeof(float))
                return false;
            if (to == typeof(double) || to == typeof(decimal))
                return true;
            if (from == typeof(float))
                return false;
            return IsInteger(to) && IntegerSize(to) >= IntegerSize(from) && (to != typeof(ulong) && to != typeof(uint) && to != typeof(ushort) || from == typeof(byte) || from == typeof(ushort) || from == typeof(uint));
        }

        private void FlushLookups(List<BsonDocument> stages)
        {
            if (pendingLookups.Count == 0)
                return;
            stages.AddRange(pendingLookups);
            pendingLookups.Clear();
        }

        /// <summary>
        /// Converts the condition of an update or delete by condition, with the lookups it needs, which are none when the condition is of the model's own fields.
        /// </summary>
        /// <returns>The filter of the documents, or null when the condition reads through relations, whose stages find the documents instead.</returns>
        public static BsonDocument? ConvertPersistWhere(LambdaExpression where, ModelDetail modelDetail, out List<BsonDocument>? lookupStages)
        {
            var converter = new LinqMongoDBConverter();
            var stages = new List<BsonDocument>();
            converter.WriteWhere(where, new Scope("$", String.Empty, modelDetail, null), stages);
            foreach (var stage in stages)
            {
                if (!stage.Contains("$match"))
                {
                    lookupStages = stages;
                    return null;
                }
            }
            lookupStages = null;
            if (stages.Count == 0)
                return new BsonDocument();
            if (stages.Count == 1)
                return stages[0]["$match"].AsBsonDocument;
            return new BsonDocument("$and", new BsonArray { stages[0]["$match"], stages[1]["$match"] });
        }

        /// <summary>
        /// Converts the members an update by condition sets, each an expression of the document's own fields.
        /// </summary>
        public static BsonDocument ConvertPersistUpdate(ProjectionMember[] sets, ModelDetail modelDetail)
        {
            var converter = new LinqMongoDBConverter();
            var set = new BsonDocument();
            foreach (var member in sets)
            {
                var modelMember = modelDetail.GetMember(member.MemberDetail.Name);
                if (modelMember.IsIdentity)
                    throw new NotSupportedException($"An update by condition can't set the identity {modelMember.Name}");
                if (modelMember.StoreName is null || modelMember.IsRelated)
                    throw new NotSupportedException($"{modelMember.Name} isn't stored, an update by condition can't set it");
                converter.scopes[member.Parameter] = new Scope("$", String.Empty, modelDetail, null);
                var value = converter.Convert(member.Expression);
                //an enum computed from a number is stored as its name, as the member is
                if (modelMember.EnumType is not null && !IsEvaluatable(member.Expression) && converter.StoredEnum(StripConvert(member.Expression)) is null)
                    value = StoredName(value, modelMember.EnumType);
                if (converter.pendingLookups.Count > 0)
                    throw new NotSupportedException("An update by condition sets its members from the model's own members on MongoDB");
                set[FieldName(modelMember, modelDetail)] = value;
            }
            return set;
        }

        /// <summary>The field a member is stored in.</summary>
        public static string FieldName(ModelMemberDetail member, ModelDetail model)
            => member.IsIdentity && model.IdentityMembers.Count == 1 ? IdField : member.StoreName;

        private BsonValue Convert(Expression exp)
        {
            if (IsEvaluatable(exp))
                return Constant(Evaluate(exp));

            switch (exp.NodeType)
            {
                case ExpressionType.Quote:
                    return Convert(((UnaryExpression)exp).Operand);
                case ExpressionType.Lambda:
                    return Convert(((LambdaExpression)exp).Body);
                case ExpressionType.Parameter:
                    return ConvertParameter((ParameterExpression)exp);
                case ExpressionType.MemberAccess:
                    return ConvertMember((MemberExpression)exp);
                case ExpressionType.Call:
                    return ConvertCall((MethodCallExpression)exp);
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    return ConvertConvert((UnaryExpression)exp);
                case ExpressionType.TypeAs:
                case ExpressionType.UnaryPlus:
                    return Convert(((UnaryExpression)exp).Operand);
                case ExpressionType.Not:
                    {
                        var operand = ((UnaryExpression)exp).Operand;
                        if (Underlying(operand.Type) == typeof(bool))
                            return new BsonDocument("$not", new BsonArray { Convert(operand) });
                        return new BsonDocument("$bitNot", Convert(operand));
                    }
                case ExpressionType.OnesComplement:
                    return new BsonDocument("$bitNot", Convert(((UnaryExpression)exp).Operand));
                case ExpressionType.Power:
                    {
                        var binary = (BinaryExpression)exp;
                        return new BsonDocument("$pow", new BsonArray { Convert(binary.Left), Convert(binary.Right) });
                    }
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    return new BsonDocument("$subtract", new BsonArray { 0, Convert(((UnaryExpression)exp).Operand) });
                case ExpressionType.ArrayLength:
                    return new BsonDocument("$size", Convert(((UnaryExpression)exp).Operand));
                case ExpressionType.AndAlso:
                    {
                        var binary = (BinaryExpression)exp;
                        return new BsonDocument("$and", new BsonArray { Convert(binary.Left), Convert(binary.Right) });
                    }
                case ExpressionType.OrElse:
                    {
                        var binary = (BinaryExpression)exp;
                        return new BsonDocument("$or", new BsonArray { Convert(binary.Left), Convert(binary.Right) });
                    }
                case ExpressionType.And:
                case ExpressionType.Or:
                case ExpressionType.ExclusiveOr:
                    {
                        var binary = (BinaryExpression)exp;
                        if (Underlying(binary.Left.Type) == typeof(bool))
                        {
                            var left = Convert(binary.Left);
                            var right = Convert(binary.Right);
                            return exp.NodeType switch
                            {
                                ExpressionType.And => new BsonDocument("$and", new BsonArray { left, right }),
                                ExpressionType.Or => new BsonDocument("$or", new BsonArray { left, right }),
                                _ => new BsonDocument("$ne", new BsonArray { left, right }),
                            };
                        }
                        var op = exp.NodeType switch
                        {
                            ExpressionType.And => "$bitAnd",
                            ExpressionType.Or => "$bitOr",
                            _ => "$bitXor",
                        };
                        return new BsonDocument(op, new BsonArray { NumberValue(binary.Left), NumberValue(binary.Right) });
                    }
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                    return ConvertComparison((BinaryExpression)exp);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    {
                        var binary = (BinaryExpression)exp;
                        if (binary.Type == typeof(string))
                            return Concat([binary.Left, binary.Right]);
                        if (IsDate(binary.Left.Type) && IsTicks(binary.Right.Type))
                            return new BsonDocument("$add", new BsonArray { Convert(binary.Left), TicksToMilliseconds(Convert(binary.Right)) });
                        return new BsonDocument("$add", new BsonArray { Convert(binary.Left), Convert(binary.Right) });
                    }
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    {
                        var binary = (BinaryExpression)exp;
                        if (IsDate(binary.Left.Type) && IsDate(binary.Right.Type))
                            return new BsonDocument("$multiply", new BsonArray { new BsonDocument("$subtract", new BsonArray { Convert(binary.Left), Convert(binary.Right) }), 10000L });
                        //the time from one TimeOnly to another goes forward round midnight
                        if (Underlying(binary.Left.Type) == typeof(TimeOnly) && Underlying(binary.Right.Type) == typeof(TimeOnly))
                            return WrapDay(new BsonDocument("$subtract", new BsonArray { Convert(binary.Left), Convert(binary.Right) }));
                        if (IsDate(binary.Left.Type) && IsTicks(binary.Right.Type))
                            return new BsonDocument("$subtract", new BsonArray { Convert(binary.Left), TicksToMilliseconds(Convert(binary.Right)) });
                        return new BsonDocument("$subtract", new BsonArray { Convert(binary.Left), Convert(binary.Right) });
                    }
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    {
                        var binary = (BinaryExpression)exp;
                        return new BsonDocument("$multiply", new BsonArray { Convert(binary.Left), Convert(binary.Right) });
                    }
                case ExpressionType.Divide:
                    {
                        var binary = (BinaryExpression)exp;
                        var divide = new BsonDocument("$divide", new BsonArray { Convert(binary.Left), Convert(binary.Right) });
                        //integer division drops the fraction toward zero
                        return IsInteger(Underlying(binary.Type)) ? new BsonDocument("$trunc", divide) : divide;
                    }
                case ExpressionType.Modulo:
                    {
                        var binary = (BinaryExpression)exp;
                        return new BsonDocument("$mod", new BsonArray { Convert(binary.Left), Convert(binary.Right) });
                    }
                case ExpressionType.Coalesce:
                    {
                        var binary = (BinaryExpression)exp;
                        return new BsonDocument("$ifNull", new BsonArray { Convert(binary.Left), Convert(binary.Right) });
                    }
                case ExpressionType.Conditional:
                    {
                        var conditional = (ConditionalExpression)exp;
                        return new BsonDocument("$cond", new BsonArray { Convert(conditional.Test), Convert(conditional.IfTrue), Convert(conditional.IfFalse) });
                    }
                default:
                    throw new NotSupportedException($"{exp.NodeType} isn't supported by MongoDB: {exp}");
            }
        }

        private BsonValue ConvertParameter(ParameterExpression parameter)
        {
            if (!scopes.TryGetValue(parameter, out var scope))
                throw new NotSupportedException($"{parameter.Name} isn't a parameter MongoDB can read");
            if (scope.Value is not null)
                return scope.Value;
            if (scope.Result is not null)
                return ResultValue(scope, scope.Result);
            if (scope.Model is null)
                return scope.Prefix.EndsWith('.') ? scope.Prefix.Substring(0, scope.Prefix.Length - 1) : scope.Prefix;
            throw new NotSupportedException($"The whole {scope.Model.Type.Name} isn't a value MongoDB can compare, compare its members");
        }

        private BsonValue ConvertMember(MemberExpression member)
        {
            var owner = member.Expression!;
            var ownerType = owner.Type;

            if (Nullable.GetUnderlyingType(ownerType) is not null)
            {
                return member.Member.Name switch
                {
                    "Value" => Convert(owner),
                    "HasValue" => new BsonDocument("$gt", new BsonArray { Convert(owner), BsonNull.Value }),
                    _ => throw new NotSupportedException($"{member.Member.Name} of a nullable isn't supported by MongoDB"),
                };
            }

            if (ownerType == typeof(string))
            {
                if (member.Member.Name == "Length")
                {
                    var name = NewVariable();
                    return Let(name, Convert(owner), new BsonDocument("$cond", new BsonArray { IsString("$$" + name), new BsonDocument("$strLenCP", "$$" + name), BsonNull.Value }));
                }
                throw new NotSupportedException($"string.{member.Member.Name} isn't supported by MongoDB");
            }

            if (ownerType == typeof(DateTime) || ownerType == typeof(DateTimeOffset) || ownerType == typeof(DateOnly))
                return ConvertDateMember(member.Member.Name, Convert(owner), ownerType);

            if (ownerType == typeof(TimeSpan) || ownerType == typeof(TimeOnly))
                return ConvertTicksMember(member.Member.Name, Convert(owner), ownerType);

            if (TryResultMember(member, out var resultField))
                return resultField;

            //a member of one element of a relation, such as x.Lines.OrderBy(l => l.Price).First().Quantity, is null when there's no element
            if (owner is MethodCallExpression or ConditionalExpression)
            {
                var elementModel = ModelAnalyzer.GetModel(ownerType);
                if (!elementModel.TryGetProperty(member.Member.Name, out var elementMember) || elementMember.StoreName is null || elementMember.IsRelated)
                    throw new NotSupportedException($"{member} isn't a member MongoDB can read");
                return new BsonDocument("$getField", new BsonDocument { { "field", new BsonDocument("$literal", FieldName(elementMember, elementModel)) }, { "input", Convert(owner) } });
            }

            var (scope, model, modelMember) = ResolveModelMember(member);
            if (modelMember.IsRelated)
            {
                if (modelMember.IsEnumerable)
                    return "$" + Many(scope, model, modelMember);
                //a single relation as a value is its document, which is missing when there isn't one
                return "$" + Lookup(scope, model, modelMember);
            }
            if (modelMember.StoreName is null)
                throw new NotSupportedException($"{model.Type.Name}.{modelMember.Name} isn't stored");
            return scope.Prefix + FieldName(modelMember, model);
        }

        //a member of a model the parameter is, through single relations
        private (Scope Scope, ModelDetail Model, ModelMemberDetail Member) ResolveModelMember(MemberExpression member)
        {
            var owner = member.Expression;
            Scope scope;
            ModelDetail model;
            if (owner is ParameterExpression parameter && scopes.TryGetValue(parameter, out var parameterScope) && parameterScope.Model is not null)
            {
                scope = parameterScope;
                model = parameterScope.Model;
            }
            else if (owner is MemberExpression ownerMember)
            {
                var (ownerScope, ownerModel, relation) = ResolveModelMember(ownerMember);
                if (!relation.IsRelated || relation.IsEnumerable)
                    throw new NotSupportedException($"{member} isn't a member MongoDB can read");
                var lookup = Lookup(ownerScope, ownerModel, relation);
                model = ModelAnalyzer.GetModel(relation.ActualType);
                scope = new Scope("$" + lookup + ".", lookup + ".", model, null);
            }
            else if (owner is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.TypeAs } cast && cast.Operand is ParameterExpression castParameter && scopes.TryGetValue(castParameter, out var castScope) && castScope.Model is not null)
            {
                scope = castScope;
                model = castScope.Model;
            }
            else
            {
                throw new NotSupportedException($"{member} isn't a member MongoDB can read");
            }
            if (!model.TryGetProperty(member.Member.Name, out var modelMember))
                throw new NotSupportedException($"{model.Type.Name}.{member.Member.Name} isn't a member of the model");
            return (scope, model, modelMember);
        }

        //the related document of a single relation, looked up once into a field of the document
        private string Lookup(Scope scope, ModelDetail model, ModelMemberDetail relation)
        {
            if (scope.FieldPath is null)
                throw new NotSupportedException($"{relation.Name} can't be read through a relation's elements on MongoDB yet");
            var name = scope.FieldPath + LookupPrefix + relation.Name;
            if (lookups.Add(name))
            {
                var related = ModelAnalyzer.GetModel(relation.ActualType);
                var names = relation.ForeignIdentities ?? throw new NotSupportedException($"Reading {relation.Name} needs a foreign identity");
                if (names.Length != related.IdentityMembers.Count)
                    throw new NotSupportedException($"{relation.Name} names {names.Length} foreign identities where {related.Type.Name} has {related.IdentityMembers.Count} identities");
                var localFields = new string[names.Length];
                var foreignFields = new string[names.Length];
                for (var i = 0; i < names.Length; i++)
                {
                    localFields[i] = scope.FieldPath + FieldName(model.GetMember(names[i]), model);
                    foreignFields[i] = FieldName(related.IdentityMembers[i], related);
                }
                pendingLookups.Add(LookupStage(related.DataSourceEntityName, localFields, foreignFields, name));
                pendingLookups.Add(new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$" + name },
                    { "preserveNullAndEmptyArrays", true }
                }));
            }
            return name;
        }
        //a key of one member is looked up by its field, one of several by a pipeline matching each pair, as SQL joins on them
        private static BsonDocument LookupStage(string from, string[] localFields, string[] foreignFields, string name)
        {
            if (localFields.Length == 1)
            {
                return new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", from },
                    { "localField", localFields[0] },
                    { "foreignField", foreignFields[0] },
                    { "as", name }
                });
            }
            var let = new BsonDocument();
            var matches = new BsonArray();
            for (var i = 0; i < localFields.Length; i++)
            {
                var variable = "k" + i.ToString(CultureInfo.InvariantCulture);
                let[variable] = "$" + localFields[i];
                matches.Add(new BsonDocument("$eq", new BsonArray { "$" + foreignFields[i], "$$" + variable }));
            }
            return new BsonDocument("$lookup", new BsonDocument
            {
                { "from", from },
                { "let", let },
                { "pipeline", new BsonArray { new BsonDocument("$match", new BsonDocument("$expr", new BsonDocument("$and", matches))) } },
                { "as", name }
            });
        }
        //the related documents of a collection relation, looked up once as an array field of the document
        private string Many(Scope scope, ModelDetail model, ModelMemberDetail relation)
        {
            if (scope.FieldPath is null)
                throw new NotSupportedException($"{relation.Name} can't be read through a relation's elements on MongoDB yet");
            var name = scope.FieldPath + ManyPrefix + relation.Name;
            if (lookups.Add(name))
            {
                var related = ModelAnalyzer.GetModel(relation.ActualType);
                var names = relation.ForeignIdentities ?? throw new NotSupportedException($"Reading {relation.Name} needs a foreign identity");
                if (names.Length != model.IdentityMembers.Count)
                    throw new NotSupportedException($"{relation.Name} names {names.Length} foreign identities where {model.Type.Name} has {model.IdentityMembers.Count} identities");
                var localFields = new string[names.Length];
                var foreignFields = new string[names.Length];
                for (var i = 0; i < names.Length; i++)
                {
                    localFields[i] = scope.FieldPath + FieldName(model.IdentityMembers[i], model);
                    foreignFields[i] = FieldName(related.GetMember(names[i]), related);
                }
                pendingLookups.Add(LookupStage(related.DataSourceEntityName, localFields, foreignFields, name));
            }
            return name;
        }

        //a member of elements built from columns, such as the results of groups, is the column the shape reads it from
        private bool TryResultMember(MemberExpression member, [MaybeNullWhen(false)] out BsonValue field)
        {
            field = null;
            var members = new List<MemberExpression>();
            Expression? owner = member;
            while (owner is MemberExpression parent)
            {
                members.Add(parent);
                owner = parent.Expression;
            }
            if (owner is not ParameterExpression parameter || !scopes.TryGetValue(parameter, out var scope) || scope.Result is null || scope.Group)
                return false;
            var shape = scope.Result;
            for (var i = members.Count - 1; i >= 0; i--)
            {
                if (!ResultPart(shape, members[i].Member.Name, out var next, out var column))
                    throw new NotSupportedException($"{members[i].Member.Name} isn't a member of the elements MongoDB can read");
                if (column is not null)
                {
                    var columnField = scope.Prefix + column.Index.ToString(CultureInfo.InvariantCulture);
                    if (i == 0)
                    {
                        field = columnField;
                        return true;
                    }
                    //a member of a value, such as a date's Year, is computed from the column
                    var value = Expression.Parameter(members[i].Type, "v");
                    Expression rest = value;
                    for (var j = i - 1; j >= 0; j--)
                        rest = Expression.MakeMemberAccess(rest, members[j].Member);
                    scopes[value] = new Scope(String.Empty, null, null, null) { Value = columnField };
                    field = Convert(rest);
                    return true;
                }
                shape = next!;
            }
            field = ResultValue(scope, shape);
            return true;
        }
        //an element's value is its column; a model or an object is only compared with null, by the column saying whether the join had its row
        private static BsonValue ResultValue(Scope scope, LinqShape shape)
        {
            if (shape.Kind == LinqShapeKind.Column)
                return scope.Prefix + shape.Column!.Index.ToString(CultureInfo.InvariantCulture);
            if (shape == scope.Result && scope.NullField is not null)
                return scope.NullField;
            if (shape.JoinedKey is not null)
                return scope.Prefix + shape.JoinedKey.Index.ToString(CultureInfo.InvariantCulture);
            throw new NotSupportedException("A condition or order on elements compares their values, not objects");
        }
        //the column of elements built from columns a value is, with the scope it's read in, or null when it isn't one
        private (LinqColumn Column, Scope Scope)? ResultColumn(Expression exp)
        {
            var names = new List<string>();
            var owner = exp;
            while (owner is MemberExpression parent)
            {
                names.Add(parent.Member.Name);
                owner = parent.Expression!;
            }
            //an element taken as an object is read as its own type
            if (owner is UnaryExpression { NodeType: ExpressionType.Convert, Operand: ParameterExpression boxed } && boxed.Type == typeof(object))
                owner = boxed;
            if (owner is not ParameterExpression parameter || !scopes.TryGetValue(parameter, out var scope) || scope.Result is null || scope.Group)
                return null;
            var shape = scope.Result;
            if (names.Count == 0)
                return shape.Kind == LinqShapeKind.Column ? (shape.Column!, scope) : null;
            for (var i = names.Count - 1; i >= 0; i--)
            {
                if (!ResultPart(shape, names[i], out var next, out var column))
                    return null;
                if (column is not null)
                    return i == 0 ? (column, scope) : null;
                shape = next!;
            }
            return null;
        }
        //the part of an element a member reads, a column for a value or a model's member
        //whether a column holds an enum's stored name: a model's member, or the key of a group or a value that is one
        private bool ColumnHoldsStoredEnum(LinqColumn column, LinqPlan? plan)
        {
            if (column.Member is not null)
                return column.Member.EnumType is not null;
            if (column.Kind == LinqColumnKind.KeyPart)
                return plan?.KeyParts is not null && plan.KeyParameter is not null && StoredEnumOf(plan.KeyParts[column.KeyPart], plan.KeyParameter);
            if (column.Kind != LinqColumnKind.Value || column.Expression is null || column.From is not null)
                return false;
            if (column.Parameter is not null && scopes.ContainsKey(column.Parameter))
                return StoredEnum(StripConvert(column.Expression)) is not null;
            return column.Parameter is not null && StoredEnumOf(column.Expression, column.Parameter);
        }
        //a member of the model a parameter is, through single relations, that is an enum stored as its name
        private static bool StoredEnumOf(Expression exp, ParameterExpression parameter)
        {
            exp = StripConvert(exp);
            if (exp is MemberExpression { Member.Name: "Value", Expression: { } nullable } && Nullable.GetUnderlyingType(nullable.Type) is not null)
                exp = nullable;
            var names = new List<string>();
            while (exp is MemberExpression member)
            {
                names.Add(member.Member.Name);
                exp = member.Expression!;
            }
            if (exp != parameter || names.Count == 0 || parameter.Type.IsValueType)
                return false;
            var model = ModelAnalyzer.GetModel(parameter.Type);
            for (var i = names.Count - 1; i >= 0; i--)
            {
                if (!model.TryGetProperty(names[i], out var modelMember))
                    return false;
                if (i == 0)
                    return modelMember.EnumType is not null;
                if (!modelMember.IsRelated || modelMember.IsEnumerable)
                    return false;
                model = ModelAnalyzer.GetModel(modelMember.ActualType);
            }
            return false;
        }
        private static bool ResultPart(LinqShape shape, string name, out LinqShape? next, out LinqColumn? column)
        {
            next = null;
            column = null;
            if (shape.Kind == LinqShapeKind.Model)
            {
                for (var i = 0; i < shape.ModelMembers!.Length; i++)
                {
                    if (shape.ModelMembers[i].Name == name)
                    {
                        column = shape.ModelColumns![i];
                        return true;
                    }
                }
                return false;
            }
            if (shape.Kind != LinqShapeKind.Object)
                return false;
            for (var i = 0; i < (shape.ArgumentNames?.Length ?? 0); i++)
            {
                if (shape.ArgumentNames![i] == name)
                    next = shape.Arguments![i];
            }
            for (var i = 0; next is null && i < (shape.Members?.Length ?? 0); i++)
            {
                if (shape.Members![i].Name == name)
                    next = shape.Values![i];
            }
            if (next is null)
                return false;
            if (next.Kind == LinqShapeKind.Column)
                column = next.Column;
            return true;
        }

        private BsonValue ConvertComparison(BinaryExpression binary)
        {
            var left = binary.Left;
            var right = binary.Right;
            var equality = binary.NodeType is ExpressionType.Equal or ExpressionType.NotEqual;

            //C# compiles a comparison of enums to one of numbers; a stored enum is its name, compared by text for equality and by number otherwise
            if (equality)
            {
                var bareLeft = StripConvert(left);
                var bareRight = StripConvert(right);
                var leftEnum = StoredEnum(bareLeft);
                var rightEnum = StoredEnum(bareRight);
                if (leftEnum is not null && rightEnum is not null && leftEnum == rightEnum)
                    return Compare(binary.NodeType, Convert(bareLeft), Convert(bareRight));
                if (leftEnum is not null && TryEnumText(bareRight, leftEnum, out var rightText))
                    return Compare(binary.NodeType, Convert(bareLeft), rightText);
                if (rightEnum is not null && TryEnumText(bareLeft, rightEnum, out var leftText))
                    return Compare(binary.NodeType, leftText, Convert(bareRight));
            }

            //null and a missing value, such as a relation there's none of, are below everything else
            if (equality && IsEvaluatable(right) && Evaluate(right) is null)
                return new BsonDocument(binary.NodeType == ExpressionType.Equal ? "$lte" : "$gt", new BsonArray { Convert(left), BsonNull.Value });
            if (equality && IsEvaluatable(left) && Evaluate(left) is null)
                return new BsonDocument(binary.NodeType == ExpressionType.Equal ? "$lte" : "$gt", new BsonArray { Convert(right), BsonNull.Value });

            //any value read from a document can be missing, such as a member of a relation's element there's none of, which a comparison of order is false for
            //C# compiles a char to its number, a stored char is its text
            var chars = Underlying(StripConvert(left).Type) == typeof(char) || Underlying(StripConvert(right).Type) == typeof(char);
            var leftValue = chars ? CharValue(left) : Convert(left);
            var rightValue = chars ? CharValue(right) : Convert(right);
            if (equality)
                return Compare(binary.NodeType, leftValue, rightValue);
            var compare = Compare(binary.NodeType, leftValue, rightValue);
            var guards = new BsonArray();
            if (!IsEvaluatable(left))
                guards.Add(new BsonDocument("$gt", new BsonArray { leftValue, BsonNull.Value }));
            if (!IsEvaluatable(right))
                guards.Add(new BsonDocument("$gt", new BsonArray { rightValue, BsonNull.Value }));
            else if (Evaluate(right) is null)
                return false;
            if (IsEvaluatable(left) && Evaluate(left) is null)
                return false;
            if (guards.Count == 0)
                return compare;
            guards.Add(compare);
            return new BsonDocument("$and", guards);
        }
        private BsonValue CharValue(Expression exp)
        {
            if (IsEvaluatable(exp))
            {
                var value = Evaluate(exp);
                return value is null ? BsonNull.Value : Constant(((char)System.Convert.ToInt32(value, CultureInfo.InvariantCulture)).ToString());
            }
            var bare = StripConvert(exp);
            if (Underlying(bare.Type) != typeof(char))
                throw new NotSupportedException("A char is compared with a char or a constant on MongoDB, not a computed number");
            return Convert(bare);
        }
        private static BsonValue Compare(ExpressionType type, BsonValue left, BsonValue right)
        {
            var op = type switch
            {
                ExpressionType.Equal => "$eq",
                ExpressionType.NotEqual => "$ne",
                ExpressionType.LessThan => "$lt",
                ExpressionType.LessThanOrEqual => "$lte",
                ExpressionType.GreaterThan => "$gt",
                _ => "$gte",
            };
            return new BsonDocument(op, new BsonArray { left, right });
        }

        private BsonValue ConvertConvert(UnaryExpression convert)
        {
            var operand = convert.Operand;
            var from = Underlying(operand.Type);
            var to = Underlying(convert.Type);
            if (from == to || to == typeof(object) || to.IsAssignableFrom(from))
                return Convert(operand);
            if (from.IsEnum)
            {
                var stored = StoredEnum(StripConvert(operand));
                var value = stored is not null ? EnumNumber(Convert(StripConvert(operand)), stored) : Convert(operand);
                return to.IsEnum ? value : ConvertNumber(value, Enum.GetUnderlyingType(from), to);
            }
            //an enum computed from a number stays a number
            if (to.IsEnum)
                return Convert(operand);
            if (to == typeof(string))
                return new BsonDocument("$toString", Convert(operand));
            if (from == typeof(char) || to == typeof(char))
                throw new NotSupportedException("Converting between char and a number isn't supported by MongoDB");
            return ConvertNumber(Convert(operand), from, to);
        }
        private static BsonValue ConvertNumber(BsonValue value, Type from, Type to)
        {
            if (from == to)
                return value;
            if (to == typeof(double) || to == typeof(float))
                return IsInteger(from) || from == typeof(float) || from == typeof(double) ? value : new BsonDocument("$toDouble", value);
            if (to == typeof(decimal))
                return new BsonDocument("$toDecimal", value);
            if (IsInteger(to))
            {
                //a fraction is cut toward zero by a cast, a wider integer is cut to the narrower one's size by $toInt
                if (IsInteger(from) && IntegerSize(from) <= IntegerSize(to))
                    return value;
                return new BsonDocument(IntegerSize(to) > 4 ? "$toLong" : "$toInt", new BsonDocument("$trunc", value));
            }
            if (to == typeof(bool))
                return new BsonDocument("$toBool", value);
            throw new NotSupportedException($"Converting {from.Name} to {to.Name} isn't supported by MongoDB");
        }

        private BsonValue ConvertCall(MethodCallExpression call)
        {
            var method = call.Method;
            var declaring = method.DeclaringType;

            if (call.Object is ConstantExpression { Value: LinqSubquery subquery })
                return ConvertSubquery(subquery, call.Arguments[0]);

            if (declaring == typeof(string))
                return ConvertStringCall(call);

            if (declaring == typeof(Enumerable))
                return ConvertEnumerableCall(call);

            //C# 14 reads an array's Contains as the span's
            if (declaring == typeof(MemoryExtensions) && method.Name == "Contains" && (call.Arguments.Count == 2 || (call.Arguments.Count == 3 && IsEvaluatable(call.Arguments[2]) && Evaluate(call.Arguments[2]) is null)))
            {
                var span = call.Arguments[0];
                if (span is MethodCallExpression { Method.Name: "op_Implicit" } implicitSpan && implicitSpan.Arguments.Count == 1)
                    span = implicitSpan.Arguments[0];
                if (IsEvaluatable(span))
                    return ConvertIn(span, call.Arguments[1]);
            }

            if (declaring == typeof(Math) || declaring == typeof(MathF))
                return ConvertMathCall(call);

            if (declaring == typeof(System.Convert))
                return ConvertConvertCall(call);

            if (method.Name == "ToString" && call.Object is not null)
                return ConvertToString(call.Object, call.Arguments.Count == 0 ? null : call.Arguments[0]);

            if (declaring == typeof(DateTime) || declaring == typeof(DateTimeOffset) || declaring == typeof(DateOnly))
                return ConvertDateCall(call);

            if (declaring is not null && declaring.IsGenericType && declaring.GetGenericTypeDefinition() == typeof(Nullable<>) && method.Name == "GetValueOrDefault")
            {
                var fallback = call.Arguments.Count == 1 ? Convert(call.Arguments[0]) : Constant(EvaluateDefault(call));
                return new BsonDocument("$ifNull", new BsonArray { Convert(call.Object!), fallback });
            }

            //a collection's own Contains, such as a List<T>, with the collection given by the caller
            if (method.Name == "Contains" && call.Object is not null && call.Arguments.Count == 1 && IsEvaluatable(call.Object) && call.Object.Type != typeof(string))
                return ConvertIn(call.Object, call.Arguments[0]);

            if (method.Name == "Equals" && call.Object is not null && call.Arguments.Count == 1)
                return ConvertComparison(Expression.Equal(call.Object, call.Arguments[0].Type == call.Object.Type ? call.Arguments[0] : Expression.Convert(call.Arguments[0], call.Object.Type)));
            if (method.Name == "Equals" && call.Object is null && call.Arguments.Count == 2 && method.IsStatic)
                return ConvertComparison(Expression.Equal(call.Arguments[0], call.Arguments[1]));

            if (method.Name == "CompareTo" && call.Object is not null && call.Arguments.Count == 1)
                return new BsonDocument("$cmp", new BsonArray { Convert(call.Object), Convert(call.Arguments[0]) });

            if (method.Name == "ToString" && call.Object is not null)
                return ConvertToString(call.Object, call.Arguments.Count == 0 ? null : call.Arguments[0]);

            //the parse of text a store holds or computes
            if (method.Name == "Parse" && call.Object is null && call.Arguments.Count == 1 && call.Arguments[0].Type == typeof(string))
            {
                var text = Convert(call.Arguments[0]);
                if (declaring == typeof(int) || declaring == typeof(short) || declaring == typeof(byte))
                    return new BsonDocument("$toInt", text);
                if (declaring == typeof(long))
                    return new BsonDocument("$toLong", text);
                if (declaring == typeof(decimal))
                    return new BsonDocument("$toDecimal", text);
                if (declaring == typeof(double) || declaring == typeof(float))
                    return new BsonDocument("$toDouble", text);
                if (declaring == typeof(Guid))
                    return new BsonDocument("$convert", new BsonDocument { { "input", text }, { "to", new BsonDocument { { "type", "binData" }, { "subtype", 4 } } }, { "format", "uuid" } });
                throw new NotSupportedException($"{declaring?.Name}.Parse isn't supported by MongoDB");
            }

            if ((declaring == typeof(TimeOnly) || declaring == typeof(TimeSpan)) && call.Object is not null)
                return ConvertTicksCall(call);

            //HasFlag is whether the value has all of the flags
            if (method.Name == "HasFlag" && call.Object is not null && call.Arguments.Count == 1)
            {
                var flags = NumberValue(call.Arguments[0]);
                return new BsonDocument("$eq", new BsonArray { new BsonDocument("$bitAnd", new BsonArray { NumberValue(call.Object), flags }), flags });
            }

            throw new NotSupportedException($"{declaring?.Name}.{method.Name} isn't supported by MongoDB");
        }

        //a query a condition looks in is read once by a $lookup of its own pipeline, its values compared as an array
        private BsonValue ConvertSubquery(LinqSubquery subquery, Expression value)
        {
            if (value is UnaryExpression { NodeType: ExpressionType.Convert } boxed)
                value = boxed.Operand;
            var query = subquery.Query;
            var statement = ConvertLinq(query, ModelAnalyzer.GetModel(query.ModelType));
            if (statement.Pieces is not null)
                throw new NotSupportedException("A query a condition looks in selects values of its own row, not a relation");
            var name = "__s" + (subqueries++).ToString(CultureInfo.InvariantCulture);
            pendingLookups.Add(new BsonDocument("$lookup", new BsonDocument
            {
                { "from", statement.Collection },
                { "pipeline", new BsonArray(statement.Stages) },
                { "as", name }
            }));
            var shape = statement.Plan.Shape!;
            if (value is MemberInitExpression keyRow && keyRow.Type == typeof(LinqKeyRow))
            {
                //a key of several members is there when a row of the query has each of them
                var matches = new BsonArray();
                for (var i = 0; i < keyRow.Bindings.Count; i++)
                {
                    var part = StripConvert(((MemberAssignment)keyRow.Bindings[i]).Expression);
                    matches.Add(new BsonDocument("$eq", new BsonArray { "$$r.c" + shape.Values![i].Column!.Index.ToString(CultureInfo.InvariantCulture), Convert(part) }));
                }
                return new BsonDocument("$anyElementTrue", new BsonArray { new BsonDocument("$map", new BsonDocument { { "input", "$" + name }, { "as", "r" }, { "in", new BsonDocument("$and", matches) } }) });
            }
            if (shape.Kind != LinqShapeKind.Column)
                throw new NotSupportedException("A query a condition looks in selects one value");
            //a null the query gives is never looked for, as it isn't in memory
            var values = new BsonDocument("$filter", new BsonDocument
            {
                { "input", new BsonDocument("$map", new BsonDocument { { "input", "$" + name }, { "as", "r" }, { "in", "$$r.c" + shape.Column!.Index.ToString(CultureInfo.InvariantCulture) } }) },
                { "cond", new BsonDocument("$gt", new BsonArray { "$$this", BsonNull.Value }) }
            });
            return new BsonDocument("$in", new BsonArray { Convert(StripConvert(value)), values });
        }

        private BsonValue ConvertStringCall(MethodCallExpression call)
        {
            var name = call.Method.Name;
            if (call.Object is null)
            {
                switch (name)
                {
                    case "IsNullOrEmpty":
                        {
                            var v = NewVariable();
                            return Let(v, Convert(call.Arguments[0]), new BsonDocument("$or", new BsonArray
                            {
                                new BsonDocument("$lte", new BsonArray { "$$" + v, BsonNull.Value }),
                                new BsonDocument("$eq", new BsonArray { "$$" + v, String.Empty })
                            }));
                        }
                    case "IsNullOrWhiteSpace":
                        {
                            var v = NewVariable();
                            return Let(v, Convert(call.Arguments[0]), new BsonDocument("$or", new BsonArray
                            {
                                new BsonDocument("$lte", new BsonArray { "$$" + v, BsonNull.Value }),
                                new BsonDocument("$eq", new BsonArray { new BsonDocument("$trim", new BsonDocument("input", "$$" + v)), String.Empty })
                            }));
                        }
                    case "Concat":
                        {
                            if (call.Arguments.Count == 1 && call.Arguments[0] is NewArrayExpression array)
                                return Concat(array.Expressions);
                            return Concat(call.Arguments);
                        }
                    case "Join":
                        return ConvertJoin(call);
                    case "Compare":
                        if (call.Arguments.Count != 2)
                            throw new NotSupportedException("string.Compare with options isn't supported by MongoDB");
                        return new BsonDocument("$cmp", new BsonArray { Convert(call.Arguments[0]), Convert(call.Arguments[1]) });
                    case "Equals":
                        if (call.Arguments.Count == 3 && IgnoresCase(call.Arguments[2]))
                            return new BsonDocument("$eq", new BsonArray { NullOr(Convert(call.Arguments[0]), v => new BsonDocument("$toLower", v)), NullOr(Convert(call.Arguments[1]), v => new BsonDocument("$toLower", v)) });
                        return ConvertComparison(Expression.Equal(call.Arguments[0], call.Arguments[1]));
                    default:
                        throw new NotSupportedException($"string.{name} isn't supported by MongoDB");
                }
            }

            //an ordinal comparison is the default, one ignoring case compares the texts in lower case
            var ignoreCase = call.Arguments.Count > 0 && call.Arguments[^1].Type == typeof(StringComparison) && IgnoresCase(call.Arguments[^1]);
            var instance = Convert(call.Object);
            if (ignoreCase)
                instance = new BsonDocument("$toLower", instance);
            BsonValue Argument(int index)
            {
                var value = call.Arguments[index].Type == typeof(char) ? CharText(call.Arguments[index]) : Convert(call.Arguments[index]);
                return ignoreCase ? new BsonDocument("$toLower", value) : value;
            }
            switch (name)
            {
                case "Contains":
                    return new BsonDocument("$gte", new BsonArray { new BsonDocument("$indexOfCP", new BsonArray { instance, Argument(0) }), 0 });
                case "StartsWith":
                    return new BsonDocument("$eq", new BsonArray { new BsonDocument("$indexOfCP", new BsonArray { instance, Argument(0) }), 0 });
                case "EndsWith":
                    {
                        var s = NewVariable();
                        var e = NewVariable();
                        var text = "$$" + s;
                        var end = "$$" + e;
                        var length = new BsonDocument("$strLenCP", text);
                        var endLength = new BsonDocument("$strLenCP", end);
                        return new BsonDocument("$let", new BsonDocument
                        {
                            { "vars", new BsonDocument { { s, instance }, { e, Argument(0) } } },
                            { "in", new BsonDocument("$and", new BsonArray
                                {
                                    IsString(text),
                                    IsString(end),
                                    new BsonDocument("$gte", new BsonArray { length, endLength }),
                                    new BsonDocument("$eq", new BsonArray { new BsonDocument("$substrCP", new BsonArray { text, new BsonDocument("$subtract", new BsonArray { length, endLength }), endLength }), end })
                                })
                            }
                        });
                    }
                case "ToUpper":
                case "ToUpperInvariant":
                    return NullOr(instance, v => new BsonDocument("$toUpper", v));
                case "ToLower":
                case "ToLowerInvariant":
                    return NullOr(instance, v => new BsonDocument("$toLower", v));
                case "Trim":
                    if (call.Arguments.Count > 0)
                        throw new NotSupportedException("Trim of given characters isn't supported by MongoDB yet");
                    return NullOr(instance, v => new BsonDocument("$trim", new BsonDocument("input", v)));
                case "TrimStart":
                    if (call.Arguments.Count > 0)
                        throw new NotSupportedException("TrimStart of given characters isn't supported by MongoDB yet");
                    return NullOr(instance, v => new BsonDocument("$ltrim", new BsonDocument("input", v)));
                case "TrimEnd":
                    if (call.Arguments.Count > 0)
                        throw new NotSupportedException("TrimEnd of given characters isn't supported by MongoDB yet");
                    return NullOr(instance, v => new BsonDocument("$rtrim", new BsonDocument("input", v)));
                case "Substring":
                    {
                        var start = Convert(call.Arguments[0]);
                        BsonValue length = call.Arguments.Count > 1 ? Convert(call.Arguments[1]) : Int32.MaxValue;
                        return NullOr(instance, v => new BsonDocument("$substrCP", new BsonArray { v, start, length }));
                    }
                case "Replace":
                    if (call.Arguments[0].Type == typeof(char))
                        return NullOr(instance, v => new BsonDocument("$replaceAll", new BsonDocument { { "input", v }, { "find", CharText(call.Arguments[0]) }, { "replacement", CharText(call.Arguments[1]) } }));
                    return NullOr(instance, v => new BsonDocument("$replaceAll", new BsonDocument { { "input", v }, { "find", Convert(call.Arguments[0]) }, { "replacement", new BsonDocument("$ifNull", new BsonArray { Convert(call.Arguments[1]), String.Empty }) } }));
                case "PadLeft":
                case "PadRight":
                    {
                        var padding = call.Arguments.Count > 1 ? CharText(call.Arguments[1]) : new BsonDocument("$literal", " ");
                        return NullOr(instance, v => Pad(v, Convert(call.Arguments[0]), padding, name == "PadLeft"));
                    }
                case "IndexOf":
                    if (call.Arguments.Count != 1 && !(call.Arguments.Count == 2 && call.Arguments[1].Type == typeof(StringComparison)))
                        throw new NotSupportedException("IndexOf from a position isn't supported by MongoDB yet");
                    return new BsonDocument("$indexOfCP", new BsonArray { instance, Argument(0) });
                case "Equals":
                    if (ignoreCase)
                        return new BsonDocument("$eq", new BsonArray { instance, Argument(0) });
                    return ConvertComparison(Expression.Equal(call.Object!, call.Arguments[0]));
                case "CompareTo":
                    return new BsonDocument("$cmp", new BsonArray { instance, Convert(call.Arguments[0]) });
                case "ToString":
                    return instance;
                default:
                    throw new NotSupportedException($"string.{name} isn't supported by MongoDB");
            }
        }
        //the text of a value as C# writes it with no format, or with a date's custom format or a number's fixed or digits format
        private BsonValue ConvertToString(Expression value, Expression? formatExpression)
        {
            var type = Underlying(value.Type);
            var nullable = Nullable.GetUnderlyingType(value.Type) is not null;
            string? format = null;
            if (formatExpression is not null)
            {
                if (formatExpression.Type != typeof(string) || !IsEvaluatable(formatExpression))
                    throw new NotSupportedException("ToString takes a format MongoDB knows when the query is written");
                format = (string?)Evaluate(formatExpression);
            }
            var bare = StripConvert(value);
            BsonValue text;
            if (type.IsEnum)
            {
                if (format is not null)
                    throw new NotSupportedException("ToString of an enum with a format isn't supported by MongoDB");
                //C# gives the field's name, where a stored enum holds the name from EnumName; flags are the text EnumName writes, as in memory
                if (StoredEnum(bare) is not null)
                    text = IsFlagsEnum(type) ? Convert(bare) : FieldNameOfStored(Convert(bare), type);
                else if (IsFlagsEnum(type))
                    throw new NotSupportedException("ToString of a flags enum computed from a number isn't supported by MongoDB");
                else
                    text = FieldNameOfNumber(Convert(value), type);
            }
            else if (type == typeof(string))
            {
                text = Convert(value);
            }
            else if (type == typeof(bool))
            {
                if (format is not null)
                    throw new NotSupportedException("ToString of a bool with a format isn't supported by MongoDB");
                var v = NewVariable();
                text = Let(v, Convert(value), new BsonDocument("$cond", new BsonArray { new BsonDocument("$lte", new BsonArray { "$$" + v, BsonNull.Value }), BsonNull.Value, new BsonDocument("$cond", new BsonArray { "$$" + v, "True", "False" }) }));
            }
            else if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly))
            {
                var dateFormat = format is null ? null : DateFormat(format);
                if (dateFormat is null)
                    throw new NotSupportedException("A date's text is written with a custom format of yyyy, MM, dd, HH, mm, ss, and fff on MongoDB, such as ToString(\"yyyy-MM-dd\")");
                text = new BsonDocument("$dateToString", new BsonDocument { { "date", Convert(value) }, { "format", dateFormat } });
            }
            else if (type == typeof(Guid) || type == typeof(char) || IsInteger(type) || type == typeof(decimal) || type == typeof(double) || type == typeof(float))
            {
                if (format is null)
                {
                    text = new BsonDocument("$toString", Convert(value));
                }
                else if (format.Length > 1 && (format[0] == 'D' || format[0] == 'd') && IsInteger(type) && Int32.TryParse(format.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var width))
                {
                    //digits are padded with zeros after the sign
                    var v = NewVariable();
                    var number = "$$" + v;
                    var digits = Pad(new BsonDocument("$toString", new BsonDocument("$abs", number)), width, new BsonDocument("$literal", "0"), true);
                    text = Let(v, Convert(value), new BsonDocument("$cond", new BsonArray { new BsonDocument("$lte", new BsonArray { number, BsonNull.Value }), BsonNull.Value, new BsonDocument("$concat", new BsonArray { new BsonDocument("$cond", new BsonArray { new BsonDocument("$lt", new BsonArray { number, 0 }), "-", String.Empty }), digits }) }));
                }
                else if (format.Length > 1 && (format[0] == 'F' || format[0] == 'f') && Int32.TryParse(format.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var places) && !IsInteger(type))
                {
                    //fixed rounds a midpoint away from zero, then a decimal $round keeps the places
                    var rounded = RoundAwayFromZero(new BsonDocument("$toDecimal", Convert(value)), places);
                    text = new BsonDocument("$toString", new BsonDocument("$round", new BsonArray { rounded, places }));
                }
                else
                {
                    throw new NotSupportedException($"ToString(\"{format}\") isn't supported by MongoDB, numbers take Dn for an integer or Fn");
                }
            }
            else
            {
                throw new NotSupportedException($"ToString of {type.Name} isn't supported by MongoDB");
            }
            //the text of a nullable with no value is empty
            return nullable ? new BsonDocument("$ifNull", new BsonArray { text, String.Empty }) : text;
        }
        private static string? DateFormat(string format)
        {
            if (format.Length < 2)
                return null;
            var result = new System.Text.StringBuilder();
            var i = 0;
            while (i < format.Length)
            {
                var c = format[i];
                var run = 1;
                while (i + run < format.Length && format[i + run] == c)
                    run++;
                string? part = (c, run) switch
                {
                    ('y', 4) => "%Y",
                    ('M', 2) => "%m",
                    ('d', 2) => "%d",
                    ('H', 2) => "%H",
                    ('m', 2) => "%M",
                    ('s', 2) => "%S",
                    ('f', 3) => "%L",
                    _ => null,
                };
                if (part is null)
                {
                    if (Char.IsLetter(c) || c == '\'' || c == '"' || c == '\\')
                        return null;
                    part = new string(c == '%' ? '%' : c, c == '%' ? run * 2 : run);
                }
                _ = result.Append(part);
                i += run;
            }
            return result.ToString();
        }
        //the name EnumName stores for the number of an enum
        private static BsonValue StoredName(BsonValue number, Type enumType)
        {
            if (IsFlagsEnum(enumType))
                throw new NotSupportedException("Setting a flags enum from a computed number isn't supported by MongoDB");
            var branches = new BsonArray();
            foreach (var value in Enum.GetValuesAsUnderlyingType(enumType))
                branches.Add(new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { number, ToBson(value) }) }, { "then", new BsonDocument("$literal", EnumName.GetName(enumType, Enum.ToObject(enumType, value))) } });
            if (branches.Count == 0)
                return BsonNull.Value;
            return new BsonDocument("$switch", new BsonDocument { { "branches", branches }, { "default", BsonNull.Value } });
        }
        //the field's name of an enum computed from a number, null for a number the enum doesn't name
        private static BsonValue FieldNameOfNumber(BsonValue number, Type enumType)
        {
            var names = Enum.GetNames(enumType);
            var values = Enum.GetValuesAsUnderlyingType(enumType);
            var branches = new BsonArray();
            for (var i = 0; i < names.Length; i++)
                branches.Add(new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { number, ToBson(values.GetValue(i)) }) }, { "then", new BsonDocument("$literal", names[i]) } });
            if (branches.Count == 0)
                return BsonNull.Value;
            return new BsonDocument("$switch", new BsonDocument { { "branches", branches }, { "default", BsonNull.Value } });
        }
        //the field's name of a stored enum, from the name EnumName stored
        private static BsonValue FieldNameOfStored(BsonValue text, Type enumType)
        {
            var names = Enum.GetNames(enumType);
            var values = Enum.GetValuesAsUnderlyingType(enumType);
            var branches = new BsonArray();
            var differs = false;
            for (var i = 0; i < names.Length; i++)
            {
                var stored = EnumName.GetName(enumType, values.GetValue(i)!);
                differs |= stored != names[i];
                branches.Add(new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { text, new BsonDocument("$literal", stored) }) }, { "then", new BsonDocument("$literal", names[i]) } });
            }
            if (!differs)
                return text;
            return new BsonDocument("$switch", new BsonDocument { { "branches", branches }, { "default", BsonNull.Value } });
        }
        //text padded to a width with a character repeated
        private BsonValue Pad(BsonValue text, BsonValue width, BsonValue padding, bool left)
        {
            var t = NewVariable();
            var n = NewVariable();
            var repeated = new BsonDocument("$reduce", new BsonDocument
            {
                { "input", new BsonDocument("$range", new BsonArray { 0, "$$" + n }) },
                { "initialValue", String.Empty },
                { "in", new BsonDocument("$concat", new BsonArray { "$$value", padding }) }
            });
            var padded = new BsonDocument("$concat", left ? new BsonArray { repeated, "$$" + t } : new BsonArray { "$$" + t, repeated });
            return Let(t, text, Let(n, new BsonDocument("$subtract", new BsonArray { width, new BsonDocument("$strLenCP", "$$" + t) }),
                new BsonDocument("$cond", new BsonArray { new BsonDocument("$lte", new BsonArray { "$$" + n, 0 }), "$$" + t, padded })));
        }
        //string.Join of values given or of a relation's rows, a null joined as no text
        private BsonValue ConvertJoin(MethodCallExpression call)
        {
            if (call.Arguments.Count != 2 || (call.Arguments[0].Type != typeof(string) && call.Arguments[0].Type != typeof(char)))
                throw new NotSupportedException("string.Join of a separator and values is supported by MongoDB");
            var separator = CharText(call.Arguments[0]);
            var values = call.Arguments[1];
            BsonValue array;
            if (values is NewArrayExpression newArray)
            {
                var items = new BsonArray();
                foreach (var item in newArray.Expressions)
                    items.Add(Underlying(item.Type) == typeof(string) ? Convert(item) : ConvertToString(item, null));
                array = items;
            }
            else
            {
                array = ConvertCollection(values).Input;
            }
            var s = NewVariable();
            var joined = new BsonDocument("$reduce", new BsonDocument
            {
                { "input", array },
                { "initialValue", BsonNull.Value },
                { "in", new BsonDocument("$cond", new BsonArray
                    {
                        new BsonDocument("$eq", new BsonArray { "$$value", BsonNull.Value }),
                        new BsonDocument("$ifNull", new BsonArray { "$$this", String.Empty }),
                        new BsonDocument("$concat", new BsonArray { "$$value", "$$" + s, new BsonDocument("$ifNull", new BsonArray { "$$this", String.Empty }) })
                    })
                }
            });
            return Let(s, new BsonDocument("$ifNull", new BsonArray { separator, String.Empty }), new BsonDocument("$ifNull", new BsonArray { joined, String.Empty }));
        }

        //a TimeOnly wraps round midnight, a TimeSpan doesn't
        private BsonValue ConvertTicksCall(MethodCallExpression call)
        {
            var ticks = Convert(call.Object!);
            var time = call.Method.DeclaringType == typeof(TimeOnly);
            BsonValue Per(long perUnit) => new BsonDocument("$toLong", new BsonDocument("$multiply", new BsonArray { Convert(call.Arguments[0]), perUnit }));
            BsonValue result = call.Method.Name switch
            {
                "AddHours" when time => new BsonDocument("$add", new BsonArray { ticks, Per(TimeSpan.TicksPerHour) }),
                "AddMinutes" when time => new BsonDocument("$add", new BsonArray { ticks, Per(TimeSpan.TicksPerMinute) }),
                "Add" => new BsonDocument("$add", new BsonArray { ticks, Convert(call.Arguments[0]) }),
                "Subtract" when !time => new BsonDocument("$subtract", new BsonArray { ticks, Convert(call.Arguments[0]) }),
                _ => throw new NotSupportedException($"{call.Method.DeclaringType!.Name}.{call.Method.Name} isn't supported by MongoDB"),
            };
            return time ? WrapDay(result) : result;
        }
        private static BsonValue WrapDay(BsonValue ticks)
            => new BsonDocument("$mod", new BsonArray { new BsonDocument("$add", new BsonArray { new BsonDocument("$mod", new BsonArray { ticks, TimeSpan.TicksPerDay }), TimeSpan.TicksPerDay }), TimeSpan.TicksPerDay });

        private static bool IgnoresCase(Expression comparison)
            => (StringComparison)Evaluate(comparison)! is StringComparison.OrdinalIgnoreCase or StringComparison.CurrentCultureIgnoreCase or StringComparison.InvariantCultureIgnoreCase;
        private BsonValue CharText(Expression exp)
        {
            if (IsEvaluatable(exp))
                return Constant(Evaluate(exp)?.ToString());
            return Convert(exp);
        }
        private BsonValue Concat(IReadOnlyList<Expression> parts)
        {
            //null is no text when concatenated in C#
            var array = new BsonArray();
            foreach (var part in parts)
            {
                var value = Underlying(part.Type) == typeof(string) || Underlying(part.Type) == typeof(char) ? Convert(part) : new BsonDocument("$toString", Convert(StripConvert(part)));
                array.Add(new BsonDocument("$ifNull", new BsonArray { value, String.Empty }));
            }
            return new BsonDocument("$concat", array);
        }

        private BsonValue ConvertEnumerableCall(MethodCallExpression call)
        {
            var name = call.Method.Name;
            if (name == "Contains" && call.Arguments.Count == 2 && IsEvaluatable(call.Arguments[0]))
                return ConvertIn(call.Arguments[0], call.Arguments[1]);

            var (input, element) = ConvertCollection(call.Arguments[0]);
            LambdaExpression? lambda = call.Arguments.Count > 1 && call.Arguments[1].Type != typeof(int) && call.Arguments[1].Type != typeof(Index) ? Lambda(call.Arguments[1]) : null;
            switch (name)
            {
                case "First":
                case "FirstOrDefault":
                case "Single":
                case "SingleOrDefault":
                    return new BsonDocument("$first", lambda is null ? input : Filter(input, element, lambda));
                case "Last":
                case "LastOrDefault":
                    return new BsonDocument("$last", lambda is null ? input : Filter(input, element, lambda));
                case "ElementAt":
                case "ElementAtOrDefault":
                    if (call.Arguments[1].Type != typeof(int))
                        throw new NotSupportedException($"{name} by an Index isn't supported by MongoDB");
                    return new BsonDocument("$arrayElemAt", new BsonArray { input, Convert(call.Arguments[1]) });
                case "Any":
                    if (lambda is null)
                        return new BsonDocument("$gt", new BsonArray { Size(input), 0 });
                    return new BsonDocument("$gt", new BsonArray { Size(Filter(input, element, lambda)), 0 });
                case "All":
                    return new BsonDocument("$allElementsTrue", new BsonArray { Map(input, element, lambda!) });
                case "Count":
                case "LongCount":
                    if (lambda is null)
                        return Size(input);
                    return Size(Filter(input, element, lambda));
                case "Sum":
                    return new BsonDocument("$sum", lambda is null ? input : Map(input, element, lambda));
                case "Average":
                    return new BsonDocument("$avg", lambda is null ? input : Map(input, element, lambda));
                case "Min":
                    return new BsonDocument("$min", lambda is null ? input : Map(input, element, lambda));
                case "Max":
                    return new BsonDocument("$max", lambda is null ? input : Map(input, element, lambda));
                case "Contains":
                    return new BsonDocument("$in", new BsonArray { Convert(call.Arguments[1]), input });
                default:
                    throw new NotSupportedException($"{name} of a relation isn't supported by MongoDB yet");
            }
        }
        //an array of related documents, or of values selected from them, with what an element of it is
        private (BsonValue Input, Element Element) ConvertCollection(Expression source)
        {
            if (source is MethodCallExpression ordered && ordered.Method.DeclaringType == typeof(Enumerable) && ordered.Arguments.Count == 2 && ordered.Method.Name is "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending")
            {
                //the keys of the whole order, the ThenBy calls being outside their OrderBy
                var orders = new List<(LambdaExpression Key, bool Descending)>();
                Expression below = ordered;
                while (below is MethodCallExpression call && call.Method.DeclaringType == typeof(Enumerable) && call.Arguments.Count == 2 && call.Method.Name is "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending")
                {
                    orders.Insert(0, (Lambda(call.Arguments[1]), call.Method.Name.EndsWith("Descending", StringComparison.Ordinal)));
                    below = call.Arguments[0];
                    if (call.Method.Name is "OrderBy" or "OrderByDescending")
                        break;
                }
                var (unordered, element) = ConvertCollection(below);
                //each element is paired with its keys, sorted by them, and taken back out
                var name = NewVariable();
                var pair = new BsonDocument("e", "$$" + name);
                var sortBy = new BsonDocument();
                for (var i = 0; i < orders.Count; i++)
                {
                    scopes[orders[i].Key.Parameters[0]] = ElementScope(name, element);
                    pair["k" + i.ToString(CultureInfo.InvariantCulture)] = SortValue(orders[i].Key.Body);
                    sortBy["k" + i.ToString(CultureInfo.InvariantCulture)] = orders[i].Descending ? -1 : 1;
                }
                var paired = new BsonDocument("$map", new BsonDocument { { "input", unordered }, { "as", name }, { "in", pair } });
                var sorted = new BsonDocument("$sortArray", new BsonDocument { { "input", paired }, { "sortBy", sortBy } });
                return (new BsonDocument("$map", new BsonDocument { { "input", sorted }, { "as", "p" }, { "in", "$$p.e" } }), element);
            }
            if (source is MethodCallExpression paging && paging.Method.DeclaringType == typeof(Enumerable) && paging.Arguments.Count == 2 && paging.Method.Name is "Skip" or "Take" && paging.Arguments[1].Type == typeof(int))
            {
                var (all, element) = ConvertCollection(paging.Arguments[0]);
                var count = Convert(paging.Arguments[1]);
                var name = NewVariable();
                var array = "$$" + name;
                var size = new BsonDocument("$size", array);
                BsonValue page = paging.Method.Name == "Skip"
                    ? new BsonDocument("$cond", new BsonArray { new BsonDocument("$gte", new BsonArray { count, size }), new BsonArray(), new BsonDocument("$slice", new BsonArray { array, new BsonDocument("$max", new BsonArray { count, 0 }), size }) })
                    : new BsonDocument("$cond", new BsonArray { new BsonDocument("$lte", new BsonArray { count, 0 }), new BsonArray(), new BsonDocument("$slice", new BsonArray { array, count }) });
                return (Let(name, new BsonDocument("$ifNull", new BsonArray { all, new BsonArray() }), page), element);
            }
            if (source is MethodCallExpression distinct && distinct.Method.DeclaringType == typeof(Enumerable) && distinct.Arguments.Count == 1 && distinct.Method.Name == "Distinct")
            {
                var (all, element) = ConvertCollection(distinct.Arguments[0]);
                return (new BsonDocument("$setUnion", new BsonArray { new BsonDocument("$ifNull", new BsonArray { all, new BsonArray() }), new BsonArray() }), element);
            }
            if (source is MethodCallExpression chain && chain.Method.DeclaringType == typeof(Enumerable) && chain.Arguments.Count == 2)
            {
                var lambda = Lambda(chain.Arguments[1]);
                var (input, element) = ConvertCollection(chain.Arguments[0]);
                switch (chain.Method.Name)
                {
                    case "Where":
                        return (Filter(input, element, lambda), element);
                    case "Select":
                        return (Map(input, element, lambda), default);
                }
                throw new NotSupportedException($"{chain.Method.Name} of a relation isn't supported by MongoDB yet");
            }
            if (source is MemberExpression member)
            {
                var (scope, model, relation) = ResolveModelMember(member);
                if (!relation.IsRelated || !relation.IsEnumerable)
                    throw new NotSupportedException($"{member} isn't a collection MongoDB can read");
                return ("$" + Many(scope, model, relation), new Element(ModelAnalyzer.GetModel(relation.ActualType), null, null));
            }
            //a GroupJoin's group of the elements of the second query
            if (source is ParameterExpression parameter && scopes.TryGetValue(parameter, out var group) && group.Group)
                return (group.Prefix, new Element(null, group.Result, group.Plan));
            throw new NotSupportedException($"{source} isn't a collection MongoDB can read");
        }
        private BsonValue Filter(BsonValue input, Element element, LambdaExpression lambda)
        {
            var name = NewVariable();
            scopes[lambda.Parameters[0]] = ElementScope(name, element);
            return new BsonDocument("$filter", new BsonDocument { { "input", input }, { "as", name }, { "cond", Convert(lambda.Body) } });
        }
        private BsonValue Map(BsonValue input, Element element, LambdaExpression lambda)
        {
            var name = NewVariable();
            scopes[lambda.Parameters[0]] = ElementScope(name, element);
            return new BsonDocument("$map", new BsonDocument { { "input", input }, { "as", name }, { "in", Convert(lambda.Body) } });
        }
        private static BsonValue Size(BsonValue input) => new BsonDocument("$size", new BsonDocument("$ifNull", new BsonArray { input, new BsonArray() }));

        //a value in a collection the caller gives, an enum by its stored name
        private BsonValue ConvertIn(Expression collection, Expression value)
        {
            var bareValue = StripConvert(value);
            if (bareValue is MemberInitExpression keyRow && keyRow.Type == typeof(LinqKeyRow))
            {
                //a key of several members matches a key when each of them does
                var parts = new (BsonValue Value, Type? Stored)[keyRow.Bindings.Count];
                for (var i = 0; i < parts.Length; i++)
                {
                    var part = StripConvert(((MemberAssignment)keyRow.Bindings[i]).Expression);
                    parts[i] = (Convert(part), StoredEnum(part));
                }
                var keys = new BsonArray();
                if (Evaluate(collection) is IEnumerable rows)
                {
                    foreach (var row in rows)
                    {
                        if (row is not LinqKeyRow key)
                            continue;
                        var matches = new BsonArray();
                        for (var i = 0; i < parts.Length; i++)
                            matches.Add(new BsonDocument("$eq", new BsonArray { parts[i].Value, parts[i].Stored is not null ? EnumConstant(key[i], parts[i].Stored!) : Constant(key[i]) }));
                        keys.Add(new BsonDocument("$and", matches));
                    }
                }
                return keys.Count == 0 ? false : new BsonDocument("$or", keys);
            }
            var stored = StoredEnum(bareValue);
            var items = Evaluate(collection);
            if (stored is not null && items is not null)
            {
                var names = new BsonArray();
                foreach (var item in (IEnumerable)items)
                {
                    if (item is null)
                    {
                        names.Add(BsonNull.Value);
                        continue;
                    }
                    if (!TryEnumItem(item, Underlying(item.GetType()), stored, out var enumItem))
                    {
                        names = null;
                        break;
                    }
                    names.Add(EnumItemText(enumItem, stored));
                }
                if (names is not null)
                    return new BsonDocument("$in", new BsonArray { Convert(bareValue), new BsonDocument("$literal", names) });
            }
            var values = new BsonArray();
            if (items is not null)
            {
                foreach (var item in (IEnumerable)items)
                    values.Add(item is Enum ? ToBson(System.Convert.ChangeType(item, Enum.GetUnderlyingType(item.GetType()), CultureInfo.InvariantCulture)) : ToBson(item));
            }
            var converted = stored is not null ? EnumNumber(Convert(bareValue), stored) : Convert(value);
            return new BsonDocument("$in", new BsonArray { converted, new BsonDocument("$literal", values) });
        }

        private BsonValue ConvertMathCall(MethodCallExpression call)
        {
            var args = call.Arguments;
            switch (call.Method.Name)
            {
                case "Abs": return new BsonDocument("$abs", Convert(args[0]));
                case "Ceiling": return new BsonDocument("$ceil", Convert(args[0]));
                case "Floor": return new BsonDocument("$floor", Convert(args[0]));
                case "Truncate": return new BsonDocument("$trunc", Convert(args[0]));
                case "Sqrt": return new BsonDocument("$sqrt", Convert(args[0]));
                case "Exp": return new BsonDocument("$exp", Convert(args[0]));
                case "Log10": return new BsonDocument("$log10", Convert(args[0]));
                case "Log":
                    if (args.Count == 2)
                        return new BsonDocument("$log", new BsonArray { Convert(args[0]), Convert(args[1]) });
                    return new BsonDocument("$ln", Convert(args[0]));
                case "Pow": return new BsonDocument("$pow", new BsonArray { Convert(args[0]), Convert(args[1]) });
                case "Min": return new BsonDocument("$min", new BsonArray { Convert(args[0]), Convert(args[1]) });
                case "Max": return new BsonDocument("$max", new BsonArray { Convert(args[0]), Convert(args[1]) });
                case "Sign":
                    {
                        var v = NewVariable();
                        return Let(v, Convert(args[0]), new BsonDocument("$cmp", new BsonArray { "$$" + v, 0 }));
                    }
                case "Round":
                    {
                        BsonValue digits = 0;
                        var mode = MidpointRounding.ToEven;
                        for (var i = 1; i < args.Count; i++)
                        {
                            if (args[i].Type == typeof(MidpointRounding))
                                mode = (MidpointRounding)Evaluate(args[i])!;
                            else
                                digits = Convert(args[i]);
                        }
                        var value = Convert(args[0]);
                        switch (mode)
                        {
                            case MidpointRounding.ToEven:
                                //$round rounds a midpoint to even, as C# does by default
                                return new BsonDocument("$round", new BsonArray { value, digits });
                            case MidpointRounding.ToZero:
                                return new BsonDocument("$trunc", new BsonArray { value, digits });
                            case MidpointRounding.AwayFromZero:
                                return RoundAwayFromZero(value, digits);
                            case MidpointRounding.ToNegativeInfinity:
                            case MidpointRounding.ToPositiveInfinity:
                                {
                                    var scale = PowerOfTen(digits);
                                    var scaled = new BsonDocument(mode == MidpointRounding.ToNegativeInfinity ? "$floor" : "$ceil", new BsonDocument("$multiply", new BsonArray { value, scale }));
                                    return new BsonDocument("$divide", new BsonArray { scaled, scale });
                                }
                            default:
                                throw new NotSupportedException($"Math.Round with {mode} isn't supported by MongoDB yet");
                        }
                    }
                default:
                    throw new NotSupportedException($"Math.{call.Method.Name} isn't supported by MongoDB");
            }
        }

        //$pow of a decimal ten to 0 gives 0 on MongoDB 8, so the scale of a number of places is read from a list of them
        private static readonly BsonArray powersOfTen = PowersOfTen();
        private static BsonArray PowersOfTen()
        {
            var powers = new BsonArray();
            var power = 1m;
            for (var i = 0; i <= 28; i++)
            {
                powers.Add(new BsonDecimal128(new Decimal128(power)));
                if (i < 28)
                    power *= 10m;
            }
            return powers;
        }
        private static BsonValue PowerOfTen(BsonValue digits) => new BsonDocument("$arrayElemAt", new BsonArray { new BsonDocument("$literal", powersOfTen), digits });

        private BsonValue RoundAwayFromZero(BsonValue value, BsonValue digits)
        {
            var v = NewVariable();
            var scale = PowerOfTen(digits);
            var abs = new BsonDocument("$floor", new BsonDocument("$add", new BsonArray { new BsonDocument("$multiply", new BsonArray { new BsonDocument("$abs", "$$" + v), scale }), new BsonDecimal128(new Decimal128(0.5m)) }));
            return Let(v, value, new BsonDocument("$multiply", new BsonArray { new BsonDocument("$cmp", new BsonArray { "$$" + v, 0 }), new BsonDocument("$divide", new BsonArray { abs, scale }) }));
        }

        private BsonValue ConvertConvertCall(MethodCallExpression call)
        {
            var value = call.Arguments[0];
            var from = Underlying(StripConvert(value).Type);
            //Convert rounds a fraction to even where a cast cuts it
            BsonValue Rounded() => IsInteger(from) ? Convert(value) : new BsonDocument("$round", new BsonArray { Convert(value), 0 });
            return call.Method.Name switch
            {
                "ToInt16" or "ToInt32" or "ToByte" or "ToSByte" or "ToUInt16" => new BsonDocument("$toInt", Rounded()),
                "ToInt64" or "ToUInt32" or "ToUInt64" => new BsonDocument("$toLong", Rounded()),
                "ToDouble" or "ToSingle" => new BsonDocument("$toDouble", Convert(value)),
                "ToDecimal" => new BsonDocument("$toDecimal", Convert(value)),
                "ToString" => call.Arguments.Count == 1 ? ConvertToString(StripConvert(value), null) : throw new NotSupportedException("Convert.ToString with a format provider isn't supported by MongoDB"),
                "ToBoolean" => from == typeof(string) ? throw new NotSupportedException("Convert.ToBoolean of text isn't supported by MongoDB") : new BsonDocument("$ne", new BsonArray { Convert(value), 0 }),
                _ => throw new NotSupportedException($"Convert.{call.Method.Name} isn't supported by MongoDB"),
            };
        }

        private BsonValue ConvertDateCall(MethodCallExpression call)
        {
            if (call.Object is null)
                throw new NotSupportedException($"{call.Method.DeclaringType!.Name}.{call.Method.Name} isn't supported by MongoDB");
            var date = Convert(call.Object);
            BsonValue Milliseconds(double perUnit) => new BsonDocument("$add", new BsonArray { date, new BsonDocument("$multiply", new BsonArray { Convert(call.Arguments[0]), perUnit }) });
            BsonValue DateAdd(string unit) => new BsonDocument("$dateAdd", new BsonDocument { { "startDate", date }, { "unit", unit }, { "amount", new BsonDocument("$toLong", Convert(call.Arguments[0])) } });
            switch (call.Method.Name)
            {
                case "AddYears": return DateAdd("year");
                case "AddMonths": return DateAdd("month");
                case "AddDays": return Milliseconds(86400000d);
                case "AddHours": return Milliseconds(3600000d);
                case "AddMinutes": return Milliseconds(60000d);
                case "AddSeconds": return Milliseconds(1000d);
                case "AddMilliseconds": return Milliseconds(1d);
                case "AddTicks": return new BsonDocument("$add", new BsonArray { date, TicksToMilliseconds(Convert(call.Arguments[0])) });
                case "Add": return new BsonDocument("$add", new BsonArray { date, TicksToMilliseconds(Convert(call.Arguments[0])) });
                case "Subtract":
                    if (IsDate(call.Arguments[0].Type))
                        return new BsonDocument("$multiply", new BsonArray { new BsonDocument("$subtract", new BsonArray { date, Convert(call.Arguments[0]) }), 10000L });
                    return new BsonDocument("$subtract", new BsonArray { date, TicksToMilliseconds(Convert(call.Arguments[0])) });
                default:
                    throw new NotSupportedException($"{call.Method.DeclaringType!.Name}.{call.Method.Name} isn't supported by MongoDB");
            }
        }

        private static BsonValue ConvertDateMember(string name, BsonValue date, Type type)
        {
            switch (name)
            {
                case "Year": return new BsonDocument("$year", date);
                case "Month": return new BsonDocument("$month", date);
                case "Day": return new BsonDocument("$dayOfMonth", date);
                case "DayOfYear": return new BsonDocument("$dayOfYear", date);
                //$dayOfWeek counts from 1 for Sunday, DayOfWeek from 0
                case "DayOfWeek": return new BsonDocument("$subtract", new BsonArray { new BsonDocument("$dayOfWeek", date), 1 });
            }
            if (type == typeof(DateOnly))
                throw new NotSupportedException($"DateOnly.{name} isn't supported by MongoDB");
            switch (name)
            {
                case "Hour": return new BsonDocument("$hour", date);
                case "Minute": return new BsonDocument("$minute", date);
                case "Second": return new BsonDocument("$second", date);
                case "Millisecond": return new BsonDocument("$millisecond", date);
                case "Date": return new BsonDocument("$dateTrunc", new BsonDocument { { "date", date }, { "unit", "day" } });
                case "UtcDateTime":
                case "DateTime":
                    return date;
                case "TimeOfDay":
                    return new BsonDocument("$multiply", new BsonArray { new BsonDocument("$subtract", new BsonArray { date, new BsonDocument("$dateTrunc", new BsonDocument { { "date", date }, { "unit", "day" } }) }), 10000L });
                case "Ticks":
                    return new BsonDocument("$add", new BsonArray { new BsonDocument("$multiply", new BsonArray { new BsonDocument("$toLong", date), 10000L }), DateTime.UnixEpoch.Ticks });
                default:
                    throw new NotSupportedException($"{type.Name}.{name} isn't supported by MongoDB");
            }
        }

        //a TimeSpan and a TimeOnly are stored as ticks
        private static BsonValue ConvertTicksMember(string name, BsonValue ticks, Type type)
        {
            BsonValue Part(long perUnit, long count) => new BsonDocument("$mod", new BsonArray { new BsonDocument("$trunc", new BsonDocument("$divide", new BsonArray { ticks, perUnit })), count });
            BsonValue Total(double perUnit) => new BsonDocument("$divide", new BsonArray { ticks, perUnit });
            switch (name)
            {
                case "Ticks": return ticks;
                case "Hours":
                case "Hour":
                    return Part(TimeSpan.TicksPerHour, 24);
                case "Minutes":
                case "Minute":
                    return Part(TimeSpan.TicksPerMinute, 60);
                case "Seconds":
                case "Second":
                    return Part(TimeSpan.TicksPerSecond, 60);
                case "Milliseconds":
                case "Millisecond":
                    return Part(TimeSpan.TicksPerMillisecond, 1000);
            }
            if (type == typeof(TimeSpan))
            {
                switch (name)
                {
                    case "Days": return new BsonDocument("$trunc", new BsonDocument("$divide", new BsonArray { ticks, TimeSpan.TicksPerDay }));
                    case "TotalDays": return Total(TimeSpan.TicksPerDay);
                    case "TotalHours": return Total(TimeSpan.TicksPerHour);
                    case "TotalMinutes": return Total(TimeSpan.TicksPerMinute);
                    case "TotalSeconds": return Total(TimeSpan.TicksPerSecond);
                    case "TotalMilliseconds": return Total(TimeSpan.TicksPerMillisecond);
                }
            }
            throw new NotSupportedException($"{type.Name}.{name} isn't supported by MongoDB");
        }
        private static BsonValue TicksToMilliseconds(BsonValue ticks) => new BsonDocument("$divide", new BsonArray { ticks, (double)TimeSpan.TicksPerMillisecond });

        //an enum member of a model is stored as its name
        private Type? StoredEnum(Expression exp)
        {
            var type = Underlying(exp.Type);
            if (!type.IsEnum)
                return null;
            if (ResultColumn(exp) is { } result)
                return ColumnHoldsStoredEnum(result.Column, result.Scope.Plan) ? type : null;
            if (exp is MemberExpression member && member.Expression is not null && !IsEvaluatable(member) && Nullable.GetUnderlyingType(member.Expression.Type) is null)
            {
                if (TryResultMember(member, out _))
                    return null;
                try
                {
                    var (_, _, modelMember) = ResolveModelMember(member);
                    return modelMember.EnumType is not null ? type : null;
                }
                catch (NotSupportedException)
                {
                    return null;
                }
            }
            if (exp is MemberExpression nullable && nullable.Member.Name == "Value" && nullable.Expression is not null && Nullable.GetUnderlyingType(nullable.Expression.Type) is not null)
                return StoredEnum(nullable.Expression);
            if (exp is ConditionalExpression conditional)
                return StoredEnum(StripConvert(conditional.IfTrue)) ?? StoredEnum(StripConvert(conditional.IfFalse));
            if (exp is BinaryExpression { NodeType: ExpressionType.Coalesce } coalesce)
                return StoredEnum(StripConvert(coalesce.Left)) ?? StoredEnum(StripConvert(coalesce.Right));
            return null;
        }
        //a value compared with a stored enum is its name, a number the enum doesn't name matches nothing as in C#, and one beyond the enum's range is compared as a number
        private static bool TryEnumText(Expression exp, Type enumType, [MaybeNullWhen(false)] out BsonValue text)
        {
            text = null;
            if (!IsEvaluatable(exp))
                return false;
            var type = Underlying(exp.Type);
            if (type != enumType && !IsInteger(type))
                return false;
            var value = Evaluate(exp);
            if (value is null)
            {
                text = BsonNull.Value;
                return true;
            }
            if (!TryEnumItem(value, type, enumType, out var item))
                return false;
            text = new BsonDocument("$literal", EnumItemText(item, enumType));
            return true;
        }
        private static bool TryEnumItem(object value, Type type, Type enumType, [MaybeNullWhen(false)] out object item)
        {
            item = value.GetType() == enumType ? value : Enum.ToObject(enumType, value);
            if (type != enumType && System.Convert.ToDecimal(System.Convert.ChangeType(item, Enum.GetUnderlyingType(enumType), CultureInfo.InvariantCulture), CultureInfo.InvariantCulture) != System.Convert.ToDecimal(value, CultureInfo.InvariantCulture))
            {
                item = null;
                return false;
            }
            return true;
        }
        private static string EnumItemText(object item, Type enumType)
            => enumType.IsDefined(typeof(FlagsAttribute), false) || Enum.IsDefined(enumType, item) ? EnumName.GetName(enumType, item) : System.Convert.ToString(System.Convert.ChangeType(item, Enum.GetUnderlyingType(enumType), CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)!;
        private static BsonValue EnumConstant(object? value, Type enumType)
        {
            if (value is null)
                return BsonNull.Value;
            var item = value.GetType() == enumType ? value : Enum.ToObject(enumType, value);
            return new BsonDocument("$literal", EnumItemText(item, enumType));
        }
        //the number of a stored enum, from the name of each of its values
        private static BsonValue EnumNumber(BsonValue text, Type enumType)
        {
            if (IsFlagsEnum(enumType))
                return FlagsNumber(text, enumType);
            var branches = new BsonArray();
            var values = Enum.GetValuesAsUnderlyingType(enumType);
            foreach (var value in values)
            {
                var name = EnumName.GetName(enumType, Enum.ToObject(enumType, value));
                branches.Add(new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { text, new BsonDocument("$literal", name) }) }, { "then", ToBson(value) } });
            }
            if (branches.Count == 0)
                return BsonNull.Value;
            return new BsonDocument("$switch", new BsonDocument { { "branches", branches }, { "default", BsonNull.Value } });
        }
        //flags are stored as EnumName joins the names of the set fields with |, so the number is the OR of each name found among them
        private static BsonValue FlagsNumber(BsonValue text, Type enumType)
        {
            if (Enum.GetUnderlyingType(enumType) == typeof(ulong))
                throw new NotSupportedException("The number of a ulong flags enum isn't supported by MongoDB");
            var names = Enum.GetNames(enumType);
            var values = Enum.GetValuesAsUnderlyingType(enumType);
            var bounded = new BsonDocument("$concat", new BsonArray { "|", "$$t", "|" });
            var parts = new BsonArray();
            for (var i = 0; i < names.Length; i++)
            {
                var value = values.GetValue(i)!;
                var stored = EnumName.GetName(enumType, Enum.ToObject(enumType, value));
                parts.Add(new BsonDocument("$cond", new BsonArray { new BsonDocument("$gte", new BsonArray { new BsonDocument("$indexOfCP", new BsonArray { bounded, new BsonDocument("$literal", "|" + stored + "|") }), 0 }), ToBson(value), 0 }));
            }
            var number = new BsonDocument("$reduce", new BsonDocument { { "input", parts }, { "initialValue", 0 }, { "in", new BsonDocument("$bitOr", new BsonArray { "$$value", "$$this" }) } });
            return Let("t", text, new BsonDocument("$cond", new BsonArray { new BsonDocument("$lte", new BsonArray { "$$t", BsonNull.Value }), BsonNull.Value, number }));
        }
        //a stored enum as its number, anything else as it is
        private BsonValue NumberValue(Expression exp)
        {
            var bare = StripConvert(exp);
            if (IsEvaluatable(bare) && Underlying(bare.Type).IsEnum)
            {
                var value = Evaluate(bare);
                return value is null ? BsonNull.Value : new BsonDocument("$literal", ToBson(System.Convert.ChangeType(value, Enum.GetUnderlyingType(Underlying(bare.Type)), CultureInfo.InvariantCulture)));
            }
            var stored = StoredEnum(bare);
            return stored is not null ? EnumNumber(Convert(bare), stored) : Convert(exp);
        }
        private static bool IsFlagsEnum(Type type)
        {
            var underlying = Underlying(type);
            return underlying.IsEnum && underlying.IsDefined(typeof(FlagsAttribute), false);
        }

        private string NewVariable() => "v" + (variables++).ToString(CultureInfo.InvariantCulture);
        private static BsonValue Let(string name, BsonValue value, BsonValue body) => new BsonDocument("$let", new BsonDocument { { "vars", new BsonDocument(name, value) }, { "in", body } });
        private static BsonValue IsString(BsonValue value) => new BsonDocument("$eq", new BsonArray { new BsonDocument("$type", value), "string" });
        //a string function of null is null, as in C# where it would throw and in SQL where it's NULL
        private BsonValue NullOr(BsonValue value, Func<BsonValue, BsonValue> function)
        {
            var name = NewVariable();
            return Let(name, value, new BsonDocument("$cond", new BsonArray { new BsonDocument("$lte", new BsonArray { "$$" + name, BsonNull.Value }), BsonNull.Value, function("$$" + name) }));
        }

        private static LambdaExpression Lambda(Expression argument)
        {
            if (argument.NodeType == ExpressionType.Quote)
                argument = ((UnaryExpression)argument).Operand;
            return (LambdaExpression)argument;
        }
        private static Expression StripConvert(Expression exp)
        {
            while (exp is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert && convert.Operand.Type != typeof(object))
                exp = convert.Operand;
            return exp;
        }
        private static Type Underlying(Type type) => Nullable.GetUnderlyingType(type) ?? type;
        private static bool CanBeNull(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
        private static bool IsDate(Type type)
        {
            var underlying = Underlying(type);
            return underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset) || underlying == typeof(DateOnly);
        }
        private static bool IsTicks(Type type)
        {
            var underlying = Underlying(type);
            return underlying == typeof(TimeSpan) || underlying == typeof(TimeOnly);
        }
        private static bool IsInteger(Type type)
            => type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong);
        private static int IntegerSize(Type type)
            => type == typeof(long) || type == typeof(ulong) || type == typeof(uint) ? 8 : 4;

        //an expression that doesn't read a parameter is evaluated to its value
        private static bool IsEvaluatable(Expression exp) => !ParameterFinder.Uses(exp);
        private sealed class ParameterFinder : ExpressionVisitor
        {
            private bool found;
            private HashSet<ParameterExpression>? declared;
            public static bool Uses(Expression expression)
            {
                var finder = new ParameterFinder();
                _ = finder.Visit(expression);
                return finder.found;
            }
            public override Expression? Visit(Expression? node)
            {
                if (found)
                    return node;
                return base.Visit(node);
            }
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                //a query a condition looks in is read by the store
                if (node.Object is ConstantExpression { Value: LinqSubquery })
                {
                    found = true;
                    return node;
                }
                return base.VisitMethodCall(node);
            }
            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (declared is null || !declared.Contains(node))
                    found = true;
                return node;
            }
            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                //a lambda's own parameters are its values, not the query's
                declared ??= new();
                foreach (var parameter in node.Parameters)
                    _ = declared.Add(parameter);
                _ = Visit(node.Body);
                return node;
            }
        }

        private static object? Evaluate(Expression exp)
        {
            return exp.NodeType switch
            {
                ExpressionType.Constant => ((ConstantExpression)exp).Value,
                ExpressionType.MemberAccess => EvaluateMemberAccess((MemberExpression)exp),
                ExpressionType.Default => EvaluateDefault(exp),
                ExpressionType.Convert when ((UnaryExpression)exp).Operand.Type.IsEnum && Underlying(exp.Type) != ((UnaryExpression)exp).Operand.Type => EvaluateInvoke(exp),
                ExpressionType.Quote => ((UnaryExpression)exp).Operand,
                _ => EvaluateInvoke(exp),
            };
        }
        [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method.", Justification = "Only value types are created here and their default value doesn't use a constructor.")]
        private static object? EvaluateDefault(Expression exp)
        {
            if (!exp.Type.IsValueType || Nullable.GetUnderlyingType(exp.Type) is not null)
                return null;
            return RuntimeHelpers.GetUninitializedObject(exp.Type);
        }
        private static object? EvaluateMemberAccess(MemberExpression member)
        {
            var instance = member.Expression is null ? null : Evaluate(member.Expression);
            switch (member.Member)
            {
                case FieldInfo field:
                    if (instance is null && !field.IsStatic)
                        return null;
                    return field.GetValue(instance);
                case PropertyInfo property:
                    if (property.GetMethod is null || (instance is null && !property.GetMethod.IsStatic))
                        return null;
                    return property.GetValue(instance);
                default:
                    throw new NotSupportedException($"{member} can't be evaluated");
            }
        }
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Falls back to InvalidOperationException at runtime when dynamic code is unsupported.")]
        private static object? EvaluateInvoke(Expression exp)
        {
            if (exp is LambdaExpression)
                return exp;
            try
            {
                return Expression.Lambda(exp).Compile().DynamicInvoke();
            }
            catch (Exception ex)
            {
                if (!RuntimeFeature.IsDynamicCodeSupported)
                    throw new InvalidOperationException("Dynamic code execution is not supported in this runtime environment.", ex);
                throw;
            }
        }

        private static BsonValue Constant(object? value)
        {
            if (value is null)
                return BsonNull.Value;
            //a literal is never read as a field path or an operator, and a number in a $project is never read as including a field
            return new BsonDocument("$literal", ToBson(value));
        }

        /// <summary>
        /// The BSON of a value as it is stored: an enum by its name, a date in UTC, a TimeSpan or TimeOnly as ticks, a Guid as a standard UUID.
        /// </summary>
        public static BsonValue ToBson(object? value)
        {
            switch (value)
            {
                case null: return BsonNull.Value;
                case string text: return new BsonString(text);
                case bool boolean: return boolean ? BsonBoolean.True : BsonBoolean.False;
                case int number: return new BsonInt32(number);
                case long number: return new BsonInt64(number);
                case short number: return new BsonInt32(number);
                case byte number: return new BsonInt32(number);
                case sbyte number: return new BsonInt32(number);
                case ushort number: return new BsonInt32(number);
                case uint number: return new BsonInt64(number);
                case ulong number: return new BsonDecimal128(new Decimal128(number));
                case double number: return new BsonDouble(number);
                case float number: return new BsonDouble(number);
                case decimal number: return new BsonDecimal128(new Decimal128(number));
                case char character: return new BsonString(character.ToString());
                case DateTime date: return new BsonDateTime(ToUtc(date));
                case DateTimeOffset offset: return new BsonDateTime(offset.UtcDateTime);
                case DateOnly day: return new BsonDateTime(day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
                case TimeSpan span: return new BsonInt64(span.Ticks);
                case TimeOnly time: return new BsonInt64(time.Ticks);
                case Guid guid: return new BsonBinaryData(guid, GuidRepresentation.Standard);
                case byte[] bytes: return new BsonBinaryData(bytes);
                case Enum item: return new BsonString(EnumName.GetName(item.GetType(), item));
                case IEnumerable items:
                    {
                        var array = new BsonArray();
                        foreach (var item in items)
                            array.Add(ToBson(item));
                        return array;
                    }
                default:
                    throw new NotSupportedException($"{value.GetType().Name} values aren't supported by MongoDB");
            }
        }
        /// <summary>
        /// The BSON of a member's value, a date whose member keeps only the date stored at midnight.
        /// </summary>
        public static BsonValue ToBson(object? value, ModelMemberDetail member)
        {
            if (member.DatePart == StoreDatePart.Date)
            {
                if (value is DateTime date)
                    return new BsonDateTime(DateTime.SpecifyKind(ToUtc(date).Date, DateTimeKind.Utc));
                if (value is DateTimeOffset offset)
                    return new BsonDateTime(DateTime.SpecifyKind(offset.UtcDateTime.Date, DateTimeKind.Utc));
            }
            return ToBson(value);
        }
        //a date with no zone is UTC, as dates are read
        public static DateTime ToUtc(DateTime date) => date.Kind switch
        {
            DateTimeKind.Local => date.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(date, DateTimeKind.Utc),
            _ => date,
        };

        /// <summary>
        /// The value of a type from its BSON, an enum from its name or number.
        /// </summary>
        public static object? FromBson(BsonValue? value, Type type)
        {
            if (value is null || value.IsBsonNull || value.IsBsonUndefined)
                return null;
            var underlying = Underlying(type);
            if (underlying.IsEnum)
            {
                if (value.IsString)
                    return EnumName.Parse(value.AsString, underlying);
                return Enum.ToObject(underlying, System.Convert.ChangeType(FromBsonNumber(value), Enum.GetUnderlyingType(underlying), CultureInfo.InvariantCulture));
            }
            if (underlying == typeof(string))
                return value.IsString ? value.AsString : value.ToString();
            if (underlying == typeof(bool))
                return value.IsBoolean ? value.AsBoolean : value.ToBoolean();
            if (underlying == typeof(Guid))
                return value.AsBsonBinaryData.ToGuid(GuidRepresentation.Standard);
            if (underlying == typeof(byte[]))
                return value.AsBsonBinaryData.Bytes;
            if (underlying == typeof(char))
                return value.AsString.Length == 0 ? '\0' : value.AsString[0];
            if (underlying == typeof(DateTime))
                return value.IsBsonDateTime ? value.ToUniversalTime() : new DateTime(value.ToInt64(), DateTimeKind.Utc);
            if (underlying == typeof(DateTimeOffset))
                return new DateTimeOffset(value.ToUniversalTime());
            if (underlying == typeof(DateOnly))
                return DateOnly.FromDateTime(value.ToUniversalTime());
            if (underlying == typeof(TimeSpan))
                return new TimeSpan(System.Convert.ToInt64(FromBsonNumber(value), CultureInfo.InvariantCulture));
            if (underlying == typeof(TimeOnly))
                return new TimeOnly(System.Convert.ToInt64(FromBsonNumber(value), CultureInfo.InvariantCulture));
            var number = FromBsonNumber(value);
            if (number.GetType() == underlying || underlying == typeof(object))
                return number;
            //a fraction to an integer, such as an average, keeps the whole part as a cast would
            if (IsInteger(underlying) && number is double or decimal)
                number = number is double d ? Math.Truncate(d) : Math.Truncate((decimal)number);
            return System.Convert.ChangeType(number, underlying, CultureInfo.InvariantCulture);
        }
        private static object FromBsonNumber(BsonValue value)
        {
            return value.BsonType switch
            {
                BsonType.Int32 => value.AsInt32,
                BsonType.Int64 => value.AsInt64,
                BsonType.Double => value.AsDouble,
                BsonType.Decimal128 => (decimal)value.AsDecimal128,
                BsonType.Boolean => value.AsBoolean ? 1 : 0,
                BsonType.String => value.AsString,
                BsonType.DateTime => value.ToUniversalTime(),
                _ => throw new NotSupportedException($"A BSON {value.BsonType} isn't a number"),
            };
        }
    }
}

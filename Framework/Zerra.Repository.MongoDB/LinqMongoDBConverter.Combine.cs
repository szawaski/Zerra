// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using MongoDB.Bson;
using System.Globalization;
using System.Linq.Expressions;
using Zerra.Reflection;
using Zerra.Repository.Linq;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.MongoDB
{
    //queries combined by a set operation or a join: each query is the pipeline of its own collection giving its columns, the second read with $unionWith or $lookup
    internal sealed partial class LinqMongoDBConverter
    {
        private const string NumberField = "__n";
        private const string PairedField = "__r";
        private const string MatchedField = "__m";
        private const string PartField = "__p";

        //the place of each row in the order it's numbered in, from 1, before it's paged
        private void WriteNumbering(LinqPlan plan, Scope root, List<BsonDocument> stages)
        {
            if (plan.NumberColumn is not int number)
                return;
            var orders = plan.Columns[number].Orders;
            var sort = WriteSortKeys(orders, root, stages, "__k");
            //$documentNumber takes one key, an order of several is sorted and numbered as an array
            if (sort is not null && sort.ElementCount > 1)
                stages.Add(new BsonDocument("$sort", sort));
            else if (sort is not null)
            {
                stages.Add(new BsonDocument("$setWindowFields", new BsonDocument
                {
                    { "sortBy", sort },
                    { "output", new BsonDocument(NumberField, new BsonDocument("$documentNumber", new BsonDocument())) }
                }));
                return;
            }
            //in the order the store reads them, kept by an array of them
            stages.Add(new BsonDocument("$group", new BsonDocument { { IdField, BsonNull.Value }, { "d", new BsonDocument("$push", "$$ROOT") } }));
            stages.Add(new BsonDocument("$unwind", new BsonDocument { { "path", "$d" }, { "includeArrayIndex", "__i" } }));
            stages.Add(new BsonDocument("$replaceRoot", new BsonDocument("newRoot", new BsonDocument("$mergeObjects", new BsonArray { "$d", new BsonDocument(NumberField, new BsonDocument("$add", new BsonArray { "$__i", 1 })) }))));
        }

        //a combined query read on its own, ended as the query ends
        private static MongoDBStatement ConvertCombinedTop(LinqQuery query, ModelDetail modelDetail)
        {
            var converter = new LinqMongoDBConverter();
            var statement = converter.ConvertCombined(query, modelDetail, true);
            var stages = statement.Stages;
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
                    {
                        var value = query.Value!;
                        var body = value.Body is UnaryExpression { NodeType: ExpressionType.Convert } boxed && boxed.Type == typeof(object) ? boxed.Operand : value.Body;
                        converter.scopes[value.Parameters[0]] = new Scope("$c", null, null, statement.Plan.Shape) { Plan = statement.Plan };
                        //an enum's smallest and largest are by its number
                        var bson = query.Terminal is LinqTerminal.Min or LinqTerminal.Max ? converter.NumberValue(body) : converter.Convert(body);
                        var accumulator = query.Terminal switch
                        {
                            LinqTerminal.Sum => "$sum",
                            LinqTerminal.Min => "$min",
                            LinqTerminal.Max => "$max",
                            _ => "$avg",
                        };
                        stages.Add(new BsonDocument("$group", new BsonDocument { { IdField, BsonNull.Value }, { "v", new BsonDocument(accumulator, bson) } }));
                        break;
                    }
            }
            return statement;
        }

        //the elements of a set or a join and what follows them
        private MongoDBStatement ConvertCombined(LinqQuery query, ModelDetail modelDetail, bool top)
        {
            var statement = query.Set is not null ? ConvertSet(query.Set) : ConvertJoin(query.Join!);
            var plan = statement.Plan;
            var placed = plan.PositionColumns is not null && (top || query.Skip.HasValue || query.Take.HasValue);
            if (query.Having is not null || query.ResultOrder is not null || query.Skip.HasValue || query.Take.HasValue || query.Select is not null || query.SelectedHaving is not null || query.SelectedOrder is not null || query.Distinct || placed)
                plan = WriteCombinedResults(query, plan, statement.Stages, modelDetail, top);
            return new MongoDBStatement(statement.Collection, statement.Stages, plan, null, null);
        }

        //each query combined is a statement of its own, its rows only its columns
        private MongoDBStatement ConvertPart(LinqQuery part)
        {
            var modelDetail = ModelAnalyzer.GetModel(part.ModelType);
            if (part.Combines)
                return new LinqMongoDBConverter().ConvertCombined(part, modelDetail, false);
            var statement = ConvertLinq(part, modelDetail);
            if (statement.Pieces is not null)
                throw new NotSupportedException("A query combined with a set operation reads its own rows, not a relation read after them");
            return statement;
        }
        private static BsonDocument UnionWith(MongoDBStatement statement, BsonDocument? added = null)
        {
            var pipeline = new BsonArray(statement.Stages);
            if (added is not null)
                pipeline.Add(added);
            return new BsonDocument("$unionWith", new BsonDocument { { "coll", statement.Collection }, { "pipeline", pipeline } });
        }
        //the document of a row's columns, which rows are the same by
        private static BsonDocument RowOf(LinqPlan plan)
        {
            var row = new BsonDocument();
            for (var i = 0; i < plan.Columns.Count; i++)
            {
                var name = "c" + i.ToString(CultureInfo.InvariantCulture);
                row[name] = "$" + name;
            }
            return row;
        }

        private MongoDBStatement ConvertSet(LinqSet set)
        {
            var left = ConvertPart(set.Left);
            var right = ConvertPart(set.Right);
            if (!SameShape(left.Plan.Shape!, right.Plan.Shape!))
                throw new NotSupportedException($"The queries {set.Operation} combines build their elements from the same values in the same order, as rows combine by position");
            var stages = new List<BsonDocument>(left.Stages);
            var row = RowOf(left.Plan);
            switch (set.Operation)
            {
                case LinqSetOperation.Concat:
                    stages.Add(UnionWith(right));
                    break;
                case LinqSetOperation.Union:
                    stages.Add(UnionWith(right));
                    stages.Add(new BsonDocument("$group", new BsonDocument(IdField, row)));
                    stages.Add(new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$" + IdField)));
                    break;
                case LinqSetOperation.Intersect:
                case LinqSetOperation.Except:
                    {
                        //each different row once, with whether each query has it
                        stages.Add(new BsonDocument("$addFields", new BsonDocument(PartField, 1)));
                        stages.Add(UnionWith(right, new BsonDocument("$addFields", new BsonDocument(PartField, 2))));
                        stages.Add(new BsonDocument("$group", new BsonDocument
                        {
                            { IdField, row },
                            { "l", new BsonDocument("$max", new BsonDocument("$eq", new BsonArray { "$" + PartField, 1 })) },
                            { "r", new BsonDocument("$max", new BsonDocument("$eq", new BsonArray { "$" + PartField, 2 })) }
                        }));
                        stages.Add(new BsonDocument("$match", new BsonDocument { { "l", true }, { "r", set.Operation == LinqSetOperation.Intersect } }));
                        stages.Add(new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$" + IdField)));
                        break;
                    }
                default:
                    throw new NotSupportedException($"{set.Operation} isn't a set operation");
            }
            return new MongoDBStatement(left.Collection, stages, left.Plan, null, null);
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

        //the first query's rows, each with the second's paired with it in a field, or the second's first for a RightJoin
        private MongoDBStatement ConvertJoin(LinqJoin join)
        {
            var left = ConvertPart(join.Left);
            var right = join.Right is null ? null : ConvertPart(join.Right);
            var modelDetail = ModelAnalyzer.GetModel(join.Left.ModelType);
            var plan = LinqPlan.ReadJoin(join, left.Plan.Shape!, right?.Plan.Shape, left.Plan.NumberColumn, modelDetail);
            if (join.Kind is not (LinqJoinKind.Inner or LinqJoinKind.Left or LinqJoinKind.Right or LinqJoinKind.Group))
                return ConvertPosition(join, left, right, plan);
            if (right is null)
                throw new InvalidOperationException("A join reads two queries");

            //the rows read first, and the query looked up for each
            var rightFirst = join.Kind == LinqJoinKind.Right;
            var first = rightFirst ? right : left;
            var paired = rightFirst ? left : right;
            var firstElement = rightFirst ? join.RightElement : join.LeftElement;
            var firstKey = rightFirst ? join.RightKey : join.LeftKey;
            var pairedElement = rightFirst ? join.LeftElement : join.RightElement;
            var pairedKey = rightFirst ? join.LeftKey : join.RightKey;
            var group = join.Kind == LinqJoinKind.Group;
            var optional = join.Kind is LinqJoinKind.Left or LinqJoinKind.Right;

            //in the lookup's pipeline the first row's columns are variables, the paired row's are its own
            var let = new BsonDocument();
            for (var i = 0; i < first.Plan.Columns.Count; i++)
                let["lc" + i.ToString(CultureInfo.InvariantCulture)] = "$c" + i.ToString(CultureInfo.InvariantCulture);
            var firstInside = new Scope("$$lc", null, null, first.Plan.Shape) { Plan = first.Plan };
            var pairedInside = new Scope("$c", null, null, paired.Plan.Shape) { Plan = paired.Plan };
            SetElement(firstElement, firstKey, firstInside);
            SetElement(pairedElement, pairedKey, pairedInside);
            var conditions = new BsonArray();
            if (join.LeftKey is not null)
                conditions.Add(KeyMatch(join));
            if (join.Condition is not null)
                conditions.Add(Convert(join.Condition));
            var pipeline = new BsonArray(paired.Stages);
            if (conditions.Count > 0)
                pipeline.Add(new BsonDocument("$match", new BsonDocument("$expr", conditions.Count == 1 ? conditions[0] : new BsonDocument("$and", conditions))));
            if (optional)
                pipeline.Add(new BsonDocument("$addFields", new BsonDocument(MatchedField, 1)));
            if (pendingLookups.Count > 0)
                throw new NotSupportedException("A join's keys and condition are read from the elements of its queries");

            var stages = new List<BsonDocument>(first.Stages)
            {
                new BsonDocument("$lookup", new BsonDocument { { "from", paired.Collection }, { "let", let }, { "pipeline", pipeline }, { "as", PairedField } })
            };
            if (!group)
                stages.Add(new BsonDocument("$unwind", new BsonDocument { { "path", "$" + PairedField }, { "preserveNullAndEmptyArrays", optional } }));

            //after the lookup the first row's columns are its own and the paired row's are in its field
            SetElement(firstElement, firstKey, new Scope("$c", null, null, first.Plan.Shape) { Plan = first.Plan });
            if (group)
                scopes[pairedElement] = new Scope("$" + PairedField, null, null, paired.Plan.Shape) { Plan = paired.Plan, Group = true };
            else
                SetElement(pairedElement, pairedKey, new Scope("$" + PairedField + ".c", null, null, paired.Plan.Shape) { Plan = paired.Plan, NullField = optional ? "$" + PairedField + "." + MatchedField : null });

            stages.Add(new BsonDocument("$project", JoinColumns(plan, rightFirst)));
            return new MongoDBStatement(first.Collection, stages, plan, null, null);
        }
        private void SetElement(ParameterExpression element, LambdaExpression? key, Scope scope)
        {
            scopes[element] = scope;
            if (key is not null)
                scopes[key.Parameters[0]] = scope;
        }
        //the keys of a pair are equal; a key of one part that is null matches none as in LINQ, where parts of a key of several compare as their object does, null equal to null
        private BsonValue KeyMatch(LinqJoin join)
        {
            var leftParts = AggregateCommon.GetKeyParts(join.LeftKey!);
            var rightParts = AggregateCommon.GetKeyParts(join.RightKey!);
            if (leftParts.Length != rightParts.Length)
                throw new NotSupportedException("The keys of a join have the same parts");
            var matches = new BsonArray();
            for (var i = 0; i < leftParts.Length; i++)
            {
                var leftPart = leftParts[i];
                var rightPart = rightParts[i].Type == leftPart.Type ? rightParts[i] : Expression.Convert(rightParts[i], leftPart.Type);
                matches.Add(ConvertComparison(Expression.Equal(leftPart, rightPart)));
                if (leftParts.Length == 1 && (!leftPart.Type.IsValueType || Nullable.GetUnderlyingType(leftPart.Type) is not null))
                    matches.Add(new BsonDocument("$gt", new BsonArray { Convert(leftPart), BsonNull.Value }));
            }
            return matches.Count == 1 ? matches[0] : new BsonDocument("$and", matches);
        }
        //the columns of a join's plan, each of either query's or computed from them
        private BsonDocument JoinColumns(LinqPlan plan, bool rightFirst)
        {
            var project = new BsonDocument(IdField, 0);
            for (var i = 0; i < plan.Columns.Count; i++)
            {
                var column = plan.Columns[i];
                BsonValue value;
                if (column.From is int from)
                {
                    //the query read first has its columns as they are, the other's are in the field it was paired into
                    var prefix = column.FromRight != rightFirst ? "$" + PairedField + "." : "$";
                    value = from switch
                    {
                        -1 => prefix + MatchedField,
                        -2 => "$" + PartField,
                        _ => prefix + "c" + from.ToString(CultureInfo.InvariantCulture),
                    };
                }
                else
                {
                    value = Convert(column.Expression!);
                }
                project["c" + i.ToString(CultureInfo.InvariantCulture)] = value;
            }
            if (pendingLookups.Count > 0)
                throw new NotSupportedException("The result of a join is built from the elements of its queries");
            return project;
        }

        //an operation by position reads the place of each row of a query, numbered in its order
        private MongoDBStatement ConvertPosition(LinqJoin join, MongoDBStatement left, MongoDBStatement? right, LinqPlan plan)
        {
            var place = "$c" + left.Plan.NumberColumn!.Value.ToString(CultureInfo.InvariantCulture);
            SetElement(join.LeftElement, join.LeftKey, new Scope("$c", null, null, left.Plan.Shape) { Plan = left.Plan });
            if (right is not null && join.Kind != LinqJoinKind.UnionBy)
                scopes[join.RightElement] = new Scope("$" + PairedField + ".c", null, null, right.Plan.Shape) { Plan = right.Plan };
            else
                scopes[join.RightElement] = new Scope(String.Empty, null, null, null) { Value = new BsonDocument("$subtract", new BsonArray { place, 1 }) };

            var stages = new List<BsonDocument>(left.Stages);
            if (join.Kind == LinqJoinKind.SequenceEqual)
            {
                if (!SameShape(left.Plan.Shape!, right!.Plan.Shape!))
                    throw new NotSupportedException("The queries SequenceEqual compares build their elements from the same values in the same order");
                var rightPlace = "$c" + right.Plan.NumberColumn!.Value.ToString(CultureInfo.InvariantCulture);
                //a row differs when the other query has none at its place or a value differs, null equal to null
                var differs = new BsonArray { new BsonDocument("$eq", new BsonArray { new BsonDocument("$type", "$" + PairedField), "missing" }) };
                for (var i = 0; i < left.Plan.Columns.Count; i++)
                {
                    if (i == left.Plan.NumberColumn)
                        continue;
                    var name = "c" + i.ToString(CultureInfo.InvariantCulture);
                    differs.Add(new BsonDocument("$ne", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$" + name, BsonNull.Value }), new BsonDocument("$ifNull", new BsonArray { "$" + PairedField + "." + name, BsonNull.Value }) }));
                }
                stages.Add(PlaceLookup(right, place, rightPlace));
                stages.Add(new BsonDocument("$unwind", new BsonDocument { { "path", "$" + PairedField }, { "preserveNullAndEmptyArrays", true } }));
                stages.Add(new BsonDocument("$match", new BsonDocument("$expr", new BsonDocument("$or", differs))));
                stages.Add(new BsonDocument("$project", new BsonDocument { { IdField, 0 }, { "c0", new BsonDocument("$literal", 1) } }));
                //and a row of the second query at a place the first has none of
                var extra = new BsonArray(right.Stages)
                {
                    PlaceLookup(left, rightPlace, place),
                    new BsonDocument("$match", new BsonDocument(PairedField, new BsonDocument("$size", 0))),
                    new BsonDocument("$project", new BsonDocument { { IdField, 0 }, { "c0", new BsonDocument("$literal", 1) } })
                };
                stages.Add(new BsonDocument("$unionWith", new BsonDocument { { "coll", right.Collection }, { "pipeline", extra } }));
                return new MongoDBStatement(left.Collection, stages, plan, null, null);
            }

            switch (join.Kind)
            {
                case LinqJoinKind.Zip:
                    stages.Add(PlaceLookup(right!, place, "$c" + right!.Plan.NumberColumn!.Value.ToString(CultureInfo.InvariantCulture)));
                    stages.Add(new BsonDocument("$unwind", "$" + PairedField));
                    break;
                case LinqJoinKind.Index:
                    if (join.Condition is not null)
                        stages.Add(new BsonDocument("$match", new BsonDocument("$expr", Convert(join.Condition))));
                    break;
                case LinqJoinKind.Append:
                    stages.Add(AppendRow(join, left.Plan));
                    break;
                case LinqJoinKind.While:
                    //the elements before the first that fails are those with no failure up to them
                    stages.Add(new BsonDocument("$setWindowFields", new BsonDocument
                    {
                        { "sortBy", new BsonDocument(place.Substring(1), 1) },
                        { "output", new BsonDocument("__f", new BsonDocument
                            {
                                { "$sum", new BsonDocument("$cond", new BsonArray { Convert(join.Condition!), 0, 1 }) },
                                { "window", new BsonDocument("documents", new BsonArray { "unbounded", "current" }) }
                            })
                        }
                    }));
                    stages.Add(new BsonDocument("$match", join.Skips ? new BsonDocument("__f", new BsonDocument("$gt", 0)) : new BsonDocument("__f", 0)));
                    break;
                case LinqJoinKind.Last:
                    {
                        stages.Add(new BsonDocument("$setWindowFields", new BsonDocument("output", new BsonDocument("__t", new BsonDocument("$count", new BsonDocument())))));
                        var from = new BsonDocument("$subtract", new BsonArray { "$__t", join.Count });
                        var condition = join.At ? new BsonDocument("$eq", new BsonArray { place, new BsonDocument("$add", new BsonArray { from, 1 }) })
                            : join.Skips ? new BsonDocument("$lte", new BsonArray { place, from })
                            : new BsonDocument("$gt", new BsonArray { place, from });
                        stages.Add(new BsonDocument("$match", new BsonDocument("$expr", condition)));
                        break;
                    }
                case LinqJoinKind.DistinctBy:
                case LinqJoinKind.UnionBy:
                    {
                        var sortBy = new BsonDocument();
                        if (join.Kind == LinqJoinKind.UnionBy)
                        {
                            //the first query's elements before the second's, each in its own order
                            stages.Add(new BsonDocument("$addFields", new BsonDocument(PartField, 1)));
                            stages.Add(UnionWith(right!, new BsonDocument("$addFields", new BsonDocument(PartField, 2))));
                            sortBy[PartField] = 1;
                        }
                        sortBy[place.Substring(1)] = 1;
                        //the first element of each key in that order
                        var key = new BsonDocument();
                        var parts = AggregateCommon.GetKeyParts(join.LeftKey!);
                        for (var i = 0; i < parts.Length; i++)
                            key["k" + i.ToString(CultureInfo.InvariantCulture)] = Convert(parts[i]);
                        stages.Add(new BsonDocument("$sort", sortBy));
                        stages.Add(new BsonDocument("$group", new BsonDocument { { IdField, key }, { "d", new BsonDocument("$first", "$$ROOT") } }));
                        stages.Add(new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$d")));
                        break;
                    }
                default:
                    throw new NotSupportedException($"{join.Kind} isn't supported by MongoDB");
            }
            if (pendingLookups.Count > 0)
                throw new NotSupportedException("An operation by position reads the elements of its query");
            stages.Add(new BsonDocument("$project", JoinColumns(plan, false)));
            return new MongoDBStatement(left.Collection, stages, plan, null, null);
        }
        //the row of another query at the same place
        private static BsonDocument PlaceLookup(MongoDBStatement other, string place, string otherPlace)
        {
            var pipeline = new BsonArray(other.Stages)
            {
                new BsonDocument("$match", new BsonDocument("$expr", new BsonDocument("$eq", new BsonArray { otherPlace, "$$place" })))
            };
            return new BsonDocument("$lookup", new BsonDocument { { "from", other.Collection }, { "let", new BsonDocument("place", place) }, { "pipeline", pipeline }, { "as", PairedField } });
        }
        //the rows of the query and one more of the element added, first or last by its place
        private static BsonDocument AppendRow(LinqJoin join, LinqPlan plan)
        {
            var values = new object?[plan.Columns.Count];
            var written = new bool[values.Length];
            ElementValues(plan.Shape!, join.Value, values, written);
            var row = new BsonDocument();
            for (var i = 0; i < values.Length; i++)
            {
                var name = "c" + i.ToString(CultureInfo.InvariantCulture);
                if (i == plan.NumberColumn)
                    row[name] = join.Prepends ? 0 : Int32.MaxValue;
                else if (written[i])
                    row[name] = plan.Columns[i].Member is { } member ? ToBson(values[i], member) : ToBson(values[i]);
            }
            return new BsonDocument("$unionWith", new BsonDocument("pipeline", new BsonArray { new BsonDocument("$documents", new BsonDocument("$literal", new BsonArray { row })) }));
        }
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

        //the condition, Select, order, and page of the combined elements, each member of an element the column holding it
        private LinqPlan WriteCombinedResults(LinqQuery query, LinqPlan plan, List<BsonDocument> stages, ModelDetail modelDetail, bool top)
        {
            var elements = new Scope("$c", null, null, plan.Shape) { Plan = plan };
            var positions = plan.PositionColumns;
            var select = query.Select is not null && (query.ReadsElements || query.SelectedHaving is not null || query.Distinct) ? LinqPlan.ReadSetSelect(query.Select, modelDetail) : null;

            if (query.Having is not null)
            {
                scopes[query.Having.Parameters[0]] = elements;
                stages.Add(new BsonDocument("$match", new BsonDocument("$expr", Convert(query.Having.Body))));
            }
            Scope? selected = null;
            if (select is not null)
            {
                //what the Select builds is in fields of its own next to the columns its order may still read
                var values = new BsonDocument();
                for (var i = 0; i < select.Columns.Count; i++)
                {
                    var column = select.Columns[i];
                    scopes[column.Parameter!] = elements;
                    values["s" + i.ToString(CultureInfo.InvariantCulture)] = Convert(column.Expression!);
                }
                stages.Add(new BsonDocument("$addFields", values));
                selected = new Scope("$s", null, null, select.Shape) { Plan = select };
                if (query.SelectedHaving is not null)
                {
                    scopes[query.SelectedHaving.Parameters[0]] = selected;
                    stages.Add(new BsonDocument("$match", new BsonDocument("$expr", Convert(query.SelectedHaving.Body))));
                }
            }

            var none = query.Take == 0;
            var paged = !none && (query.Skip.HasValue || query.Take.HasValue);
            var selectedOrder = select is null ? null : query.SelectedOrder;
            var ordered = (query.ResultOrder is not null || selectedOrder is not null) && (query.ReadsElements || paged);
            //elements of an operation by position are in their places unless ordered otherwise, which the Distinct of what a Select builds loses
            var byPosition = !ordered && positions is not null && ((query.ReadsElements && top) || paged) && !(query.Distinct && select is not null);

            if (query.Distinct)
            {
                //the different elements are ordered and paged once they're found, an order then reads what the Select built
                var row = new BsonDocument();
                if (select is not null)
                {
                    for (var i = 0; i < select.Columns.Count; i++)
                        row["s" + i.ToString(CultureInfo.InvariantCulture)] = "$s" + i.ToString(CultureInfo.InvariantCulture);
                    elements = selected!;
                }
                else
                {
                    row = RowOf(plan);
                }
                stages.Add(new BsonDocument("$group", new BsonDocument(IdField, row)));
                stages.Add(new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$" + IdField)));
            }

            if (byPosition)
            {
                var sort = new BsonDocument();
                foreach (var position in positions!)
                    sort["c" + position.ToString(CultureInfo.InvariantCulture)] = 1;
                stages.Add(new BsonDocument("$sort", sort));
            }
            else if (ordered)
            {
                var orders = new List<(LinqOrder Order, Scope Scope)>();
                foreach (var order in query.ResultOrder ?? [])
                    orders.Add((order, elements));
                foreach (var order in selectedOrder ?? [])
                    orders.Add((order, selected!));
                var sort = new BsonDocument();
                var computed = new BsonDocument();
                for (var i = 0; i < orders.Count; i++)
                {
                    var (order, scope) = orders[i];
                    scopes[order.Expression.Parameters[0]] = scope;
                    var body = order.Expression.Body is UnaryExpression { NodeType: ExpressionType.Convert } boxed && boxed.Type == typeof(object) ? boxed.Operand : order.Expression.Body;
                    var field = "__o" + i.ToString(CultureInfo.InvariantCulture);
                    computed[field] = SortValue(body);
                    if (!sort.Contains(field))
                        sort[field] = order.Descending ? -1 : 1;
                }
                stages.Add(new BsonDocument("$addFields", computed));
                stages.Add(new BsonDocument("$sort", sort));
            }
            WritePage(query.Skip, query.Take, stages);

            if (select is not null && (query.ReadsElements || query.Distinct))
            {
                var project = new BsonDocument(IdField, 0);
                for (var i = 0; i < select.Columns.Count; i++)
                    project["c" + i.ToString(CultureInfo.InvariantCulture)] = "$s" + i.ToString(CultureInfo.InvariantCulture);
                stages.Add(new BsonDocument("$project", project));
                return select;
            }
            return plan;
        }
    }
}

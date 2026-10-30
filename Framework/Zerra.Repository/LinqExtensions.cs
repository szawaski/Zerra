// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Zerra.Linq;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// Extension methods for applying <see cref="QueryOrder"/> and <see cref="Zerra.Repository.Query"/> to LINQ sequences and queryables, and for calculating over and projecting sequences in memory.
    /// </summary>
    public static class LinqExtensions
    {
        /// <summary>
        /// Sorts the elements of a sequence according to the specified <see cref="QueryOrder"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to order.</param>
        /// <param name="order">The <see cref="QueryOrder"/> that defines the ordering expressions.</param>
        /// <returns>An <see cref="IOrderedEnumerable{TSource}"/> whose elements are sorted according to <paramref name="order"/>.</returns>
        public static IOrderedEnumerable<TSource> OrderBy<TSource>(this IEnumerable<TSource> source, QueryOrder order)
            where TSource : class, new()
        {
            IOrderedEnumerable<TSource>? result = null;
            foreach (var item in order.OrderExpressions.Reverse())
                result = item.OrderBy(result ?? source);
            return result!;
        }

        /// <summary>
        /// Sorts the elements of a queryable according to the specified <see cref="QueryOrder"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The queryable to order.</param>
        /// <param name="order">The <see cref="QueryOrder"/> that defines the ordering expressions.</param>
        /// <returns>An <see cref="IOrderedQueryable{TSource}"/> whose elements are sorted according to <paramref name="order"/>.</returns>
        public static IOrderedQueryable<TSource> OrderBy<TSource>(this IQueryable<TSource> source, QueryOrder order)
            where TSource : class, new()
        {
            IOrderedQueryable<TSource>? result = null;
            foreach (var item in order.OrderExpressions.Reverse())
                result = item.OrderBy(result ?? source);
            return result!;
        }

        /// <summary>
        /// Applies the filtering, ordering, skip, and take from a <see cref="Zerra.Repository.Query"/> to a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to query.</param>
        /// <param name="query">The <see cref="Zerra.Repository.Query"/> containing the where, order, skip, and take clauses.</param>
        /// <param name="caseSensitive">Indicates if string comparisons should be case sensitive.</param>
        /// <returns>An <see cref="IEnumerable{TSource}"/> that contains elements from the input sequence that satisfy the query.</returns>
        /// <exception cref="ArgumentException">Thrown when <see cref="Query.Where"/> is not of type <c>Expression&lt;Func&lt;TSource, bool&gt;&gt;</c>.</exception>
        public static IEnumerable<TSource> Query<TSource>(this IEnumerable<TSource> source, Query query, bool caseSensitive = false)
           where TSource : class, new()
        {
            var whereQuery = source;
            if (query.Where is not null)
            {
                var whereExpression = query.Where as Expression<Func<TSource, bool>>;
                if (whereExpression == null)
                    throw new ArgumentException($"Where must be of type Expression<Func<TSource, bool>>");
                whereQuery = whereQuery.Where(MemoryExpressionRewriter.Rewrite(whereExpression, caseSensitive).Compile());
            }

            var orderQuery = whereQuery;
            if (query.Order is not null)
            {
                orderQuery = orderQuery.OrderBy(query.Order);
            }

            var skiptake = orderQuery;
            if (query.Skip.HasValue)
                skiptake = skiptake.Skip(query.Skip.Value);
            if (query.Take.HasValue)
                skiptake = skiptake.Take(query.Take.Value);

            return skiptake;
        }

        /// <summary>
        /// Applies the filtering, ordering, skip, and take to a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to query.</param>
        /// <param name="where">The filter expression to apply, or null for no filtering.</param>
        /// <param name="order">The <see cref="QueryOrder"/> that defines the ordering expressions, or null for no ordering.</param>
        /// <param name="skip">The number of elements to skip, or null to skip none.</param>
        /// <param name="take">The number of elements to take, or null to take all.</param>
        /// <param name="caseSensitive">Indicates if string comparisons should be case sensitive.</param>
        /// <returns>An <see cref="IEnumerable{TSource}"/> that contains elements from the input sequence that satisfy the query.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="where"/> is not of type <c>Expression&lt;Func&lt;TSource, bool&gt;&gt;</c>.</exception>
        public static IEnumerable<TSource> Query<TSource>(this IEnumerable<TSource> source, LambdaExpression? where, QueryOrder? order, int? skip, int? take, bool caseSensitive = false)
           where TSource : class, new()
        {
            var whereQuery = source;
            if (where is not null)
            {
                var whereExpression = where as Expression<Func<TSource, bool>>;
                if (whereExpression == null)
                    throw new ArgumentException($"Where must be of type Expression<Func<TSource, bool>>");
                whereQuery = whereQuery.Where(MemoryExpressionRewriter.Rewrite(whereExpression, caseSensitive).Compile());
            }

            var orderQuery = whereQuery;
            if (order is not null)
            {
                orderQuery = orderQuery.OrderBy(order);
            }

            var skiptake = orderQuery;
            if (skip.HasValue)
                skiptake = skiptake.Skip(skip.Value);
            if (take.HasValue)
                skiptake = skiptake.Take(take.Value);

            return skiptake;
        }

        /// <summary>
        /// Applies the filtering, ordering, skip, and take from a <see cref="Zerra.Repository.Query"/> to a queryable.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The queryable to query.</param>
        /// <param name="query">The <see cref="Zerra.Repository.Query"/> containing the where, order, skip, and take clauses.</param>
        /// <param name="caseSensitive">Indicates if string comparisons should be case sensitive.</param>
        /// <returns>An <see cref="IQueryable{TSource}"/> that contains elements from the input queryable that satisfy the query.</returns>
        /// <exception cref="ArgumentException">Thrown when <see cref="Query.Where"/> is not of type <c>Expression&lt;Func&lt;TSource, bool&gt;&gt;</c>.</exception>
        public static IQueryable<TSource> Query<TSource>(this IQueryable<TSource> source, Query query, bool caseSensitive = false)
            where TSource : class, new()
        {
            var whereQuery = source;
            if (query.Where is not null)
            {
                var whereExpression = query.Where as Expression<Func<TSource, bool>>;
                if (whereExpression == null)
                    throw new ArgumentException($"Where must be of type Expression<Func<TSource, bool>>");
                whereQuery = whereQuery.Where(whereExpression);
                whereQuery = whereQuery.Where(MemoryExpressionRewriter.Rewrite(whereExpression, caseSensitive));
            }

            var orderQuery = whereQuery;
            if (query.Order is not null)
            {
                orderQuery = orderQuery.OrderBy(query.Order);
            }

            var skiptake = orderQuery;
            if (query.Skip.HasValue)
                skiptake = skiptake.Skip(query.Skip.Value);
            if (query.Take.HasValue)
                skiptake = skiptake.Take(query.Take.Value);

            return skiptake;
        }

        /// <summary>
        /// Applies the filtering, ordering, skip, and take to a queryable.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The queryable to query.</param>
        /// <param name="where">The filter expression to apply, or null for no filtering.</param>
        /// <param name="order">The <see cref="QueryOrder"/> that defines the ordering expressions, or null for no ordering.</param>
        /// <param name="skip">The number of elements to skip, or null to skip none.</param>
        /// <param name="take">The number of elements to take, or null to take all.</param>
        /// <param name="caseSensitive">Indicates if string comparisons should be case sensitive.</param>
        /// <returns>An <see cref="IQueryable{TSource}"/> that contains elements from the input queryable that satisfy the query.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="where"/> is not of type <c>Expression&lt;Func&lt;TSource, bool&gt;&gt;</c>.</exception>
        public static IQueryable<TSource> Query<TSource>(this IQueryable<TSource> source, LambdaExpression? where, QueryOrder? order, int? skip, int? take, bool caseSensitive = false)
            where TSource : class, new()
        {
            var whereQuery = source;
            if (where is not null)
            {
                var whereExpression = where as Expression<Func<TSource, bool>>;
                if (whereExpression == null)
                    throw new ArgumentException($"Where must be of type Expression<Func<TSource, bool>>");
                whereQuery = whereQuery.Where(MemoryExpressionRewriter.Rewrite(whereExpression, caseSensitive));
            }

            var orderQuery = whereQuery;
            if (order is not null)
            {
                orderQuery = orderQuery.OrderBy(order);
            }

            var skiptake = orderQuery;
            if (skip.HasValue)
                skiptake = skiptake.Skip(skip.Value);
            if (take.HasValue)
                skiptake = skiptake.Take(take.Value);

            return skiptake;
        }

        /// <summary>
        /// Calculates over a sequence, the way the memory engine and the event store do, with the same empty and null rules as LINQ.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to calculate over, already filtered.</param>
        /// <param name="operation">The calculation.</param>
        /// <param name="value">The value expression, or <see langword="null"/> for a count.</param>
        /// <param name="caseSensitive">Indicates if string comparisons should be case sensitive.</param>
        /// <returns>The boxed result, as <see cref="CalculationCommon"/> describes.</returns>
        public static object? Calculate<TSource>(this IEnumerable<TSource> source, QueryOperation operation, LambdaExpression? value, bool caseSensitive = false)
            where TSource : class, new()
        {
            var getValue = value is null ? null : CalculationCommon.CompileBoxed<TSource>(value.Body, value.Parameters[0], caseSensitive);
            var values = new List<object?>();
            foreach (var model in source)
                values.Add(getValue is null ? null : getValue(model));
            return CalculationCommon.Aggregate(operation, values, value?.ReturnType ?? typeof(long));
        }

        /// <summary>
        /// Calculates per group of a sequence, the way the memory engine and the event store do, with the same empty and null rules as LINQ.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The group key type.</typeparam>
        /// <typeparam name="TValue">The calculated value type.</typeparam>
        /// <param name="source">The sequence to calculate over, already filtered.</param>
        /// <param name="operation">The calculation.</param>
        /// <param name="groupBy">The group key expression.</param>
        /// <param name="value">The value expression, or <see langword="null"/> for a count.</param>
        /// <param name="caseSensitive">Indicates if string keys that differ only in case are different groups.</param>
        /// <returns>The key and value of each group.</returns>
        public static IReadOnlyCollection<(TKey Key, TValue Value)> CalculateGroups<TSource, TKey, TValue>(this IEnumerable<TSource> source, QueryOperation operation, LambdaExpression groupBy, LambdaExpression? value, bool caseSensitive = false)
            where TSource : class, new()
        {
            var getValue = value is null ? null : CalculationCommon.CompileBoxed<TSource>(value.Body, value.Parameters[0], caseSensitive);
            var valueType = value?.ReturnType ?? typeof(long);
            var parts = CalculationCommon.GetKeyParts(groupBy);
            var getParts = new Func<TSource, object?>[parts.Length];
            for (var i = 0; i < parts.Length; i++)
                getParts[i] = CalculationCommon.CompileBoxed<TSource>(parts[i], groupBy.Parameters[0], caseSensitive);
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);

            var groups = new Dictionary<object?[], List<object?>>(new CalculationCommon.KeyComparer(caseSensitive));
            var keys = new List<object?[]>();
            foreach (var model in source)
            {
                var key = new object?[parts.Length];
                for (var i = 0; i < key.Length; i++)
                    key[i] = getParts[i](model);
                if (!groups.TryGetValue(key, out var values))
                {
                    values = new List<object?>();
                    groups.Add(key, values);
                    keys.Add(key);
                }
                values.Add(getValue is null ? null : getValue(model));
            }

            //a key of one member is its value, a key of several is its parts
            var rows = new (TKey Key, TValue Value)[keys.Count];
            for (var i = 0; i < rows.Length; i++)
            {
                var aggregate = CalculationCommon.Aggregate(operation, groups[keys[i]], valueType);
                rows[i] = (buildKey(parts.Length == 1 ? keys[i][0] : keys[i]), aggregate is null ? default! : (TValue)aggregate);
            }
            return rows;
        }

        /// <summary>
        /// Returns the different values of an expression over a sequence, the way the memory engine and the event store do.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="source">The sequence, already filtered.</param>
        /// <param name="value">The value expression, a member or an anonymous object of several.</param>
        /// <param name="caseSensitive">Indicates if string values that differ only in case are different values.</param>
        /// <returns>The different values.</returns>
        public static IReadOnlyCollection<TValue> Distinct<TSource, TValue>(this IEnumerable<TSource> source, LambdaExpression value, bool caseSensitive = false)
            where TSource : class, new()
        {
            var parts = CalculationCommon.GetKeyParts(value);
            var getParts = new Func<TSource, object?>[parts.Length];
            for (var i = 0; i < parts.Length; i++)
                getParts[i] = CalculationCommon.CompileBoxed<TSource>(parts[i], value.Parameters[0], caseSensitive);
            var buildKey = CalculationCommon.GetKeyBuilder<TValue>(value);

            var seen = new HashSet<object?[]>(new CalculationCommon.KeyComparer(caseSensitive));
            var values = new List<TValue>();
            foreach (var model in source)
            {
                var key = new object?[parts.Length];
                for (var i = 0; i < key.Length; i++)
                    key[i] = getParts[i](model);
                if (seen.Add(key))
                    values.Add(buildKey(parts.Length == 1 ? key[0] : key));
            }
            return values;
        }

        /// <summary>
        /// Reads a projection, <c>new TResult { Member = ..., ... }</c>, into the members it sets. For a group, each member is the group's <c>Key</c>, a part of it such as <c>Key.A</c>,
        /// <c>Count()</c>, <c>Count(x => condition)</c>, <c>Select(x => x.Member).Distinct().Count()</c>, or <c>Sum</c>, <c>Average</c>, <c>Min</c>, or <c>Max</c> of a value.
        /// </summary>
        /// <param name="select">The projection.</param>
        /// <param name="groupBy">The group key expression for a projection of groups, otherwise <see langword="null"/>.</param>
        /// <returns>The members in the order the projection sets them.</returns>
        public static ProjectionMember[] GetProjectionMembers(this LambdaExpression select, LambdaExpression? groupBy) => ReadProjectionMembers(select, groupBy, Type.EmptyTypes);
        //path is the row models above this one, so a collection within a row whose rows repeat one of them is refused rather than followed round and round; a model with a collection of itself nests one level
        private static ProjectionMember[] ReadProjectionMembers(LambdaExpression select, LambdaExpression? groupBy, Type[] path)
        {
            if (select.Body is not MemberInitExpression init)
                throw new NotSupportedException($"A projection builds its result with new {select.ReturnType.Name} {{ Member = ..., ... }}");

            var resultDetail = TypeAnalyzer.GetTypeDetail(select.ReturnType);
            if (resultDetail.CreatorBoxed is null)
                throw new NotSupportedException($"{select.ReturnType.Name} needs a parameterless constructor to be a projection result");
            var keyParts = groupBy is null ? null : CalculationCommon.GetKeyParts(groupBy);
            var parameter = select.Parameters[0];
            var members = new ProjectionMember[init.Bindings.Count];
            for (var i = 0; i < members.Length; i++)
            {
                if (init.Bindings[i] is not MemberAssignment assignment)
                    throw new NotSupportedException($"A projection sets each member of {select.ReturnType.Name} directly");
                var member = resultDetail.GetMember(assignment.Member.Name);
                if (member.SetterBoxed is null)
                    throw new NotSupportedException($"{select.ReturnType.Name}.{member.Name} has no setter");
                var expression = assignment.Expression;

                if (keyParts is null)
                {
                    //the results built from the rows of a related collection, after the calls that filter and order them, as an array or a list; a row's result can have collections of its own
                    var collectionCall = expression;
                    if (collectionCall is MethodCallExpression { Method.Name: "ToArray" or "ToList" } wrap && wrap.Method.DeclaringType == typeof(Enumerable) && wrap.Arguments.Count == 1)
                        collectionCall = wrap.Arguments[0];
                    if (collectionCall is MethodCallExpression { Method.Name: "Select" } selectCall && selectCall.Method.DeclaringType == typeof(Enumerable) && selectCall.Arguments.Count == 2)
                    {
                        LambdaExpression? where = null;
                        var orders = new List<OrderExpression>();
                        var source = selectCall.Arguments[0];
                        while (source is MethodCallExpression chain && chain.Method.DeclaringType == typeof(Enumerable) && chain.Arguments.Count == 2)
                        {
                            var argument = chain.Arguments[1];
                            if (argument.NodeType == ExpressionType.Quote)
                                argument = ((UnaryExpression)argument).Operand;
                            if (argument is not LambdaExpression lambda)
                                throw new NotSupportedException($"{select.ReturnType.Name}.{member.Name}: {chain.Method.Name} before the Select of a related collection takes a lambda");
                            if (ParameterFinder.Uses(lambda.Body, parameter))
                                throw new NotSupportedException($"{select.ReturnType.Name}.{member.Name}: the rows of a related collection are filtered and ordered by themselves alone, not by the model");
                            switch (chain.Method.Name)
                            {
                                case "Where":
                                    where = where is null ? lambda : Expression.Lambda(lambda.Type, Expression.AndAlso(lambda.Body, LinqRebinder.RebindExpression(where.Body, where.Parameters[0], lambda.Parameters[0])), lambda.Parameters);
                                    break;
                                case "OrderBy":
                                case "ThenBy":
                                    orders.Insert(0, new ProjectionOrderExpression(lambda, false));
                                    break;
                                case "OrderByDescending":
                                case "ThenByDescending":
                                    orders.Insert(0, new ProjectionOrderExpression(lambda, true));
                                    break;
                                default:
                                    throw new NotSupportedException($"{select.ReturnType.Name}.{member.Name}: {chain.Method.Name} before the Select of a related collection is not supported, filter the rows with Where and sort them with OrderBy");
                            }
                            source = chain.Arguments[0];
                        }
                        if (source is not MemberExpression relationMember || relationMember.Expression != parameter || !ModelAnalyzer.GetModel(parameter.Type).TryGetProperty(relationMember.Member.Name, out var relation) || !relation.IsRelated || !relation.IsEnumerable)
                            throw new NotSupportedException($"{select.ReturnType.Name}.{member.Name}: results are built from a related collection of the model, such as x.Lines.Select(l => new LineResult {{ .. }})");
                        var selectorArgument = selectCall.Arguments[1];
                        if (selectorArgument.NodeType == ExpressionType.Quote)
                            selectorArgument = ((UnaryExpression)selectorArgument).Operand;
                        if (selectorArgument is not LambdaExpression selector)
                            throw new NotSupportedException($"{select.ReturnType.Name}.{member.Name}: the Select of a related collection takes a lambda building a result from each row");
                        if (ParameterFinder.Uses(selector.Body, parameter))
                            throw new NotSupportedException($"{select.ReturnType.Name}.{member.Name}: the result of a related row is built from the row alone, not from the model");
                        var rowType = relation.ActualType;
                        if (Array.IndexOf(path, rowType) >= 0)
                            throw new NotSupportedException($"{select.ReturnType.Name}.{member.Name}: the rows of {rowType.Name} lead back to a model already projected above them");
                        var elementType = selector.ReturnType;
                        var memberTypeDetail = TypeAnalyzer.GetTypeDetail(member.Type);
                        if (member.Type.IsArray ? member.Type.GetElementType() != elementType : memberTypeDetail.InnerType != elementType || !(memberTypeDetail.IsListGeneric || memberTypeDetail.IsIEnumerableGeneric || memberTypeDetail.IsICollectionGeneric || memberTypeDetail.IsIListGeneric || memberTypeDetail.IsIReadOnlyCollectionGeneric || memberTypeDetail.IsIReadOnlyListGeneric))
                            throw new NotSupportedException($"{select.ReturnType.Name}.{member.Name} takes {elementType.Name}[], List<{elementType.Name}>, or a collection interface of it");
                        members[i] = new ProjectionMember(member, selectCall.Arguments[0], parameter, elementType, ReadProjectionMembers(selector, null, [.. path, rowType]), relation, where, orders.Count == 0 ? null : new ProjectionOrder(orders.ToArray()));
                        continue;
                    }
                    members[i] = new ProjectionMember(member, ProjectionMemberKind.Value, expression, parameter, 0, expression.Type);
                    continue;
                }

                //a member wider than the calculation, such as a long set to Count(), wraps it in a conversion which the result's setter does anyway
                while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion)
                    expression = conversion.Operand;

                switch (expression)
                {
                    case MemberExpression { Member.Name: "Key" } key when key.Expression == parameter:
                        if (keyParts.Length != 1)
                        {
                            var firstPartName = groupBy!.Body is NewExpression { Members: not null } newExpression ? newExpression.Members[0].Name : "Item1";
                            throw new NotSupportedException($"{select.ReturnType.Name}.{member.Name} takes the whole key of several parts; set one part such as Key.{firstPartName}");
                        }
                        members[i] = new ProjectionMember(member, ProjectionMemberKind.KeyPart, null, null, 0, keyParts[0].Type);
                        break;
                    case MemberExpression part when part.Expression is MemberExpression { Member.Name: "Key" } key && key.Expression == parameter:
                        {
                            var index = -1;
                            if (groupBy!.Body is NewExpression newExpression)
                            {
                                if (newExpression.Members is not null)
                                {
                                    for (var p = 0; p < newExpression.Members.Count; p++)
                                    {
                                        if (newExpression.Members[p].Name == part.Member.Name)
                                        {
                                            index = p;
                                            break;
                                        }
                                    }
                                }
                                else if (part.Member.Name.StartsWith("Item", StringComparison.Ordinal) && Int32.TryParse(part.Member.Name.AsSpan(4), out var item) && item >= 1 && item <= newExpression.Arguments.Count)
                                {
                                    index = item - 1;
                                }
                            }
                            if (index < 0)
                                throw new NotSupportedException($"{part.Member.Name} is not a part of the group key");
                            members[i] = new ProjectionMember(member, ProjectionMemberKind.KeyPart, null, null, index, keyParts[index].Type);
                            break;
                        }
                    case MethodCallExpression { Method.Name: "Count" or "LongCount" } count when count.Method.DeclaringType == typeof(Enumerable) && count.Arguments.Count == 1
                        && count.Arguments[0] is MethodCallExpression { Method.Name: "Distinct" } distinct && distinct.Method.DeclaringType == typeof(Enumerable) && distinct.Arguments.Count == 1
                        && distinct.Arguments[0] is MethodCallExpression { Method.Name: "Select" } selectCall && selectCall.Method.DeclaringType == typeof(Enumerable) && selectCall.Arguments.Count == 2 && selectCall.Arguments[0] == parameter:
                        {
                            //g.Select(x => x.Member).Distinct().Count() counts the different values
                            var selectorArgument = selectCall.Arguments[1];
                            if (selectorArgument.NodeType == ExpressionType.Quote)
                                selectorArgument = ((UnaryExpression)selectorArgument).Operand;
                            var selector = (LambdaExpression)selectorArgument;
                            members[i] = new ProjectionMember(member, ProjectionMemberKind.CountDistinct, selector.Body, selector.Parameters[0], 0, typeof(long));
                            break;
                        }
                    case MethodCallExpression call when call.Method.DeclaringType == typeof(Enumerable) && call.Arguments.Count > 0 && call.Arguments[0] == parameter:
                        switch (call.Method.Name)
                        {
                            case "Count":
                            case "LongCount":
                                {
                                    if (call.Arguments.Count == 1)
                                    {
                                        members[i] = new ProjectionMember(member, ProjectionMemberKind.Count, null, null, 0, typeof(long));
                                        break;
                                    }
                                    var predicateArgument = call.Arguments[1];
                                    if (predicateArgument.NodeType == ExpressionType.Quote)
                                        predicateArgument = ((UnaryExpression)predicateArgument).Operand;
                                    if (predicateArgument is not LambdaExpression predicate)
                                        throw new NotSupportedException($"{select.ReturnType.Name}.{member.Name}: a group's Count takes a condition");
                                    members[i] = new ProjectionMember(member, ProjectionMemberKind.CountIf, predicate.Body, predicate.Parameters[0], 0, typeof(long));
                                    break;
                                }
                            case "Sum":
                            case "Average":
                            case "Min":
                            case "Max":
                                {
                                    var selectorArgument = call.Arguments.Count == 2 ? call.Arguments[1] : throw new NotSupportedException($"{select.ReturnType.Name}.{member.Name}: a group's {call.Method.Name} takes the value to calculate");
                                    if (selectorArgument.NodeType == ExpressionType.Quote)
                                        selectorArgument = ((UnaryExpression)selectorArgument).Operand;
                                    if (selectorArgument is not LambdaExpression selector)
                                        throw new NotSupportedException($"{select.ReturnType.Name}.{member.Name}: a group's {call.Method.Name} takes the value to calculate");
                                    var (kind, operation) = call.Method.Name switch
                                    {
                                        "Sum" => (ProjectionMemberKind.Sum, QueryOperation.Sum),
                                        "Average" => (ProjectionMemberKind.Average, QueryOperation.Average),
                                        "Min" => (ProjectionMemberKind.Min, QueryOperation.Min),
                                        _ => (ProjectionMemberKind.Max, QueryOperation.Max),
                                    };
                                    members[i] = new ProjectionMember(member, kind, selector.Body, selector.Parameters[0], 0, CalculationCommon.GetResultType(operation, selector));
                                    break;
                                }
                            default:
                                throw new NotSupportedException($"{select.ReturnType.Name}.{member.Name}: {call.Method.Name} is not a group calculation; use Count, Sum, Average, Min, or Max");
                        }
                        break;
                    default:
                        throw new NotSupportedException($"{select.ReturnType.Name}.{member.Name} must be the group's Key, a part of it, Count() with or without a condition, Select(..).Distinct().Count(), or Sum, Average, Min, or Max of a value");
                }
            }
            return members;
        }

        /// <summary>
        /// Projects each element of a sequence, the way the memory engine and the event store do.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The projection's result type.</typeparam>
        /// <param name="source">The sequence to project, already filtered, ordered, and paged.</param>
        /// <param name="select">The projection, <c>new TResult { Member = ..., ... }</c>.</param>
        /// <param name="caseSensitive">Indicates if string comparisons should be case sensitive.</param>
        /// <returns>The results.</returns>
        public static IReadOnlyCollection<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, LambdaExpression select, bool caseSensitive = false)
            where TSource : class, new()
        {
            var members = select.GetProjectionMembers(null);
            var resultDetail = TypeAnalyzer<TResult>.GetTypeDetail();
            var getters = new Func<TSource, object?>[members.Length];
            //a collection member reads the related rows filtered and ordered, and builds a result from each with its own getters, and so on for the collections within each row
            CollectionBuilder?[]? collections = null;
            for (var i = 0; i < getters.Length; i++)
            {
                getters[i] = CalculationCommon.CompileBoxed<TSource>(members[i].Expression!, members[i].Parameter!, caseSensitive);
                if (members[i].Kind != ProjectionMemberKind.Collection)
                    continue;
                collections ??= new CollectionBuilder?[members.Length];
                collections[i] = new CollectionBuilder(members[i], caseSensitive);
            }

            var results = new List<TResult>();
            foreach (var model in source)
            {
                var result = resultDetail.Creator!();
                for (var i = 0; i < members.Length; i++)
                {
                    var value = getters[i](model);
                    var memberDetail = members[i].MemberDetail;
                    if (members[i].Kind == ProjectionMemberKind.Collection)
                    {
                        memberDetail.SetterBoxed!(result!, collections![i]!.Build(value));
                        continue;
                    }
                    if (value is null)
                        continue;
                    memberDetail.SetterBoxed!(result!, value.GetType() == memberDetail.Type ? value : TypeAnalyzer.Convert(value, memberDetail.Type));
                }
                results.Add(result);
            }
            return results;
        }
        //builds the results of a collection member from its related rows, already filtered and ordered, with the collections within each row built the same way
        private sealed class CollectionBuilder
        {
            private readonly ProjectionMember collection;
            private readonly ProjectionMember[] members;
            private readonly Func<object, object?>[] getters;
            private readonly CollectionBuilder?[]? collections;
            private readonly TypeDetail rowDetail;
            public CollectionBuilder(ProjectionMember collection, bool caseSensitive)
            {
                this.collection = collection;
                this.members = collection.Members!;
                this.getters = new Func<object, object?>[members.Length];
                this.rowDetail = TypeAnalyzer.GetTypeDetail(collection.Type);
                for (var j = 0; j < members.Length; j++)
                {
                    getters[j] = CalculationCommon.CompileBoxed(members[j].Expression!, members[j].Parameter!, caseSensitive);
                    if (members[j].Kind != ProjectionMemberKind.Collection)
                        continue;
                    collections ??= new CollectionBuilder?[members.Length];
                    collections[j] = new CollectionBuilder(members[j], caseSensitive);
                }
            }
            public object Build(object? related)
            {
                var rows = new List<object>();
                if (related is IEnumerable relatedModels)
                {
                    foreach (var relatedModel in relatedModels)
                    {
                        var row = rowDetail.CreatorBoxed!();
                        for (var j = 0; j < members.Length; j++)
                        {
                            var value = getters[j](relatedModel);
                            var memberDetail = members[j].MemberDetail;
                            if (members[j].Kind == ProjectionMemberKind.Collection)
                            {
                                memberDetail.SetterBoxed!(row, collections![j]!.Build(value));
                                continue;
                            }
                            if (value is null)
                                continue;
                            memberDetail.SetterBoxed!(row, value.GetType() == memberDetail.Type ? value : TypeAnalyzer.Convert(value, memberDetail.Type));
                        }
                        rows.Add(row);
                    }
                }
                return collection.CreateCollection(rows);
            }
        }

        /// <summary>
        /// Groups a sequence and projects each group, the way the memory engine and the event store do, then filters, orders, and pages the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The projection's result type.</typeparam>
        /// <param name="source">The sequence to group, already filtered.</param>
        /// <param name="groupBy">The group key expression.</param>
        /// <param name="select">The projection of a group.</param>
        /// <param name="having">An optional filter on the results.</param>
        /// <param name="order">An optional order of the results.</param>
        /// <param name="skip">The number of results to skip, or null to skip none.</param>
        /// <param name="take">The number of results to take, or null to take all.</param>
        /// <param name="caseSensitive">Indicates if string keys that differ only in case are different groups.</param>
        /// <returns>The results.</returns>
        public static IReadOnlyCollection<TResult> Group<TSource, TResult>(this IEnumerable<TSource> source, LambdaExpression groupBy, LambdaExpression select, LambdaExpression? having, QueryOrder? order, int? skip, int? take, bool caseSensitive = false)
            where TSource : class, new()
        {
            var members = select.GetProjectionMembers(groupBy);
            var resultDetail = TypeAnalyzer<TResult>.GetTypeDetail();
            var parts = CalculationCommon.GetKeyParts(groupBy);
            var getParts = new Func<TSource, object?>[parts.Length];
            for (var i = 0; i < parts.Length; i++)
                getParts[i] = CalculationCommon.CompileBoxed<TSource>(parts[i], groupBy.Parameters[0], caseSensitive);
            var getValues = new Func<TSource, object?>?[members.Length];
            for (var i = 0; i < members.Length; i++)
            {
                if (members[i].Expression is not null)
                    getValues[i] = CalculationCommon.CompileBoxed<TSource>(members[i].Expression!, members[i].Parameter!, caseSensitive);
            }

            var groups = new Dictionary<object?[], List<TSource>>(new CalculationCommon.KeyComparer(caseSensitive));
            var keys = new List<object?[]>();
            foreach (var model in source)
            {
                var key = new object?[parts.Length];
                for (var i = 0; i < key.Length; i++)
                    key[i] = getParts[i](model);
                if (!groups.TryGetValue(key, out var group))
                {
                    group = new List<TSource>();
                    groups.Add(key, group);
                    keys.Add(key);
                }
                group.Add(model);
            }

            var built = new List<TResult>(keys.Count);
            foreach (var key in keys)
            {
                var group = groups[key];
                var result = resultDetail.Creator!();
                for (var c = 0; c < members.Length; c++)
                {
                    var member = members[c];
                    object? value;
                    switch (member.Kind)
                    {
                        case ProjectionMemberKind.KeyPart:
                            value = key[member.KeyPart];
                            break;
                        case ProjectionMemberKind.Count:
                            value = (long)group.Count;
                            break;
                        case ProjectionMemberKind.CountIf:
                            {
                                var count = 0L;
                                foreach (var model in group)
                                    if ((bool)getValues[c]!(model)!)
                                        count++;
                                value = count;
                                break;
                            }
                        default:
                            {
                                var values = new List<object?>(group.Count);
                                foreach (var model in group)
                                    values.Add(getValues[c]!(model));
                                var operation = member.Kind switch
                                {
                                    ProjectionMemberKind.CountDistinct => QueryOperation.CountDistinct,
                                    ProjectionMemberKind.Sum => QueryOperation.Sum,
                                    ProjectionMemberKind.Average => QueryOperation.Average,
                                    ProjectionMemberKind.Min => QueryOperation.Min,
                                    _ => QueryOperation.Max,
                                };
                                value = CalculationCommon.Aggregate(operation, values, member.Expression!.Type);
                                break;
                            }
                    }
                    if (value is null)
                        continue;
                    var memberDetail = member.MemberDetail;
                    memberDetail.SetterBoxed!(result!, value.GetType() == memberDetail.Type ? value : TypeAnalyzer.Convert(value, memberDetail.Type));
                }
                built.Add(result);
            }

            if (having is null && order is null && !skip.HasValue && !take.HasValue)
                return built;

            IEnumerable<TResult> results = built;
            if (having is not null)
            {
                results = results.Where(((Expression<Func<TResult, bool>>)MemoryExpressionRewriter.Rewrite(having, caseSensitive)).Compile());
            }
            if (order is not null)
            {
                IOrderedEnumerable<TResult>? ordered = null;
                foreach (var orderExpression in order.OrderExpressions)
                {
                    var parameter = Expression.Parameter(typeof(TResult), "result");
                    var lambda = MemoryExpressionRewriter.Rewrite(orderExpression.Expression, caseSensitive);
                    var body = Expression.Convert(Expression.Invoke(lambda, parameter), typeof(object));
                    var key = Expression.Lambda<Func<TResult, object?>>(body, parameter).Compile();
                    var comparer = Comparer<object?>.Default;
                    if (ordered is null)
                        ordered = orderExpression.Descending ? results.OrderByDescending(key, comparer) : results.OrderBy(key, comparer);
                    else
                        ordered = orderExpression.Descending ? ordered.ThenByDescending(key, comparer) : ordered.ThenBy(key, comparer);
                }
                results = ordered ?? results;
            }
            if (skip.HasValue)
                results = results.Skip(skip.Value);
            if (take.HasValue)
                results = results.Take(take.Value);
            return results.ToList();
        }

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

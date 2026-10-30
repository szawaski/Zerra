// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Repository.PostgreSql;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test.PostgreSql
{
    // PostgreSQL generates lowercase unquoted identifiers: testtypes.keya
    // FROM uses a space before the identifier: FROM testtypes
    public class LinqPostgreSqlConverterTests : BaseLinqSqlConverterTests
    {
        protected override string ConvertToSql(QueryOperation select, LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail)
            => LinqPostgreSqlConverter.Convert(select, where, order, skip, take, graph, modelDetail);

        protected override string ConvertCalculationToSql(QueryOperation operation, LambdaExpression? where, LambdaExpression? groupBy, LambdaExpression? value, ModelDetail modelDetail)
            => Zerra.Repository.PostgreSql.LinqPostgreSqlConverter.ConvertCalculation(operation, where, groupBy, value, modelDetail);

        protected override string ConvertPersistWhereToSql(LambdaExpression where, ModelDetail modelDetail)
            => Zerra.Repository.PostgreSql.LinqPostgreSqlConverter.ConvertPersistWhere(where, modelDetail);

        protected override string ConvertPersistUpdateToSql(ProjectionMember[] sets, LambdaExpression where, ModelDetail modelDetail)
            => Zerra.Repository.PostgreSql.LinqPostgreSqlConverter.ConvertPersistUpdate(sets, where, modelDetail);

        protected override string ConvertSelectToSql(ProjectionMember[] members, LambdaExpression? where, QueryOrder? order, int? skip, int? take, ModelDetail modelDetail)
            => Zerra.Repository.PostgreSql.LinqPostgreSqlConverter.ConvertSelect(members, where, order, skip, take, modelDetail);

        protected override string ConvertSelectElementsToSql(ProjectionMember collection, LambdaExpression where, ModelDetail relatedDetail)
            => Zerra.Repository.PostgreSql.LinqPostgreSqlConverter.ConvertSelectElements(collection, where, relatedDetail);

        protected override string ConvertGroupToSql(LambdaExpression groupBy, ProjectionMember[] members, LambdaExpression? where, LambdaExpression? having, QueryOrder? order, int? skip, int? take, ModelDetail modelDetail, Type resultType)
            => Zerra.Repository.PostgreSql.LinqPostgreSqlConverter.ConvertGroup(groupBy, members, where, having, order, skip, take, modelDetail, resultType);

        protected override SqlDialectExpectations GetDialect() => new()
        {
            TableQuoteOpen = "",
            TableQuoteClose = "",
            ColumnQuoteOpen = "",
            ColumnQuoteClose = "",
            FromSpaceBeforeQuote = true,
            LowercaseIdentifiers = true,
            StringPrefix = "",
            HasTopClause = false,
            HasLimitClause = true,
            HasOffsetFetch = true,
            HasLimitOffset = false,
            BooleanTrue = "TRUE",
            BooleanFalse = "FALSE",
        };
    }
}

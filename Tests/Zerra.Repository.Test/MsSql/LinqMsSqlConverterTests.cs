// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Repository.MsSql;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test.MsSql
{
    // MsSql generates bracket-quoted identifiers: [TestTypes].[KeyA]
    // FROM has no space before the bracket: FROM[TestTypes]
    public class LinqMsSqlConverterTests : BaseLinqSqlConverterTests
    {
        protected override string ConvertToSql(QueryOperation select, LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail)
            => LinqMsSqlConverter.Convert(select, where, order, skip, take, graph, modelDetail);

        protected override string ConvertCalculationToSql(QueryOperation operation, LambdaExpression? where, LambdaExpression? groupBy, LambdaExpression? value, ModelDetail modelDetail)
            => Zerra.Repository.MsSql.LinqMsSqlConverter.ConvertCalculation(operation, where, groupBy, value, modelDetail);

        protected override string ConvertPersistWhereToSql(LambdaExpression where, ModelDetail modelDetail)
            => Zerra.Repository.MsSql.LinqMsSqlConverter.ConvertPersistWhere(where, modelDetail);

        protected override string ConvertPersistUpdateToSql(ProjectionMember[] sets, LambdaExpression where, ModelDetail modelDetail)
            => Zerra.Repository.MsSql.LinqMsSqlConverter.ConvertPersistUpdate(sets, where, modelDetail);

        protected override string ConvertSelectToSql(ProjectionMember[] members, LambdaExpression? where, QueryOrder? order, int? skip, int? take, ModelDetail modelDetail)
            => Zerra.Repository.MsSql.LinqMsSqlConverter.ConvertSelect(members, where, order, skip, take, modelDetail);

        protected override string ConvertSelectElementsToSql(ProjectionMember collection, LambdaExpression where, ModelDetail relatedDetail)
            => Zerra.Repository.MsSql.LinqMsSqlConverter.ConvertSelectElements(collection, where, relatedDetail);

        protected override string ConvertGroupToSql(LambdaExpression groupBy, ProjectionMember[] members, LambdaExpression? where, LambdaExpression? having, QueryOrder? order, int? skip, int? take, ModelDetail modelDetail, Type resultType)
            => Zerra.Repository.MsSql.LinqMsSqlConverter.ConvertGroup(groupBy, members, where, having, order, skip, take, modelDetail, resultType);

        protected override SqlDialectExpectations GetDialect() => new()
        {
            TableQuoteOpen = "[",
            TableQuoteClose = "]",
            ColumnQuoteOpen = "[",
            ColumnQuoteClose = "]",
            FromSpaceBeforeQuote = false,
            LowercaseIdentifiers = false,
            StringPrefix = "N",
            HasTopClause = true,
            HasLimitClause = false,
            HasOffsetFetch = true,
            HasLimitOffset = false,
            HasBooleanValues = false,
        };
    }
}

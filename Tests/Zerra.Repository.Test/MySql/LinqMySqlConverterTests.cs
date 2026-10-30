// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Repository.MySql;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test.MySql
{
    // MySql generates backtick-quoted identifiers: `TestTypes`.`KeyA`
    // FROM has no space before the backtick: FROM`TestTypes`
    public class LinqMySqlConverterTests : BaseLinqSqlConverterTests
    {
        protected override string ConvertToSql(QueryOperation select, LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail)
            => LinqMySqlConverter.Convert(select, where, order, skip, take, graph, modelDetail);

        protected override string ConvertCalculationToSql(QueryOperation operation, LambdaExpression? where, LambdaExpression? groupBy, LambdaExpression? value, ModelDetail modelDetail)
            => Zerra.Repository.MySql.LinqMySqlConverter.ConvertCalculation(operation, where, groupBy, value, modelDetail);

        protected override string ConvertPersistWhereToSql(LambdaExpression where, ModelDetail modelDetail)
            => Zerra.Repository.MySql.LinqMySqlConverter.ConvertPersistWhere(where, modelDetail);

        protected override string ConvertPersistUpdateToSql(ProjectionMember[] sets, LambdaExpression where, ModelDetail modelDetail)
            => Zerra.Repository.MySql.LinqMySqlConverter.ConvertPersistUpdate(sets, where, modelDetail);

        protected override string ConvertSelectToSql(ProjectionMember[] members, LambdaExpression? where, QueryOrder? order, int? skip, int? take, ModelDetail modelDetail)
            => Zerra.Repository.MySql.LinqMySqlConverter.ConvertSelect(members, where, order, skip, take, modelDetail);

        protected override string ConvertSelectElementsToSql(ProjectionMember collection, LambdaExpression where, ModelDetail relatedDetail)
            => Zerra.Repository.MySql.LinqMySqlConverter.ConvertSelectElements(collection, where, relatedDetail);

        protected override string ConvertGroupToSql(LambdaExpression groupBy, ProjectionMember[] members, LambdaExpression? where, LambdaExpression? having, QueryOrder? order, int? skip, int? take, ModelDetail modelDetail, Type resultType)
            => Zerra.Repository.MySql.LinqMySqlConverter.ConvertGroup(groupBy, members, where, having, order, skip, take, modelDetail, resultType);

        protected override SqlDialectExpectations GetDialect() => new()
        {
            TableQuoteOpen = "`",
            TableQuoteClose = "`",
            ColumnQuoteOpen = "`",
            ColumnQuoteClose = "`",
            FromSpaceBeforeQuote = false,
            LowercaseIdentifiers = false,
            StringPrefix = "",
            HasTopClause = false,
            HasLimitClause = true,
            HasOffsetFetch = false,
            HasLimitOffset = true,
        };
    }
}

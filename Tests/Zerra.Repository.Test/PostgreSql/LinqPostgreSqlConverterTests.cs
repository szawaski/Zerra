// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Repository.PostgreSql;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test
{
    // PostgreSQL generates lowercase unquoted identifiers: testtypes.keya
    // FROM uses a space before the identifier: FROM testtypes
    public class LinqPostgreSqlConverterTests : BaseLinqSqlConverterTests
    {
        protected override string ConvertToSql(QueryOperation select, Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail)
            => LinqPostgreSqlConverter.Convert(select, where, order, skip, take, graph, modelDetail);

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
        };
    }
}

// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Repository.MsSql;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test
{
    // MsSql generates bracket-quoted identifiers: [TestTypes].[KeyA]
    // FROM has no space before the bracket: FROM[TestTypes]
    public class LinqMsSqlConverterTests : BaseLinqSqlConverterTests
    {
        protected override string ConvertToSql(QueryOperation select, Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail)
            => LinqMsSqlConverter.Convert(select, where, order, skip, take, graph, modelDetail);

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
        };
    }
}

// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Repository.MySql;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test
{
    // MySql generates backtick-quoted identifiers: `TestTypes`.`KeyA`
    // FROM has no space before the backtick: FROM`TestTypes`
    public class LinqMySqlConverterTests : BaseLinqSqlConverterTests
    {
        protected override string ConvertToSql(QueryOperation select, Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail)
            => LinqMySqlConverter.Convert(select, where, order, skip, take, graph, modelDetail);

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
            HasOffsetFetch = true,
            HasLimitOffset = false,
        };
    }
}

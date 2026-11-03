// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Repository.Linq;
using Zerra.Repository.PostgreSql;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test.PostgreSql
{
    // PostgreSQL generates lowercase unquoted identifiers: testtypes.keya
    // FROM uses a space before the identifier: FROM testtypes
    public class LinqPostgreSqlConverterTests : BaseLinqSqlConverterTests
    {

        protected override string ConvertPersistWhereToSql(LambdaExpression where, ModelDetail modelDetail)
            => Zerra.Repository.PostgreSql.LinqPostgreSqlConverter.ConvertPersistWhere(where, modelDetail);

        protected override string ConvertPersistUpdateToSql(ProjectionMember[] sets, LambdaExpression where, ModelDetail modelDetail)
            => Zerra.Repository.PostgreSql.LinqPostgreSqlConverter.ConvertPersistUpdate(sets, where, modelDetail);

        protected override string ConvertLinqToSql(LinqQuery query, ModelDetail modelDetail)
            => LinqPostgreSqlConverter.ConvertLinq(query, modelDetail).Sql;

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
            HasOffsetFetch = false,
            HasLimitOffset = true,
            BooleanTrue = "TRUE",
            BooleanFalse = "FALSE",
        };
    }
}

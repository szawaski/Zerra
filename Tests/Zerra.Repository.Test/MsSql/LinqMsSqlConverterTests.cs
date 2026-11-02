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

        protected override string ConvertPersistWhereToSql(LambdaExpression where, ModelDetail modelDetail)
            => Zerra.Repository.MsSql.LinqMsSqlConverter.ConvertPersistWhere(where, modelDetail);

        protected override string ConvertPersistUpdateToSql(ProjectionMember[] sets, LambdaExpression where, ModelDetail modelDetail)
            => Zerra.Repository.MsSql.LinqMsSqlConverter.ConvertPersistUpdate(sets, where, modelDetail);

        protected override string ConvertLinqToSql(LinqQuery query, ModelDetail modelDetail)
            => LinqMsSqlConverter.ConvertLinq(query, modelDetail).Sql;

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

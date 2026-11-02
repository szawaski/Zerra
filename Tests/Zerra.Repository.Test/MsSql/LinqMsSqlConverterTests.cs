// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Globalization;
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
        {
            var statement = LinqMsSqlConverter.ConvertLinq(query, modelDetail);
            return Inline(statement.Sql, statement.Parameters);
        }

        //values are parameters, written back as their text so the shared tests read them, MsSqlParameterTests covers the parameters
        private static string Inline(string sql, List<object>? parameters)
        {
            if (parameters is null)
                return sql;
            for (var i = parameters.Count - 1; i >= 0; i--)
            {
                var text = parameters[i] switch
                {
                    string value => $"N'{value.Replace("'", "''")}'",
                    Guid value => $"'{value}'",
                    IFormattable value => value.ToString(null, CultureInfo.InvariantCulture),
                    var value => value.ToString()!,
                };
                sql = sql.Replace("@p" + i, text);
            }
            return sql;
        }

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

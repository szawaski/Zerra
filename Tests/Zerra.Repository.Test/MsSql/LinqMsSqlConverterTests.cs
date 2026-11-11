// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Globalization;
using System.Linq.Expressions;
using Zerra.Repository.Linq;
using Zerra.Repository.MsSql;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test.MsSql
{
    // MsSql generates bracket-quoted identifiers: [TestTypes].[KeyA]
    // FROM has no space before the bracket: FROM[TestTypes]
    public class LinqMsSqlConverterTests : BaseLinqSqlConverterTests
    {

        //a date compared to a datetime2 column has every digit cast to its precision, to a datetime column three, the most it takes as text
        [Xunit.Fact]
        public void DateTakesTheColumnsPrecision()
        {
            var value = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc).AddTicks(1234567);
            var modelDetail = ModelAnalyzer.GetModel(typeof(TestTypesModel));

            Expression<Func<TestTypesModel, bool>> precise = x => x.DateTimeThing == value;
            Xunit.Assert.Contains("CAST(N'2024-05-06 07:08:09.1234567' AS datetime2(6))", ConvertToSql(LinqTerminal.Sequence, precise, null, null, null, null, modelDetail));

            Expression<Func<TestTypesModel, bool>> plain = x => x.DateTimeDefaultPrecisionThing < value;
            Xunit.Assert.Contains("'2024-05-06 07:08:09.123'", ConvertToSql(LinqTerminal.Sequence, plain, null, null, null, null, modelDetail));
        }

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

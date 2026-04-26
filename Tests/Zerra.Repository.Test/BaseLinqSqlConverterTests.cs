// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test
{
    public abstract class BaseLinqSqlConverterTests
    {
        private static readonly ModelDetail testTypesModelDetail = ModelAnalyzer.GetModel(typeof(TestTypesModel));

        protected abstract string ConvertToSql(QueryOperation select, Expression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail);
        protected abstract SqlDialectExpectations GetDialect();

        #region Basic Select

        [Fact]
        public void Convert_Many_NoWhere_GeneratesSelectAll()
        {
            var d = GetDialect();
            var sql = ConvertToSql(QueryOperation.Many, null, null, null, null, null, testTypesModelDetail);

            Assert.Contains("SELECT", sql);
            Assert.Contains($"{d.Table("TestTypes")}.*", sql);
            Assert.Contains(d.From("TestTypes"), sql);
            Assert.DoesNotContain("WHERE", sql);
        }

        [Fact]
        public void Convert_Count_NoWhere_GeneratesCount()
        {
            var d = GetDialect();
            var sql = ConvertToSql(QueryOperation.Count, null, null, null, null, null, testTypesModelDetail);

            Assert.Contains("SELECT COUNT(1)", sql);
            Assert.Contains(d.From("TestTypes"), sql);
        }

        [Fact]
        public void Convert_First_NoWhere_GeneratesLimit1()
        {
            var d = GetDialect();
            var sql = ConvertToSql(QueryOperation.First, null, null, null, null, null, testTypesModelDetail);

            Assert.Contains("SELECT", sql);
            if (d.HasTopClause)
                Assert.Contains("SELECT TOP(1)", sql);
            if (d.HasLimitClause)
                Assert.Contains("LIMIT 1", sql);
        }

        [Fact]
        public void Convert_Single_NoWhere_GeneratesLimit2()
        {
            var d = GetDialect();
            var sql = ConvertToSql(QueryOperation.Single, null, null, null, null, null, testTypesModelDetail);

            Assert.Contains("SELECT", sql);
            if (d.HasTopClause)
                Assert.Contains("SELECT TOP(2)", sql);
            if (d.HasLimitClause)
                Assert.Contains("LIMIT 2", sql);
        }

        [Fact]
        public void Convert_Any_NoWhere_GeneratesAny()
        {
            var d = GetDialect();
            var sql = ConvertToSql(QueryOperation.Any, null, null, null, null, null, testTypesModelDetail);

            if (d.HasTopClause)
                Assert.Contains("SELECT TOP(1) 1", sql);
            if (d.HasLimitClause)
            {
                Assert.Contains("SELECT 1", sql);
                Assert.Contains("LIMIT 1", sql);
            }
            Assert.Contains(d.From("TestTypes"), sql);
        }

        #endregion

        #region Where Clauses

        [Fact]
        public void Convert_WhereEquals_GeneratesEquals()
        {
            var d = GetDialect();
            var testGuid = Guid.NewGuid();
            Expression<Func<TestTypesModel, bool>> where = x => x.KeyA == testGuid;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains(d.Column("TestTypes", "KeyA"), sql);
            Assert.Contains("=", sql);
            Assert.True(sql.Contains(testGuid.ToString()) || sql.Contains(testGuid.ToString("N")));
        }

        [Fact]
        public void Convert_WhereNotEquals_GeneratesNotEquals()
        {
            var d = GetDialect();
            var testGuid = Guid.NewGuid();
            Expression<Func<TestTypesModel, bool>> where = x => x.KeyA != testGuid;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains(d.Column("TestTypes", "KeyA"), sql);
            Assert.Contains("!=", sql);
        }

        [Fact]
        public void Convert_WhereEqualsNull_GeneratesIsNull()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationAKey == null;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains(d.Column("TestTypes", "RelationAKey"), sql);
            Assert.Contains("IS NULL", sql);
        }

        [Fact]
        public void Convert_WhereNotEqualsNull_GeneratesIsNotNull()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationAKey != null;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains(d.Column("TestTypes", "RelationAKey"), sql);
            Assert.Contains("IS NOT NULL", sql);
        }

        [Fact]
        public void Convert_WhereLessThan_GeneratesLessThan()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.Int32Thing < 100;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains(d.Column("TestTypes", "Int32Thing"), sql);
            Assert.Contains("<", sql);
            Assert.Contains("100", sql);
        }

        [Fact]
        public void Convert_WhereLessThanOrEquals_GeneratesLessThanOrEquals()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.Int32Thing <= 100;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains(d.Column("TestTypes", "Int32Thing"), sql);
            Assert.Contains("<=", sql);
        }

        [Fact]
        public void Convert_WhereGreaterThan_GeneratesGreaterThan()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.Int32Thing > 100;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains(d.Column("TestTypes", "Int32Thing"), sql);
            Assert.Contains(">", sql);
            Assert.Contains("100", sql);
        }

        [Fact]
        public void Convert_WhereGreaterThanOrEquals_GeneratesGreaterThanOrEquals()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.Int32Thing >= 100;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains(d.Column("TestTypes", "Int32Thing"), sql);
            Assert.Contains(">=", sql);
        }

        [Fact]
        public void Convert_WhereAnd_GeneratesAnd()
        {
            var d = GetDialect();
            var testGuid = Guid.NewGuid();
            Expression<Func<TestTypesModel, bool>> where = x => x.KeyA == testGuid && x.Int32Thing > 100;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains(d.Column("TestTypes", "KeyA"), sql);
            Assert.Contains("AND", sql);
            Assert.Contains(d.Column("TestTypes", "Int32Thing"), sql);
        }

        [Fact]
        public void Convert_WhereOr_GeneratesOr()
        {
            var d = GetDialect();
            var testGuid = Guid.NewGuid();
            Expression<Func<TestTypesModel, bool>> where = x => x.KeyA == testGuid || x.Int32Thing > 100;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains(d.Column("TestTypes", "KeyA"), sql);
            Assert.Contains("OR", sql);
            Assert.Contains(d.Column("TestTypes", "Int32Thing"), sql);
        }

        [Fact]
        public void Convert_StringContains_GeneratesLike()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.StringThing.Contains("test");
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains(d.Column("TestTypes", "StringThing"), sql);
            Assert.Contains("LIKE", sql);
            Assert.Contains("test", sql);
        }

        [Fact]
        public void Convert_ArrayContains_GeneratesIn()
        {
            var d = GetDialect();
            var testGuids = new[] { Guid.NewGuid(), Guid.NewGuid() };
            Expression<Func<TestTypesModel, bool>> where = x => testGuids.Contains(x.KeyA);
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains(d.Column("TestTypes", "KeyA"), sql);
            Assert.Contains("IN", sql);
            Assert.True(sql.Contains(testGuids[0].ToString()) || sql.Contains(testGuids[0].ToString("N")));
            Assert.True(sql.Contains(testGuids[1].ToString()) || sql.Contains(testGuids[1].ToString("N")));
        }

        [Fact]
        public void Convert_NullableValue_HandlesNullable()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.Int32NullableThing.Value > 100;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains(d.Column("TestTypes", "Int32NullableThing"), sql);
            Assert.Contains(">", sql);
        }

        [Fact]
        public void Convert_TernaryConditional_GeneratesCaseWhen()
        {
            Expression<Func<TestTypesModel, bool>> where = x => (x.Int32Thing > 100 ? x.StringThing : "default") == "test";
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains("CASE WHEN", sql);
            Assert.Contains("THEN", sql);
            Assert.Contains("ELSE", sql);
            Assert.Contains("END", sql);
        }

        [Fact]
        public void Convert_StringEscapesSingleQuotes()
        {
            Expression<Func<TestTypesModel, bool>> where = x => x.StringThing == "test's";
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("test''s", sql);
        }

        #endregion

        #region Order By

        [Fact]
        public void Convert_OrderByAscending_GeneratesOrderBy()
        {
            var d = GetDialect();
            var order = QueryOrder<TestTypesModel>.Create(x => x.Int32Thing, false);
            var sql = ConvertToSql(QueryOperation.Many, null, order, null, null, null, testTypesModelDetail);

            Assert.Contains("ORDER BY", sql);
            Assert.Contains(d.Column("TestTypes", "Int32Thing"), sql);
            Assert.DoesNotContain("DESC", sql);
        }

        [Fact]
        public void Convert_OrderByDescending_GeneratesOrderByDesc()
        {
            var d = GetDialect();
            var order = QueryOrder<TestTypesModel>.Create(x => x.Int32Thing, true);
            var sql = ConvertToSql(QueryOperation.Many, null, order, null, null, null, testTypesModelDetail);

            Assert.Contains("ORDER BY", sql);
            Assert.Contains(d.Column("TestTypes", "Int32Thing"), sql);
            Assert.Contains("DESC", sql);
        }

        #endregion

        #region Skip and Take

        [Fact]
        public void Convert_Skip_GeneratesOffset()
        {
            var d = GetDialect();
            var sql = ConvertToSql(QueryOperation.Many, null, null, 10, null, null, testTypesModelDetail);

            Assert.Contains("OFFSET 10", sql);
        }

        [Fact]
        public void Convert_Take_GeneratesLimit()
        {
            var d = GetDialect();
            var sql = ConvertToSql(QueryOperation.Many, null, null, null, 20, null, testTypesModelDetail);

            if (d.HasOffsetFetch)
                Assert.Contains("FETCH NEXT 20 ROWS ONLY", sql);
            if (d.HasLimitOffset)
                Assert.Contains("LIMIT 20", sql);
        }

        [Fact]
        public void Convert_SkipAndTake_GeneratesOffsetAndLimit()
        {
            var d = GetDialect();
            var sql = ConvertToSql(QueryOperation.Many, null, null, 10, 20, null, testTypesModelDetail);

            Assert.Contains("OFFSET 10", sql);
            if (d.HasOffsetFetch)
                Assert.Contains("FETCH NEXT 20 ROWS ONLY", sql);
            if (d.HasLimitOffset)
                Assert.Contains("LIMIT 20", sql);
        }

        #endregion

        #region Graph

        [Fact]
        public void Convert_GraphAllMembers_GeneratesWildcard()
        {
            var d = GetDialect();
            var graph = new Graph<TestTypesModel>(true);
            var sql = ConvertToSql(QueryOperation.Many, null, null, null, null, graph, testTypesModelDetail);

            Assert.Contains($"{d.Table("TestTypes")}.*", sql);
        }

        [Fact]
        public void Convert_GraphSpecificProperties_SelectsColumns()
        {
            var d = GetDialect();
            var graph = new Graph<TestTypesModel>(x => x.KeyA, x => x.StringThing);
            var sql = ConvertToSql(QueryOperation.Many, null, null, null, null, graph, testTypesModelDetail);

            Assert.Contains(d.Column("TestTypes", "KeyA"), sql);
            Assert.Contains(d.Column("TestTypes", "StringThing"), sql);
            Assert.DoesNotContain(".*", sql);
        }

        #endregion
    }
}

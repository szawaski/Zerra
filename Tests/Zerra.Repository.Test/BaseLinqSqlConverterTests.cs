// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Globalization;
using System.Linq.Expressions;
using Xunit;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test
{
    public abstract class BaseLinqSqlConverterTests
    {
        private static readonly ModelDetail testTypesModelDetail = ModelAnalyzer.GetModel(typeof(TestTypesModel));
        private static readonly ModelDetail testRelationsModelDetail = ModelAnalyzer.GetModel(typeof(TestRelationsModel));

        protected abstract string ConvertToSql(QueryOperation select, LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, ModelDetail modelDetail);
        protected abstract SqlDialectExpectations GetDialect();
        protected abstract string ConvertCalculationToSql(QueryOperation operation, LambdaExpression? where, LambdaExpression? groupBy, LambdaExpression? value, ModelDetail modelDetail);
        protected abstract string ConvertPersistWhereToSql(LambdaExpression where, ModelDetail modelDetail);
        protected abstract string ConvertPersistUpdateToSql(ProjectionMember[] sets, LambdaExpression where, ModelDetail modelDetail);
        protected abstract string ConvertSelectToSql(ProjectionMember[] members, LambdaExpression? where, QueryOrder? order, int? skip, int? take, ModelDetail modelDetail);
        protected abstract string ConvertSelectElementsToSql(ProjectionMember collection, LambdaExpression where, ModelDetail relatedDetail);
        protected abstract string ConvertGroupToSql(LambdaExpression groupBy, ProjectionMember[] members, LambdaExpression? where, LambdaExpression? having, QueryOrder? order, int? skip, int? take, ModelDetail modelDetail, Type resultType);

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
        public void Convert_WhereNumbers_IgnoresCurrentCulture()
        {
            var original = CultureInfo.CurrentCulture;
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NumberDecimalSeparator = ",";
            culture.NumberFormat.NegativeSign = "−";
            CultureInfo.CurrentCulture = culture;
            try
            {
                var decimalValue = -1.5m;
                var doubleValue = 2.25;
                var singleValue = 0.5f;
                var int32Value = -42;
                Expression<Func<TestTypesModel, bool>> where = x => x.DecimalThing > decimalValue && x.DoubleThing < doubleValue && x.SingleThing == singleValue && x.Int32Thing == int32Value;
                var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

                Assert.Contains("-1.5", sql);
                Assert.Contains("2.25", sql);
                Assert.Contains("0.5", sql);
                Assert.Contains("-42", sql);
                Assert.DoesNotContain(",5", sql);
                Assert.DoesNotContain("−", sql);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

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
        public void Convert_WhereBoolMember_GeneratesComparison()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.BooleanThing;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains($"{d.Column("TestTypes", "BooleanThing")}={d.BooleanTrue}", sql);
        }

        [Fact]
        public void Convert_WhereNotBoolMember_GeneratesInvertedComparison()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => !x.BooleanThing;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains($"{d.Column("TestTypes", "BooleanThing")}={d.BooleanFalse}", sql);
        }

        [Fact]
        public void Convert_WhereAndNotBoolMember_KeepsNegation()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.Int32Thing > 0 && !x.BooleanThing;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("AND", sql);
            Assert.Contains($"{d.Column("TestTypes", "BooleanThing")}={d.BooleanFalse}", sql);
        }

        [Fact]
        public void Convert_WhereNotAndBoolMember_InvertsEachSide()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => !(x.Int32Thing > 0 && x.BooleanThing);
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("OR", sql);
            Assert.Contains("<=", sql);
            Assert.Contains($"{d.Column("TestTypes", "BooleanThing")}={d.BooleanFalse}", sql);
        }

        [Fact]
        public void Convert_WhereBoolEqualsValue_GeneratesBooleanLiteral()
        {
            var d = GetDialect();
            var value = true;
            Expression<Func<TestTypesModel, bool>> where = x => x.BooleanThing == value;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains($"({d.Column("TestTypes", "BooleanThing")})=({d.BooleanTrue})", sql);
        }

        [Fact]
        public void Convert_WhereNullableBoolValue_GeneratesComparison()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => !x.BooleanNullableThing.Value;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains($"{d.Column("TestTypes", "BooleanNullableThing")}={d.BooleanFalse}", sql);
        }

        [Fact]
        public void Convert_WhereNotCapturedBoolInAnd_InvertsConstant()
        {
            var value = true;
            Expression<Func<TestTypesModel, bool>> where = x => !(x.Int32Thing > 0 && value);
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("1=0", sql);
            Assert.DoesNotContain("1=1", sql);
        }

        [Fact]
        public void Convert_OrderByBoolMember_GeneratesColumnOnly()
        {
            var d = GetDialect();
            var sql = ConvertToSql(QueryOperation.Many, null, QueryOrder<TestTypesModel>.Create(x => x.BooleanThing), null, null, null, testTypesModelDetail);

            Assert.Contains($"ORDER BY({d.Column("TestTypes", "BooleanThing")})", sql);
            Assert.DoesNotContain($"{d.Column("TestTypes", "BooleanThing")}=", sql);
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

        #region Related Model (Join) Where Clauses

        [Fact]
        public void Convert_WhereOnOneToOneRelation_GeneratesJoinAndWhere()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationA.SomeValue == "hello";
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains("JOIN", sql);
            Assert.Contains(d.Column("TestRelations", "SomeValue"), sql);
            Assert.Contains("hello", sql);
        }

        [Fact]
        public void Convert_WhereOnOneToOneRelationNotEquals_GeneratesJoinAndNotEquals()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationA.SomeValue != "hello";
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains("JOIN", sql);
            Assert.Contains(d.Column("TestRelations", "SomeValue"), sql);
            Assert.Contains("!=", sql);
        }

        [Fact]
        public void Convert_WhereOnOneToOneRelationNull_GeneratesJoinAndIsNull()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationA.SomeValue == null;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains("JOIN", sql);
            Assert.Contains(d.Column("TestRelations", "SomeValue"), sql);
            Assert.Contains("IS NULL", sql);
        }

        [Fact]
        public void Convert_WhereOnOneToOneRelationKey_GeneratesJoinAndWhere()
        {
            var d = GetDialect();
            var testGuid = 7;
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationA.RelationAKey == testGuid;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains("JOIN", sql);
            Assert.Contains(d.Column("TestRelations", "RelationAKey"), sql);
        }

        [Fact]
        public void Convert_WhereOnOneToManyRelation_GeneratesSubqueryAndWhere()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationB.Any(r => r.SomeValue == "hello");
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.DoesNotContain("JOIN", sql);
            Assert.Contains("ANY", sql);
            Assert.Contains("SELECT", sql);
            Assert.Contains(d.Column("TestRelations", "SomeValue"), sql);
            Assert.Contains("hello", sql);
        }

        [Fact]
        public void Convert_WhereOnOneToManyRelationNotEquals_GeneratesSubqueryAndNotEquals()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationB.Any(r => r.SomeValue != "hello");
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.DoesNotContain("JOIN", sql);
            Assert.Contains("ANY", sql);
            Assert.Contains("SELECT", sql);
            Assert.Contains(d.Column("TestRelations", "SomeValue"), sql);
            Assert.Contains("!=", sql);
        }

        [Fact]
        public void Convert_WhereOnOneToManyRelationNull_GeneratesSubqueryAndIsNull()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationB.Any(r => r.SomeValue == null);
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.DoesNotContain("JOIN", sql);
            Assert.Contains("ANY", sql);
            Assert.Contains("SELECT", sql);
            Assert.Contains(d.Column("TestRelations", "SomeValue"), sql);
            Assert.Contains("IS NULL", sql);
        }

        [Fact]
        public void Convert_WhereOnBothDirectAndRelationProperty_GeneratesJoinAndAnd()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.Int32Thing > 5 && x.RelationA.SomeValue == "hello";
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains("JOIN", sql);
            Assert.Contains("AND", sql);
            Assert.Contains(d.Column("TestTypes", "Int32Thing"), sql);
            Assert.Contains(d.Column("TestRelations", "SomeValue"), sql);
        }

        [Fact]
        public void Convert_WhereOnOneToOneRelationStringContains_GeneratesJoinAndLike()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationA.SomeValue.Contains("hello");
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("WHERE", sql);
            Assert.Contains("JOIN", sql);
            Assert.Contains(d.Column("TestRelations", "SomeValue"), sql);
            Assert.Contains("LIKE", sql);
            Assert.Contains("hello", sql);
        }

        #endregion

        #region Functions

        [Fact]
        public void Convert_StringStartsWith_EscapesWildcards()
        {
            Expression<Func<TestTypesModel, bool>> where = x => x.StringThing.StartsWith("a_b%");
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("LIKE", sql);
            Assert.Contains("a!_b!%%'", sql);
            Assert.Contains("ESCAPE '!'", sql);
        }

        [Fact]
        public void Convert_NotStringEndsWith_GeneratesNotLike()
        {
            Expression<Func<TestTypesModel, bool>> where = x => !x.StringThing.EndsWith("b");
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("NOT LIKE", sql);
            Assert.Contains("%b'", sql);
        }

        [Fact]
        public void Convert_Coalesce_GeneratesCoalesce()
        {
            Expression<Func<TestTypesModel, bool>> where = x => (x.Int32NullableThing ?? 5) > 3;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("COALESCE(", sql);
        }

        [Fact]
        public void Convert_HasValue_GeneratesIsNotNull()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.Int32NullableThing.HasValue;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains($"{d.Column("TestTypes", "Int32NullableThing")} IS NOT NULL", sql);
        }

        [Fact]
        public void Convert_NotHasValue_GeneratesIsNull()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => !x.Int32NullableThing.HasValue;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains($"{d.Column("TestTypes", "Int32NullableThing")} IS NULL", sql);
        }

        [Fact]
        public void Convert_CallValue_NotComparedToNull()
        {
            Expression<Func<TestTypesModel, bool>> where = x => x.KeyA != Guid.NewGuid();
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.DoesNotContain("NULL", sql);
            Assert.Contains("!=", sql);
        }

        [Fact]
        public void Convert_EnumValue_GeneratesNumber()
        {
            var day = DayOfWeek.Friday;
            Expression<Func<TestTypesModel, bool>> where = x => x.Int32Thing == (int)day;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("(5)", sql);
            Assert.DoesNotContain("Friday", sql);
        }

        [Fact]
        public void Convert_InlineArrayContains_GeneratesIn()
        {
            Expression<Func<TestTypesModel, bool>> where = x => new[] { 1, 2 }.Contains(x.Int32Thing);
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("IN", sql);
            Assert.Contains("(1,2)", sql);
        }

        [Fact]
        public void Convert_BitwiseAnd_GeneratesBitwiseOperator()
        {
            Expression<Func<TestTypesModel, bool>> where = x => (x.Int32Thing & 4) != 0;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains(")&(4)", sql);
            Assert.DoesNotContain("AND", sql);
        }

        [Fact]
        public void Convert_NegateExpression_KeepsBrackets()
        {
            Expression<Func<TestTypesModel, bool>> where = x => -(x.Int32Thing + 1) == 4;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("-((", sql);
        }

        [Fact]
        public void Convert_StringLength_GeneratesFunction()
        {
            Expression<Func<TestTypesModel, bool>> where = x => x.StringThing.Length > 3;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("LEN", sql.ToUpperInvariant());
        }

        [Fact]
        public void Convert_MathAbs_GeneratesAbs()
        {
            Expression<Func<TestTypesModel, bool>> where = x => Math.Abs(x.Int32Thing) > 3;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("ABS((", sql);
        }

        [Fact]
        public void Convert_RelationCountWithoutCondition_MatchesParent()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationB.Count() > 1;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("SELECT COUNT(1)", sql);
            Assert.Contains($"{d.Column("TestTypes", "KeyA")}={d.Column("TestRelations", "RelationBKey")}", sql);
        }

        [Fact]
        public void Convert_RelationAll_LooksForFailingRow()
        {
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationB.All(r => r.SomeValue == "a");
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.StartsWith("NOT ", sql.Substring(sql.IndexOf("WHERE") + 6).TrimStart());
            Assert.Contains("!=", sql);
            Assert.DoesNotContain("ALL(", sql);
        }

        [Fact]
        public void Convert_RelationSum_GeneratesSubquery()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationB.Sum(r => r.RelationAKey) > 1;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("COALESCE(SUM(", sql);
            Assert.Contains(d.Column("TestRelations", "RelationAKey"), sql);
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

        #region Date Parts Of Mixed Expressions

        //a date part of an expression that mixes the model with captured values is written as a function of the whole expression,
        //so the captured values are written as plain values
        private static readonly DateTime partDate = new(2021, 2, 3, 4, 5, 6, 7, DateTimeKind.Utc);
        private static readonly DateTimeOffset partOffset = new(2021, 2, 3, 4, 5, 6, 7, TimeSpan.FromHours(2));
        private static readonly DateOnly partDateOnly = new(2021, 2, 3);
        private static readonly TimeOnly partTimeOnly = new(4, 5, 6, 7);
        private static readonly TimeSpan partTimeSpan = new(0, 4, 5, 6, 7);

        private static readonly Dictionary<string, Expression<Func<TestTypesModel, bool>>> datePartExpressions = new()
        {
            ["DateTime.Year"] = x => (x.BooleanThing ? x.DateTimeThing : partDate).Year == 1,
            ["DateTime.Month"] = x => (x.BooleanThing ? x.DateTimeThing : partDate).Month == 1,
            ["DateTime.Day"] = x => (x.BooleanThing ? x.DateTimeThing : partDate).Day == 1,
            ["DateTime.Hour"] = x => (x.BooleanThing ? x.DateTimeThing : partDate).Hour == 1,
            ["DateTime.Minute"] = x => (x.BooleanThing ? x.DateTimeThing : partDate).Minute == 1,
            ["DateTime.Second"] = x => (x.BooleanThing ? x.DateTimeThing : partDate).Second == 1,
            ["DateTime.Millisecond"] = x => (x.BooleanThing ? x.DateTimeThing : partDate).Millisecond == 1,
            ["DateTime.DayOfYear"] = x => (x.BooleanThing ? x.DateTimeThing : partDate).DayOfYear == 1,
            ["DateTime.DayOfWeek"] = x => (x.BooleanThing ? x.DateTimeThing : partDate).DayOfWeek == DayOfWeek.Monday,
            ["DateTime.Coalesce.Year"] = x => (x.DateTimeNullableThing ?? partDate).Year == 1,
            ["DateTime.Values.Year"] = x => (x.BooleanThing ? partDate : partDate.AddYears(1)).Year == 1,
            ["DateTimeOffset.Year"] = x => (x.BooleanThing ? x.DateTimeOffsetThing : partOffset).Year == 1,
            ["DateTimeOffset.Month"] = x => (x.BooleanThing ? x.DateTimeOffsetThing : partOffset).Month == 1,
            ["DateTimeOffset.Day"] = x => (x.BooleanThing ? x.DateTimeOffsetThing : partOffset).Day == 1,
            ["DateTimeOffset.Hour"] = x => (x.BooleanThing ? x.DateTimeOffsetThing : partOffset).Hour == 1,
            ["DateTimeOffset.Minute"] = x => (x.BooleanThing ? x.DateTimeOffsetThing : partOffset).Minute == 1,
            ["DateTimeOffset.Second"] = x => (x.BooleanThing ? x.DateTimeOffsetThing : partOffset).Second == 1,
            ["DateTimeOffset.Millisecond"] = x => (x.BooleanThing ? x.DateTimeOffsetThing : partOffset).Millisecond == 1,
            ["DateTimeOffset.DayOfYear"] = x => (x.BooleanThing ? x.DateTimeOffsetThing : partOffset).DayOfYear == 1,
            ["DateTimeOffset.DayOfWeek"] = x => (x.BooleanThing ? x.DateTimeOffsetThing : partOffset).DayOfWeek == DayOfWeek.Monday,
            ["DateOnly.Year"] = x => (x.BooleanThing ? x.DateOnlyThing : partDateOnly).Year == 1,
            ["DateOnly.Month"] = x => (x.BooleanThing ? x.DateOnlyThing : partDateOnly).Month == 1,
            ["DateOnly.Day"] = x => (x.BooleanThing ? x.DateOnlyThing : partDateOnly).Day == 1,
            ["DateOnly.DayOfYear"] = x => (x.BooleanThing ? x.DateOnlyThing : partDateOnly).DayOfYear == 1,
            ["DateOnly.DayOfWeek"] = x => (x.BooleanThing ? x.DateOnlyThing : partDateOnly).DayOfWeek == DayOfWeek.Monday,
            ["TimeOnly.Hour"] = x => (x.BooleanThing ? x.TimeOnlyThing : partTimeOnly).Hour == 1,
            ["TimeOnly.Minute"] = x => (x.BooleanThing ? x.TimeOnlyThing : partTimeOnly).Minute == 1,
            ["TimeOnly.Second"] = x => (x.BooleanThing ? x.TimeOnlyThing : partTimeOnly).Second == 1,
            ["TimeOnly.Millisecond"] = x => (x.BooleanThing ? x.TimeOnlyThing : partTimeOnly).Millisecond == 1,
            ["TimeSpan.Hours"] = x => (x.BooleanThing ? x.TimeSpanThing : partTimeSpan).Hours == 1,
            ["TimeSpan.Minutes"] = x => (x.BooleanThing ? x.TimeSpanThing : partTimeSpan).Minutes == 1,
            ["TimeSpan.Seconds"] = x => (x.BooleanThing ? x.TimeSpanThing : partTimeSpan).Seconds == 1,
            ["TimeSpan.Milliseconds"] = x => (x.BooleanThing ? x.TimeSpanThing : partTimeSpan).Milliseconds == 1,
            ["TimeSpan.TotalHours"] = x => (x.BooleanThing ? x.TimeSpanThing : partTimeSpan).TotalHours > 1,
            ["TimeSpan.TotalMinutes"] = x => (x.BooleanThing ? x.TimeSpanThing : partTimeSpan).TotalMinutes > 1,
            ["TimeSpan.TotalSeconds"] = x => (x.BooleanThing ? x.TimeSpanThing : partTimeSpan).TotalSeconds > 1,
            ["TimeSpan.TotalMilliseconds"] = x => (x.BooleanThing ? x.TimeSpanThing : partTimeSpan).TotalMilliseconds > 1,
        };

        public static TheoryData<string> DatePartNames() => new(datePartExpressions.Keys);

        [Theory]
        [MemberData(nameof(DatePartNames))]
        public void Convert_DatePartOfMixedExpression_WritesFunctionOfExpression(string name)
        {
            var sql = ConvertToSql(QueryOperation.Many, datePartExpressions[name], null, null, null, null, testTypesModelDetail);

            //the part is taken by the SQL function, the captured value is written whole rather than as its part
            Assert.DoesNotContain("'1'", sql);
        }

        [Fact]
        public void Convert_UnsupportedPartOfMixedExpression_Throws()
        {
            _ = Assert.Throws<NotSupportedException>(() => ConvertToSql(QueryOperation.Many, (Expression<Func<TestTypesModel, bool>>)(x => (x.BooleanThing ? x.DateTimeThing : partDate).Ticks == 1), null, null, null, null, testTypesModelDetail));
        }

        #endregion

        #region Calculations

        private static readonly Expression<Func<TestTypesModel, int>> int32Value = x => x.Int32Thing;
        private static readonly Expression<Func<TestTypesModel, decimal>> decimalValue = x => x.DecimalThing;
        private static readonly Expression<Func<TestTypesModel, bool>> boolKey = x => x.BooleanThing;
        private static readonly Expression<Func<TestTypesModel, bool>> calculationWhere = x => x.Int32Thing > 1;
        private static LambdaExpression Key<TKey>(Expression<Func<TestTypesModel, TKey>> groupBy) => groupBy;

        [Fact]
        public void ConvertCalculation_Sum_AggregatesAnInnerSelect()
        {
            var d = GetDialect();
            var sql = ConvertCalculationToSql(QueryOperation.Sum, null, null, int32Value, testTypesModelDetail);

            Assert.StartsWith("SELECT COALESCE(SUM(", sql);
            Assert.Contains("FROM (", sql);
            Assert.Contains(d.From("TestTypes"), sql);
            Assert.Contains(d.Column("TestTypes", "Int32Thing"), sql);
            Assert.DoesNotContain("GROUP BY", sql);
            Assert.DoesNotContain("WHERE", sql);
        }

        [Fact]
        public void ConvertCalculation_GroupedCount_GroupsAndFilters()
        {
            var d = GetDialect();
            var sql = ConvertCalculationToSql(QueryOperation.Count, calculationWhere, boolKey, null, testTypesModelDetail);

            Assert.Contains("COUNT(1)", sql);
            Assert.Contains("WHERE", sql);
            Assert.Contains("GROUP BY", sql);
            Assert.Contains(d.Column("TestTypes", "BooleanThing"), sql);
            Assert.DoesNotContain("SUM(", sql);
        }

        [Fact]
        public void ConvertCalculation_Distinct_SelectsTheDifferentKeys()
        {
            var sql = ConvertCalculationToSql(QueryOperation.Distinct, calculationWhere, boolKey, null, testTypesModelDetail);

            Assert.Contains("SELECT DISTINCT", sql);
            Assert.Contains("WHERE", sql);
            Assert.DoesNotContain("GROUP BY", sql);
            Assert.DoesNotContain("COUNT(", sql);
        }

        [Fact]
        public void ConvertCalculation_CountDistinct_CountsTheDifferentValues()
        {
            Expression<Func<TestTypesModel, int>> value = x => x.Int32Thing;
            var sql = ConvertCalculationToSql(QueryOperation.CountDistinct, calculationWhere, null, value, testTypesModelDetail);
            Assert.Contains("COUNT(DISTINCT", sql);
            Assert.Contains("WHERE", sql);
            Assert.DoesNotContain("GROUP BY", sql);

            var grouped = ConvertCalculationToSql(QueryOperation.CountDistinct, null, boolKey, value, testTypesModelDetail);
            Assert.Contains("COUNT(DISTINCT", grouped);
            Assert.Contains("GROUP BY", grouped);
        }

        [Fact]
        public void ConvertGroup_ConditionalAndDistinctCounts()
        {
            Expression<Func<IGrouping<bool, TestTypesModel>, TypesProjection>> select = g => new TypesProjection { Flagged = g.Count(x => x.Int32Thing > 1), Distinct = g.Select(x => x.StringThing).Distinct().Count() };
            var sql = ConvertGroupToSql(boolKey, select.GetProjectionMembers(boolKey), null, null, null, null, null, testTypesModelDetail, typeof(TypesProjection));

            Assert.True(sql.Contains("FILTER (WHERE") || sql.Contains("CASE WHEN"), sql);
            Assert.Contains("COUNT(DISTINCT", sql);
            Assert.Contains("GROUP BY", sql);
        }

        [Fact]
        public void ConvertCalculation_AverageOfIntegers_CastsToFloatingPoint()
        {
            var sql = ConvertCalculationToSql(QueryOperation.Average, null, null, int32Value, testTypesModelDetail);
            Assert.Contains("AVG(CAST(", sql);
        }

        [Fact]
        public void ConvertCalculation_AverageOfDecimals_DoesNotCast()
        {
            var sql = ConvertCalculationToSql(QueryOperation.Average, null, null, decimalValue, testTypesModelDetail);
            Assert.Contains("AVG(", sql);
            Assert.DoesNotContain("CAST(", sql);
        }

        [Fact]
        public void ConvertCalculation_MinAndMax()
        {
            Assert.Contains("MIN(", ConvertCalculationToSql(QueryOperation.Min, null, null, int32Value, testTypesModelDetail));
            Assert.Contains("MAX(", ConvertCalculationToSql(QueryOperation.Max, null, boolKey, int32Value, testTypesModelDetail));
        }

        [Fact]
        public void ConvertCalculation_TupleKey_OneColumnPerPart()
        {
            var sql = ConvertCalculationToSql(QueryOperation.Count, null, Key(x => new { x.BooleanThing, x.Int32Thing }), null, testTypesModelDetail);

            Assert.Contains("K0", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("K1", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("K2", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("GROUP BY", sql);
        }

        [Fact]
        public void ConvertCalculation_RelationKey_Joins()
        {
            Expression<Func<TestTypesModel, string>> key = x => x.RelationA.SomeValue;
            var sql = ConvertCalculationToSql(QueryOperation.Count, null, key, null, testTypesModelDetail);

            Assert.Contains("LEFT JOIN", sql);
            Assert.Contains("GROUP BY", sql);
        }

        public sealed class TypesProjection
        {
            public int Number { get; set; }
            public string? Text { get; set; }
            public string? Related { get; set; }
            public long Rows { get; set; }
            public double Average { get; set; }
            public decimal Total { get; set; }
            public bool Big { get; set; }
            public long Flagged { get; set; }
            public long Distinct { get; set; }
        }

        [Fact]
        public void ConvertSelect_ColumnsAliasedByMember_WithWhereOrderAndPaging()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, TypesProjection>> select = x => new TypesProjection { Number = x.Int32Thing * 2, Text = x.StringThing, Related = x.RelationA.SomeValue, Big = x.Int32Thing > 1 };
            var sql = ConvertSelectToSql(select.GetProjectionMembers(null), calculationWhere, QueryOrder<TestTypesModel>.Create(x => x.Int32Thing, true), 2, 5, testTypesModelDetail);

            Assert.StartsWith("SELECT ", sql);
            Assert.Contains("Number", sql);
            Assert.Contains("Text", sql);
            Assert.Contains("Related", sql);
            if (d.HasBooleanValues)
                Assert.DoesNotContain("CASE WHEN", sql);
            else
                Assert.Contains("CASE WHEN", sql);
            Assert.Contains(d.Column("TestTypes", "Int32Thing"), sql);
            Assert.Contains("LEFT JOIN", sql);
            Assert.Contains("WHERE", sql);
            Assert.Contains("ORDER BY", sql);
            Assert.DoesNotContain("GROUP BY", sql);
        }

        [Fact]
        public void ConvertGroup_WithoutHaving_IsTheGroupedSelect()
        {
            var d = GetDialect();
            Expression<Func<IGrouping<bool, TestTypesModel>, TypesProjection>> select = g => new TypesProjection { Rows = g.Count(), Average = g.Average(x => x.Int32Thing), Total = g.Sum(x => x.DecimalThing) };
            var sql = ConvertGroupToSql(boolKey, select.GetProjectionMembers(boolKey), calculationWhere, null, null, null, null, testTypesModelDetail, typeof(TypesProjection));

            Assert.StartsWith("SELECT COUNT(1)", sql);
            Assert.Contains("AVG(CAST(", sql);
            Assert.Contains("COALESCE(SUM(", sql);
            Assert.Contains("GROUP BY", sql);
            Assert.Contains("WHERE", sql);
            Assert.Contains(d.Column("TestTypes", "BooleanThing"), sql);
            Assert.DoesNotContain("SELECT * FROM", sql);
            Assert.DoesNotContain("TypesProjection", sql);
        }

        [Fact]
        public void ConvertGroup_WithHavingAndOrder_WrapsTheResultsAsTheResultType()
        {
            var d = GetDialect();
            Expression<Func<IGrouping<bool, TestTypesModel>, TypesProjection>> select = g => new TypesProjection { Rows = g.Count(), Total = g.Sum(x => x.DecimalThing) };
            Expression<Func<TypesProjection, bool>> having = r => r.Rows > 1;
            var sql = ConvertGroupToSql(boolKey, select.GetProjectionMembers(boolKey), null, having, QueryOrder<TypesProjection>.Create(r => r.Total, true), null, 10, testTypesModelDetail, typeof(TypesProjection));

            Assert.StartsWith("SELECT * FROM (", sql);
            Assert.Contains(d.Column("TypesProjection", "Rows"), sql);
            Assert.Contains(d.Column("TypesProjection", "Total"), sql);
            Assert.Contains("GROUP BY", sql);
            Assert.Contains("ORDER BY", sql);
            var groupBy = sql.IndexOf("GROUP BY", StringComparison.Ordinal);
            Assert.True(sql.IndexOf("WHERE", StringComparison.Ordinal) > groupBy, "the having is a where after the grouping");
        }

        [Fact]
        public void ConvertGroup_RelationKey_Joins()
        {
            Expression<Func<TestTypesModel, string>> key = x => x.RelationA.SomeValue;
            Expression<Func<IGrouping<string, TestTypesModel>, TypesProjection>> select = g => new TypesProjection { Text = g.Key, Rows = g.Count() };
            var sql = ConvertGroupToSql(key, select.GetProjectionMembers(key), null, null, null, null, null, testTypesModelDetail, typeof(TypesProjection));

            Assert.Contains("LEFT JOIN", sql);
            Assert.Contains("GROUP BY", sql);
        }

        [Fact]
        public void ConvertPersistWhere_PlainCondition_IsAWhereClause()
        {
            var d = GetDialect();
            var sql = ConvertPersistWhereToSql(calculationWhere, testTypesModelDetail);

            Assert.StartsWith(" WHERE", sql);
            Assert.Contains(d.Column("TestTypes", "Int32Thing"), sql);
            Assert.DoesNotContain("SELECT", sql);
            Assert.DoesNotContain(" IN (", sql);
        }

        [Fact]
        public void ConvertPersistWhere_RelatedCondition_SelectsTheIdentities()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationA.SomeValue == "a";
            var sql = ConvertPersistWhereToSql(where, testTypesModelDetail);

            Assert.StartsWith(" WHERE", sql);
            Assert.Contains(" IN (", sql);
            Assert.Contains("SELECT", sql);
            Assert.Contains("LEFT JOIN", sql);
            Assert.Contains(d.Column("TestTypes", "KeyA"), sql);
            Assert.Contains(d.Column("TestRelations", "SomeValue"), sql);
        }

        [Fact]
        public void ConvertPersistWhere_RelatedCollectionCondition_StaysASubqueryWhere()
        {
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationB.Any(r => r.SomeValue == "a");
            var sql = ConvertPersistWhereToSql(where, testTypesModelDetail);

            Assert.StartsWith(" WHERE", sql);
            Assert.DoesNotContain("LEFT JOIN", sql);
        }

        [Fact]
        public void ConvertPersistUpdate_ComputedValues_SetEachMemberFromTheRow()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, TestTypesModel>> set = x => new TestTypesModel { Int32Thing = x.Int32Thing + 1, StringThing = x.StringThing + "!" };
            var sql = ConvertPersistUpdateToSql(set.GetProjectionMembers(null), calculationWhere, testTypesModelDetail);

            Assert.StartsWith("UPDATE " + d.Table("TestTypes") + " SET ", sql);
            var column = d.LowercaseIdentifiers ? "int32thing" : "Int32Thing";
            Assert.Contains($"{d.ColumnQuoteOpen}{column}{d.ColumnQuoteClose}=(", sql);
            Assert.Contains(d.Column("TestTypes", "Int32Thing"), sql);
            Assert.Contains(d.Column("TestTypes", "StringThing"), sql);
            Assert.Contains("WHERE", sql);
            Assert.DoesNotContain("SELECT", sql);
        }

        [Fact]
        public void ConvertPersistUpdate_RelatedCondition_SelectsTheIdentities()
        {
            Expression<Func<TestTypesModel, TestTypesModel>> set = x => new TestTypesModel { Int32Thing = x.Int32Thing * 2 };
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationA.SomeValue == "y";
            var sql = ConvertPersistUpdateToSql(set.GetProjectionMembers(null), where, testTypesModelDetail);

            Assert.StartsWith("UPDATE ", sql);
            Assert.Contains(" IN (", sql);
            Assert.Contains("SELECT", sql);
        }

        [Fact]
        public void ConvertCalculation_RelatedCollectionValue_IsASubquery()
        {
            Expression<Func<TestTypesModel, int>> value = x => x.RelationB.Count();
            var sql = ConvertCalculationToSql(QueryOperation.Sum, calculationWhere, boolKey, value, testTypesModelDetail);

            Assert.Contains("COUNT(1)", sql);
            Assert.Contains("SUM(", sql);
            Assert.Contains("GROUP BY", sql);
        }

        #endregion

        #region Related Elements

        public sealed class ElementResult
        {
            public string? Value { get; set; }
            public int Number { get; set; }
        }

        [Fact]
        public void Convert_RelatedElementMember_IsASubqueryOfOneRow()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationB.OrderByDescending(r => r.SomeValue).First().SomeValue == "a";
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains((d.HasTopClause ? "(SELECT TOP(1) " : "(SELECT ") + d.Column("TestRelations", "SomeValue"), sql);
            Assert.Contains(d.Column("TestTypes", "KeyA") + "=" + d.Column("TestRelations", "RelationBKey"), sql);
            Assert.Contains("ORDER BY(" + d.Column("TestRelations", "SomeValue") + ")DESC", sql);
            if (!d.HasTopClause)
                Assert.Contains("LIMIT 1", sql);
        }

        [Fact]
        public void Convert_RelatedElementAfterWhereAndSkip_FiltersAndPagesTheSubquery()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationB.Where(r => r.SomeValue != null).OrderBy(r => r.RelationAKey).Skip(1).FirstOrDefault()!.RelationAKey > 0;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("AND(", sql);
            Assert.Contains("IS NOT NULL", sql);
            Assert.Contains("ORDER BY(" + d.Column("TestRelations", "RelationAKey") + ")", sql);
            if (d.HasTopClause)
                Assert.Contains("OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY", sql);
            else
                Assert.Contains("LIMIT 1 OFFSET 1", sql);
        }

        [Fact]
        public void Convert_RelatedElementAt_SkipsToTheRow()
        {
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationB.OrderBy(r => r.RelationAKey).ElementAt(2).RelationAKey == 1;
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("OFFSET 2", sql);
        }

        [Fact]
        public void Convert_RelatedLast_ReversesTheOrder()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, bool>> where = x => x.RelationB.OrderBy(r => r.SomeValue).ThenByDescending(r => r.RelationAKey).Last().SomeValue == "a";
            var sql = ConvertToSql(QueryOperation.Many, where, null, null, null, null, testTypesModelDetail);

            Assert.Contains("ORDER BY(" + d.Column("TestRelations", "SomeValue") + ")DESC,(" + d.Column("TestRelations", "RelationAKey") + ")", sql);
            Assert.DoesNotContain(")DESC)", sql);
        }

        [Fact]
        public void Convert_RelatedElement_RefusesWhatHasNoSqlShape()
        {
            _ = Assert.Throws<NotSupportedException>(() => ConvertToSql(QueryOperation.Many, (Expression<Func<TestTypesModel, bool>>)(x => x.RelationB.Last().SomeValue == "a"), null, null, null, null, testTypesModelDetail));
            _ = Assert.Throws<NotSupportedException>(() => ConvertToSql(QueryOperation.Many, (Expression<Func<TestTypesModel, bool>>)(x => x.RelationB.Take(1).First().SomeValue == "a"), null, null, null, null, testTypesModelDetail));
            _ = Assert.Throws<NotSupportedException>(() => ConvertToSql(QueryOperation.Many, (Expression<Func<TestTypesModel, bool>>)(x => x.RelationB.OrderBy(r => r.RelationAKey).First() == x.RelationA), null, null, null, null, testTypesModelDetail));
        }

        [Fact]
        public void ConvertSelect_RelatedElementMember_IsASubqueryColumn()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, ElementResult>> select = x => new ElementResult { Value = x.RelationB.OrderBy(r => r.SomeValue).First().SomeValue, Number = x.Int32Thing };
            var sql = ConvertSelectToSql(select.GetProjectionMembers(null), null, null, null, null, testTypesModelDetail);

            Assert.StartsWith("SELECT (", sql);
            Assert.Contains("ORDER BY(" + d.Column("TestRelations", "SomeValue") + ")", sql);
            Assert.Contains(d.Column("TestTypes", "KeyA") + "=" + d.Column("TestRelations", "RelationBKey"), sql);
        }

        public sealed class NestedProjection
        {
            public int Number { get; set; }
            public ElementResult[]? Items { get; set; }
        }

        [Fact]
        public void ConvertSelect_CollectionMember_TakesTheIdentityAndItsRowsAreASecondStatement()
        {
            var d = GetDialect();
            Expression<Func<TestTypesModel, NestedProjection>> select = x => new NestedProjection { Number = x.Int32Thing, Items = x.RelationB.Where(r => r.RelationAKey > 0).OrderBy(r => r.SomeValue).Select(r => new ElementResult { Value = r.SomeValue, Number = r.RelationAKey }).ToArray() };
            var members = select.GetProjectionMembers(null);
            Assert.Equal(ProjectionMemberKind.Collection, members[1].Kind);
            Assert.Equal(2, members[1].Members!.Length);
            Assert.NotNull(members[1].Where);
            Assert.Single(members[1].Order!.OrderExpressions);

            var sql = ConvertSelectToSql(members, null, null, null, null, testTypesModelDetail);
            Assert.Contains(d.Column("TestTypes", "KeyA") + " AS " + (d.LowercaseIdentifiers ? "Items" : d.ColumnQuoteOpen + "Items" + d.ColumnQuoteClose), sql);
            Assert.DoesNotContain(d.From("TestRelations"), sql);

            Expression<Func<TestRelationsModel, bool>> rowWhere = r => r.RelationAKey > 0;
            var rowSql = ConvertSelectElementsToSql(members[1], rowWhere, testRelationsModelDetail);
            Assert.StartsWith("SELECT (", rowSql);
            Assert.Contains(d.Column("TestRelations", "RelationBKey"), rowSql);
            Assert.Contains("ORDER BY(" + d.Column("TestRelations", "SomeValue") + ")", rowSql);
            Assert.Contains(d.From("TestRelations"), rowSql);
            Assert.DoesNotContain(d.From("TestTypes"), rowSql);
        }

        #endregion
    }
}

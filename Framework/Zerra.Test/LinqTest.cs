// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Linq.Expressions;
namespace Zerra.Test
{
    [TestClass]
    public partial class LinqTest
    {
        [TestMethod]
        public void TestToString()
        {
            Expression<Func<AllTypesModel, decimal>> exp = x => (decimal)x.Int16Thing;
            var str = exp.ToLinqString();
        }

        [TestMethod]
        public void TestToStringNoParameters()
        {
            Expression<Func<decimal>> exp = () => (decimal)15;
            var str = exp.ToLinqString();
        }

        [TestMethod]
        public void TestToStringMultipleParameters()
        {
            Expression<Func<int, int, decimal>> exp = (x, y) => x + (y + 15);
            var str = exp.ToLinqString();
        }

        [TestMethod]
        public void TestToStringValueProperty()
        {
            Expression<Func<DateTime, int>> exp = x => x.Year + DateTime.UtcNow.Year;
            var str = exp.ToLinqString();
        }

        [TestMethod]
        public void TestToStringExternalReference()
        {
            var external = 15m;
            Expression<Func<AllTypesModel, decimal>> exp = x => (decimal)x.Int16Thing + external;
            var str = exp.ToLinqString();
        }

        [TestMethod]
        public void TestToStringMethods()
        {
            Expression<Func<AllTypesModel, string>> exp1 = x => x.Int32Thing.ToString();
            var str1 = exp1.ToLinqString();

            Expression<Func<AllTypesModel, string>> exp2 = x => Convert(x.Int32Thing);
            var str2 = exp2.ToLinqString();
        }

        public static string Convert(int val) => val.ToString();
    }
}

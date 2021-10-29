// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Linq.Expressions;
using Zerra.Repository.Reflection;

namespace Zerra.DevTest
{
    //public static class TestLinqValueExtract
    //{
    //    public static void TestGetIDs()
    //    {
    //        var modelInfo = ModelAnalyzer.GetModel<TestCustomerEventStoreModel>();
    //        var identity = modelInfo;

    //        var identityProperties = ModelAnalyzer.GetIdentityPropertyNames(typeof(TestCustomerEventStoreModel));

    //        var id = 5;
    //        var ids = new int[] { 1, 2, 3, 4, 5 };
    //        Expression<Func<TestCustomerEventStoreModel, bool>> where1 = x => x.CustomerID == id;
    //        Expression<Func<TestCustomerEventStoreModel, bool>> where2 = x => ids.Contains(x.CustomerID);
    //        Expression<Func<TestCustomerEventStoreModel, bool>> where3 = x => x.CustomerID == id && x.Name.Contains("Bob") && x.Credit > 5 && x.Orders.Any(y => y.Amount > 10);
    //        Expression<Func<TestCustomerEventStoreModel, bool>> where4 = x => x.Name == "Bob";
    //        Expression<Func<TestCustomerEventStoreModel, bool>> where5 = x => x.CustomerID != id;
    //        Expression<Func<TestCustomerEventStoreModel, bool>> where6 = x => !ids.Contains(x.CustomerID);

    //        foreach (var identityProperty in identityProperties)
    //        {
    //            var test1 = LinqValueExtractor.Extract(where1, typeof(TestCustomerEventStoreModel), identityProperty);
    //            if (test1.Keys.Count != 1) throw new Exception();
    //            if (test1[identityProperty].Count != 1) throw new Exception();
    //            if ((int)test1[identityProperty][0] != id) throw new Exception();

    //            var test2 = LinqValueExtractor.Extract(where2, typeof(TestCustomerEventStoreModel), identityProperty);
    //            if (test2.Keys.Count != 1) throw new Exception();
    //            if (test2[identityProperty].Count != 5) throw new Exception();
    //            if ((int)test2[identityProperty][0] != ids[0]) throw new Exception();
    //            if ((int)test2[identityProperty][1] != ids[1]) throw new Exception();
    //            if ((int)test2[identityProperty][2] != ids[2]) throw new Exception();
    //            if ((int)test2[identityProperty][3] != ids[3]) throw new Exception();
    //            if ((int)test2[identityProperty][4] != ids[4]) throw new Exception();

    //            var test3 = LinqValueExtractor.Extract(where3, typeof(TestCustomerEventStoreModel), identityProperty);
    //            if (test3.Keys.Count != 1) throw new Exception();
    //            if (test3[identityProperty].Count != 1) throw new Exception();
    //            if ((int)test3[identityProperty][0] != id) throw new Exception();

    //            var test4 = LinqValueExtractor.Extract(where4, typeof(TestCustomerEventStoreModel), identityProperty);
    //            if (test4.Keys.Count != 1) throw new Exception();
    //            if (test4[identityProperty].Count != 0) throw new Exception();

    //            var test5 = LinqValueExtractor.Extract(where5, typeof(TestCustomerEventStoreModel), identityProperty);
    //            if (test5.Keys.Count != 1) throw new Exception();
    //            if (test5[identityProperty].Count != 0) throw new Exception();

    //            var test6 = LinqValueExtractor.Extract(where6, typeof(TestCustomerEventStoreModel), identityProperty);
    //            if (test6.Keys.Count != 1) throw new Exception();
    //            if (test6[identityProperty].Count != 0) throw new Exception();
    //        }
    //    }
    //}
}

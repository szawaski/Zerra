// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zerra.Serialization.QueryString;

namespace Zerra.Test
{
    [TestClass]
    public class QueryStringSerializerTest
    {
        [TestMethod]
        public void Types()
        {
            var model1 = Factory.GetCoreTypesModel();
            var str = QueryStringSerializer.Serialize(model1);
            var model2 = QueryStringSerializer.Deserialize<CoreTypesModel>(str);
            Factory.AssertAreEqual(model1, model2);
        }
    }
}


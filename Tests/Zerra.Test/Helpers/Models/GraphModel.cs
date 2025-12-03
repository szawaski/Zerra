// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Test.Helpers.Models
{
    [Zerra.SourceGeneration.GenerateTypeDetail]
    public class GraphModel
    {
        public bool Prop1 { get; set; }
        public byte Prop2 { get; set; }
        public SimpleModel Class { get; set; }
        public SimpleModel[] Array { get; set; }
        public List<SimpleModel> List { get; set; }

        public GraphModel Nested { get; set; }
        public GraphModel[] NestedArray { get; set; }

        public static GraphModel Create()
        {
            var model = new GraphModel()
            {
                Prop1 = true,
                Prop2 = 1,
                Class = new SimpleModel { Value1 = 1234, Value2 = "S-1234" },
                Array = [new SimpleModel { Value1 = 10, Value2 = "S-10" }, new SimpleModel { Value1 = 11, Value2 = "S-11" }],
                List = new List<SimpleModel>() { new SimpleModel { Value1 = 14, Value2 = "S-14" }, new SimpleModel { Value1 = 15, Value2 = "S-15" } },
            };
            return model;
        }
    }
}

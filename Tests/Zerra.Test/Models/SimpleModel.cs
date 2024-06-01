// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zerra.Test
{
    public class SimpleModel : IBasicModel
    {
        public int Value1 { get; set; }
        public string Value2 { get; set; }

        public static SimpleModel[] CreateArray()
        {
            var model = new SimpleModel[]
            {
                new() { Value1 = 1, Value2 = "S-1" },
                new() { Value1 = 2, Value2 = "S-2" },
                new() { Value1 = 3, Value2 = "S-3" }
            };
            return model;
        }
    }
}

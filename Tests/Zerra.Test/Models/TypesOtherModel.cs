// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Test
{
    public class TypesOtherModel
    {
        public string[][] StringArrayOfArrayThing { get; set; }

        public static TypesOtherModel Create()
        {
            var model = new TypesOtherModel()
            {
                StringArrayOfArrayThing = [["a", "b", "c"], null, ["d", "e", "f"], ["", null, ""]]
            };
            return model;
        }
    }
}
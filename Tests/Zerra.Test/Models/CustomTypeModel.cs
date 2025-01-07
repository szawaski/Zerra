// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.ComponentModel;

namespace Zerra.Test
{
    public class CustomTypeModel
    {
        public CustomType Value { get; set; }

        public static CustomTypeModel Create()
        {
            var model = new CustomTypeModel()
            {
                Value = new CustomType()
                {
                    Things1 = "What",
                    Things2 = "Doing"
                }
            };
            return model;
        }
    }
}

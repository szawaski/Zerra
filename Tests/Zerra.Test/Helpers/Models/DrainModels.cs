// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Test.Helpers.Models
{
    public class DrainSourceModel<T>
    {
        public int Before { get; set; }
        public T Value { get; set; }
        public string After { get; set; }
    }

    [Zerra.Reflection.GenerateTypeDetail]
    public class DrainTargetModel
    {
        public int Before { get; set; }
        public string After { get; set; }
    }
}
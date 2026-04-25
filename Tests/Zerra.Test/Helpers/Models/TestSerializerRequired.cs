// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Test.Helpers.Models
{
    [Zerra.Reflection.GenerateTypeDetail]
    public class TestSerializerRequired
    {
        public required int Value1 { get; set; }
        public required int Value2 { get; init; }
        public required int Value3 { get; set; }
    }
}

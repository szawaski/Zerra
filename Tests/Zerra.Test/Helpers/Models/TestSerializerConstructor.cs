// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Test.Helpers.Models
{
    [Zerra.SourceGeneration.GenerateTypeDetail]
    public class TestSerializerConstructor
    {
        public string _Value1 { get; init; }
        public int value2 { get; init; }

        public TestSerializerConstructor(string value1, int Value2)
        {
            this._Value1 = value1;
            this.value2 = Value2;
        }
    }
}

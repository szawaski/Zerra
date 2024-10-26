// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json;

namespace Zerra.Test
{
    public class JsonPropertyNameAttributeTestModel
    {
        [JsonPropertyName("1property")]
        public int _1_Property { get; set; }

        public int property2 { get; set; }

        [JsonPropertyName("3property")]
        public SimpleModel _3_Property { get; set; }

        public static JsonPropertyNameAttributeTestModel Create()
        {
            return new JsonPropertyNameAttributeTestModel()
            {
                _1_Property = 5,
                property2 = 7,
                _3_Property = new SimpleModel()
                {
                    Value1 = 10,
                    Value2 = "11"
                }
            };
        }
    }
}
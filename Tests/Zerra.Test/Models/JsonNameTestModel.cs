// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization;

namespace Zerra.Test
{
    public class JsonNameTestModel
    {
        [JsonPropertyName("1property")]
        public int _1_Property { get; set; }

        public int property2 { get; set; }

        [JsonPropertyName("3property")]
        public BasicModel _3_Property { get; set; }
    }
}
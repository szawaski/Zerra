// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json;

namespace Zerra.Test
{
    public class JsonIgnoreAttributeTestModel
    {
        [JsonIgnore(JsonIgnoreCondition.Never)]
        public int Property1 { get; set; }

        [JsonIgnore(JsonIgnoreCondition.Always)]
        public int Property2 { get; set; }

        [JsonIgnore(JsonIgnoreCondition.WhenReading)]
        public int Property3 { get; set; }

        [JsonIgnore(JsonIgnoreCondition.WhenWriting)]
        public int Property4 { get; set; }

        [JsonIgnore(JsonIgnoreCondition.WhenWritingNull)]
        public int? Property5a { get; set; }

        [JsonIgnore(JsonIgnoreCondition.WhenWritingNull)]
        public int? Property5b { get; set; }

        [JsonIgnore(JsonIgnoreCondition.WhenWritingDefault)]
        public int Property6a { get; set; }

        [JsonIgnore(JsonIgnoreCondition.WhenWritingDefault)]
        public int Property6b { get; set; }

        public static JsonIgnoreAttributeTestModel Create()
        {
            return new JsonIgnoreAttributeTestModel()
            {
                Property1 = 1,
                Property2 = 2,
                Property3 = 3,
                Property4 = 4,
                Property5a = 5,
                Property5b = null,
                Property6a = 6,
                Property6b = default
            };
        }
    }
}
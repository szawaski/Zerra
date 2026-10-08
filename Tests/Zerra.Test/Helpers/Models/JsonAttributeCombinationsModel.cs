// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json;

namespace Zerra.Test.Helpers.Models
{
    public class JsonAttributeCombinationsModel
    {
        [JsonPropertyName("renamed")]
        [JsonIgnore]
        public int NameThenIgnore { get; set; }

        [JsonIgnore]
        [JsonPropertyName("x")]
        public int IgnoreThenName { get; set; }

        [JsonPropertyName("named")]
        [JsonIgnore(JsonIgnoreCondition.WhenWritingNull)]
        public string? NameThenWhenNull { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("stj")]
        public int StjName { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public int StjIgnore { get; set; }

        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? StjWhenNull { get; set; }

        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Never)]
        public int StjNever { get; set; }

        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
        public int StjWhenDefault { get; set; }

        public int Kept { get; set; }

        [NonSerialized]
        public int NonSerializedField;
    }
}
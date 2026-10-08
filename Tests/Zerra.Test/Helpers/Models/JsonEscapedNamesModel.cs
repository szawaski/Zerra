// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json;

namespace Zerra.Test.Helpers.Models
{
    public class JsonEscapedNamesModel
    {
        public const string EscapedName = "quote\"slash\\b\bf\fn\nr\rt\tc\u0001é";
        private const string Ten = "abcdefghi\n";
        public const string LongName = Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten + Ten;

        [JsonPropertyName(EscapedName)]
        public int Escaped { get; set; }

        [JsonPropertyName(LongName)]
        public string? Long { get; set; }

        public int Plain { get; set; }
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Test.Helpers.Models
{
    [Zerra.SourceGeneration.GenerateTypeDetail]
    public record RecordModel(bool Property1)
    {
        public int Property2 { get; init; }
        public string Property3 { get; init; }
    }
}

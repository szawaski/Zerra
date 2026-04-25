// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.Test
{
    [Entity("TestRelations")]
    public sealed class TestRelationsModel
    {
        [Identity]
        public Guid RelationAKey { get; set; }

        public Guid? RelationBKey { get; set; }

        public string SomeValue { get; set; }
    }
}

// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository.Test
{
    [DataSourceEntity("TestRelations")]
    public abstract class BaseTestRelationsModel
    {
        [Identity]
        public Guid RelationKey { get; set; }
    }
}

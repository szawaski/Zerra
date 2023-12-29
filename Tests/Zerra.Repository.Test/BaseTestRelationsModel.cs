﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository.Test
{
    [Entity("TestRelations")]
    public abstract class BaseTestRelationsModel
    {
        [Identity]
        public Guid RelationAKey { get; set; }

        public Guid? RelationBKey { get; set; }
    }
}

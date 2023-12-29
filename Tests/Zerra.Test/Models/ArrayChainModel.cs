// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Test
{
    public class ArrayChainModel
    {
        public Guid? ID { get; set; }
        public ArrayChainModel[] Children { get; set; }
    }
}

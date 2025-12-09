// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Test.Helpers.Models
{
    [Zerra.Reflection.GenerateTypeDetail]
    public class ArrayChainModel
    {
        public Guid? ID { get; set; }
        public ArrayChainModel[] Children { get; set; }
    }
}

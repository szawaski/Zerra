// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Test.Helpers.Models
{
    public class MismatchModel<T>
    {
        public T? Value { get; set; }
        public int After { get; set; }
    }
}
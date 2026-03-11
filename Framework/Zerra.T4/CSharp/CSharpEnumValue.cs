// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.T4.CSharp
{
    public class CSharpEnumValue
    {
        public string Name { get; }
        public long Value { get; }
        public IReadOnlyList<CSharpAttribute> Attributes { get; }
        public CSharpEnumValue(string name, long value, IReadOnlyList<CSharpAttribute> attributes)
        {
            this.Name = name;
            this.Value = value;
            this.Attributes = attributes;
        }
        public override string ToString()
        {
            return $"{Name} = {Value}";
        }
    }
}
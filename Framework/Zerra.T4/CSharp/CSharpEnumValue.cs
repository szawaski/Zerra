// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.T4.CSharp
{
    public class CSharpEnumValue
    {
        public string Name { get; private set; }
        public long Value { get; private set; }
        public IReadOnlyList<CSharpAttribute> Attributes { get; private set; }
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
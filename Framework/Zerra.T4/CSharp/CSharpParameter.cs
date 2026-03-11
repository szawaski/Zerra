// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.T4.CSharp
{
    public class CSharpParameter
    {
        public string Name { get; }
        public CSharpUnresolvedType Type { get; }
        public bool IsIn { get; }
        public bool IsOut { get; }
        public bool IsRef { get; }
        public string DefaultValue { get; }
        public IReadOnlyList<CSharpAttribute> Attributes { get; }
        public CSharpParameter(string name, CSharpUnresolvedType type, bool isIn, bool isOut, bool isRef, string defaultValue)
        {
            this.Name = name;
            this.Type = type;
            this.IsIn = isIn;
            this.IsOut = isOut;
            this.IsRef = isRef;
            this.DefaultValue = defaultValue;
        }
        public override string ToString()
        {
            return $"{Type} {Name}";
        }
    }
}
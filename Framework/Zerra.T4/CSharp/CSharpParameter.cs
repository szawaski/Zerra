// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.T4.CSharp
{
    public class CSharpParameter
    {
        public string Name { get; private set; }
        public CSharpUnresolvedType Type { get; private set; }
        public bool IsIn { get; private set; }
        public bool IsOut { get; private set; }
        public bool IsRef { get; private set; }
        public string DefaultValue { get; private set; }
        public IReadOnlyList<CSharpAttribute> Attributes { get; private set; }
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
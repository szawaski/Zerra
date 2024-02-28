// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.T4.CSharp
{
    public class CSharpField
    {
        public string Name { get; }
        public CSharpUnresolvedType Type { get; }
        public bool IsPublic { get; }
        public bool IsStatic { get; }
        public bool IsReadOnly { get; }
        public IReadOnlyList<CSharpAttribute> Attributes { get; }
        public CSharpField(string name, CSharpUnresolvedType type, bool isPublic, bool isStatic, bool isReadOnly, IReadOnlyList<CSharpAttribute> attribues)
        {
            this.Name = name;
            this.Type = type;
            this.IsPublic = isPublic;
            this.IsStatic = isStatic;
            this.IsReadOnly = isReadOnly;
            this.Attributes = attribues;
        }
        public override string ToString()
        {
            return $"{Type} {Name}";
        }
    }
}
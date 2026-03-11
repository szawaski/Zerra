// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.T4.CSharp
{
    public class CSharpProperty
    {
        public string Name { get; set; }
        public CSharpUnresolvedType Type { get; set; }
        public bool IsPublic { get; }
        public bool IsStatic { get; }
        public bool IsVirtual { get; set; }
        public bool IsAbstract { get; set; }
        public bool HasGet { get; }
        public bool HasSet { get; }
        public bool IsGetPublic { get; }
        public bool IsSetPublic { get; }
        public IReadOnlyList<CSharpAttribute> Attributes { get; }
        public CSharpProperty(string name, CSharpUnresolvedType type, bool isPublic, bool isStatic, bool isVirtual, bool isAbstract, bool hasGet, bool hasSet, bool isGetPublic, bool isSetPublic, IReadOnlyList<CSharpAttribute> attributes)
        {
            this.Name = name;
            this.Type = type;
            this.IsPublic = isPublic;
            this.IsStatic = isStatic;
            this.IsVirtual = isVirtual;
            this.IsAbstract = isAbstract;
            this.HasGet = hasGet;
            this.HasSet = hasSet;
            this.IsGetPublic = isGetPublic;
            this.IsSetPublic = isSetPublic;
            this.Attributes = attributes;
        }
        public override string ToString()
        {
            return $"{Type} {Name}";
        }
    }
}
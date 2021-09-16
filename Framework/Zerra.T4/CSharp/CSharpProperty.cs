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
        public bool IsPublic { get; private set; }
        public bool IsStatic { get; private set; }
        public bool IsVirtual { get; set; }
        public bool IsAbstract { get; set; }
        public bool HasGet { get; private set; }
        public bool HasSet { get; private set; }
        public bool IsGetPublic { get; private set; }
        public bool IsSetPublic { get; private set; }
        public IReadOnlyList<CSharpAttribute> Attributes { get; private set; }
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
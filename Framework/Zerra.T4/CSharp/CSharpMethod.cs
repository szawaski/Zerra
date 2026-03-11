// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.T4.CSharp
{
    public class CSharpMethod
    {
        public string Name { get; set; }
        public CSharpUnresolvedType ReturnType { get; set; }
        public bool IsPublic { get; set; }
        public bool IsStatic { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsImplemented { get; set; }
        public IReadOnlyList<CSharpParameter> Parameters { get; set; }
        public IReadOnlyList<CSharpAttribute> Attributes { get; set; }
        public CSharpMethod(string name, CSharpUnresolvedType returnType, bool isPublic, bool isStatic, bool isVirtual, bool isAbstract, bool isImplemented, IReadOnlyList<CSharpParameter> parameters, IReadOnlyList<CSharpAttribute> attributes)
        {
            this.Name = name;
            this.ReturnType = returnType;
            this.IsPublic = isPublic;
            this.IsStatic = isStatic;
            this.IsVirtual = isVirtual;
            this.IsAbstract = isAbstract;
            this.IsImplemented = isImplemented;
            this.Parameters = parameters;
            this.Attributes = attributes;
        }
        public override string ToString()
        {
            return $"{ReturnType} {Name}({String.Join(", ", Parameters)})";
        }
    }
}
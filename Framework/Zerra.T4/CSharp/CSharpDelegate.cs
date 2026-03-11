// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.T4.CSharp
{
    public class CSharpDelegate : CSharpObject
    {
        public CSharpUnresolvedType ReturnType { get; set; }
        public IReadOnlyList<CSharpParameter> Parameters { get; set; }
        public CSharpDelegate(CSharpNamespace ns, IReadOnlyList<CSharpNamespace> usings, string name, CSharpUnresolvedType returnType, bool isPublic, IReadOnlyList<CSharpParameter> parameters, IReadOnlyList<CSharpAttribute> attributes)
            : base(ns, usings, name, CSharpObjectType.Delegate, Array.Empty<CSharpUnresolvedType>(), isPublic, false, false, false, Array.Empty<CSharpObject>(), Array.Empty<CSharpObject>(), Array.Empty<CSharpObject>(), Array.Empty<CSharpEnum>(), Array.Empty<CSharpDelegate>(), Array.Empty<CSharpProperty>(), Array.Empty<CSharpMethod>(), attributes)
        {
            this.ReturnType = returnType;
            this.Parameters = parameters;
        }
        public override string ToString()
        {
            return $"{base.ToString()}({String.Join(", ", Parameters)})";
        }
    }
}
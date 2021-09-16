// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.T4.CSharp
{
    public class CSharpEnum : CSharpObject
    {
        public Type Type { get; private set; }
        public IReadOnlyList<CSharpEnumValue> Values { get; private set; }
        public CSharpEnum(CSharpSolution solution, CSharpNamespace ns, IReadOnlyList<CSharpNamespace> usings, string name, Type type, bool isPublic, IReadOnlyList<CSharpEnumValue> values, IReadOnlyList<CSharpAttribute> attributes)
            : base(ns, usings, name, CSharpObjectType.Enum, new CSharpUnresolvedType[] { new CSharpUnresolvedType(solution, ns, usings, type.Name) }, isPublic, false, false, false, Array.Empty<CSharpObject>(), Array.Empty<CSharpObject>(), Array.Empty<CSharpObject>(), Array.Empty<CSharpEnum>(), Array.Empty<CSharpDelegate>(), Array.Empty<CSharpProperty>(), Array.Empty<CSharpMethod>(), attributes)
        {
            this.Type = type;
            this.Values = values;
        }
    }
}
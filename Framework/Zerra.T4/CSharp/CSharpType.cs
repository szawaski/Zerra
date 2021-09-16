// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.T4.CSharp
{
    public class CSharpType
    {
        public string Name { get; private set; }
        public Type NativeType { get; private set; }
        public CSharpObject SolutionType { get; private set; }
        public IReadOnlyList<CSharpType> GenericArguments { get; private set; }
        public CSharpType(string name, Type nativeType, CSharpObject solutionType, IReadOnlyList<CSharpType> genericArguments)
        {
            this.Name = name;
            this.NativeType = nativeType;
            this.SolutionType = solutionType;
            this.GenericArguments = genericArguments;
        }
        public override string ToString()
        {
            if (NativeType != null)
            {
                return NativeType.FullName;
            }
            else if (SolutionType != null)
            {
                var nsText = SolutionType.Namespace == null ? "" : $"{SolutionType.Namespace}.";
                return $"{nsText}{SolutionType.Name}";
            }
            else
            {
                return Name;
            }
        }
    }
}
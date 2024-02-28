// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.T4.CSharp
{
    public class CSharpAttribute
    {
        public string Name { get; }
        public IReadOnlyList<string> Arguments { get; }
        public CSharpAttribute(string name, IReadOnlyList<string> arguments)
        {
            this.Name = name;
            this.Arguments = arguments;
        }
        public override string ToString()
        {
            return $"[{Name}({String.Join(", ", Arguments)})]";
        }
    }
}
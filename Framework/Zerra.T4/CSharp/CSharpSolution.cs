// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.T4.CSharp
{
    public class CSharpSolution
    {
        public IList<CSharpNamespace> Namespaces { get; }
        public IList<CSharpObject> Classes { get; }
        public IList<CSharpObject> Structs { get; }
        public IList<CSharpObject> Interfaces { get; }
        public IList<CSharpEnum> Enums { get; }
        public IList<CSharpDelegate> Delegates { get; }
        public CSharpSolution()
        {
            this.Namespaces = new List<CSharpNamespace>();
            this.Classes = new List<CSharpObject>();
            this.Structs = new List<CSharpObject>();
            this.Interfaces = new List<CSharpObject>();
            this.Enums = new List<CSharpEnum>();
            this.Delegates = new List<CSharpDelegate>();
        }
    }
}
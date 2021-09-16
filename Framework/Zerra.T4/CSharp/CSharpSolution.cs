// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.T4.CSharp
{
    public class CSharpSolution
    {
        public IList<CSharpNamespace> Namespaces { get; private set; }
        public IList<CSharpObject> Classes { get; private set; }
        public IList<CSharpObject> Structs { get; private set; }
        public IList<CSharpObject> Interfaces { get; private set; }
        public IList<CSharpEnum> Enums { get; private set; }
        public IList<CSharpDelegate> Delegates { get; private set; }
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
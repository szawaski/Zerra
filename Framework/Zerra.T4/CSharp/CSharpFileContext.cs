// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.T4.CSharp
{
    public class CSharpFileContext
    {
        public string FileName { get; }
        public int Line { get; set; }
        public List<CSharpNamespace> Usings { get; }
        public Stack<CSharpNamespace> Namespaces { get; }
        public CSharpNamespace CurrentNamespace { get { return Namespaces.Count > 0 ? Namespaces.Peek() : null; } }
        public CSharpFileContext(string fileName)
        {
            this.FileName = fileName;
            this.Line = 1;
            this.Usings = new List<CSharpNamespace>();
            this.Namespaces = new Stack<CSharpNamespace>();
        }
    }
}
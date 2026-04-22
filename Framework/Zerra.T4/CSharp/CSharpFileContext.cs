// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.T4.CSharp
{
    /// <summary>
    /// Represents the parsing context for a C# source file.
    /// </summary>
    public class CSharpFileContext
    {
        /// <summary>
        /// Gets the name of the file being parsed.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets or sets the current line number in the file.
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Gets the using directives declared in the file.
        /// </summary>
        public List<CSharpNamespace> Usings { get; }

        /// <summary>
        /// Gets the stack of namespaces currently being parsed.
        /// </summary>
        public Stack<CSharpNamespace> Namespaces { get; }

        /// <summary>
        /// Gets the current namespace being parsed, or null if not within a namespace.
        /// </summary>
        public CSharpNamespace? CurrentNamespace { get { return Namespaces.Count > 0 ? Namespaces.Peek() : null; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpFileContext"/> class.
        /// </summary>
        /// <param name="fileName">The name of the file being parsed.</param>
        public CSharpFileContext(string fileName)
        {
            this.FileName = fileName;
            this.Line = 1;
            this.Usings = new List<CSharpNamespace>();
            this.Namespaces = new Stack<CSharpNamespace>();
        }
    }
}
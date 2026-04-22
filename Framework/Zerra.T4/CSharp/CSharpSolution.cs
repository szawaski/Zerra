// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.T4.CSharp
{
    /// <summary>
    /// Represents a collection of parsed C# types and namespaces from source files.
    /// </summary>
    public class CSharpSolution
    {
        /// <summary>
        /// Gets the namespaces found in the solution.
        /// </summary>
        public IList<CSharpNamespace> Namespaces { get; }

        /// <summary>
        /// Gets the classes found in the solution.
        /// </summary>
        public IList<CSharpObject> Classes { get; }

        /// <summary>
        /// Gets the structs found in the solution.
        /// </summary>
        public IList<CSharpObject> Structs { get; }

        /// <summary>
        /// Gets the interfaces found in the solution.
        /// </summary>
        public IList<CSharpObject> Interfaces { get; }

        /// <summary>
        /// Gets the enumerations found in the solution.
        /// </summary>
        public IList<CSharpEnum> Enums { get; }

        /// <summary>
        /// Gets the delegates found in the solution.
        /// </summary>
        public IList<CSharpDelegate> Delegates { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpSolution"/> class.
        /// </summary>
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
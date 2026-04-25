// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.T4.CSharp
{
    /// <summary>
    /// Represents a namespace declaration in C# code.
    /// </summary>
    public class CSharpNamespace
    {
        /// <summary>
        /// Gets the individual parts of the namespace name.
        /// </summary>
        public string[] Names { get; }

        /// <summary>
        /// Gets the full namespace name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpNamespace"/> class.
        /// </summary>
        /// <param name="name">The namespace name.</param>
        public CSharpNamespace(string name)
        {
            this.Name = name;
            this.Names = name.Split('.');
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpNamespace"/> class with a root namespace.
        /// </summary>
        /// <param name="root">The root namespace to prepend, or null.</param>
        /// <param name="name">The namespace name.</param>
        public CSharpNamespace(CSharpNamespace? root, string name)
        {
            if (root is not null)
                this.Name = $"{root.Name}.{name}";
            else
                this.Name = name;
            this.Names = name.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return String.Join(".", Names);
        }
    }
}
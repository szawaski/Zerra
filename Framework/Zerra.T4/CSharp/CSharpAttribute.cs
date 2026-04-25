// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.T4.CSharp
{
    /// <summary>
    /// Represents an attribute applied to a C# code element.
    /// </summary>
    public class CSharpAttribute
    {
        /// <summary>
        /// Gets the name of the attribute.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the arguments passed to the attribute.
        /// </summary>
        public IReadOnlyList<string> Arguments { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="arguments">The arguments passed to the attribute.</param>
        public CSharpAttribute(string name, IReadOnlyList<string> arguments)
        {
            this.Name = name;
            this.Arguments = arguments;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{Name}({String.Join(", ", Arguments)})]";
        }
    }
}
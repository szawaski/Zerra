// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.T4.CSharp
{
    /// <summary>
    /// Represents a parameter in a method, delegate, or constructor declaration.
    /// </summary>
    public class CSharpParameter
    {
        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the parameter.
        /// </summary>
        public CSharpUnresolvedType Type { get; }

        /// <summary>
        /// Gets a value indicating whether the parameter is an 'in' parameter.
        /// </summary>
        public bool IsIn { get; }

        /// <summary>
        /// Gets a value indicating whether the parameter is an 'out' parameter.
        /// </summary>
        public bool IsOut { get; }

        /// <summary>
        /// Gets a value indicating whether the parameter is a 'ref' parameter.
        /// </summary>
        public bool IsRef { get; }

        /// <summary>
        /// Gets the default value of the parameter, if any.
        /// </summary>
        public string? DefaultValue { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpParameter"/> class.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        /// <param name="isIn">Whether the parameter is an 'in' parameter.</param>
        /// <param name="isOut">Whether the parameter is an 'out' parameter.</param>
        /// <param name="isRef">Whether the parameter is a 'ref' parameter.</param>
        /// <param name="defaultValue">The default value of the parameter, if any.</param>
        public CSharpParameter(string name, CSharpUnresolvedType type, bool isIn, bool isOut, bool isRef, string? defaultValue)
        {
            this.Name = name;
            this.Type = type;
            this.IsIn = isIn;
            this.IsOut = isOut;
            this.IsRef = isRef;
            this.DefaultValue = defaultValue;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Type} {Name}";
        }
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.T4.CSharp
{
    /// <summary>
    /// Represents a resolved C# type with generic arguments and type information.
    /// </summary>
    public class CSharpType
    {
        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the native .NET type, if this represents a runtime type.
        /// </summary>
        public Type? NativeType { get; }

        /// <summary>
        /// Gets the parsed C# type declaration, if this represents a type from the solution.
        /// </summary>
        public CSharpObject? SolutionType { get; }

        /// <summary>
        /// Gets the generic type arguments, if this is a generic type.
        /// </summary>
        public IReadOnlyList<CSharpType> GenericArguments { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpType"/> class.
        /// </summary>
        /// <param name="name">The name of the type.</param>
        /// <param name="nativeType">The native .NET type, if applicable.</param>
        /// <param name="solutionType">The parsed C# type declaration, if applicable.</param>
        /// <param name="genericArguments">The generic type arguments, if this is a generic type.</param>
        public CSharpType(string name, Type? nativeType, CSharpObject? solutionType, IReadOnlyList<CSharpType> genericArguments)
        {
            this.Name = name;
            this.NativeType = nativeType;
            this.SolutionType = solutionType;
            this.GenericArguments = genericArguments;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (NativeType is not null)
            {
                return NativeType.FullName;
            }
            else if (SolutionType is not null)
            {
                var nsText = SolutionType.Namespace is null ? "" : $"{SolutionType.Namespace}.";
                return $"{nsText}{SolutionType.Name}";
            }
            else
            {
                return Name;
            }
        }
    }
}
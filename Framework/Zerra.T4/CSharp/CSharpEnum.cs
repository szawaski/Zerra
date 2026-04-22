// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.T4.CSharp
{
    /// <summary>
    /// Represents an enumeration declaration in C# code.
    /// </summary>
    public class CSharpEnum : CSharpObject
    {
        /// <summary>
        /// Gets the underlying type of the enumeration.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Gets the values defined in the enumeration.
        /// </summary>
        public IReadOnlyList<CSharpEnumValue> Values { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpEnum"/> class.
        /// </summary>
        /// <param name="solution">The solution context.</param>
        /// <param name="ns">The namespace containing the enumeration.</param>
        /// <param name="usings">The using directives available to the enumeration.</param>
        /// <param name="name">The name of the enumeration.</param>
        /// <param name="type">The underlying type of the enumeration.</param>
        /// <param name="isPublic">Whether the enumeration is public.</param>
        /// <param name="values">The values defined in the enumeration.</param>
        /// <param name="attributes">The attributes applied to the enumeration.</param>
        public CSharpEnum(CSharpSolution solution, CSharpNamespace? ns, IReadOnlyList<CSharpNamespace> usings, string name, Type type, bool isPublic, IReadOnlyList<CSharpEnumValue> values, IReadOnlyList<CSharpAttribute> attributes)
            : base(ns, usings, name, CSharpObjectType.Enum, [new(solution, ns, usings, type.Name)], isPublic, false, false, false, [], [], [], [], [], [], [], attributes)
        {
            this.Type = type;
            this.Values = values;
        }
    }
}
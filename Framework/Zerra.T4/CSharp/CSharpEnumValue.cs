// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.T4.CSharp
{
    /// <summary>
    /// Represents a single value within an enumeration.
    /// </summary>
    public class CSharpEnumValue
    {
        /// <summary>
        /// Gets the name of the enumeration value.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the numeric value of the enumeration member.
        /// </summary>
        public long Value { get; }

        /// <summary>
        /// Gets the attributes applied to the enumeration value.
        /// </summary>
        public IReadOnlyList<CSharpAttribute> Attributes { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpEnumValue"/> class.
        /// </summary>
        /// <param name="name">The name of the enumeration value.</param>
        /// <param name="value">The numeric value of the enumeration member.</param>
        /// <param name="attributes">The attributes applied to the enumeration value.</param>
        public CSharpEnumValue(string name, long value, IReadOnlyList<CSharpAttribute> attributes)
        {
            this.Name = name;
            this.Value = value;
            this.Attributes = attributes;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Name} = {Value}";
        }
    }
}
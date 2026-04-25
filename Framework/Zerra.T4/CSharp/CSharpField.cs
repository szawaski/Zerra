// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.T4.CSharp
{
    /// <summary>
    /// Represents a field declaration in C# code.
    /// </summary>
    public class CSharpField
    {
        /// <summary>
        /// Gets the name of the field.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the field.
        /// </summary>
        public CSharpUnresolvedType Type { get; }

        /// <summary>
        /// Gets a value indicating whether the field is public.
        /// </summary>
        public bool IsPublic { get; }

        /// <summary>
        /// Gets a value indicating whether the field is static.
        /// </summary>
        public bool IsStatic { get; }

        /// <summary>
        /// Gets a value indicating whether the field is readonly.
        /// </summary>
        public bool IsReadOnly { get; }

        /// <summary>
        /// Gets the attributes applied to the field.
        /// </summary>
        public IReadOnlyList<CSharpAttribute> Attributes { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpField"/> class.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <param name="type">The type of the field.</param>
        /// <param name="isPublic">Whether the field is public.</param>
        /// <param name="isStatic">Whether the field is static.</param>
        /// <param name="isReadOnly">Whether the field is readonly.</param>
        /// <param name="attribues">The attributes applied to the field.</param>
        public CSharpField(string name, CSharpUnresolvedType type, bool isPublic, bool isStatic, bool isReadOnly, IReadOnlyList<CSharpAttribute> attribues)
        {
            this.Name = name;
            this.Type = type;
            this.IsPublic = isPublic;
            this.IsStatic = isStatic;
            this.IsReadOnly = isReadOnly;
            this.Attributes = attribues;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Type} {Name}";
        }
    }
}
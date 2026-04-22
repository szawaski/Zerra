// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.T4.CSharp
{
    /// <summary>
    /// Represents a property declaration in C# code.
    /// </summary>
    public class CSharpProperty
    {
        /// <summary>
        /// Gets or sets the name of the property.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the property.
        /// </summary>
        public CSharpUnresolvedType Type { get; set; }

        /// <summary>
        /// Gets a value indicating whether the property is public.
        /// </summary>
        public bool IsPublic { get; }

        /// <summary>
        /// Gets a value indicating whether the property is static.
        /// </summary>
        public bool IsStatic { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the property is virtual.
        /// </summary>
        public bool IsVirtual { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the property is abstract.
        /// </summary>
        public bool IsAbstract { get; set; }

        /// <summary>
        /// Gets a value indicating whether the property has a getter.
        /// </summary>
        public bool HasGet { get; }

        /// <summary>
        /// Gets a value indicating whether the property has a setter.
        /// </summary>
        public bool HasSet { get; }

        /// <summary>
        /// Gets a value indicating whether the getter is public.
        /// </summary>
        public bool IsGetPublic { get; }

        /// <summary>
        /// Gets a value indicating whether the setter is public.
        /// </summary>
        public bool IsSetPublic { get; }

        /// <summary>
        /// Gets the attributes applied to the property.
        /// </summary>
        public IReadOnlyList<CSharpAttribute> Attributes { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpProperty"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="type">The type of the property.</param>
        /// <param name="isPublic">Whether the property is public.</param>
        /// <param name="isStatic">Whether the property is static.</param>
        /// <param name="isVirtual">Whether the property is virtual.</param>
        /// <param name="isAbstract">Whether the property is abstract.</param>
        /// <param name="hasGet">Whether the property has a getter.</param>
        /// <param name="hasSet">Whether the property has a setter.</param>
        /// <param name="isGetPublic">Whether the getter is public.</param>
        /// <param name="isSetPublic">Whether the setter is public.</param>
        /// <param name="attributes">The attributes applied to the property.</param>
        public CSharpProperty(string name, CSharpUnresolvedType type, bool isPublic, bool isStatic, bool isVirtual, bool isAbstract, bool hasGet, bool hasSet, bool isGetPublic, bool isSetPublic, IReadOnlyList<CSharpAttribute> attributes)
        {
            this.Name = name;
            this.Type = type;
            this.IsPublic = isPublic;
            this.IsStatic = isStatic;
            this.IsVirtual = isVirtual;
            this.IsAbstract = isAbstract;
            this.HasGet = hasGet;
            this.HasSet = hasSet;
            this.IsGetPublic = isGetPublic;
            this.IsSetPublic = isSetPublic;
            this.Attributes = attributes;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Type} {Name}";
        }
    }
}
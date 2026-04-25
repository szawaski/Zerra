// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.T4.CSharp
{
    /// <summary>
    /// Represents a method or constructor declaration in C# code.
    /// </summary>
    public class CSharpMethod
    {
        /// <summary>
        /// Gets or sets the name of the method.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the return type of the method.
        /// </summary>
        public CSharpUnresolvedType ReturnType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the method is public.
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the method is static.
        /// </summary>
        public bool IsStatic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the method is virtual.
        /// </summary>
        public bool IsVirtual { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the method is abstract.
        /// </summary>
        public bool IsAbstract { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the method has an implementation.
        /// </summary>
        public bool IsImplemented { get; set; }

        /// <summary>
        /// Gets or sets the parameters of the method.
        /// </summary>
        public IReadOnlyList<CSharpParameter> Parameters { get; set; }

        /// <summary>
        /// Gets or sets the attributes applied to the method.
        /// </summary>
        public IReadOnlyList<CSharpAttribute> Attributes { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpMethod"/> class.
        /// </summary>
        /// <param name="name">The name of the method.</param>
        /// <param name="returnType">The return type of the method.</param>
        /// <param name="isPublic">Whether the method is public.</param>
        /// <param name="isStatic">Whether the method is static.</param>
        /// <param name="isVirtual">Whether the method is virtual.</param>
        /// <param name="isAbstract">Whether the method is abstract.</param>
        /// <param name="isImplemented">Whether the method has an implementation.</param>
        /// <param name="parameters">The parameters of the method.</param>
        /// <param name="attributes">The attributes applied to the method.</param>
        public CSharpMethod(string name, CSharpUnresolvedType returnType, bool isPublic, bool isStatic, bool isVirtual, bool isAbstract, bool isImplemented, IReadOnlyList<CSharpParameter> parameters, IReadOnlyList<CSharpAttribute> attributes)
        {
            this.Name = name;
            this.ReturnType = returnType;
            this.IsPublic = isPublic;
            this.IsStatic = isStatic;
            this.IsVirtual = isVirtual;
            this.IsAbstract = isAbstract;
            this.IsImplemented = isImplemented;
            this.Parameters = parameters;
            this.Attributes = attributes;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{ReturnType} {Name}({String.Join(", ", Parameters)})";
        }
    }
}
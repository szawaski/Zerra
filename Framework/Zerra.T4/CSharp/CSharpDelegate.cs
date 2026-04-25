// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.T4.CSharp
{
    /// <summary>
    /// Represents a delegate declaration in C# code.
    /// </summary>
    public class CSharpDelegate : CSharpObject
    {
        /// <summary>
        /// Gets or sets the return type of the delegate.
        /// </summary>
        public CSharpUnresolvedType ReturnType { get; set; }

        /// <summary>
        /// Gets or sets the parameters of the delegate.
        /// </summary>
        public IReadOnlyList<CSharpParameter> Parameters { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpDelegate"/> class.
        /// </summary>
        /// <param name="ns">The namespace containing the delegate.</param>
        /// <param name="usings">The using directives available to the delegate.</param>
        /// <param name="name">The name of the delegate.</param>
        /// <param name="returnType">The return type of the delegate.</param>
        /// <param name="isPublic">Whether the delegate is public.</param>
        /// <param name="parameters">The parameters of the delegate.</param>
        /// <param name="attributes">The attributes applied to the delegate.</param>
        public CSharpDelegate(CSharpNamespace? ns, IReadOnlyList<CSharpNamespace> usings, string name, CSharpUnresolvedType returnType, bool isPublic, IReadOnlyList<CSharpParameter> parameters, IReadOnlyList<CSharpAttribute> attributes)
            : base(ns, usings, name, CSharpObjectType.Delegate, [], isPublic, false, false, false, [], [], [], [], [], [], [], attributes)
        {
            this.ReturnType = returnType;
            this.Parameters = parameters;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{base.ToString()}({String.Join(", ", Parameters)})";
        }
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.T4.CSharp
{
    /// <summary>
    /// Represents a type declaration (class, struct, interface, record) in C# code.
    /// </summary>
    public class CSharpObject
    {
        /// <summary>
        /// Gets the namespace containing this type.
        /// </summary>
        public CSharpNamespace? Namespace { get; }

        /// <summary>
        /// Gets the using directives available to this type.
        /// </summary>
        public IReadOnlyList<CSharpNamespace> Usings { get; }

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type category (class, struct, interface, etc.).
        /// </summary>
        public CSharpObjectType ObjectType { get; }

        /// <summary>
        /// Gets the base types and interfaces this type implements.
        /// </summary>
        public IReadOnlyList<CSharpUnresolvedType> Implements { get; }

        /// <summary>
        /// Gets a value indicating whether the type is public.
        /// </summary>
        public bool IsPublic { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the type is static.
        /// </summary>
        public bool IsStatic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the type is abstract.
        /// </summary>
        public bool IsAbstract { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the type is declared as partial.
        /// </summary>
        public bool IsPartial { get; set; }

        /// <summary>
        /// Gets the inner classes declared within this type.
        /// </summary>
        public IReadOnlyList<CSharpObject> InnerClasses { get; }

        /// <summary>
        /// Gets the inner structs declared within this type.
        /// </summary>
        public IReadOnlyList<CSharpObject> InnerStructs { get; }

        /// <summary>
        /// Gets the inner interfaces declared within this type.
        /// </summary>
        public IReadOnlyList<CSharpObject> InnerInterfaces { get; }

        /// <summary>
        /// Gets the inner enums declared within this type.
        /// </summary>
        public IReadOnlyList<CSharpEnum> InnerEnums { get; }

        /// <summary>
        /// Gets the inner delegates declared within this type.
        /// </summary>
        public IReadOnlyList<CSharpDelegate> InnerDelegates { get; }

        /// <summary>
        /// Gets the properties declared in this type.
        /// </summary>
        public IReadOnlyList<CSharpProperty> Properties { get; }

        /// <summary>
        /// Gets the methods declared in this type (excluding constructors).
        /// </summary>
        public IReadOnlyList<CSharpMethod> Methods { get; }

        /// <summary>
        /// Gets the constructors declared in this type.
        /// </summary>
        public IReadOnlyList<CSharpMethod> Constructors { get; }

        /// <summary>
        /// Gets the attributes applied to this type.
        /// </summary>
        public IReadOnlyList<CSharpAttribute> Attributes { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpObject"/> class.
        /// </summary>
        /// <param name="ns">The namespace containing this type.</param>
        /// <param name="usings">The using directives available to this type.</param>
        /// <param name="name">The name of the type.</param>
        /// <param name="objectType">The type category (class, struct, interface, etc.).</param>
        /// <param name="implements">The base types and interfaces this type implements.</param>
        /// <param name="isPublic">Whether the type is public.</param>
        /// <param name="isStatic">Whether the type is static.</param>
        /// <param name="isAbstract">Whether the type is abstract.</param>
        /// <param name="isPartial">Whether the type is declared as partial.</param>
        /// <param name="classes">The inner classes declared within this type.</param>
        /// <param name="structs">The inner structs declared within this type.</param>
        /// <param name="interfaces">The inner interfaces declared within this type.</param>
        /// <param name="enums">The inner enums declared within this type.</param>
        /// <param name="delegates">The inner delegates declared within this type.</param>
        /// <param name="properties">The properties declared in this type.</param>
        /// <param name="methods">The methods and constructors declared in this type.</param>
        /// <param name="attributes">The attributes applied to this type.</param>
        public CSharpObject(CSharpNamespace? ns, IReadOnlyList<CSharpNamespace> usings, string name, CSharpObjectType objectType, IReadOnlyList<CSharpUnresolvedType> implements, bool isPublic, bool isStatic, bool isAbstract, bool isPartial, IReadOnlyList<CSharpObject> classes, IReadOnlyList<CSharpObject> structs, IReadOnlyList<CSharpObject> interfaces, IReadOnlyList<CSharpEnum> enums, IReadOnlyList<CSharpDelegate> delegates, IReadOnlyList<CSharpProperty> properties, IReadOnlyList<CSharpMethod> methods, IReadOnlyList<CSharpAttribute> attributes)
        {
            this.Namespace = ns;
            this.Usings = usings;
            this.Name = name;
            this.ObjectType = objectType;
            this.Implements = implements;
            this.IsPublic = isPublic;
            this.IsStatic = isStatic;
            this.IsAbstract = isAbstract;
            this.IsPartial = isPartial;
            this.InnerClasses = classes;
            this.InnerStructs = structs;
            this.InnerInterfaces = interfaces;
            this.InnerEnums = enums;
            this.InnerDelegates = delegates;
            this.Properties = properties;
            this.Methods = methods.Where(x => x.Name != name).ToArray();
            this.Constructors = methods.Where(x => x.Name == name).ToArray();
            this.Attributes = attributes;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var partialText = IsPartial ? "partial " : "";
            var nsText = Namespace is null ? "" : $"{Namespace}.";
            return $"{partialText}{ObjectType.ToString().ToLower()} {nsText}{Name}";
        }
    }
}
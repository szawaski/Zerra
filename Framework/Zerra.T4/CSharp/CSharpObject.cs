// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using System.Linq;

namespace Zerra.T4.CSharp
{
    public class CSharpObject
    {
        public CSharpNamespace Namespace { get; }
        public IReadOnlyList<CSharpNamespace> Usings { get; }
        public string Name { get; }
        public CSharpObjectType ObjectType { get; }
        public IReadOnlyList<CSharpUnresolvedType> Implements { get; }
        public bool IsPublic { get; }
        public bool IsStatic { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsPartial { get; set; }
        public IReadOnlyList<CSharpObject> InnerClasses { get; }
        public IReadOnlyList<CSharpObject> InnerStructs { get; }
        public IReadOnlyList<CSharpObject> InnerInterfaces { get; }
        public IReadOnlyList<CSharpEnum> InnerEnums { get; }
        public IReadOnlyList<CSharpDelegate> InnerDelegates { get; }
        public IReadOnlyList<CSharpProperty> Properties { get; }
        public IReadOnlyList<CSharpMethod> Methods { get; }
        public IReadOnlyList<CSharpMethod> Constructors { get; }
        public IReadOnlyList<CSharpAttribute> Attributes { get; }
        public CSharpObject(CSharpNamespace ns, IReadOnlyList<CSharpNamespace> usings, string name, CSharpObjectType objectType, IReadOnlyList<CSharpUnresolvedType> implements, bool isPublic, bool isStatic, bool isAbstract, bool isPartial, IReadOnlyList<CSharpObject> classes, IReadOnlyList<CSharpObject> structs, IReadOnlyList<CSharpObject> interfaces, IReadOnlyList<CSharpEnum> enums, IReadOnlyList<CSharpDelegate> delegates, IReadOnlyList<CSharpProperty> properties, IReadOnlyList<CSharpMethod> methods, IReadOnlyList<CSharpAttribute> attributes)
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
        public override string ToString()
        {
            var partialText = IsPartial ? "partial " : "";
            var nsText = Namespace == null ? "" : $"{Namespace}.";
            return $"{partialText}{ObjectType.ToString().ToLower()} {nsText}{Name}";
        }
    }
}
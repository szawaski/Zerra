// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using System.Linq;

namespace Zerra.T4.CSharp
{
    public class CSharpObject
    {
        public CSharpNamespace Namespace { get; private set; }
        public IReadOnlyList<CSharpNamespace> Usings { get; private set; }
        public string Name { get; private set; }
        public CSharpObjectType ObjectType { get; private set; }
        public IReadOnlyList<CSharpUnresolvedType> Implements { get; private set; }
        public bool IsPublic { get; private set; }
        public bool IsStatic { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsPartial { get; set; }
        public IReadOnlyList<CSharpObject> InnerClasses { get; private set; }
        public IReadOnlyList<CSharpObject> InnerStructs { get; private set; }
        public IReadOnlyList<CSharpObject> InnerInterfaces { get; private set; }
        public IReadOnlyList<CSharpEnum> InnerEnums { get; private set; }
        public IReadOnlyList<CSharpDelegate> InnerDelegates { get; private set; }
        public IReadOnlyList<CSharpProperty> Properties { get; private set; }
        public IReadOnlyList<CSharpMethod> Methods { get; private set; }
        public IReadOnlyList<CSharpMethod> Constructors { get; private set; }
        public IReadOnlyList<CSharpAttribute> Attributes { get; private set; }
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
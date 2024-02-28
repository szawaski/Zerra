// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.T4.CSharp
{
    public class CSharpNamespace
    {
        public string[] Names { get; }
        public string Name { get; }
        public CSharpNamespace(string name)
        {
            this.Name = name;
            this.Names = name.Split('.');
        }
        public CSharpNamespace(CSharpNamespace root, string name)
        {
            if (root != null)
                this.Name = $"{root.Name}.{name}";
            else
                this.Name = name;
            this.Names = name.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        }
        public override string ToString()
        {
            return String.Join(".", Names);
        }
    }
}
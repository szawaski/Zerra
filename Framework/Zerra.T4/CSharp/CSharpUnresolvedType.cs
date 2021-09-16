// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;

namespace Zerra.T4.CSharp
{
    public class CSharpUnresolvedType
    {
        private readonly CSharpSolution solution;
        private readonly CSharpNamespace ns;
        private readonly IReadOnlyList<CSharpNamespace> usings;
        public string Name { get; private set; }
        public CSharpUnresolvedType(CSharpSolution solution, CSharpNamespace ns, IReadOnlyList<CSharpNamespace> usings, string name)
        {
            this.solution = solution;
            this.ns = ns;
            this.usings = usings;
            this.Name = name;
        }

        public CSharpType Resolved
        {
            get
            {
                return Resolve(Name);
            }
        }
        private CSharpType Resolve(string typeName)
        {
            var chars = typeName.ToCharArray();
            var index = 0;
            var buffer = new List<char>();
            var generic = 0;
            var genericName = typeName;
            var genericArguments = new List<CSharpType>();
            if (typeName.EndsWith("[]"))
            {
                genericName = "Array";
                var genericArgument = Resolve(typeName.Remove(typeName.Length - 2));
                genericArguments.Add(genericArgument);
            }
            else if (typeName.EndsWith("?"))
            {
                genericName = "Nullable";
                var genericArgument = Resolve(typeName.Remove(typeName.Length - 1));
                genericArguments.Add(genericArgument);
            }
            else
            {
                for (; index < chars.Length; index++)
                {
                    var c = chars[index];
                    switch (c)
                    {
                        case '<':
                            {
                                generic++;
                                if (generic > 1)
                                {
                                    buffer.Add(c);
                                }
                                else
                                {
                                    genericName = new String(buffer.ToArray());
                                    buffer.Clear();
                                }
                                break;
                            }
                        case ',':
                            {
                                if (generic == 1)
                                {
                                    var genericArgumentString = new String(buffer.ToArray());
                                    var genericArgument = Resolve(genericArgumentString);
                                    genericArguments.Add(genericArgument);
                                    buffer.Clear();
                                }
                                else
                                {
                                    buffer.Add(c);
                                }
                                break;
                            }
                        case '>':
                            {
                                if (generic == 0)
                                    throw new Exception();
                                generic--;
                                if (generic > 0)
                                {
                                    buffer.Add(c);
                                }
                                else
                                {
                                    var genericArgumentString = new String(buffer.ToArray());
                                    var genericArgument = Resolve(genericArgumentString);
                                    genericArguments.Add(genericArgument);
                                    buffer.Clear();
                                }
                                break;
                            }
                        default:
                            {
                                buffer.Add(c);
                                break;
                            }
                    }
                }
            }

            if (genericArguments.Count > 0)
                genericName = $"{genericName}`{genericArguments.Count}";

            var nativeType = Type.GetType(genericName);

            if (nativeType == null)
            {
                nativeType = GetSystemType(genericName);
            }

            if (nativeType == null)
            {
                foreach (var use in usings)
                {
                    nativeType = Type.GetType($"{use}.{genericName}");
                    if (nativeType != null)
                        break;
                }
            }

            CSharpObject solutionType = null;
            if (nativeType == null)
            {
                foreach (var item in solution.Classes.Concat(solution.Structs).Concat(solution.Interfaces).Concat(solution.Enums).Concat(solution.Delegates))
                {
                    if (ns.ToString() == item.Namespace?.ToString() || usings.Any(x => x.ToString() == item.Namespace?.ToString()))
                    {
                        if (item.Name == genericName)
                        {
                            solutionType = item;
                            break;
                        }
                    }
                }
            }

            var csType = new CSharpType(genericName, nativeType, solutionType, genericArguments);
            return csType;
        }
        private static Type GetSystemType(string typeName)
        {
            return typeName switch
            {
                "bool" => typeof(bool),
                "byte" => typeof(byte),
                "sbyte" => typeof(sbyte),
                "short" => typeof(short),
                "ushort" => typeof(ushort),
                "int" => typeof(int),
                "uint" => typeof(uint),
                "long" => typeof(long),
                "ulong" => typeof(ulong),
                "float" => typeof(float),
                "double" => typeof(double),
                "decimal" => typeof(decimal),
                "char" => typeof(char),
                "string" => typeof(string),
                "Boolean" => typeof(bool),
                "Byte" => typeof(byte),
                "SByte" => typeof(sbyte),
                "Int16" => typeof(short),
                "UInt16" => typeof(ushort),
                "Int32" => typeof(int),
                "UInt32" => typeof(uint),
                "Int64" => typeof(long),
                "UInt64" => typeof(ulong),
                "Single" => typeof(float),
                "Double" => typeof(double),
                "Decimal" => typeof(decimal),
                "Char" => typeof(char),
                "String" => typeof(string),
                _ => null,
            };
        }
        public override string ToString()
        {
            var nsText = ns == null ? "" : $"{ns}.";
            return $"{nsText}{Name}";
        }
    }
}
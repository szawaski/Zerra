// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.IO;

namespace Zerra
{
    public class TypeKey
    {
        private readonly string str;
        private readonly Type type1;
        private readonly Type type2;
        private readonly Type[] typeArray;

        public string Str => str;
        public Type Type1 => type1;
        public Type Type2 => type2;
        public IReadOnlyCollection<Type> TypeArray => typeArray;

        public TypeKey(Type type1, Type type2)
        {
            this.str = null;
            this.type1 = type1;
            this.type2 = type2;
            this.typeArray = null;
        }
        public TypeKey(Type[] typeArray)
        {
            this.str = null;
            this.type1 = null;
            this.type2 = null;
            this.typeArray = typeArray;
        }
        public TypeKey(Type type1, Type[] typeArray)
        {
            this.str = null;
            this.type1 = type1;
            this.type2 = null;
            this.typeArray = typeArray;
        }
        public TypeKey(Type type1, Type type2, Type[] typeArray)
        {
            this.str = null;
            this.type1 = type1;
            this.type2 = type2;
            this.typeArray = typeArray;
        }
        public TypeKey(string str, Type type1)
        {
            this.str = str;
            this.type1 = type1;
            this.type2 = null;
            this.typeArray = null;
        }
        public TypeKey(string str, Type type1, Type type2)
        {
            this.str = str;
            this.type1 = type1;
            this.type2 = type2;
            this.typeArray = null;
        }
        public TypeKey(string str, Type[] typeArray)
        {
            this.str = str;
            this.type1 = null;
            this.type2 = null;
            this.typeArray = typeArray;
        }
        public TypeKey(string str, Type type1, Type[] typeArray)
        {
            this.str = str;
            this.type1 = type1;
            this.type2 = null;
            this.typeArray = typeArray;
        }
        public TypeKey(string str, Type type1, Type type2, Type[] typeArray)
        {
            this.str = str;
            this.type1 = type1;
            this.type2 = type2;
            this.typeArray = typeArray;
        }

        public override bool Equals(object obj)
        {
            if (obj is not TypeKey objCasted)
                return false;
            if (this.type1 != objCasted.type1 || this.type2 != objCasted.type2 || this.typeArray?.Length != objCasted.typeArray?.Length || this.str != objCasted.str)
                return false;

            if (this.typeArray != null)
            {
                for (var i = 0; i < this.typeArray.Length; i++)
                {
                    if (this.typeArray[i] != objCasted.typeArray[i])
                        return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
#if !NETSTANDARD2_0

            if (typeArray == null)
                return HashCode.Combine(str, type1, type2);

            switch (typeArray.Length)
            {
                case 0: return HashCode.Combine(str, type1, type2);
                case 1: return HashCode.Combine(str, type1, type2, typeArray[0]);
                case 2: return HashCode.Combine(str, type1, type2, typeArray[0], typeArray[1]);
                case 3: return HashCode.Combine(str, type1, type2, typeArray[0], typeArray[1], typeArray[2]);
                case 4: return HashCode.Combine(str, type1, type2, typeArray[0], typeArray[1], typeArray[2], typeArray[3]);
                case 5: return HashCode.Combine(str, type1, type2, typeArray[0], typeArray[1], typeArray[2], typeArray[3], typeArray[4]);
                case 6:
                    return HashCode.Combine(str, type1, type2, typeArray[0], typeArray[1], typeArray[2], typeArray[3],
                        HashCode.Combine(typeArray[4], typeArray[5]));
                case 7:
                    return HashCode.Combine(str, type1, type2, typeArray[0], typeArray[1], typeArray[2], typeArray[3],
                        HashCode.Combine(typeArray[4], typeArray[5], typeArray[6]));
                case 8:
                    return HashCode.Combine(str, type1, type2, typeArray[0], typeArray[1], typeArray[2], typeArray[3],
                        HashCode.Combine(typeArray[4], typeArray[5], typeArray[6], typeArray[7]));
                case 9:
                    return HashCode.Combine(str, type1, type2, typeArray[0], typeArray[1], typeArray[2], typeArray[3],
                        HashCode.Combine(typeArray[4], typeArray[5], typeArray[6], typeArray[7], typeArray[8]));
                case 10:
                    return HashCode.Combine(str, type1, type2, typeArray[0], typeArray[1], typeArray[2], typeArray[3],
                        HashCode.Combine(typeArray[4], typeArray[5], typeArray[6], typeArray[7], typeArray[8], typeArray[9]));
                case 11:
                    return HashCode.Combine(str, type1, type2, typeArray[0], typeArray[1], typeArray[2], typeArray[3],
                        HashCode.Combine(typeArray[4], typeArray[5], typeArray[6], typeArray[7], typeArray[8], typeArray[9], typeArray[10]));
                case 12:
                    return HashCode.Combine(str, type1, type2, typeArray[0], typeArray[1], typeArray[2], typeArray[3],
                        HashCode.Combine(typeArray[4], typeArray[5], typeArray[6], typeArray[7], typeArray[8], typeArray[9], typeArray[10], typeArray[11]));
            }

            var code = HashCode.Combine(str, type1, type2, typeArray[0], typeArray[1], typeArray[2], typeArray[3],
                            HashCode.Combine(typeArray[4], typeArray[5], typeArray[6], typeArray[7], typeArray[8], typeArray[9], typeArray[10], typeArray[11]));
            for (var i = 12; i < typeArray.Length; i++)
                code = HashCode.Combine(code, typeArray[i]);

            return code;
#else
            unchecked
            {
                var hash = (int)2166136261;
                if (str != null)
                    hash = (hash * 16777619) ^ str.GetHashCode();
                if (type1 != null)
                    hash = (hash * 16777619) ^ type1.GetHashCode();
                if (type2 != null)
                    hash = (hash * 16777619) ^ type2.GetHashCode();
                if (typeArray != null)
                {
                    for (var i = 0; i < typeArray.Length; i++)
                        hash = (hash * 16777619) ^ typeArray[i].GetHashCode();
                }
                return hash;
            }
#endif
        }

        public override string ToString()
        {
            var writer = new CharWriter(128);
            try
            {
                if (str != null)
                {
                    writer.Write(str);
                }
                if (type1 != null)
                {
                    if (writer.Length > 0)
                        writer.Write(", ");
                    writer.Write(type1.GetNiceName());
                }
                if (type2 != null)
                {
                    if (writer.Length > 0)
                        writer.Write(", ");
                    writer.Write(type2.GetNiceName());
                }
                if (typeArray != null)
                {
                    if (writer.Length > 0)
                        writer.Write(", ");
                    writer.Write("[");
                    for (var i = 0; i < typeArray.Length; i++)
                    {
                        if (i > 0)
                            writer.Write(", ");
                        writer.Write(typeArray[i].GetNiceName());
                    }
                    writer.Write("]");
                }
                return writer.ToString();
            }
            finally
            {
                writer.Dispose();
            }
        }
    }
}
// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.IO;

namespace Zerra
{
    public readonly struct TypeKey
    {
        public readonly string Str;
        public readonly Type Type1;
        public readonly Type Type2;
        public readonly Type[] TypeArray;

        public TypeKey(Type type1, Type type2)
        {
            this.Str = null;
            this.Type1 = type1;
            this.Type2 = type2;
            this.TypeArray = null;
        }
        public TypeKey(Type[] typeArray)
        {
            this.Str = null;
            this.Type1 = null;
            this.Type2 = null;
            this.TypeArray = typeArray;
        }
        public TypeKey(Type type1, Type[] typeArray)
        {
            this.Str = null;
            this.Type1 = type1;
            this.Type2 = null;
            this.TypeArray = typeArray;
        }
        public TypeKey(Type type1, Type type2, Type[] typeArray)
        {
            this.Str = null;
            this.Type1 = type1;
            this.Type2 = type2;
            this.TypeArray = typeArray;
        }
        public TypeKey(string str, Type type1)
        {
            this.Str = str;
            this.Type1 = type1;
            this.Type2 = null;
            this.TypeArray = null;
        }
        public TypeKey(string str, Type type1, Type type2)
        {
            this.Str = str;
            this.Type1 = type1;
            this.Type2 = type2;
            this.TypeArray = null;
        }
        public TypeKey(string str, Type[] typeArray)
        {
            this.Str = str;
            this.Type1 = null;
            this.Type2 = null;
            this.TypeArray = typeArray;
        }
        public TypeKey(string str, Type type1, Type[] typeArray)
        {
            this.Str = str;
            this.Type1 = type1;
            this.Type2 = null;
            this.TypeArray = typeArray;
        }
        public TypeKey(string str, Type type1, Type type2, Type[] typeArray)
        {
            this.Str = str;
            this.Type1 = type1;
            this.Type2 = type2;
            this.TypeArray = typeArray;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TypeKey objCasted))
                return false;
            if (this.Type1 != objCasted.Type1 || this.Type2 != objCasted.Type2 || this.TypeArray?.Length != objCasted.TypeArray?.Length || this.Str != objCasted.Str)
                return false;

            if (this.TypeArray != null)
            {
                for (var i = 0; i < this.TypeArray.Length; i++)
                    if (this.TypeArray[i] != objCasted.TypeArray[i])
                        return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)2166136261;
                if (Str != null)
                    hash = (hash * 16777619) ^ Str.GetHashCode();
                if (Type1 != null)
                    hash = (hash * 16777619) ^ Type1.GetHashCode();
                if (Type2 != null)
                    hash = (hash * 16777619) ^ Type2.GetHashCode();
                if (TypeArray != null)
                {
                    for (var i = 0; i < TypeArray.Length; i++)
                        hash = (hash * 16777619) ^ TypeArray[i].GetHashCode();
                }
                return hash;
            }
        }

        public override string ToString()
        {
            var writer = new CharWriteBuffer(128);
            try
            {
                if (Str != null)
                {
                    writer.Write(Str);
                }
                if (Type1 != null)
                {
                    if (writer.Length > 0)
                        writer.Write(", ");
                    writer.Write(Type1.GetNiceName());
                }
                if (Type2 != null)
                {
                    if (writer.Length > 0)
                        writer.Write(", ");
                    writer.Write(Type2.GetNiceName());
                }
                if (TypeArray != null)
                {
                    if (writer.Length > 0)
                        writer.Write(", ");
                    writer.Write("[");
                    for (var i = 0; i < TypeArray.Length; i++)
                    {
                        if (i > 0)
                            writer.Write(", ");
                        writer.Write(TypeArray[i].GetNiceName());
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
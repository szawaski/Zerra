﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal abstract class ByteConverterObjectMember
    {
        protected ByteConverterOptions options { get; private set; }
        protected TypeDetail parent { get; private set; }
        public MemberDetail Member { get; private set; }

        public ByteConverterObjectMember(ByteConverterOptions options, TypeDetail parent, MemberDetail member)
        {
            this.options = options;
            this.parent = parent;
            this.Member = member;
        }

        public override string ToString()
        {
            return Member.Name;
        }
    }
}
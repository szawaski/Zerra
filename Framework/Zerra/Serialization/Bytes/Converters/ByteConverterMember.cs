// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal abstract class ByteConverterMember
    {
        protected OptionsStruct options { get; private set; }
        public MemberDetail Member { get; private set; }

        public ByteConverterMember(OptionsStruct options, MemberDetail member)
        {
            this.options = options;
            this.Member = member;
        }

        public override string ToString()
        {
            return Member.Name;
        }
    }
}
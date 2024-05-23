﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterObjectMember<TParent> : ByteConverterObjectMember
    {
        public ByteConverterObjectMember(ByteConverterOptions options, TypeDetail parent, MemberDetail member)
            : base(options, parent, member)
        {

        }

        private ByteConverter<TParent>? converter = null;
        public ByteConverter<TParent> Converter
        {
            get
            {
                if (converter == null)
                {
                    lock (this)
                    {
                        converter ??= ByteConverterFactory<TParent>.Get(options, Member.TypeDetail, parent, Member.Getter, Member.Setter);
                    }
                }
                return converter;
            }
        }
    }
}
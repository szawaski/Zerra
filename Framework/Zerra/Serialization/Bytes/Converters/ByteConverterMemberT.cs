// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterMember<TParent> : ByteConverterMember
    {
        public ByteConverterMember(OptionsStruct options, MemberDetail member)
            : base(options, member)
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
                        if (converter == null)
                        {
                            var converterRaw = ByteConverterFactory.Get<TParent>(options, Member.TypeDetail, Member);
                            converter ??= ByteConverterFactory.GetMayNeedTypeInfo(options, Member.TypeDetail, converterRaw);
                        }
                    }
                }
                return converter;
            }
        }
    }
}
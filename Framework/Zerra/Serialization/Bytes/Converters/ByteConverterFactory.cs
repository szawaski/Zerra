// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal static class ByteConverterFactory
    {
        public static ByteConverter<TParent> Get<TParent>(OptionsStruct options, TypeDetail typeDetail, MemberDetail? member)
        {
            //TODO need to cache by options, type, member
            var newConverter = Create<TParent>(typeDetail);
            newConverter.Setup(options, typeDetail, member);
            return newConverter;
        }

        public static bool NeedTypeInfo(bool includePropertyTypes, TypeDetail typeDetail)
        {
            return includePropertyTypes || (typeDetail.Type.IsInterface && !typeDetail.IsIEnumerableGeneric);
        }

        public static ByteConverter<TParent> GetMayNeedTypeInfo<TParent>(OptionsStruct options, TypeDetail typeDetail, ByteConverter<TParent> converter)
        {
            if (NeedTypeInfo(options.IncludePropertyTypes, typeDetail))
                return GetNeedTypeInfo(options, converter);
            return converter;
        }

        public static ByteConverter<TParent> GetNeedTypeInfo<TParent>(OptionsStruct options, ByteConverter<TParent>? converter = null)
        {
            //TODO need to cache by parent, converter
            var newConverter = new ByteConverterTypeInfo<TParent>(converter);
            newConverter.Setup(options, null, null);
            return newConverter;
        }

        private static ByteConverter<TParent> Create<TParent>(TypeDetail typeDetail)
        {
            var discoveredTypes = Discovery.GetImplementationClasses(typeof(IByteConverter<>));
            foreach (var discoveredType in discoveredTypes)
            {
                var discoveredTypeDetail = discoveredType.GetTypeDetail();
                if (discoveredTypeDetail.InnerTypes.Count == 2 && discoveredTypeDetail.InnerTypes[1] == typeDetail.Type)
                {
                    var converter = discoveredTypeDetail.Creator();
                    return (ByteConverter<TParent>)converter;
                }
            }

            var converterObject = typeof(ByteConverterObject<,>).GetGenericTypeDetail(typeof(TParent), typeDetail.Type).Creator();
            return (ByteConverter<TParent>)converterObject;
        }
    }
}
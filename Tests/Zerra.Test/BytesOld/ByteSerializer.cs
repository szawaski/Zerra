// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Zerra.Collections;

namespace Zerra.Serialization.Bytes
{
    /*
    Specs:
    -Deserialization depends on using the exact same contract (object type) including property order and serializer settings.  Property names disreguarded.
    -Option to use SerializerIndex attribute to define indexes 0 to (255-1) instead of depending on property order.  Property names disreguarded.
    -Option (propertyNames parameter) to use property name to match properties instead of property indexes or property ordering. Significant data size increase.
    -Option (propertyTypes parameter) to include object types to record and instanciate the exact instance type which is useful when properties are boxing/polymorphic. Significant data size increase.
    -Option (ignoreIndexAttribute parameter) to disreguard SerializerIndex attributes and rely on ordering.
    -Option (indexSize parameter) to increase property index size to ushort to allow indexes of 0 to (65535-1). Some data size increase.  Note: One byte reserved for end object flag.
    -Option (encoding parameter) to change the default string encoder from Unicode.  Does not affect char or char[].  Changes data size and may have loss if using UTF8 for example.

    Segments:
        TypeInfo: {int-length}{string-typename} - Excluded by default
        PropertyName: {int-length}{string-propertyname} - Excluded by default
        ObjectData: {bool-notNull}foreachproperty:[({byte||ushort-propertyIndex}||{PropertyName}){Object}]({byte||ushort-endObjectFlag}||{0})

    Object:
        CoreType: {TypeInfo?}{TypeData}
        String: {TypeInfo?}{int-length}{encodedbytes}
        Object: {TypeInfo?}{ObjectData}
        Enumerable: {TypeInfo?}{int-count}foreachobject:[{ObjectData}]
    */

    /// <summary>
    /// Converts objects to bytes and back with minimal size and maximum speed.
    /// </summary>
    public static partial class ByteSerializerOld
    {
        private const int defaultBufferSize = 8 * 1024;
#if DEBUG
        public static bool Testing { get; set; }
#endif

        private static readonly Type genericListType = typeof(List<>);
        private static readonly Type genericHashSetType = typeof(HashSet<>);
        private static readonly MethodInfo enumerableToArrayMethod = typeof(Enumerable).GetMethod("ToArray") ?? throw new Exception($"{nameof(Enumerable)}.ToArray method not found");
        private static readonly Type enumerableType = typeof(IEnumerable<>);
        private static readonly Type dictionaryType = typeof(Dictionary<,>);

        private static readonly Encoding defaultEncoding = Encoding.UTF8;

        //In byte array, object properties start with index values from SerializerIndexAttribute or property order
        private const ushort indexOffset = 1; //offset index values to reseve for Flag: 0

        //Flag: 0 indicating the end of an object
        private const ushort endObjectFlagUShort = 0;
        private const byte endObjectFlagByte = 0;
        private static readonly byte[] endObjectFlagUInt16 = [0, 0];

        private static readonly ByteSerializerOptions defaultOptions = new();

        private static readonly ConcurrentFactoryDictionary<int, ConcurrentFactoryDictionary<Type, SerializerTypeDetail>> typeInfoCache = new();
        private static SerializerTypeDetail GetTypeInformation(Type type, ByteSerializerIndexType indexSize, bool ignoreIndexAttribute)
        {
            var dictionarySetIndex = ((int)indexSize + 1) * (ignoreIndexAttribute ? 1 : 2);
            var dictionarySet = typeInfoCache.GetOrAdd(dictionarySetIndex, (_) =>
            {
                return new ConcurrentFactoryDictionary<Type, SerializerTypeDetail>();
            });
            var typeInfo = dictionarySet.GetOrAdd(type, (_) =>
            {
                return new SerializerTypeDetail(indexSize, ignoreIndexAttribute, type);
            });
            return typeInfo;
        }
    }
}
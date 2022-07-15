// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Zerra.Collections;

namespace Zerra.Serialization
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
    public partial class ByteSerializer
    {
        private const int defaultBufferSize = 8 * 1024;

        private static readonly Type genericListType = typeof(List<>);
        private static readonly MethodInfo enumerableToArrayMethod = typeof(Enumerable).GetMethod("ToArray");
        private static readonly Type enumerableType = typeof(IEnumerable<>);

        private static readonly Encoding defaultEncoding = Encoding.UTF8;

        //In byte array, object properties start with index values from SerializerIndexAttribute or property order
        public const ushort IndexOffset = 1; //offset index values to reseve for Flag: 0

        //Flag: 0 indicating the end of an object
        private const ushort endObjectFlagUShort = 0;
        private const byte endObjectFlagByte = 0;
        private static readonly byte[] endObjectFlagUInt16 = new byte[2] { 0, 0 };

        private readonly bool usePropertyNames;
        private readonly bool includePropertyTypes;
        private readonly bool ignoreIndexAttribute;
        private readonly ByteSerializerIndexSize indexSize;
        private readonly Encoding encoding;

        /// <summary>
        /// Create serializer to convert objects to bytes and back.  Collections must be arrays or convertible from a List.  The deserializing instance must match the the specifications used to serialize.  When inlcuding type information, type safety is enforced so the deserialized types must be the same or inherit from the origional.
        /// </summary>
        /// <param name="propertyNames">Include property names in the serialization. Default false.</param>
        /// <param name="propertyTypes">Include type information for non-coretypes in the serialization. This allows the data to be deserialized without knowing the type beforehand and needed for properties that are boxed or an interface. Default false.</param>
        /// <param name="ignoreIndexAttribute">Ignore the index attribute marked on properties. Default false.</param>
        /// <param name="indexSize">Size of the property indexes. Use Byte unless the number of properties in an object exceeds 255. Default Byte.</param>
        /// <param name="encoding">The text encoder used for char and string.  Default System.Text.Encoding.Unicode.</param>
        public ByteSerializer(bool propertyNames = false, bool propertyTypes = false, bool ignoreIndexAttribute = false, ByteSerializerIndexSize indexSize = ByteSerializerIndexSize.Byte, Encoding encoding = null)
        {
            this.usePropertyNames = propertyNames;
            this.includePropertyTypes = propertyTypes;
            this.ignoreIndexAttribute = ignoreIndexAttribute;
            this.indexSize = indexSize;
            if (encoding != null)
                this.encoding = encoding;
            else
                this.encoding = defaultEncoding;
        }

        private static readonly ConcurrentFactoryDictionary<int, ConcurrentFactoryDictionary<Type, SerializerTypeDetails>> typeInfoCache = new();
        private static SerializerTypeDetails GetTypeInformation(Type type, ByteSerializerIndexSize indexSize, bool ignoreIndexAttribute)
        {
            var dictionarySetIndex = ((int)indexSize + 1) * (ignoreIndexAttribute ? 1 : 2);
            var dictionarySet = typeInfoCache.GetOrAdd(dictionarySetIndex, (factoryKey) =>
            {
                return new ConcurrentFactoryDictionary<Type, SerializerTypeDetails>();
            });
            var typeInfo = dictionarySet.GetOrAdd(type, (factoryKey) =>
            {
                return new SerializerTypeDetails(indexSize, ignoreIndexAttribute, type);
            });
            return typeInfo;
        }
    }
}
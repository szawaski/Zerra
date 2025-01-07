// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Serialization.Bytes.Converters;

namespace Zerra.Serialization.Bytes
{
    /*
    Specs:
    -Default Deserialization depends on using the exact same contract (object type) including property type and order.  Property names disreguarded.
    -Attribute SerializerIndex - Define indexes 0 to 254 (0 to 65534 for ushort) on object members instead of depending on property order.
    -Attribute NonSerialized - Object member will not be serialized.
    -Option UsePropertyTypes - Include object types to record and instanciate the exact instance type which is useful when properties are boxed/interfaced/polymorphic. Significant data size and performance penalty.
    -Option IgnoreIndexAttribute - Disreguard all SerializerIndex attributes and rely on ordering.
    -Option IndexType - Increase property index size from byte to ushort to allow indexes of 0 to 65534 or use Propery Names in place of indexes.

    Object:
        CoreType: {bool-notNull}{TypeInfo?}{Segment-TypeData}
        String: {bool-notNull}{TypeInfo?}{int-length}{encodedbytes}
        Object: {bool-notNull}{TypeInfo?}{Segment-ObjectData}
        Enumerable: {bool-notNull}{TypeInfo?}{int-count}foreachobject:[{Segment-ObjectData}]
    *If from an ObjectData property, null flags are omitted, null is indicated by absensce of the property

    Segments:
        TypeInfo: {int-length}{string-typename} - Excluded by default
        PropertyName: {int-length}{string-propertyname} - Excluded by default
        ObjectData: {bool-notNull}foreachproperty:[({byte||ushort-propertyIndex}||{PropertyName}){Object}]({byte||ushort-endObjectFlag}||{0})
    */

    /// <summary>
    /// Converts objects to bytes and back with minimal size and maximum speed.
    /// </summary>
    public static partial class ByteSerializer
    {
        private const int defaultBufferSize = 8 * 1024;        

        private static readonly ByteSerializerOptions defaultOptions = new();

        public static void AddConverter(Type converterType, Type valueType) => ByteConverterDiscovery.AddConverter(converterType, valueType);
    }
}
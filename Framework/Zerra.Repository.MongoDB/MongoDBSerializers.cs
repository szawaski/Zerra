// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System.Dynamic;

namespace Zerra.Repository.MongoDB
{
    //the driver reads every reply with serializers for dynamic arrays and documents, which it creates by reflection a native build trims away,
    //so they're registered as objects before any client is made; one the application registered first is kept
    internal static class MongoDBSerializers
    {
        private static readonly bool registered = Register();

        public static void Ensure() => _ = registered;

        private static bool Register()
        {
            _ = BsonSerializer.TryRegisterSerializer(typeof(List<object>), new EnumerableInterfaceImplementerSerializer<List<object>, object>());
            _ = BsonSerializer.TryRegisterSerializer(typeof(ExpandoObject), new ExpandoObjectSerializer());
            return true;
        }
    }
}

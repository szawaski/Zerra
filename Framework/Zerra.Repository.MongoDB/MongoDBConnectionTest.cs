// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using MongoDB.Bson;
using MongoDB.Driver;
using Zerra.Logging;

namespace Zerra.Repository.MongoDB
{
    /// <summary>
    /// Checks whether a MongoDB server can be reached, such as at startup to choose between MongoDB and another data store.
    /// </summary>
    public static class MongoDBConnectionTest
    {
        /// <summary>
        /// Tests the connection by pinging the server, so the database itself doesn't have to exist yet.
        /// </summary>
        /// <param name="connectionString">The MongoDB connection string, its serverSelectionTimeoutMS sets how long to wait.</param>
        /// <param name="log">Optional logger, told why the connection failed.</param>
        /// <returns>True if the server answered; otherwise false.</returns>
        public static bool Test(string connectionString, ILogger? log = null)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            try
            {
                MongoDBSerializers.Ensure();
                var client = new MongoClient(connectionString);
                var result = client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
                if (result.TryGetValue("ok", out var ok) && ok.ToDouble() == 1)
                    return true;

                log?.Warn($"{nameof(MongoDBConnectionTest)} could not connect: {result}");
                return false;
            }
            catch (Exception ex)
            {
                log?.Warn($"{nameof(MongoDBConnectionTest)} could not connect: {ex.Message}");
                return false;
            }
        }
    }
}

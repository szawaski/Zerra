// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.MariaDb;
using Zerra.Repository.MsSql;
using Zerra.Repository.MySql;
using Zerra.Repository.PostgreSql;
using Zerra.Repository.Test.MariaDb;
using Zerra.Repository.Test.MsSql;
using Zerra.Repository.Test.MySql;
using Zerra.Repository.Test.PostgreSql;

namespace Zerra.Repository.Test
{
    //each store's connection test answers for a running server, and logs why when nothing answers
    public class ConnectionTests
    {
        private sealed class WarnLog : Zerra.Logging.ILogger
        {
            public readonly List<string> Warnings = new();
            public void Trace(string message) { }
            public void Debug(string message) { }
            public void Info(string message) { }
            public void Warn(string message) { lock (Warnings) Warnings.Add(message); }
            public void Error(string? message = null, Exception? ex = null) { }
            public void Error(Exception? ex = null) { }
            public void Critical(string? message = null, Exception? ex = null) { }
            public void Critical(Exception? ex = null) { }
        }

        public static TheoryData<string> Stores => new() { "MsSql", "PostgreSql", "MySql", "MariaDb" };

        private static bool Test(string store, string connectionString, Zerra.Logging.ILogger? log) => store switch
        {
            "MsSql" => MsSqlConnectionTest.Test(connectionString, log),
            "PostgreSql" => PostgreSqlConnectionTest.Test(connectionString, log),
            "MySql" => MySqlConnectionTest.Test(connectionString, log),
            _ => MariaDbConnectionTest.Test(connectionString, log),
        };

        [Theory]
        [MemberData(nameof(Stores))]
        public void RunningServerAnswers(string store)
        {
            var connectionString = store switch
            {
                "MsSql" => MsSqlTestSqlDataContext.ConnectionString,
                "PostgreSql" => PostgreSqlTestSqlDataContext.ConnectionString,
                "MySql" => MySqlTestSqlDataContext.ConnectionString,
                _ => MariaDbTestSqlDataContext.ConnectionString,
            };
            var log = new WarnLog();
            Assert.True(Test(store, connectionString, log));
            Assert.Empty(log.Warnings);
        }

        [Theory]
        [MemberData(nameof(Stores))]
        public void NothingListening(string store)
        {
            var connectionString = store switch
            {
                "MsSql" => "Server=localhost,1;Connect Timeout=1;TrustServerCertificate=True;User ID=x;Password=y",
                "PostgreSql" => "Host=localhost;Port=1;Timeout=1;Username=x;Password=y",
                _ => "Server=localhost;Port=1;Uid=x;Pwd=y;Connection Timeout=1",
            };
            var log = new WarnLog();
            Assert.False(Test(store, connectionString, log));
            Assert.Contains("could not connect", Assert.Single(log.Warnings));
            Assert.False(Test(store, connectionString, null));
            _ = Assert.Throws<ArgumentNullException>(() => Test(store, " ", null));
        }
    }
}

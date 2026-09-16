// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.Data.SqlClient;
using Zerra.Repository.MsSql;

namespace Zerra.Repository.Test.MsSql
{
    public class MsSqlTestSqlDataContext : MsSqlDataContext
    {
        private const string sqlAccountConnectionString = "data source=.;initial catalog=ZerraSqlTest;user id=sa;password=Password123;MultipleActiveResultSets=True;TrustServerCertificate=True;";
        private const string windowsAuthConnectionString = "data source=.;initial catalog=ZerraSqlTest;integrated security=True;MultipleActiveResultSets=True;TrustServerCertificate=True;";

        //the SQL Server account is tried first, which a SQL Server in Docker needs, then Windows authentication for a local install without it
        private static readonly Lazy<string> connectionString = new(() =>
        {
            var builder = new SqlConnectionStringBuilder(sqlAccountConnectionString)
            {
                InitialCatalog = "master",
                ConnectTimeout = 3
            };
            try
            {
                using var connection = new SqlConnection(builder.ToString());
                connection.Open();
                return sqlAccountConnectionString;
            }
            catch (SqlException)
            {
                return windowsAuthConnectionString;
            }
        });

        public override string GetConnectionString() => connectionString.Value;
    }
}

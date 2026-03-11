using System;
using System.Collections.Generic;
using System.Linq;
using Zerra.Logging;

namespace Zerra.Repository.PostgreSql
{
    public sealed class PostgreSqlDataStoreGenerationPlan : IDataStoreGenerationPlan
    {
        private readonly PostgreSqlEngine engine;
        private readonly string? createDatabaseName;
        private readonly ICollection<string> sql;

        public ICollection<string> Plan
        {
            get
            {
                if (String.IsNullOrWhiteSpace(createDatabaseName))
                    return sql;
                else
                    return new string[] { $"Create Database: {createDatabaseName}" }.Concat(sql).ToArray();
            }
        }

        public PostgreSqlDataStoreGenerationPlan(PostgreSqlEngine engine, string? createDatabaseName, ICollection<string> sql)
        {
            this.engine = engine;
            this.createDatabaseName = createDatabaseName;
            this.sql = sql;
        }

        public void Execute()
        {
            if (!String.IsNullOrWhiteSpace(createDatabaseName))
            {
                try
                {
                    engine.CreateDatabase(createDatabaseName);
                    _ = Log.InfoAsync($"{nameof(PostgreSqlEngine)}: Create Database {createDatabaseName}");
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync($"{nameof(PostgreSqlEngine)} error while creating datastore.", ex);
                }
            }

            foreach (var line in sql)
            {
                try
                {
                    engine.ExecuteSql(line);
                    _ = Log.InfoAsync($"{nameof(PostgreSqlEngine)}: {line}");
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync($"{nameof(PostgreSqlEngine)} error while assuring datastore: {line}.", ex);
                    throw;
                }
            }
        }
    }
}

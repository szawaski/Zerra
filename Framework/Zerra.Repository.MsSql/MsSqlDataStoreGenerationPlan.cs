using System;
using System.Collections.Generic;
using System.Linq;
using Zerra.Logging;

namespace Zerra.Repository.MsSql
{
    public sealed class MsSqlDataStoreGenerationPlan : IDataStoreGenerationPlan
    {
        private readonly MsSqlEngine engine;
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

        public MsSqlDataStoreGenerationPlan(MsSqlEngine engine, string? createDatabaseName, ICollection<string> sql)
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
                    Log.Info($"{nameof(MsSqlEngine)}: Create Database {createDatabaseName}");
                }
                catch (Exception ex)
                {
                    Log.Error($"{nameof(MsSqlEngine)} error while creating database.", ex);
                }
            }

            foreach (var line in sql)
            {
                try
                {
                    engine.ExecuteSql(line);
                    Log.Info($"{nameof(MsSqlEngine)}: {line}");
                }
                catch (Exception ex)
                {
                    Log.Error($"{nameof(MsSqlEngine)} error while assuring datastore: {line}.", ex);
                    throw;
                }
            }
        }
    }
}

using Zerra.Logging;

namespace Zerra.Repository.PostgreSql
{
    /// <summary>
    /// Represents a generation plan for building or updating a PostgreSQL data store schema.
    /// </summary>
    public sealed class PostgreSqlDataStoreGenerationPlan : IDataStoreGenerationPlan
    {
        private readonly PostgreSqlEngine engine;
        private readonly string? createDatabaseName;
        private readonly ICollection<string> sql;

        /// <summary>
        /// Gets the ordered collection of SQL statements that make up the generation plan.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of <see cref="PostgreSqlDataStoreGenerationPlan"/>.
        /// </summary>
        /// <param name="engine">The <see cref="PostgreSqlEngine"/> used to execute the plan.</param>
        /// <param name="createDatabaseName">The name of the database to create, or <see langword="null"/> if no database creation is required.</param>
        /// <param name="sql">The SQL statements to execute as part of the plan.</param>
        public PostgreSqlDataStoreGenerationPlan(PostgreSqlEngine engine, string? createDatabaseName, ICollection<string> sql)
        {
            this.engine = engine;
            this.createDatabaseName = createDatabaseName;
            this.sql = sql;
        }

        /// <summary>
        /// Executes the generation plan against the PostgreSQL database, creating the database and running all SQL statements.
        /// </summary>
        public void Execute(ILogger? log)
        {
            if (!String.IsNullOrWhiteSpace(createDatabaseName))
            {
                try
                {
                    engine.CreateDatabase(createDatabaseName);
                    log?.Info($"{nameof(PostgreSqlEngine)}: Create Database {createDatabaseName}");
                }
                catch (Exception ex)
                {
                    log?.Error($"{nameof(PostgreSqlEngine)} error while creating datastore.", ex);
                }
            }

            foreach (var line in sql)
            {
                try
                {
                    engine.ExecuteSql(line);
                    log?.Info($"{nameof(PostgreSqlEngine)}: {line}");
                }
                catch (Exception ex)
                {
                    log?.Error($"{nameof(PostgreSqlEngine)} error while assuring datastore: {line}.", ex);
                    throw;
                }
            }
        }
    }
}

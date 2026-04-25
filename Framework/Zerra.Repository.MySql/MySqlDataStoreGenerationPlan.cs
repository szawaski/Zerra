using Zerra.Logging;

namespace Zerra.Repository.MySql
{
    /// <summary>
    /// Represents a generation plan for building or updating a MySQL data store schema.
    /// </summary>
    public sealed class MySqlDataStoreGenerationPlan : IDataStoreGenerationPlan
    {
        private readonly MySqlEngine engine;
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
        /// Initializes a new instance of <see cref="MySqlDataStoreGenerationPlan"/>.
        /// </summary>
        /// <param name="engine">The <see cref="MySqlEngine"/> used to execute the plan.</param>
        /// <param name="createDatabaseName">The name of the database to create, or <see langword="null"/> if no database creation is required.</param>
        /// <param name="sql">The SQL statements to execute as part of the plan.</param>
        public MySqlDataStoreGenerationPlan(MySqlEngine engine, string? createDatabaseName, ICollection<string> sql)
        {
            this.engine = engine;
            this.createDatabaseName = createDatabaseName;
            this.sql = sql;
        }

        /// <summary>
        /// Executes the generation plan against the MySQL database, creating the database and running all SQL statements.
        /// </summary>
        public void Execute(ILogger? log)
        {
            if (!String.IsNullOrWhiteSpace(createDatabaseName))
            {
                try
                {
                    engine.CreateDatabase(createDatabaseName);
                    log?.Info($"{nameof(MySqlEngine)}: Create Database {createDatabaseName}");
                }
                catch (Exception ex)
                {
                    log?.Error($"{nameof(MySqlEngine)} error while creating datastore.", ex);
                }
            }

            foreach (var line in sql)
            {
                try
                {
                    engine.ExecuteSql(line);
                    log?.Info($"{nameof(MySqlEngine)}: {line}");
                }
                catch (Exception ex)
                {
                    log?.Error($"{nameof(MySqlEngine)} error while assuring datastore: {line}.", ex);
                    throw;
                }
            }
        }
    }
}

using Zerra.Logging;

namespace Zerra.Repository.MsSql
{
    /// <summary>
    /// Represents a generation plan for building or updating a Microsoft SQL Server data store schema.
    /// </summary>
    public sealed class MsSqlDataStoreGenerationPlan : IDataStoreGenerationPlan
    {
        private readonly MsSqlEngine engine;
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
        /// Initializes a new instance of <see cref="MsSqlDataStoreGenerationPlan"/>.
        /// </summary>
        /// <param name="engine">The <see cref="MsSqlEngine"/> used to execute the plan.</param>
        /// <param name="createDatabaseName">The name of the database to create, or <see langword="null"/> if no database creation is required.</param>
        /// <param name="sql">The SQL statements to execute as part of the plan.</param>
        public MsSqlDataStoreGenerationPlan(MsSqlEngine engine, string? createDatabaseName, ICollection<string> sql)
        {
            this.engine = engine;
            this.createDatabaseName = createDatabaseName;
            this.sql = sql;
        }

        /// <summary>
        /// Executes the generation plan against the SQL Server database, creating the database and running all SQL statements.
        /// </summary>
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

using Zerra;
using Zerra.Repository.PostgreSql;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    public class Ledger1PostgreSqlDataContext : PostgreSqlDataContext
    {
        public override string ConnectionString => connectionString;

        private readonly string connectionString;
        public Ledger1PostgreSqlDataContext()
        {
            this.connectionString = Config.GetSetting("Ledger1SqlConnectionStringPOSTGRESQL");
        }
    }
}

using Zerra;
using Zerra.Repository.Sql.MsSql;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    public class Ledger1SqlDataContext : MsSqlDataContext
    {
        public override string ConnectionString => connectionString;

        private readonly string connectionString;
        public Ledger1SqlDataContext()
        {
            this.connectionString = Config.GetSetting("LedgerSqlConnectionString");
            AssureDataStore();
        }
    }
}

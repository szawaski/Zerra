using Zerra;
using Zerra.Repository.MsSql;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    public class Ledger1MySqlDataContext : MsSqlDataContext
    {
        public override string ConnectionString => connectionString;

        private readonly string connectionString;
        public Ledger1MySqlDataContext()
        {
            this.connectionString = Config.GetSetting("Ledger1SqlConnectionStringMYSQL");
        }
    }
}

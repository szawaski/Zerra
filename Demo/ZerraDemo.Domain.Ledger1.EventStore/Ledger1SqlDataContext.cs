using Zerra;
using Zerra.Repository.Sql.MsSql;
using Zerra.Repository.Sql.PostgreSql;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    //public class Ledger1SqlDataContext : MsSqlDataContext
    public class Ledger1SqlDataContext : PostgreSqlDataContext
    {
        public override string ConnectionString => connectionString;

        private readonly string connectionString;
        public Ledger1SqlDataContext()
        {
            //this.connectionString = Config.GetSetting("Ledger1SqlConnectionStringMSSQL");
            this.connectionString = Config.GetSetting("Ledger1SqlConnectionStringPOSTGRESQL");
            AssureDataStore();
        }
    }
}

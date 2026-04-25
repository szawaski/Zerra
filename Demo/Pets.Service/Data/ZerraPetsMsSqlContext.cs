using Zerra.Repository.MsSql;

namespace Pets.Service.Data
{
    public sealed class ZerraPetsMsSqlContext : MsSqlDataContext
    {
        public override string ConnectionString => connectionString;

        private readonly string connectionString;
        public ZerraPetsMsSqlContext()
        {
            this.connectionString = "Data Source=.;Initial Catalog=ZerraPets;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        }
    }
}
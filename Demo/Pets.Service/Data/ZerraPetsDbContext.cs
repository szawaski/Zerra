using Zerra.Repository.MsSql;

namespace Pets.Service.Data
{
    public class ZerraPetsDbContext : MsSqlDataContext
    {
        public override string ConnectionString => connectionString;

        private readonly string connectionString;
        public ZerraPetsDbContext()
        {
            this.connectionString = "Data Source=.;Initial Catalog=ZerraPets;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        }
    }
}

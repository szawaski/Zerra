using Zerra.Repository.MsSql;

namespace Pets.Service.Data
{
    //used when the SQL Server account in ZerraPetsMsSqlContext can't log in, e.g. a local install without it
    public sealed class ZerraPetsMsSqlWindowsAuthContext : MsSqlDataContext
    {
        public override string GetConnectionString() => connectionString;

        private readonly string connectionString;
        public ZerraPetsMsSqlWindowsAuthContext()
        {
            this.connectionString = "Data Source=.;Initial Catalog=ZerraPets;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        }
    }
}

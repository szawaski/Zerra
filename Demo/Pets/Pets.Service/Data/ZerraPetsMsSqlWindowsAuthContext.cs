using Zerra.Repository.MsSql;

namespace Pets.Service.Data
{
    //used when the SQL Server account in ZerraPetsMsSqlContext can't log in, e.g. a local install without it
    public static class ZerraPetsMsSqlWindowsAuthContext
    {
        public const string ConnectionString = "Data Source=.;Initial Catalog=ZerraPets;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
    }
}

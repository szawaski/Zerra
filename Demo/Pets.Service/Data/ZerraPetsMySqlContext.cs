using Zerra.Repository.MySql;

namespace Pets.Service.Data
{
    public sealed class ZerraPetsMySqlContext : MySqlDataContext
    {
        public override string ConnectionString => connectionString;

        private readonly string connectionString;
        public ZerraPetsMySqlContext()
        {
            this.connectionString = "Server=localhost;Port=3306;Uid=root;Pwd=password123;Database=ZerraPets";
        }
    }
}
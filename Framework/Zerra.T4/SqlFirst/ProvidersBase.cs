// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Repository;

namespace KaKush.Domain.Sql
{
    public abstract partial class KaKushSqlBaseProvider<TModel> : TransactionalProvider<KaKushSqlDatabase, TModel>
        where TModel : class, new()
    {
        protected override bool DisableEventLinking => true;
        protected override bool DisablePersistLinking => true;
    }

    public class KaKushSqlDatabase : MsSqlDataContext
    {
        public override string ConnectionString => connectionString;

        private readonly string connectionString;
        public KaKushSqlDatabase()
        {
            this.connectionString = "data source=.;initial catalog=KaKush;integrated security=True;MultipleActiveResultSets=True";
            //this.connectionString = Config.GetSetting("ConnectionString");
        }
    }
}

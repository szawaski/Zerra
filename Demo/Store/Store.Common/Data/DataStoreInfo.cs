namespace Store.Common.Data
{
    /// <summary>
    /// Describes the data store a service ended up using. Registered as a bus service so query handlers can report it.
    /// </summary>
    public interface IDataStoreInfo
    {
        string Description { get; }
    }

    public sealed class DataStoreInfo : IDataStoreInfo
    {
        public string Description { get; }

        public DataStoreInfo(string description)
        {
            this.Description = description;
        }
    }
}

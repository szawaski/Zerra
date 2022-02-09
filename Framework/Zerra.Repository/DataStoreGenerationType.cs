namespace Zerra.Repository
{
    public enum DataStoreGenerationType
    {
        None = 0,

        CodeFirst = 1,
        Preview = 2,
        NoCreate = 4,
        NoUpdate = 8,
        NoDelete = 16
    }
}

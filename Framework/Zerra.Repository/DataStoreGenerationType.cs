namespace Zerra.Repository
{
    /// <summary>
    /// Specifies the generation behavior for a data store, controlling how schema changes are applied.
    /// Multiple values can be combined using bitwise operations.
    /// </summary>
    [Flags]
    public enum DataStoreGenerationType
    {
        /// <summary>
        /// No generation behavior is applied.
        /// </summary>
        None = 0,

        /// <summary>
        /// The data store schema is generated and maintained based on the code model.
        /// </summary>
        CodeFirst = 1,

        /// <summary>
        /// Generation changes are previewed but not applied to the data store.
        /// </summary>
        Preview = 2,

        /// <summary>
        /// New schema objects will not be created in the data store.
        /// </summary>
        NoCreate = 4,

        /// <summary>
        /// Existing schema objects will not be updated in the data store.
        /// </summary>
        NoUpdate = 8,

        /// <summary>
        /// Existing schema objects will not be deleted from the data store.
        /// </summary>
        NoDelete = 16
    }
}

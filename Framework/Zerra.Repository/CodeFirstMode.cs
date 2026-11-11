namespace Zerra.Repository
{
    /// <summary>
    /// The changes <see cref="CodeFirstGeneration"/> may make to bring a data store in line with the models.
    /// Combine the values with bitwise operations. At least one of <see cref="CreateDataStore"/>, <see cref="Columns"/>, <see cref="Constraints"/>, or <see cref="DropColumns"/> is required.
    /// <see cref="Columns"/> makes the changes that can't fail on existing rows or break code still writing the old columns. <see cref="Constraints"/> makes the ones that can.
    /// To move data between them, run <see cref="Columns"/>, then the code that moves the data, then <see cref="Constraints"/>.
    /// </summary>
    [Flags]
    public enum CodeFirstMode
    {
        /// <summary>
        /// No changes. <see cref="CodeFirstGeneration.Generate"/> throws on this value.
        /// </summary>
        None = 0,

        /// <summary>
        /// Creates the database when it doesn't exist. Without it a missing database throws.
        /// </summary>
        CreateDataStore = 1,

        /// <summary>
        /// Creates tables, adds columns, widens columns and allows nulls where the models do, makes the columns the models no longer have nullable, adds indexes that aren't unique, and drops the indexes and foreign keys the models no longer have.
        /// Without <see cref="Constraints"/>, new columns are added nullable.
        /// </summary>
        Columns = 2,

        /// <summary>
        /// Makes columns NOT NULL, narrows their types, and adds unique indexes and foreign keys.
        /// Fails if the rows don't fit, such as a null left in a column becoming NOT NULL. Fewer decimal places or less time precision rounds the values instead.
        /// </summary>
        Constraints = 4,

        /// <summary>
        /// Drops the columns the models no longer have, and their data. Tables are never dropped.
        /// </summary>
        DropColumns = 8,

        /// <summary>
        /// Logs the planned changes without making them.
        /// </summary>
        Preview = 16
    }
}

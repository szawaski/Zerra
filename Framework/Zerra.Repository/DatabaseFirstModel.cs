// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// The source of one model written by <see cref="DatabaseFirstGeneration"/>.
    /// </summary>
    public sealed class DatabaseFirstModel
    {
        /// <summary>The table the model is for.</summary>
        public string TableName { get; }
        /// <summary>The table's schema, or <see langword="null"/> for the data store's default.</summary>
        public string? SchemaName { get; }
        /// <summary>The model's class name.</summary>
        public string TypeName { get; }
        /// <summary>A file name for the source, the class name with .cs.</summary>
        public string FileName { get; }
        /// <summary>The C# source of the model.</summary>
        public string Source { get; }

        internal DatabaseFirstModel(string? schemaName, string tableName, string typeName, string source)
        {
            this.SchemaName = schemaName;
            this.TableName = tableName;
            this.TypeName = typeName;
            this.FileName = typeName + ".cs";
            this.Source = source;
        }

        /// <inheritdoc />
        public override string ToString() => TypeName;
    }
}

// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Text;

namespace Zerra.Repository
{
    /// <summary>
    /// Writes model classes for the tables of an existing data store, the reverse of <see cref="CodeFirstGeneration"/>.
    /// </summary>
    /// <remarks>
    /// Each model is a partial class in its own file, so members added in another file of the same class survive regenerating.
    /// Running <see cref="CodeFirstGeneration"/> with the generated models finds nothing to change, except where a member has a note about the column.
    /// </remarks>
    public static class DatabaseFirstGeneration
    {
        private const string tab = "    ";

        /// <summary>
        /// Reads the data store's tables and writes a model for each.
        /// </summary>
        /// <param name="engine">The engine of the data store to read, such as an MsSqlEngine.</param>
        /// <param name="modelNamespace">The namespace of the models.</param>
        /// <param name="modelSuffix">Added to each table name to name its model.</param>
        /// <returns>The source of each model.</returns>
        public static IReadOnlyList<DatabaseFirstModel> Generate(ITransactStoreEngine engine, string modelNamespace, string modelSuffix = "DataModel")
        {
            if (engine is null)
                throw new ArgumentNullException(nameof(engine));
            return Generate(engine.ReadDataStoreSchema(), modelNamespace, modelSuffix);
        }

        /// <summary>
        /// Writes a model for each table.
        /// </summary>
        /// <param name="tables">The tables, such as from <see cref="ITransactStoreEngine.ReadDataStoreSchema"/>.</param>
        /// <param name="modelNamespace">The namespace of the models.</param>
        /// <param name="modelSuffix">Added to each table name to name its model.</param>
        /// <returns>The source of each model.</returns>
        public static IReadOnlyList<DatabaseFirstModel> Generate(IReadOnlyList<DataStoreSchemaTable> tables, string modelNamespace, string modelSuffix = "DataModel")
        {
            if (tables is null)
                throw new ArgumentNullException(nameof(tables));
            if (String.IsNullOrWhiteSpace(modelNamespace))
                throw new ArgumentException("The namespace is required", nameof(modelNamespace));
            modelSuffix ??= String.Empty;

            //names first, relations refer to the members of other models
            var models = new List<TableModel>(tables.Count);
            var modelsByTable = new Dictionary<string, TableModel>(StringComparer.OrdinalIgnoreCase);
            var typeNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var table in tables)
            {
                var baseName = SafeName(table.Name, "Table");
                baseName = Char.ToUpperInvariant(baseName[0]) + baseName.Substring(1);
                var typeName = Unique(baseName + modelSuffix, typeNames);
                _ = typeNames.Add(typeName);

                var model = new TableModel(table, baseName, typeName);
                _ = model.MemberNames.Add(typeName);
                foreach (var column in table.Columns)
                {
                    var name = SafeName(column.Name, "Column");
                    if (name == typeName)
                        name += "Value";
                    name = Unique(name, model.MemberNames);
                    _ = model.MemberNames.Add(name);
                    model.ColumnNames.Add(column.Name, name);
                }
                models.Add(model);
                modelsByTable[table.Name] = model;
            }

            foreach (var model in models)
            {
                foreach (var foreignKey in model.Table.ForeignKeys)
                {
                    if (!modelsByTable.TryGetValue(foreignKey.ReferencedTable, out var referenced))
                    {
                        model.Notes.Add($"Foreign key {foreignKey.Name} refers to {foreignKey.ReferencedTable}, which wasn't read");
                        continue;
                    }
                    var identities = referenced.Table.Columns.Where(x => x.IsIdentity).Select(x => x.Name).ToArray();
                    if (identities.Length != foreignKey.ReferencedColumns.Count || !identities.Select((x, i) => String.Equals(x, foreignKey.ReferencedColumns[i], StringComparison.OrdinalIgnoreCase)).All(x => x))
                    {
                        model.Notes.Add($"Foreign key {foreignKey.Name} doesn't refer to the primary key of {foreignKey.ReferencedTable}, which a relation needs");
                        continue;
                    }
                    var columnNames = new string[foreignKey.Columns.Count];
                    var found = true;
                    for (var i = 0; i < columnNames.Length; i++)
                    {
                        if (!model.ColumnNames.TryGetValue(foreignKey.Columns[i], out var columnName))
                        {
                            found = false;
                            break;
                        }
                        columnNames[i] = columnName;
                    }
                    if (!found)
                    {
                        model.Notes.Add($"Foreign key {foreignKey.Name} has a column that wasn't read");
                        continue;
                    }

                    string name;
                    if (columnNames.Length == 1 && columnNames[0].Length > 2 && columnNames[0].EndsWith("ID", StringComparison.OrdinalIgnoreCase))
                        name = columnNames[0].Substring(0, columnNames[0].Length - 2).TrimEnd('_');
                    else if (columnNames.Length == 1 && columnNames[0].Length > 3 && columnNames[0].EndsWith("Key", StringComparison.OrdinalIgnoreCase))
                        name = columnNames[0].Substring(0, columnNames[0].Length - 3).TrimEnd('_');
                    else
                        name = referenced.BaseName;
                    if (name.Length == 0 || name == model.TypeName)
                        name = referenced.BaseName + "Relation";
                    name = Unique(name, model.MemberNames);
                    _ = model.MemberNames.Add(name);
                    model.Relations.Add(new RelationModel(name, referenced.TypeName, false, columnNames.Select(x => x).ToArray()));

                    var manyName = Pluralize(model.BaseName);
                    if (model.Table.ForeignKeys.Count(x => String.Equals(x.ReferencedTable, foreignKey.ReferencedTable, StringComparison.OrdinalIgnoreCase)) > 1)
                        manyName += "By" + String.Join(String.Empty, columnNames);
                    if (manyName == referenced.TypeName)
                        manyName += "Relation";
                    manyName = Unique(manyName, referenced.MemberNames);
                    _ = referenced.MemberNames.Add(manyName);
                    referenced.Relations.Add(new RelationModel(manyName, model.TypeName, true, columnNames.Select(x => model.TypeName + "." + x).ToArray()));
                }
            }

            var results = new List<DatabaseFirstModel>(models.Count);
            var sb = new StringBuilder();
            foreach (var model in models)
            {
                WriteModel(sb, model, modelNamespace);
                results.Add(new DatabaseFirstModel(model.Table.Name, model.TypeName, sb.ToString()));
                _ = sb.Clear();
            }
            return results;
        }

        private static void WriteModel(StringBuilder sb, TableModel model, string modelNamespace)
        {
            var table = model.Table;

            _ = sb.Append("//Generated from the ").Append(table.Name).Append(" table by Zerra DatabaseFirstGeneration. Regenerate instead of editing, and add members in another file of this partial class.").Append(Environment.NewLine);
            _ = sb.Append("#nullable enable").Append(Environment.NewLine).Append(Environment.NewLine);
            _ = sb.Append("using System;").Append(Environment.NewLine);
            if (model.Relations.Any(x => x.IsMany))
                _ = sb.Append("using System.Collections.Generic;").Append(Environment.NewLine);
            _ = sb.Append("using Zerra.Repository;").Append(Environment.NewLine).Append(Environment.NewLine);
            _ = sb.Append("namespace ").Append(modelNamespace).Append(';').Append(Environment.NewLine).Append(Environment.NewLine);

            var indexAttributes = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var prefix = "IX_" + table.Name + "_";
            foreach (var index in table.Indexes)
            {
                if (!index.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    model.Notes.Add($"Index {index.Name} isn't named {prefix}..., the only names code-first gives indexes, so it has no {nameof(StoreIndexAttribute)}");
                    continue;
                }
                var indexName = index.Name.Substring(prefix.Length);
                if (indexName.Length == 0 || !indexName.All(x => Char.IsLetterOrDigit(x) || x == '_'))
                {
                    model.Notes.Add($"Index {index.Name} has a name {nameof(StoreIndexAttribute)} can't hold");
                    continue;
                }
                var columns = index.Columns.Select(x => table.Columns.FirstOrDefault(y => String.Equals(y.Name, x, StringComparison.OrdinalIgnoreCase))).ToArray();
                if (columns.Any(x => x is null))
                {
                    model.Notes.Add($"Index {index.Name} has a column that wasn't read");
                    continue;
                }
                if (columns.Any(x => (x!.Type == typeof(string) || x.Type == typeof(byte[])) && !x.PrecisionLength.HasValue))
                {
                    model.Notes.Add($"Index {index.Name} has a column without a length, which {nameof(StoreIndexAttribute)} needs");
                    continue;
                }

                var unnamed = columns.Length == 1 && String.Equals(indexName, columns[0]!.Name, StringComparison.OrdinalIgnoreCase);
                for (var i = 0; i < columns.Length; i++)
                {
                    var attribute = new StringBuilder("[StoreIndex");
                    var arguments = new List<string>();
                    if (!unnamed)
                        arguments.Add("\"" + indexName + "\"");
                    if (index.IsUnique)
                        arguments.Add("Unique = true");
                    if (i > 0)
                        arguments.Add("Order = " + i);
                    if (index.Descending[i])
                        arguments.Add("Descending = true");
                    if (arguments.Count > 0)
                        _ = attribute.Append('(').Append(String.Join(", ", arguments)).Append(')');
                    _ = attribute.Append(']');

                    if (!indexAttributes.TryGetValue(columns[i]!.Name, out var list))
                    {
                        list = new();
                        indexAttributes.Add(columns[i]!.Name, list);
                    }
                    list.Add(attribute.ToString());
                }
            }

            var entityName = table.Name.All(x => Char.IsLetterOrDigit(x) || x == '_' || x == '`');
            if (!entityName)
                model.Notes.Add($"The table name can't be an {nameof(EntityAttribute)} name, which only has letters, digits, and underscores, so the class isn't an entity");

            foreach (var note in model.Notes)
                _ = sb.Append("//").Append(note).Append(Environment.NewLine);
            if (entityName)
                _ = sb.Append("[Entity(\"").Append(table.Name).Append("\")]").Append(Environment.NewLine);
            _ = sb.Append("public partial class ").Append(model.TypeName).Append(Environment.NewLine);
            _ = sb.Append('{').Append(Environment.NewLine);

            var first = true;
            foreach (var column in table.Columns)
            {
                if (!first)
                    _ = sb.Append(Environment.NewLine);
                first = false;

                var name = model.ColumnNames[column.Name];

                if (column.Note is not null)
                    _ = sb.Append(tab).Append("//").Append(column.Note).Append(Environment.NewLine);
                if (column.IsAutoGenerated && !column.IsIdentity)
                    _ = sb.Append(tab).Append("//The data store generates this value, but it isn't in the primary key, so the model can't say so").Append(Environment.NewLine);

                if (column.Type == typeof(object))
                {
                    _ = sb.Append(tab).Append("[StoreExclude]").Append(Environment.NewLine);
                    _ = sb.Append(tab).Append("public object? ").Append(name).Append(" { get; set; }").Append(Environment.NewLine);
                    continue;
                }

                if (!column.Name.All(x => Char.IsLetterOrDigit(x) || x == '_' || x == '`'))
                {
                    _ = sb.Append(tab).Append("//The column name can't be a ").Append(nameof(StoreNameAttribute)).Append(", which only has letters, digits, and underscores, so the member isn't stored").Append(Environment.NewLine);
                    _ = sb.Append(tab).Append("[StoreExclude]").Append(Environment.NewLine);
                }
                else if (name != column.Name)
                {
                    _ = sb.Append(tab).Append("[StoreName(\"").Append(column.Name).Append("\")]").Append(Environment.NewLine);
                }

                if (column.IsIdentity)
                    _ = sb.Append(tab).Append(column.IsAutoGenerated ? "[Identity(true)]" : "[Identity]").Append(Environment.NewLine);

                var storeProperties = GetStoreProperties(column);
                if (storeProperties is not null)
                    _ = sb.Append(tab).Append(storeProperties).Append(Environment.NewLine);

                if (indexAttributes.TryGetValue(column.Name, out var columnIndexAttributes))
                {
                    foreach (var attribute in columnIndexAttributes)
                        _ = sb.Append(tab).Append(attribute).Append(Environment.NewLine);
                }

                _ = sb.Append(tab).Append("public ").Append(GetTypeName(column)).Append(' ').Append(name).Append(" { get; set; }");
                if (column.NotNull && !column.Type.IsValueType)
                    _ = sb.Append(" = null!;");
                _ = sb.Append(Environment.NewLine);
            }

            foreach (var relation in model.Relations)
            {
                if (!first)
                    _ = sb.Append(Environment.NewLine);
                first = false;

                _ = sb.Append(tab).Append("[Relation(");
                for (var i = 0; i < relation.ForeignIdentities.Length; i++)
                {
                    if (i > 0)
                        _ = sb.Append(", ");
                    _ = sb.Append("nameof(").Append(relation.ForeignIdentities[i]).Append(')');
                }
                _ = sb.Append(")]").Append(Environment.NewLine);
                _ = sb.Append(tab).Append("public ");
                if (relation.IsMany)
                    _ = sb.Append("List<").Append(relation.TypeName).Append(">?");
                else
                    _ = sb.Append(relation.TypeName).Append('?');
                _ = sb.Append(' ').Append(relation.Name).Append(" { get; set; }").Append(Environment.NewLine);
            }

            _ = sb.Append('}').Append(Environment.NewLine);
        }

        private static string? GetStoreProperties(DataStoreSchemaColumn column)
        {
            string? arguments = null;
            if (column.Scale.HasValue)
                arguments = $"{column.PrecisionLength ?? 0}, {column.Scale.Value}";
            else if (column.TextEncoding == StoreTextEncoding.NonUnicode)
                arguments = column.PrecisionLength.HasValue ? $"{nameof(StoreTextEncoding)}.{nameof(StoreTextEncoding.NonUnicode)}, {column.PrecisionLength.Value}" : $"{nameof(StoreTextEncoding)}.{nameof(StoreTextEncoding.NonUnicode)}";
            else if (column.DatePart == StoreDatePart.Date)
                arguments = column.PrecisionLength.HasValue ? $"{nameof(StoreDatePart)}.{nameof(StoreDatePart.Date)}, {column.PrecisionLength.Value}" : $"{nameof(StoreDatePart)}.{nameof(StoreDatePart.Date)}";
            else if (column.PrecisionLength.HasValue)
                arguments = column.PrecisionLength.Value.ToString();

            if (arguments is null)
                return null;
            return $"[StoreProperties({arguments})]";
        }

        private static string GetTypeName(DataStoreSchemaColumn column)
        {
            var type = column.Type;
            string name;
            if (type == typeof(bool)) name = "bool";
            else if (type == typeof(byte)) name = "byte";
            else if (type == typeof(sbyte)) name = "sbyte";
            else if (type == typeof(short)) name = "short";
            else if (type == typeof(ushort)) name = "ushort";
            else if (type == typeof(int)) name = "int";
            else if (type == typeof(uint)) name = "uint";
            else if (type == typeof(long)) name = "long";
            else if (type == typeof(ulong)) name = "ulong";
            else if (type == typeof(float)) name = "float";
            else if (type == typeof(double)) name = "double";
            else if (type == typeof(decimal)) name = "decimal";
            else if (type == typeof(char)) name = "char";
            else if (type == typeof(string)) name = "string";
            else if (type == typeof(byte[])) name = "byte[]";
            else name = type.Name;

            if (!column.NotNull)
                name += "?";
            return name;
        }

        private static string SafeName(string name, string fallback)
        {
            var sb = new StringBuilder(name.Length);
            foreach (var c in name)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_')
                    _ = sb.Append(c);
                else if (c >= '0' && c <= '9' && sb.Length > 0)
                    _ = sb.Append(c);
            }
            return sb.Length == 0 ? fallback : sb.ToString();
        }

        private static string Unique(string name, HashSet<string> used)
        {
            if (!used.Contains(name))
                return name;
            var i = 2;
            while (used.Contains(name + i))
                i++;
            return name + i;
        }

        private static string Pluralize(string name)
        {
            if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase) && !name.EndsWith("ss", StringComparison.OrdinalIgnoreCase))
                return name;
            if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase) || name.EndsWith("x", StringComparison.OrdinalIgnoreCase) || name.EndsWith("z", StringComparison.OrdinalIgnoreCase) || name.EndsWith("ch", StringComparison.OrdinalIgnoreCase) || name.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
                return name + "es";
            if (name.Length > 1 && (name[name.Length - 1] == 'y' || name[name.Length - 1] == 'Y') && "aeiouAEIOU".IndexOf(name[name.Length - 2]) < 0)
                return name.Substring(0, name.Length - 1) + "ies";
            return name + "s";
        }

        private sealed class TableModel
        {
            public readonly DataStoreSchemaTable Table;
            public readonly string BaseName;
            public readonly string TypeName;
            public readonly HashSet<string> MemberNames = new(StringComparer.Ordinal);
            public readonly Dictionary<string, string> ColumnNames = new(StringComparer.OrdinalIgnoreCase);
            public readonly List<RelationModel> Relations = new();
            public readonly List<string> Notes = new();
            public TableModel(DataStoreSchemaTable table, string baseName, string typeName)
            {
                this.Table = table;
                this.BaseName = baseName;
                this.TypeName = typeName;
            }
        }

        private sealed class RelationModel
        {
            public readonly string Name;
            public readonly string TypeName;
            public readonly bool IsMany;
            public readonly string[] ForeignIdentities;
            public RelationModel(string name, string typeName, bool isMany, string[] foreignIdentities)
            {
                this.Name = name;
                this.TypeName = typeName;
                this.IsMany = isMany;
                this.ForeignIdentities = foreignIdentities;
            }
        }
    }
}

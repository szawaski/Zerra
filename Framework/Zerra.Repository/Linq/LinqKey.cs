// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.ComponentModel;
using System.Data.Common;

namespace Zerra.Repository.Linq
{
    /// <summary>
    /// A key of several members matching a relation's rows to their parents, compared by its values: integers of any size alike, as the columns of each side can differ in size, and bytes by their content, as a store can read a Guid as its bytes.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class LinqKey : IEquatable<LinqKey>
    {
        private readonly object[] values;
        private readonly int hash;

        private LinqKey(object[] values)
        {
            this.values = values;
            var hashCode = new HashCode();
            foreach (var value in values)
                AddHash(ref hashCode, value);
            this.hash = hashCode.ToHashCode();
        }

        /// <summary>The key in columns of the row a reader is on.</summary>
        /// <param name="reader">The reader.</param>
        /// <param name="columns">The columns of the key, in ascending order as a sequential reader reads them.</param>
        /// <returns>The key, or <see langword="null"/> when a part of it is null, which refers to nothing.</returns>
        public static LinqKey? Read(DbDataReader reader, int[] columns)
        {
            var values = new object[columns.Length];
            for (var i = 0; i < columns.Length; i++)
            {
                if (reader.IsDBNull(columns[i]))
                    return null;
                values[i] = reader.GetValue(columns[i]);
            }
            return new LinqKey(values);
        }

        /// <summary>The key in columns of a row of values.</summary>
        /// <param name="row">The row's values.</param>
        /// <param name="columns">The columns of the key.</param>
        /// <returns>The key, or <see langword="null"/> when a part of it is null, which refers to nothing.</returns>
        public static LinqKey? Of(object?[] row, int[] columns)
        {
            var values = new object[columns.Length];
            for (var i = 0; i < columns.Length; i++)
            {
                var value = row[columns[i]];
                if (value is null)
                    return null;
                values[i] = value;
            }
            return new LinqKey(values);
        }

        private static object? Normalize(object? value) => value switch
        {
            byte number => (long)number,
            sbyte number => (long)number,
            short number => (long)number,
            ushort number => (long)number,
            int number => (long)number,
            uint number => (long)number,
            _ => value,
        };
        internal static bool ValueEquals(object? value, object? other)
        {
            value = Normalize(value);
            other = Normalize(other);
            if (value is byte[] bytes)
                return other is byte[] otherBytes && bytes.AsSpan().SequenceEqual(otherBytes);
            return Object.Equals(value, other);
        }
        internal static void AddHash(ref HashCode hash, object? value)
        {
            value = Normalize(value);
            if (value is byte[] bytes)
                hash.AddBytes(bytes);
            else
                hash.Add(value);
        }

        /// <inheritdoc/>
        public bool Equals(LinqKey? other)
        {
            if (other is null || other.values.Length != values.Length)
                return false;
            for (var i = 0; i < values.Length; i++)
            {
                if (!ValueEquals(values[i], other.values[i]))
                    return false;
            }
            return true;
        }
        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is LinqKey other && Equals(other);
        /// <inheritdoc/>
        public override int GetHashCode() => hash;
    }
}

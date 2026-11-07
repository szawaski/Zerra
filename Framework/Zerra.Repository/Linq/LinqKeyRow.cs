// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Zerra.Repository.Linq
{
    /// <summary>
    /// A key of several members as an element a query selects and a condition looks for, which compares by its values as <see cref="LinqKey"/> does.
    /// A condition builds it with a member initializer setting K0, K1, and so on in the key's order.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class LinqKeyRow : IEquatable<LinqKeyRow>
    {
        /// <summary>The value of the key's member at place 0.</summary>
        public object? K0 { get; set; }
        /// <summary>The value of the key's member at place 1.</summary>
        public object? K1 { get; set; }
        /// <summary>The value of the key's member at place 2.</summary>
        public object? K2 { get; set; }
        /// <summary>The value of the key's member at place 3.</summary>
        public object? K3 { get; set; }
        /// <summary>The value of the key's member at place 4.</summary>
        public object? K4 { get; set; }
        /// <summary>The value of the key's member at place 5.</summary>
        public object? K5 { get; set; }
        /// <summary>The value of the key's member at place 6.</summary>
        public object? K6 { get; set; }
        /// <summary>The value of the key's member at place 7.</summary>
        public object? K7 { get; set; }

        private static readonly PropertyInfo[] properties = [.. Enumerable.Range(0, 8).Select(i => typeof(LinqKeyRow).GetProperty($"K{i}")!)];

        //a key of several members built from their values in an expression
        internal static MemberInitExpression New(IReadOnlyList<Expression> values)
        {
            if (values.Count > properties.Length)
                throw new NotSupportedException($"A key of more than {properties.Length} members isn't supported");
            var bindings = new MemberBinding[values.Count];
            for (var i = 0; i < values.Count; i++)
                bindings[i] = Expression.Bind(properties[i], Expression.Convert(values[i], typeof(object)));
            return Expression.MemberInit(Expression.New(typeof(LinqKeyRow)), bindings);
        }

        /// <summary>The value of the key's member at a place.</summary>
        /// <param name="index">The place, from 0.</param>
        public object? this[int index] => index switch
        {
            0 => K0,
            1 => K1,
            2 => K2,
            3 => K3,
            4 => K4,
            5 => K5,
            6 => K6,
            _ => K7,
        };

        /// <inheritdoc />
        public bool Equals(LinqKeyRow? other)
        {
            if (other is null)
                return false;
            for (var i = 0; i < properties.Length; i++)
            {
                if (!LinqKey.ValueEquals(this[i], other[i]))
                    return false;
            }
            return true;
        }
        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is LinqKeyRow other && Equals(other);
        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
            for (var i = 0; i < properties.Length; i++)
                LinqKey.AddHash(ref hash, this[i]);
            return hash.ToHashCode();
        }
    }
}

// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using System.Reflection;

namespace Zerra.Repository.Linq
{
    //a key of several members as an element a query selects and a condition looks for, which compares by its values as LinqKey does
    internal sealed class LinqKeyRow : IEquatable<LinqKeyRow>
    {
        public object? K0 { get; set; }
        public object? K1 { get; set; }
        public object? K2 { get; set; }
        public object? K3 { get; set; }
        public object? K4 { get; set; }
        public object? K5 { get; set; }
        public object? K6 { get; set; }
        public object? K7 { get; set; }

        private static readonly PropertyInfo[] properties = [.. Enumerable.Range(0, 8).Select(i => typeof(LinqKeyRow).GetProperty($"K{i}")!)];

        //a key of several members built from their values in an expression
        public static MemberInitExpression New(IReadOnlyList<Expression> values)
        {
            if (values.Count > properties.Length)
                throw new NotSupportedException($"A key of more than {properties.Length} members isn't supported");
            var bindings = new MemberBinding[values.Count];
            for (var i = 0; i < values.Count; i++)
                bindings[i] = Expression.Bind(properties[i], Expression.Convert(values[i], typeof(object)));
            return Expression.MemberInit(Expression.New(typeof(LinqKeyRow)), bindings);
        }

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
        public override bool Equals(object? obj) => obj is LinqKeyRow other && Equals(other);
        public override int GetHashCode()
        {
            var hash = new HashCode();
            for (var i = 0; i < properties.Length; i++)
                LinqKey.AddHash(ref hash, this[i]);
            return hash.ToHashCode();
        }
    }
}

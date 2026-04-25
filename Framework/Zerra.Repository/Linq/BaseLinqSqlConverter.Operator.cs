namespace Zerra.Repository
{
    public abstract partial class BaseLinqSqlConverter
    {
        /// <summary>
        /// Represents the SQL operators and expression kinds used during LINQ-to-SQL conversion.
        /// </summary>
        protected enum Operator
        {
            /// <summary>No operator; used as a placeholder or for unary pass-through conversions.</summary>
            Null,
            /// <summary>Object construction (<c>new T(...)</c>).</summary>
            New,
            /// <summary>Lambda expression.</summary>
            Lambda,
            /// <summary>Direct evaluation of a constant or captured value.</summary>
            Evaluate,
            /// <summary>Conditional (ternary) expression.</summary>
            Conditional,
            /// <summary>Method call expression.</summary>
            Call,
            /// <summary>Unary negation (<c>-x</c>).</summary>
            Negative,
            /// <summary>Logical AND (<c>AND</c>).</summary>
            And,
            /// <summary>Logical OR (<c>OR</c>).</summary>
            Or,
            /// <summary>Equality comparison (<c>=</c>).</summary>
            Equals,
            /// <summary>Inequality comparison (<c>&lt;&gt;</c> or <c>!=</c>).</summary>
            NotEquals,
            /// <summary>Less-than-or-equal comparison (<c>&lt;=</c>).</summary>
            LessThanOrEquals,
            /// <summary>Greater-than-or-equal comparison (<c>&gt;=</c>).</summary>
            GreaterThanOrEquals,
            /// <summary>Less-than comparison (<c>&lt;</c>).</summary>
            LessThan,
            /// <summary>Greater-than comparison (<c>&gt;</c>).</summary>
            GreaterThan,
            /// <summary>Division (<c>/</c>).</summary>
            Divide,
            /// <summary>Subtraction (<c>-</c>).</summary>
            Subtract,
            /// <summary>Addition (<c>+</c>).</summary>
            Add,
            /// <summary>Multiplication (<c>*</c>).</summary>
            Multiply,
            /// <summary>Modulus / remainder (<c>%</c>).</summary>
            Modulus,
            /// <summary>Null equality check (<c>IS NULL</c>).</summary>
            EqualsNull,
            /// <summary>Null inequality check (<c>IS NOT NULL</c>).</summary>
            NotEqualsNull
        }
    }
}

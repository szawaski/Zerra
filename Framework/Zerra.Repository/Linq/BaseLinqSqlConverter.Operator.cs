namespace Zerra.Repository
{
    public abstract partial class BaseLinqSqlConverter
    {
        protected enum Operator
        {
            Null,
            New,
            Lambda,
            Evaluate,
            Conditional,
            Call,
            Negative,
            And,
            Or,
            Equals,
            NotEquals,
            LessThanOrEquals,
            GreaterThanOrEquals,
            LessThan,
            GreaterThan,
            Divide,
            Subtract,
            Add,
            Multiply,
            Modulus,
            EqualsNull,
            NotEqualsNull,
            //logical negation (!x), the operand is written with the inversion applied rather than a prefix, last so the existing values don't change
            Not
        }
    }
}

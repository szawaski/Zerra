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
            NotEqualsNull
        }
    }
}

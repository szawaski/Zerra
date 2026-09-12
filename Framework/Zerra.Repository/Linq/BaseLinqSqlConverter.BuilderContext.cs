namespace Zerra.Repository
{
    public abstract partial class BaseLinqSqlConverter
    {
        protected sealed class BuilderContext
        {
            public ParameterDependant RootDependant;

            public MemberContext MemberContext;

            public int InvertStack;
            public bool Inverted { get { return InvertStack % 2 != 0; } }

            //true while converting ORDER BY expressions, where a boolean member is a value to sort by rather than a condition
            public bool IsOrderBy;

            public BuilderContext(ParameterDependant rootDependant, MemberContext memberContext)
            {
                this.RootDependant = rootDependant;
                this.MemberContext = memberContext;
                this.InvertStack = 0;
            }
        }
    }
}

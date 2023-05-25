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

            public BuilderContext(ParameterDependant rootDependant, MemberContext memberContext)
            {
                this.RootDependant = rootDependant;
                this.MemberContext = memberContext;
                this.InvertStack = 0;
            }
        }
    }
}

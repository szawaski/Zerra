namespace Zerra.Repository
{
    public abstract partial class BaseLinqSqlConverter
    {
        /// <summary>
        /// Holds the state passed through each step of the SQL expression builder,
        /// including join dependency tracking, member context, and logical inversion state.
        /// </summary>
        protected sealed class BuilderContext
        {
            /// <summary>The root <see cref="ParameterDependant"/> representing the top-level model and its JOIN graph.</summary>
            public ParameterDependant RootDependant;

            /// <summary>The <see cref="MemberContext"/> tracking stacks for operators, member accesses, and model scopes.</summary>
            public MemberContext MemberContext;

            /// <summary>The current depth of logical inversion (e.g. from <c>!</c> / <c>NOT</c> expressions).</summary>
            public int InvertStack;

            /// <summary>Gets a value indicating whether the current expression logic is inverted (odd inversion depth).</summary>
            public bool Inverted { get { return InvertStack % 2 != 0; } }

            /// <summary>
            /// Initializes a new <see cref="BuilderContext"/> with the given root dependant and member context.
            /// </summary>
            /// <param name="rootDependant">The root parameter dependant for the query.</param>
            /// <param name="memberContext">The member context for the conversion.</param>
            public BuilderContext(ParameterDependant rootDependant, MemberContext memberContext)
            {
                this.RootDependant = rootDependant;
                this.MemberContext = memberContext;
                this.InvertStack = 0;
            }
        }
    }
}

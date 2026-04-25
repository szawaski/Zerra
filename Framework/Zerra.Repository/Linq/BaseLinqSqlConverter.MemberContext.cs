using System.Linq.Expressions;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract partial class BaseLinqSqlConverter
    {
        /// <summary>
        /// Holds stacks and context maps used during LINQ-to-SQL conversion for tracking
        /// model parameters, operators, member accesses, and call-rendering state.
        /// </summary>
        protected sealed class MemberContext
        {
            /// <summary>Gets the mapping from parameter names to their corresponding <see cref="ModelDetail"/>.</summary>
            public Dictionary<ParameterExpression, ModelDetail> ModelContexts { get; }

            /// <summary>Gets the stack of active <see cref="ModelDetail"/> instances as nested model scopes are entered.</summary>
            public Stack<ModelDetail> ModelStack { get; }

            /// <summary>Gets the stack of <see cref="Operator"/> values representing the current expression operator chain.</summary>
            public Stack<Operator> OperatorStack { get; }

            /// <summary>Gets the stack of <see cref="MemberExpression"/> nodes being traversed for member access resolution.</summary>
            public Stack<MemberExpression> MemberAccessStack { get; }

            /// <summary>Gets the stack of <see cref="MemberExpression"/> nodes within lambda expressions being processed.</summary>
            public Stack<MemberExpression> MemberLambdaStack { get; }

            /// <summary>Gets the stack of <see cref="ParameterDependant"/> instances tracking join dependencies.</summary>
            public Stack<ParameterDependant> DependantStack { get; }

            /// <summary>Gets or sets a counter indicating how many nested call contexts require rendering an identity column.</summary>
            public int InCallRenderIdentity { get; set; }

            /// <summary>Gets or sets a counter indicating how many nested call contexts suppress SQL rendering.</summary>
            public int InCallNoRender { get; set; }

            /// <summary>
            /// Initializes a new <see cref="MemberContext"/> with empty stacks and zero call-render counters.
            /// </summary>
            public MemberContext()
            {
                this.ModelContexts = new();
                this.ModelStack = new();
                this.OperatorStack = new();
                this.MemberAccessStack = new();
                this.MemberLambdaStack = new();
                this.DependantStack = new();
                this.InCallRenderIdentity = 0;
                this.InCallNoRender = 0;
            }
        }
    }
}

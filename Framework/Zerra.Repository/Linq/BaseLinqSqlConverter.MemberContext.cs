using System.Collections.Generic;
using System.Linq.Expressions;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract partial class BaseLinqSqlConverter
    {
        protected class MemberContext
        {
            public Dictionary<string, ModelDetail> ModelContexts { get; private set; }
            public Stack<ModelDetail> ModelStack { get; private set; }
            public Stack<Operator> OperatorStack { get; private set; }
            public Stack<MemberExpression> MemberAccessStack { get; private set; }
            public Stack<MemberExpression> MemberLambdaStack { get; private set; }
            public Stack<ParameterDependant> DependantStack { get; private set; }
            public int InCallRenderIdentity { get; set; }
            public int InCallNoRender { get; set; }

            public MemberContext()
            {
                this.ModelContexts = new Dictionary<string, ModelDetail>();
                this.ModelStack = new Stack<ModelDetail>();
                this.OperatorStack = new Stack<Operator>();
                this.MemberAccessStack = new Stack<MemberExpression>();
                this.MemberLambdaStack = new Stack<MemberExpression>();
                this.DependantStack = new Stack<ParameterDependant>();
                this.InCallRenderIdentity = 0;
                this.InCallNoRender = 0;
            }
        }
    }
}

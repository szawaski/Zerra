using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zerra.Mathematics
{
    public sealed partial class MathParser
    {
        private sealed class StatementPart
        {
            public int Index { get; private set; }
            public string Token
            {
                get
                {
                    if (UnaryOperator != null)
                        return UnaryOperator.Token;
                    if (BinaryOperator != null)
                        return BinaryOperator.Token;
                    if (MethodOperator != null)
                        return MethodOperator.Token;
                    if (Number != null)
                        return Number;
                    if (Variable != null)
                        return Variable;
                    return null;
                }
            }
            public UnaryOperator UnaryOperator { get; private set; }
            public BinaryOperator BinaryOperator { get; private set; }
            public MethodOperator MethodOperator { get; private set; }
            public string Number { get; set; }
            public string Variable { get; set; }

            public IReadOnlyList<StatementPart> SubParts { get; private set; }
            public StatementPart(int index, UnaryOperator unaryOperator)
            {
                this.Index = index;
                this.UnaryOperator = unaryOperator;
            }
            public StatementPart(int index, BinaryOperator binaryOperator)
            {
                this.Index = index;
                this.BinaryOperator = binaryOperator;
            }
            public StatementPart(int index, UnaryOperator unaryOperator, BinaryOperator binaryOperator)
            {
                this.Index = index;
                this.UnaryOperator = unaryOperator;
                this.BinaryOperator = binaryOperator;
            }
            public StatementPart(int index, string numbersAndVariables)
            {
                this.Index = index;
                if (numbersAndVariables.All(x => numbers.Contains(x)))
                {
                    Number = numbersAndVariables;
                }
                else
                {
                    Variable = numbersAndVariables;
                }
            }
            public StatementPart(int index, IReadOnlyList<StatementPart> subParts)
            {
                this.Index = index;
                this.SubParts = subParts;
            }
            public StatementPart(int index, MethodOperator methodOperator, IReadOnlyList<StatementPart> subParts)
            {
                this.Index = index;
                this.MethodOperator = methodOperator;
                this.SubParts = subParts;
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                ToString(sb);
                return sb.ToString();
            }
            private void ToString(StringBuilder sb)
            {
                if (Token != null)
                    _ = sb.Append(Token);
                else
                    _ = sb.Append('[');
                if (SubParts != null)
                {
                    if (Token != null)
                        _ = sb.Append(MethodOperator.ArgumentOpener);
                    for (var i = 0; i < SubParts.Count; i++)
                    {
                        if (i > 0 && Token != null)
                            _ = sb.Append(MethodOperator.ArgumentSeperator);
                        SubParts[i].ToString(sb);
                    }
                    if (Token != null)
                        _ = sb.Append(MethodOperator.ArgumentCloser);
                }
                if (Token == null)
                    _ = sb.Append(']');
            }
        }
    }
}

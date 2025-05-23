﻿using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zerra.Mathematics
{
    public sealed partial class MathParser
    {
        private sealed class StatementPart
        {
            public int Index { get; }
            public string? Token
            {
                get
                {
                    if (UnaryOperator is not null)
                        return UnaryOperator.Token;
                    if (BinaryOperator is not null)
                        return BinaryOperator.Token;
                    if (TertiaryOperator1 is not null)
                        return TertiaryOperator1.Token1;
                    if (TertiaryOperator2 is not null)
                        return TertiaryOperator2.Token2;
                    if (MethodOperator is not null)
                        return MethodOperator.Token;
                    if (Number is not null)
                        return Number;
                    if (Variable is not null)
                        return Variable;
                    return null;
                }
            }
            public UnaryOperator? UnaryOperator { get; }
            public BinaryOperator? BinaryOperator { get; }
            public TertiaryOperator? TertiaryOperator1 { get; }
            public TertiaryOperator? TertiaryOperator2 { get; }
            public MethodOperator? MethodOperator { get; }
            public string? Number { get; set; }
            public string? Variable { get; set; }

            public IReadOnlyList<StatementPart>? SubParts { get; }

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
            public StatementPart(int index, TertiaryOperator tertiaryOperator, bool part2)
            {
                this.Index = index;
                if (!part2)
                    this.TertiaryOperator1 = tertiaryOperator;
                else
                    this.TertiaryOperator2 = tertiaryOperator;
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
                if (numbersAndVariables.All(numbers.Contains))
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
                if (Token is not null)
                    _ = sb.Append(Token);
                else
                    _ = sb.Append('[');
                if (SubParts is not null)
                {
                    if (Token is not null)
                        _ = sb.Append(MethodOperator.ArgumentOpener);
                    for (var i = 0; i < SubParts.Count; i++)
                    {
                        if (i > 0 && Token is not null)
                            _ = sb.Append(MethodOperator.ArgumentSeperator);
                        SubParts[i].ToString(sb);
                    }
                    if (Token is not null)
                        _ = sb.Append(MethodOperator.ArgumentCloser);
                }
                if (Token is null)
                    _ = sb.Append(']');
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Zerra.Mathematics
{
    public partial class MathParser
    {
        private ref struct ParserContext
        {
            public ReadOnlySpan<char> Chars { get; private set; }
            public Stack<char> GroupStack { get; private set; }
            public List<ParameterExpression> LinqParameters { get; private set; }
            public int Index { get; private set; }
            public char Current { get; private set; }

            public void Next()
            {
                Index++;
                if (Index < Chars.Length)
                    Current = Chars[Index];
                else
                    Current = Char.MaxValue;
            }
            public void Reset(int index)
            {
                Index = index;
                if (index >= 0 && index < Chars.Length)
                    Current = Chars[Index];
                else
                    Current = Char.MaxValue;
            }

            public ParserContext(string str)
            {
                Chars = str.AsSpan();
                GroupStack = new Stack<char>();
                LinqParameters = new List<ParameterExpression>();
                Index = -1;
                Current = Char.MaxValue;
            }
        }
    }
}

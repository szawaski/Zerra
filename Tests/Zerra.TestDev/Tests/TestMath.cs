using System;
using System.Collections.Generic;
using Zerra.Mathematics;

namespace Zerra.TestDev
{
    public static class TestMath
    {
        public static void Test()
        {
            MathParser.AddUnaryOperatorFirst("$", false, (x) => x * 100);

            var parser = new MathParser("-1 ? 100 : round(1/x+7*0.6/60) + 0.5");
            var variables = new Dictionary<string, double>();
            variables.Add("x", 2);
        
            var result1 = parser.Evalutate(variables);

            var parser2 = new MathParser("if(x<1, 0, [5!+x^2+(a2*y)] - $1 + round(1.25,1) - sin(pi*1/2) + ln(e(1)) )");
            var result2 = parser2.Evalutate(4, Math.PI, 2, 3);
            //expected 37.2
        }
    }
}

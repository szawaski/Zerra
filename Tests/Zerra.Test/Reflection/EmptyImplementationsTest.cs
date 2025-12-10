// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Reflection;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Reflection
{
    public class EmptyImplementationsTest
    {
        [Fact]
        public async Task Test()
        {
            var thingyType = EmptyImplementations.GetType(typeof(ITestInterface));
            var thingy = (ITestInterface)Activator.CreateInstance(thingyType);

            thingy.Method1();

            var resultA = thingy.MethodA();
            var resultB = thingy.MethodB();
            var resultC = thingy.MethodC();
            var resultD = thingy.MethodD();
            var resultE = thingy.MethodE();
            var resultF = thingy.MethodF();
            var resultG = thingy.MethodG();
            var resultH = thingy.MethodH();
            var resultI = thingy.MethodI();
            var resultJ = thingy.MethodJ();
            var resultK = thingy.MethodK();
            var resultL = thingy.MethodL();
            var resultM = thingy.MethodM();
            var resultN = thingy.MethodN();
            var resultO = thingy.MethodO();
            var resultP = thingy.MethodP();
            var resultQ = thingy.MethodQ();
            var resultR = thingy.MethodR();
            var resultS = thingy.MethodS();

            Assert.Equal(default, resultA);
            Assert.Equal(default, resultB);
            Assert.Equal(default, resultC);
            Assert.Equal(default, resultD);
            Assert.Equal(default, resultE);
            Assert.Equal(default, resultF);
            Assert.Equal(default, resultG);
            Assert.Equal(default, resultH);
            Assert.Equal(default, resultI);
            Assert.Equal(default, resultJ);
            Assert.Equal(default, resultK);
            Assert.Equal(default, resultL);
            Assert.Equal(default, resultM);
            Assert.Equal(default, resultN);
            Assert.Equal(default, resultO);
            Assert.Equal(default, resultP);
            Assert.Equal(default, resultQ);
            Assert.Equal(default, resultR);
            Assert.Equal(default, resultS);

            await Task.Run(thingy.Method3);
            var result3 = await Task.Run(thingy.Method4);
            Assert.Equal(0, result3);

            thingy.Property1 = 5;
            var prop1 = thingy.Property1;
            Assert.Equal(5, prop1);

            ((dynamic)thingy).Property2 = 6;
            var prop2 = thingy.Property2;
            Assert.Equal(6, prop2);

            thingy.Property3 = 7;
            var prop3 = ((dynamic)thingy).Property3;
            Assert.Equal(7, prop3);
        }
    }
}

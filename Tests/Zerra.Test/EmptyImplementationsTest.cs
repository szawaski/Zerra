// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Zerra.Reflection;

namespace Zerra.Test
{
    [TestClass]
    public class EmptyImplementationsTest
    {
        [TestMethod]
        public void Test()
        {
            var thingy = EmptyImplementations.GetEmptyImplementation<ITestInterface>();

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

            Assert.AreEqual(default, resultA);
            Assert.AreEqual(default, resultB);
            Assert.AreEqual(default, resultC);
            Assert.AreEqual(default, resultD);
            Assert.AreEqual(default, resultE);
            Assert.AreEqual(default, resultF);
            Assert.AreEqual(default, resultG);
            Assert.AreEqual(default, resultH);
            Assert.AreEqual(default, resultI);
            Assert.AreEqual(default, resultJ);
            Assert.AreEqual(default, resultK);
            Assert.AreEqual(default, resultL);
            Assert.AreEqual(default, resultM);
            Assert.AreEqual(default, resultN);
            Assert.AreEqual(default, resultO);
            Assert.AreEqual(default, resultP);
            Assert.AreEqual(default, resultQ);
            Assert.AreEqual(default, resultR);
            Assert.AreEqual(default, resultS);

            Task.Run(thingy.Method3).GetAwaiter().GetResult();
            var result3 = Task.Run(thingy.Method4).GetAwaiter().GetResult();
            Assert.AreEqual(0, result3);

            thingy.Property1 = 5;
            var prop1 = thingy.Property1;
            Assert.AreEqual(5, prop1);

            ((dynamic)thingy).Property2 = 6;
            var prop2 = thingy.Property2;
            Assert.AreEqual(6, prop2);

            thingy.Property3 = 7;
            var prop3 = ((dynamic)thingy).Property3;
            Assert.AreEqual(7, prop3);
        }
    }
}

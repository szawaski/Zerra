// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Test.Helpers.Models
{
    [Zerra.Reflection.GenerateTypeDetail]
    public interface ITestInterface
    {
        int Property1 { get; set; }
        int Property2 { get; }
        int Property3 { set; }

        void Method1();

        bool MethodA();
        byte MethodB();
        sbyte MethodC();
        short MethodD();
        ushort MethodE();
        int MethodF();
        uint MethodG();
        long MethodH();
        ulong MethodI();
        float MethodJ();
        double MethodK();
        decimal MethodL();
        char MethodM();
        DateTime MethodN();
        DateTimeOffset MethodO();
        TimeSpan MethodP();
        Guid MethodQ();
        string MethodR();
        bool? MethodS();

        Task Method3();
        Task<int> Method4();
    }
}

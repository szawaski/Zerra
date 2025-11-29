// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Test.Helpers.Models
{
    [Zerra.SourceGeneration.SourceGenerationTypeDetail]
    public class TestInterfaceImplemented : ITestInterface
    {
        public int Property1 { get; set; }
        public int Property2 { get; set; }
        public int Property3 { get; set; }

        public void Method1() { return; }

        public bool MethodA() { return default; }
        public byte MethodB() { return default; }
        public sbyte MethodC() { return default; }
        public short MethodD() { return default; }
        public ushort MethodE() { return default; }
        public int MethodF() { return default; }
        public uint MethodG() { return default; }
        public long MethodH() { return default; }
        public ulong MethodI() { return default; }
        public float MethodJ() { return default; }
        public double MethodK() { return default; }
        public decimal MethodL() { return default; }
        public char MethodM() { return default; }
        public DateTime MethodN() { return default; }
        public DateTimeOffset MethodO() { return default; }
        public TimeSpan MethodP() { return default; }
        public Guid MethodQ() { return default; }
        public string MethodR() { return default; }
        public bool? MethodS() { return default; }

        public Task Method3() { return Task.CompletedTask; }
        public Task<int> Method4() { return Task.FromResult(default(int)); }
    }
}

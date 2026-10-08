// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Test.Helpers.Models
{
    public class SpecialTypesModel
    {
        public Type? TypeValue { get; set; }
        public Type? TypeNull { get; set; }
        public byte[]? Bytes { get; set; }
        public byte[]? BytesNull { get; set; }
        public CancellationToken Token { get; set; }
        public CancellationToken? TokenNullable { get; set; }
        public int After { get; set; }

        public static SpecialTypesModel[] CreateArray(int count) => Enumerable.Range(0, count).Select(i => new SpecialTypesModel()
        {
            TypeValue = i % 2 == 0 ? typeof(string) : typeof(int),
            Bytes = Enumerable.Range(0, i % 40).Select(x => (byte)(x * 7)).ToArray(),
            After = i
        }).ToArray();
    }
}
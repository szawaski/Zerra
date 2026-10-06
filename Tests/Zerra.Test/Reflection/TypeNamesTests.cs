// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection
{
    public class TypeNamesTests
    {
        public class Outer<T>
        {
            public class Inner<U> { }
        }

        public interface IPair<T, U> { }

        [Fact]
        public void GetFullName_WritesGenericsWithFullArguments()
        {
            Assert.Equal("System.String", TypeNames.GetFullName(typeof(string)));
            Assert.Equal("System.Collections.Generic.Dictionary<System.String,System.Collections.Generic.List<System.Int32[]>>", TypeNames.GetFullName(typeof(Dictionary<string, List<int[]>>)));
            Assert.Equal("System.Collections.Generic.List<System.Int32>[,]", TypeNames.GetFullName(typeof(List<int>[,])));
            Assert.Equal("Zerra.Test.Reflection.TypeNamesTests+Outer+Inner<System.Int32,System.String>", TypeNames.GetFullName(typeof(Outer<int>.Inner<string>)));
        }

        [Fact]
        public void GetFullName_WritesGenericParametersAsT()
        {
            Assert.Equal("Zerra.Test.Reflection.TypeNamesTests+IPair<T,T>", TypeNames.GetFullName(typeof(IPair<,>)));
            var partial = typeof(IPair<,>).MakeGenericType(typeof(int), typeof(IPair<,>).GetGenericArguments()[1]);
            Assert.Equal("Zerra.Test.Reflection.TypeNamesTests+IPair<System.Int32,T>", TypeNames.GetFullName(partial));
        }

        [Fact]
        public void GetFullGenericName_WritesEveryArgumentAsT()
        {
            Assert.Equal("Zerra.Test.Reflection.TypeNamesTests+IPair<T,T>", TypeNames.GetFullGenericName(typeof(IPair<int, string>)));
            Assert.Equal(TypeNames.GetFullName(typeof(IPair<,>)), TypeNames.GetFullGenericName(typeof(IPair<int, string>)));
        }
    }
}

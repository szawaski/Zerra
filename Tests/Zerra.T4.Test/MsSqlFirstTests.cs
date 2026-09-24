// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;

namespace Zerra.T4.Test
{
    //GenerateModels and GenerateProviders read the schema from a live SQL Server, only the naming is covered here
    public class MsSqlFirstTests
    {
        [Theory]
        [InlineData("Customer", true, "Customer")]
        [InlineData("Order_Line2", true, "Order_Line2")]
        [InlineData("_Private", true, "_Private")]
        [InlineData("Order Line", false, "OrderLine")]
        [InlineData("Order-Line", false, "OrderLine")]
        [InlineData("2020Sales", false, "Sales")]
        [InlineData("Sales$", false, "Sales")]
        [InlineData("", true, "")]
        public void IsSafeName(string name, bool expectedSafe, string expectedName)
        {
            Assert.Equal(expectedSafe, MsSqlFirst.IsSafeName(name, out var safeName));
            Assert.Equal(expectedName, safeName);
        }
    }
}

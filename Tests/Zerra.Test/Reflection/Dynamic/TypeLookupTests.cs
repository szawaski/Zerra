using System.Collections.Concurrent;
using Xunit;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra.Test.Reflection.Dynamic
{
    public class TypeLookupTests
    {
        #region GetCoreType

        [Theory]
        [InlineData(typeof(bool), CoreType.Boolean)]
        [InlineData(typeof(byte), CoreType.Byte)]
        [InlineData(typeof(sbyte), CoreType.SByte)]
        [InlineData(typeof(short), CoreType.Int16)]
        [InlineData(typeof(ushort), CoreType.UInt16)]
        [InlineData(typeof(int), CoreType.Int32)]
        [InlineData(typeof(uint), CoreType.UInt32)]
        [InlineData(typeof(long), CoreType.Int64)]
        [InlineData(typeof(ulong), CoreType.UInt64)]
        [InlineData(typeof(float), CoreType.Single)]
        [InlineData(typeof(double), CoreType.Double)]
        [InlineData(typeof(decimal), CoreType.Decimal)]
        [InlineData(typeof(char), CoreType.Char)]
        [InlineData(typeof(DateTime), CoreType.DateTime)]
        [InlineData(typeof(DateTimeOffset), CoreType.DateTimeOffset)]
        [InlineData(typeof(TimeSpan), CoreType.TimeSpan)]
        [InlineData(typeof(DateOnly), CoreType.DateOnly)]
        [InlineData(typeof(TimeOnly), CoreType.TimeOnly)]
        [InlineData(typeof(Guid), CoreType.Guid)]
        [InlineData(typeof(string), CoreType.String)]
        public void GetCoreType_KnownType_ReturnsTrueWithCorrectCoreType(Type type, CoreType expected)
        {
            var result = TypeLookup.GetCoreType(type, out var coreType);

            Assert.True(result);
            Assert.Equal(expected, coreType);
        }

        [Theory]
        [InlineData(typeof(bool?))]
        [InlineData(typeof(byte?))]
        [InlineData(typeof(sbyte?))]
        [InlineData(typeof(short?))]
        [InlineData(typeof(ushort?))]
        [InlineData(typeof(int?))]
        [InlineData(typeof(uint?))]
        [InlineData(typeof(long?))]
        [InlineData(typeof(ulong?))]
        [InlineData(typeof(float?))]
        [InlineData(typeof(double?))]
        [InlineData(typeof(decimal?))]
        [InlineData(typeof(char?))]
        [InlineData(typeof(DateTime?))]
        [InlineData(typeof(DateTimeOffset?))]
        [InlineData(typeof(TimeSpan?))]
        [InlineData(typeof(DateOnly?))]
        [InlineData(typeof(TimeOnly?))]
        [InlineData(typeof(Guid?))]
        public void GetCoreType_NullableType_ReturnsTrueWithNullableCoreType(Type type)
        {
            var result = TypeLookup.GetCoreType(type, out var coreType);

            Assert.True(result);
            Assert.EndsWith(coreType.ToString(), "Nullable");
        }

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(Dictionary<string, int>))]
        [InlineData(typeof(TypeLookupTests))]
        public void GetCoreType_UnknownType_ReturnsFalse(Type type)
        {
            var result = TypeLookup.GetCoreType(type, out var coreType);

            Assert.False(result);
            Assert.Equal(default, coreType);
        }

        #endregion

        #region GetCoreEnumType

        [Theory]
        [InlineData(typeof(byte), CoreEnumType.Byte)]
        [InlineData(typeof(sbyte), CoreEnumType.SByte)]
        [InlineData(typeof(short), CoreEnumType.Int16)]
        [InlineData(typeof(ushort), CoreEnumType.UInt16)]
        [InlineData(typeof(int), CoreEnumType.Int32)]
        [InlineData(typeof(uint), CoreEnumType.UInt32)]
        [InlineData(typeof(long), CoreEnumType.Int64)]
        [InlineData(typeof(ulong), CoreEnumType.UInt64)]
        public void GetCoreEnumType_KnownType_ReturnsTrueWithCorrectEnumType(Type type, CoreEnumType expected)
        {
            var result = TypeLookup.GetCoreEnumType(type, out var coreEnumType);

            Assert.True(result);
            Assert.Equal(expected, coreEnumType);
        }

        [Theory]
        [InlineData(typeof(byte?))]
        [InlineData(typeof(sbyte?))]
        [InlineData(typeof(short?))]
        [InlineData(typeof(ushort?))]
        [InlineData(typeof(int?))]
        [InlineData(typeof(uint?))]
        [InlineData(typeof(long?))]
        [InlineData(typeof(ulong?))]
        public void GetCoreEnumType_NullableType_ReturnsTrueWithNullableEnumType(Type type)
        {
            var result = TypeLookup.GetCoreEnumType(type, out var coreEnumType);

            Assert.True(result);
            Assert.EndsWith(coreEnumType.ToString(), "Nullable");
        }

        [Theory]
        [InlineData(typeof(bool))]
        [InlineData(typeof(float))]
        [InlineData(typeof(double))]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(string))]
        [InlineData(typeof(object))]
        public void GetCoreEnumType_NonIntegralType_ReturnsFalse(Type type)
        {
            var result = TypeLookup.GetCoreEnumType(type, out var coreEnumType);

            Assert.False(result);
            Assert.Equal(default, coreEnumType);
        }

        #endregion

        #region GetSpecialType

        [Theory]
        [InlineData(typeof(Task), SpecialType.Task)]
        [InlineData(typeof(Task<int>), SpecialType.Task)]
        [InlineData(typeof(Type), SpecialType.Type)]
        [InlineData(typeof(Dictionary<string, int>), SpecialType.Dictionary)]
        [InlineData(typeof(ConcurrentDictionary<string, int>), SpecialType.Dictionary)]
        [InlineData(typeof(ConcurrentFactoryDictionary<string, int>), SpecialType.Dictionary)]
        public void GetSpecialType_KnownType_ReturnsTrueWithCorrectSpecialType(Type type, SpecialType expected)
        {
            var result = TypeLookup.GetSpecialType(type, out var specialType);

            Assert.True(result);
            Assert.Equal(expected, specialType);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(object))]
        [InlineData(typeof(TypeLookupTests))]
        public void GetSpecialType_UnknownType_ReturnsFalse(Type type)
        {
            var result = TypeLookup.GetSpecialType(type, out var specialType);

            Assert.False(result);
            Assert.Equal(default, specialType);
        }

        [Fact]
        public void GetSpecialType_IReadOnlyDictionary_ReturnsTrue()
        {
            var result = TypeLookup.GetSpecialType(typeof(IReadOnlyDictionary<string, int>), out var specialType);

            Assert.True(result);
            Assert.Equal(SpecialType.Dictionary, specialType);
        }

        [Fact]
        public void GetSpecialType_IDictionary_ReturnsTrue()
        {
            var result = TypeLookup.GetSpecialType(typeof(IDictionary<string, int>), out var specialType);

            Assert.True(result);
            Assert.Equal(SpecialType.Dictionary, specialType);
        }

        #endregion
    }
}

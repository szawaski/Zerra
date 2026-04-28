using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection.Dynamic
{
    public class TypeAnalyzerTests
    {
        #region GetTypeDetail

        [Fact]
        public void GetTypeDetail_SimpleType_ReturnsTypeDetail()
        {
            var result = TypeAnalyzer.GetTypeDetail(typeof(int));

            Assert.NotNull(result);
            Assert.Equal(typeof(int), result.Type);
        }

        [Fact]
        public void GetTypeDetail_StringType_ReturnsTypeDetail()
        {
            var result = TypeAnalyzer.GetTypeDetail(typeof(string));

            Assert.NotNull(result);
            Assert.Equal(typeof(string), result.Type);
        }

        [Fact]
        public void GetTypeDetail_ComplexType_ReturnsTypeDetail()
        {
            var result = TypeAnalyzer.GetTypeDetail(typeof(TypeAnalyzerTestModel));

            Assert.NotNull(result);
            Assert.Equal(typeof(TypeAnalyzerTestModel), result.Type);
        }

        [Fact]
        public void GetTypeDetail_CalledTwice_ReturnsSameInstance()
        {
            var result1 = TypeAnalyzer.GetTypeDetail(typeof(Guid));
            var result2 = TypeAnalyzer.GetTypeDetail(typeof(Guid));

            Assert.Same(result1, result2);
        }

        [Fact]
        public void GetTypeDetail_DifferentTypes_ReturnDifferentInstances()
        {
            var result1 = TypeAnalyzer.GetTypeDetail(typeof(int));
            var result2 = TypeAnalyzer.GetTypeDetail(typeof(string));

            Assert.NotEqual(result1.Type, result2.Type);
        }

        [Fact]
        public void GetTypeDetail_GenericType_ReturnsTypeDetail()
        {
            var result = TypeAnalyzer.GetTypeDetail(typeof(List<int>));

            Assert.NotNull(result);
            Assert.Equal(typeof(List<int>), result.Type);
        }

        [Fact]
        public void GetTypeDetail_NullableType_ReturnsTypeDetail()
        {
            var result = TypeAnalyzer.GetTypeDetail(typeof(int?));

            Assert.NotNull(result);
            Assert.Equal(typeof(int?), result.Type);
        }

        #endregion

        #region Convert<T>

        [Fact]
        public void ConvertGeneric_IntFromString_ReturnsCorrectValue()
        {
            var result = TypeAnalyzer.Convert<int>("42");

            Assert.Equal(42, result);
        }

        [Fact]
        public void ConvertGeneric_BoolFromInt_ReturnsCorrectValue()
        {
            var result = TypeAnalyzer.Convert<bool>(1);

            Assert.True(result);
        }

        [Fact]
        public void ConvertGeneric_StringFromInt_ReturnsCorrectValue()
        {
            var result = TypeAnalyzer.Convert<string>(123);

            Assert.Equal("123", result);
        }

        [Fact]
        public void ConvertGeneric_NullableInt_FromNull_ReturnsNull()
        {
            var result = TypeAnalyzer.Convert<int?>(null);

            Assert.Null(result);
        }

        [Fact]
        public void ConvertGeneric_NullableInt_FromValue_ReturnsValue()
        {
            var result = TypeAnalyzer.Convert<int?>(99);

            Assert.Equal(99, result);
        }

        [Fact]
        public void ConvertGeneric_NonCoreType_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => TypeAnalyzer.Convert<TypeAnalyzerTestModel>(new object()));
        }

        [Fact]
        public void ConvertGeneric_NullToNonNullableInt_ReturnsDefault()
        {
            var result = TypeAnalyzer.Convert<int>(null);

            Assert.Equal(0, result);
        }

        [Fact]
        public void ConvertGeneric_GuidFromString_ReturnsCorrectValue()
        {
            var guid = Guid.NewGuid();
            var result = TypeAnalyzer.Convert<Guid>(guid.ToString());

            Assert.Equal(guid, result);
        }

        [Fact]
        public void ConvertGeneric_DateTimeFromString_ReturnsCorrectValue()
        {
            var dt = new DateTime(2024, 6, 15);
            var result = TypeAnalyzer.Convert<DateTime>(dt.ToString());

            Assert.Equal(dt, result);
        }

        [Fact]
        public void ConvertGeneric_TimeSpanFromString_ReturnsCorrectValue()
        {
            var ts = new TimeSpan(1, 2, 3);
            var result = TypeAnalyzer.Convert<TimeSpan>(ts.ToString());

            Assert.Equal(ts, result);
        }

        #endregion

        #region Convert(object, Type)

        [Fact]
        public void ConvertByType_IntFromString_ReturnsCorrectValue()
        {
            var result = TypeAnalyzer.Convert("42", typeof(int));

            Assert.Equal(42, result);
        }

        [Fact]
        public void ConvertByType_NullToNonNullableInt_ReturnsDefault()
        {
            var result = TypeAnalyzer.Convert(null, typeof(int));

            Assert.Equal(0, result);
        }

        [Fact]
        public void ConvertByType_NullToNullableInt_ReturnsNull()
        {
            var result = TypeAnalyzer.Convert(null, typeof(int?));

            Assert.Null(result);
        }

        [Fact]
        public void ConvertByType_NonCoreType_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => TypeAnalyzer.Convert(new object(), typeof(TypeAnalyzerTestModel)));
        }

        #endregion

        #region Convert(object, CoreType)

        [Fact]
        public void ConvertByCoreType_NullToBoolean_ReturnsFalse()
        {
            var result = TypeAnalyzer.Convert(null, CoreType.Boolean);

            Assert.Equal(false, result);
        }

        [Fact]
        public void ConvertByCoreType_NullToBooleanNullable_ReturnsNull()
        {
            var result = TypeAnalyzer.Convert(null, CoreType.BooleanNullable);

            Assert.Null(result);
        }

        [Fact]
        public void ConvertByCoreType_NullToInt32_ReturnsZero()
        {
            var result = TypeAnalyzer.Convert(null, CoreType.Int32);

            Assert.Equal(0, result);
        }

        [Fact]
        public void ConvertByCoreType_NullToInt32Nullable_ReturnsNull()
        {
            var result = TypeAnalyzer.Convert(null, CoreType.Int32Nullable);

            Assert.Null(result);
        }

        [Fact]
        public void ConvertByCoreType_NullToString_ReturnsNull()
        {
            var result = TypeAnalyzer.Convert(null, CoreType.String);

            Assert.Null(result);
        }

        [Theory]
        [InlineData(CoreType.Boolean, "true", true)]
        [InlineData(CoreType.Byte, "200", (byte)200)]
        [InlineData(CoreType.Int16, "1000", (short)1000)]
        [InlineData(CoreType.Int32, "42", 42)]
        [InlineData(CoreType.Int64, "9999", (long)9999)]
        [InlineData(CoreType.Double, "3.14", 3.14)]
        [InlineData(CoreType.String, "hello", "hello")]
        public void ConvertByCoreType_ValueFromString_ReturnsConvertedValue(CoreType coreType, object input, object expected)
        {
            var result = TypeAnalyzer.Convert(input, coreType);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void ConvertByCoreType_GuidFromString_ReturnsCorrectGuid()
        {
            var guid = Guid.NewGuid();
            var result = TypeAnalyzer.Convert(guid.ToString(), CoreType.Guid);

            Assert.Equal(guid, result);
        }

        [Fact]
        public void ConvertByCoreType_NullToGuid_ReturnsEmptyGuid()
        {
            var result = TypeAnalyzer.Convert(null, CoreType.Guid);

            Assert.Equal(Guid.Empty, result);
        }

        [Fact]
        public void ConvertByCoreType_TimeSpanFromString_ReturnsCorrectTimeSpan()
        {
            var ts = new TimeSpan(2, 30, 0);
            var result = TypeAnalyzer.Convert(ts.ToString(), CoreType.TimeSpan);

            Assert.Equal(ts, result);
        }

        [Fact]
        public void ConvertByCoreType_DateOnlyFromString_ReturnsCorrectDateOnly()
        {
            var date = new DateOnly(2024, 1, 15);
            var result = TypeAnalyzer.Convert(date.ToString(), CoreType.DateOnly);

            Assert.Equal(date, result);
        }

        [Fact]
        public void ConvertByCoreType_TimeOnlyFromString_ReturnsCorrectTimeOnly()
        {
            var time = new TimeOnly(14, 30, 0);
            var result = TypeAnalyzer.Convert(time.ToString(), CoreType.TimeOnly);

            Assert.Equal(time, result);
        }

        #endregion
    }

    public class TypeAnalyzerGenericTests
    {
        #region GetTypeDetail<T>

        [Fact]
        public void GetTypeDetail_Int_ReturnsTypeDetailForInt()
        {
            var result = TypeAnalyzer<int>.GetTypeDetail();

            Assert.NotNull(result);
            Assert.Equal(typeof(int), result.Type);
        }

        [Fact]
        public void GetTypeDetail_String_ReturnsTypeDetailForString()
        {
            var result = TypeAnalyzer<string>.GetTypeDetail();

            Assert.NotNull(result);
            Assert.Equal(typeof(string), result.Type);
        }

        [Fact]
        public void GetTypeDetail_ComplexType_ReturnsTypeDetailForType()
        {
            var result = TypeAnalyzer<TypeAnalyzerTestModel>.GetTypeDetail();

            Assert.NotNull(result);
            Assert.Equal(typeof(TypeAnalyzerTestModel), result.Type);
        }

        [Fact]
        public void GetTypeDetail_CalledTwice_ReturnsSameInstance()
        {
            var result1 = TypeAnalyzer<Guid>.GetTypeDetail();
            var result2 = TypeAnalyzer<Guid>.GetTypeDetail();

            Assert.Same(result1, result2);
        }

        [Fact]
        public void GetTypeDetail_MatchesNonGenericTypeAnalyzer()
        {
            var genericResult = TypeAnalyzer<DateTime>.GetTypeDetail();
            var nonGenericResult = TypeAnalyzer.GetTypeDetail(typeof(DateTime));

            Assert.Same(genericResult, nonGenericResult);
        }

        [Fact]
        public void GetTypeDetail_NullableType_ReturnsTypeDetail()
        {
            var result = TypeAnalyzer<int?>.GetTypeDetail();

            Assert.NotNull(result);
            Assert.Equal(typeof(int?), result.Type);
        }

        [Fact]
        public void GetTypeDetail_GenericListType_ReturnsTypeDetail()
        {
            var result = TypeAnalyzer<List<string>>.GetTypeDetail();

            Assert.NotNull(result);
            Assert.Equal(typeof(List<string>), result.Type);
        }

        #endregion
    }

    public sealed class TypeAnalyzerTestModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}

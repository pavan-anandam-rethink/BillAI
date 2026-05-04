using System;
using Rethink.Services.Common.Helpers;
using Xunit;

namespace BillingService.XUnit.Tests.Common.Helpers
{
    public class NumberHelperTests
    {

        [Fact]
        public void ToBase36_WithZero_ReturnsEmpty()
        {
            Assert.Equal("", 0.ToBase36());
        }

        [Theory]
        [InlineData(1, "1")]
        [InlineData(10, "A")]
        [InlineData(36, "10")]
        public void ToBase36_KnownValues_ReturnExpected(int input, string expected)
        {
            Assert.Equal(expected, input.ToBase36());
        }

        [Fact]
        public void ToBase36_MaxInt_RoundTripsSuccessfully()
        {
            int max = int.MaxValue;
            string b36 = max.ToBase36();
            Assert.Equal(max, b36.FromBase36());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void FromBase36_NullOrEmpty_ReturnsZero(string input)
        {
            Assert.Equal(0, input.FromBase36());
        }

        [Fact]
        public void FromBase36_LowercaseInput_ConvertsCorrectly()
        {
            Assert.Equal(13368, "abc".FromBase36()); // ABC = 10*36^2 + 11*36 + 12
        }

        [Fact]
        public void FromBase36_NegativeSign_ReturnsNegative()
        {
            Assert.Equal(-46, "-1A".FromBase36()); // 1A = 46
        }

        [Fact]
        public void FromBase36_InvalidCharacter_Throws()
        {
            var ex = Assert.Throws<ArgumentException>(() => "12@".FromBase36());
            Assert.Equal("base36Num", ex.ParamName);
        }

        [Fact]
        public void FromBase36_MultiDigit_ReturnsExpected()
        {
            Assert.Equal(71, "1Z".FromBase36());
        }

        [Theory] 
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(35)]
        [InlineData(1000)]
        public void RoundTrip_Int_ToBase36_AndBack_ReturnsOriginal(int value)
        {
            string b36 = value.ToBase36();
            int back = b36.FromBase36();
            Assert.Equal(value, back);
        }
    }
}

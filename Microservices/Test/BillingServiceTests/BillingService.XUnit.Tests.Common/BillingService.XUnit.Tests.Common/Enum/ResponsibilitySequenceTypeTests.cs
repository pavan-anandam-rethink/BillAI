using Rethink.Services.Common.Enums.BH;
using Xunit;

namespace BillingService.XUnit.Tests.Common.Enums.BH
{
    public class ResponsibilitySequenceTypeTests
    {

        [Fact]
        public void AsString_Should_Return_Correct_Value()
        {
            Assert.Equal("P", ResponsibilitySequenceType.Primary.AsString());
            Assert.Equal("S", ResponsibilitySequenceType.Secondary.AsString());
            Assert.Equal("T", ResponsibilitySequenceType.Tertiary.AsString());
            Assert.Equal("4", ResponsibilitySequenceType.Four.AsString());
        }


        [Fact]
        public void FromString_Should_Return_Correct_Enum()
        {
            Assert.Equal(ResponsibilitySequenceType.Primary, ResponsibilitySequenceTypeHelper.FromString("P"));
            Assert.Equal(ResponsibilitySequenceType.Secondary, ResponsibilitySequenceTypeHelper.FromString("S"));
            Assert.Equal(ResponsibilitySequenceType.Four, ResponsibilitySequenceTypeHelper.FromString("4"));
        }


        [Fact]
        public void AsOrdinal_Should_Return_Correct_Ordinal()
        {
            Assert.Equal(1, ResponsibilitySequenceType.Primary.AsOrdinal());
            Assert.Equal(2, ResponsibilitySequenceType.Secondary.AsOrdinal());
            Assert.Equal(3, ResponsibilitySequenceType.Tertiary.AsOrdinal());
            Assert.Equal(4, ResponsibilitySequenceType.Four.AsOrdinal());
        }


        [Fact]
        public void FromOrdinal_Should_Return_Correct_Enum()
        {
            Assert.Equal(ResponsibilitySequenceType.Primary, ResponsibilitySequenceTypeHelper.FromOrdinal(1));
            Assert.Equal(ResponsibilitySequenceType.Secondary, ResponsibilitySequenceTypeHelper.FromOrdinal(2));
            Assert.Equal(ResponsibilitySequenceType.Tertiary, ResponsibilitySequenceTypeHelper.FromOrdinal(3));
            Assert.Equal(ResponsibilitySequenceType.Four, ResponsibilitySequenceTypeHelper.FromOrdinal(4));
        }

        [Fact]
        public void FromOrdinal_Invalid_Should_Return_Primary()
        {
            var result = ResponsibilitySequenceTypeHelper.FromOrdinal(99);

            Assert.Equal(ResponsibilitySequenceType.Primary, result);
        }


        [Fact]
        public void GetPreviousSequence_Should_Return_Correct_Value()
        {
            var result = ResponsibilitySequenceHelper.GetPreviousSequence(ResponsibilitySequenceType.Secondary);

            Assert.Equal(ResponsibilitySequenceType.Primary, result);
        }

        [Fact]
        public void GetPreviousSequence_Primary_Should_Return_Null()
        {
            var result = ResponsibilitySequenceHelper.GetPreviousSequence(ResponsibilitySequenceType.Primary);

            Assert.Null(result);
        }


        [Fact]
        public void GetCurrentSequence_Should_Return_Current_When_Exists()
        {
            var result = ResponsibilitySequenceHelper.GetCurrentSequence(ResponsibilitySequenceType.Secondary);

            Assert.Equal(ResponsibilitySequenceType.Secondary, result);
        }


        [Fact]
        public void GetEnumFromString_Should_Return_Enum_When_Valid()
        {
            var result = ResponsibilitySequenceHelper.GetEnumFromString<ResponsibilitySequenceType>("P");

            Assert.Equal(ResponsibilitySequenceType.Primary, result);
        }

        [Fact]
        public void GetEnumFromString_Invalid_Should_Return_Null()
        {
            var result = ResponsibilitySequenceHelper.GetEnumFromString<ResponsibilitySequenceType>("X");

            Assert.Null(result);
        }

        [Fact]
        public void GetEnumMemberValue_Should_Return_Attribute_Value()
        {
            var value = ResponsibilitySequenceType.Primary.GetEnumMemberValue();

            Assert.Equal("P", value);
        }
    }
}

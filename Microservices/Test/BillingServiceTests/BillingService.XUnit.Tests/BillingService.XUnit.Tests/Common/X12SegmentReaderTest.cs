using Rethink.Services.Common.Utils;
using System;
using Xunit;

namespace BillingService.XUnit.Tests.Common
{
    public class X12SegmentReaderTests
    {
        #region ResolveCoverageStatus Tests

        [Theory]
        [InlineData("1", "Active")]
        [InlineData("2", "Active")]
        [InlineData("3", "Active")]
        [InlineData("4", "Active")]
        [InlineData("5", "Active-Conditional")]
        [InlineData("M", "Active-Conditional")]
        [InlineData("6", "Inactive")]
        [InlineData("X", "Inactive")]
        [InlineData("ABC", "Unknown")]
        [InlineData(null, "Unknown")]
        public void ResolveCoverageStatus_ReturnsExpectedStatus(string input, string expected)
        {
            var result = X12SegmentReader.ResolveCoverageStatus(input);
            Assert.Equal(expected, result);
        }

        #endregion

        #region ParseSingleDate Tests

        [Fact]
        public void ParseSingleDate_ValidDate_ReturnsDate()
        {
            var result = X12SegmentReader.ParseSingleDate("20250101");

            Assert.NotNull(result);
            Assert.Equal(new DateTime(2025, 1, 1), result.Value);
        }

        [Fact]
        public void ParseSingleDate_InvalidDate_ReturnsNull()
        {
            var result = X12SegmentReader.ParseSingleDate("invalid");

            Assert.Null(result);
        }

        #endregion

        #region ParseDtp Tests

        [Fact]
        public void ParseDtp_D8Format_ReturnsStartDateOnly()
        {
            var (start, end) = X12SegmentReader.ParseDtp("D8", "20250101");

            Assert.Equal(new DateTime(2025, 1, 1), start);
            Assert.Null(end);
        }

        [Fact]
        public void ParseDtp_RD8Format_ReturnsStartAndEndDate()
        {
            var (start, end) = X12SegmentReader.ParseDtp("RD8", "20250101-20251231");

            Assert.Equal(new DateTime(2025, 1, 1), start);
            Assert.Equal(new DateTime(2025, 12, 31), end);
        }

        [Fact]
        public void ParseDtp_InvalidFormat_ReturnsNulls()
        {
            var (start, end) = X12SegmentReader.ParseDtp("XX", "20250101");

            Assert.Null(start);
            Assert.Null(end);
        }

        #endregion

        #region ParseRange Tests

        [Fact]
        public void ParseRange_ValidRange_ReturnsDates()
        {
            var (start, end) = X12SegmentReader.ParseRange("20250101-20251231");

            Assert.Equal(new DateTime(2025, 1, 1), start);
            Assert.Equal(new DateTime(2025, 12, 31), end);
        }

        [Fact]
        public void ParseRange_NullOrEmpty_ReturnsNulls()
        {
            var (start, end) = X12SegmentReader.ParseRange(null);

            Assert.Null(start);
            Assert.Null(end);
        }

        [Fact]
        public void ParseRange_InvalidFormat_ReturnsNulls()
        {
            var (start, end) = X12SegmentReader.ParseRange("20250101");

            Assert.Null(start);
            Assert.Null(end);
        }

        #endregion
    }
}

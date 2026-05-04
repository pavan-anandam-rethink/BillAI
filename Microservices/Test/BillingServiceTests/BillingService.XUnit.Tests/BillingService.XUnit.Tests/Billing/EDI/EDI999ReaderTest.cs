using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Utils;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.EDI
{
    public class EDI999ReaderTest
    {
        #region Parse Tests - Basic Functionality

        [Fact]
        public void Parse_WithEmptyContent_ShouldReturnDefaultSummary()
        {
            // Arrange
            var ediContent = "";
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(fileName, result.FileName);
            Assert.Equal(partner, result.Partner);
            Assert.Equal(0, result.TotalTransactionSets);
            Assert.Equal(0, result.Accepted);
            Assert.Equal(0, result.Rejected);
        }

        [Fact]
        public void Parse_WithNullContent_ShouldReturnDefaultSummary()
        {
            // Arrange
            string ediContent = null;
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalTransactionSets);
            Assert.Equal(0, result.Accepted);
            Assert.Equal(0, result.Rejected);
        }

        [Fact]
        public void Parse_WithWhitespaceContent_ShouldReturnDefaultSummary()
        {
            // Arrange
            var ediContent = "   ";
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalTransactionSets);
        }

        #endregion

        #region Parse Tests - AK9 Segment Processing

        [Fact]
        public void Parse_WithSingleAK9Segment_ShouldExtractTotalAndAccepted()
        {
            // Arrange
            var ediContent = "ISA*00~AK9*A*5*5*4~IEA*1~";
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.Equal(5, result.TotalTransactionSets);
            Assert.Equal(4, result.Accepted);
        }

        [Fact]
        public void Parse_WithMultipleAK9Segments_ShouldSumTotalsAndAccepted()
        {
            // Arrange
            var ediContent = "AK9*A*3*3*2~AK9*A*2*2*1~";
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.Equal(5, result.TotalTransactionSets); // 3 + 2
            Assert.Equal(3, result.Accepted); // 2 + 1
        }

        [Fact]
        public void Parse_WithAK9SegmentHavingInsufficientParts_ShouldNotThrowException()
        {
            // Arrange
            var ediContent = "AK9*A*3~"; // Only 3 parts instead of 5
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.Equal(0, result.TotalTransactionSets);
            Assert.Equal(0, result.Accepted);
        }

        [Fact]
        public void Parse_WithAK9SegmentHavingInvalidNumbers_ShouldDefaultToZero()
        {
            // Arrange
            var ediContent = "AK9*A*invalid*5*abc~";
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.Equal(0, result.TotalTransactionSets);
            Assert.Equal(0, result.Accepted);
        }

        #endregion

        #region Parse Tests - IK5 Segment Processing

        [Fact]
        public void Parse_WithIK5RejectedSegment_ShouldIncrementRejectedCount()
        {
            // Arrange
            var ediContent = "IK5*R~";
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.Equal(1, result.Rejected);
        }

        [Fact]
        public void Parse_WithMultipleIK5RejectedSegments_ShouldCountAllRejections()
        {
            // Arrange
            var ediContent = "IK5*R~IK5*R~IK5*R~";
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.Equal(3, result.Rejected);
        }

        [Fact]
        public void Parse_WithIK5AcceptedSegment_ShouldNotIncrementRejectedCount()
        {
            // Arrange
            var ediContent = "IK5*A~";
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.Equal(0, result.Rejected);
        }

        [Fact]
        public void Parse_WithMixedIK5Segments_ShouldOnlyCountRejected()
        {
            // Arrange
            var ediContent = "IK5*A~IK5*R~IK5*A~IK5*R~";
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.Equal(2, result.Rejected);
        }

        [Fact]
        public void Parse_WithIK5SegmentHavingInsufficientParts_ShouldNotThrowException()
        {
            // Arrange
            var ediContent = "IK5~"; // Only 1 part
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.Equal(0, result.Rejected);
        }

        #endregion

        #region Parse Tests - Combined Scenarios

        [Fact]
        public void Parse_WithCompleteEDI999Content_ShouldParseAllSegments()
        {
            // Arrange
            var ediContent = @"ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *200101*1200*^*00501*000000001*0*P*:~
                               GS*FA*SENDER*RECEIVER*20200101*1200*1*X*005010X231~
                               ST*999*0001~
                               AK1*HC*1~
                               AK9*A*10*10*8~
                               IK5*R~
                               IK5*R~
                               SE*6*0001~
                               GE*1*1~
                               IEA*1*000000001~";
            var fileName = "complete_test.999";
            var partner = "AvailityPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.Equal(fileName, result.FileName);
            Assert.Equal(partner, result.Partner);
            Assert.Equal(10, result.TotalTransactionSets);
            Assert.Equal(8, result.Accepted);
            Assert.Equal(2, result.Rejected);
        }

        [Fact]
        public void Parse_WithNoRelevantSegments_ShouldReturnZeroCounts()
        {
            // Arrange
            var ediContent = "ISA*00~GS*FA~ST*999~SE*4~GE*1~IEA*1~";
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.Equal(0, result.TotalTransactionSets);
            Assert.Equal(0, result.Accepted);
            Assert.Equal(0, result.Rejected);
        }

        #endregion

        #region Parse Tests - Edge Cases

        [Fact]
        public void Parse_WithSegmentsContainingSpaces_ShouldTrimAndParse()
        {
            // Arrange
            var ediContent = "  AK9*A*5*5*3  ~  IK5*R  ~";
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.Equal(5, result.TotalTransactionSets);
            Assert.Equal(3, result.Accepted);
            Assert.Equal(1, result.Rejected);
        }

        [Fact]
        public void Parse_WithConsecutiveTildes_ShouldIgnoreEmptySegments()
        {
            // Arrange
            var ediContent = "AK9*A*3*3*2~~~IK5*R~~";
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.Equal(3, result.TotalTransactionSets);
            Assert.Equal(2, result.Accepted);
            Assert.Equal(1, result.Rejected);
        }

        #endregion

        #region EDI999Summary Status Tests

        [Fact]
        public void Summary_WithOnlyAccepted_ShouldHaveAcceptedStatus()
        {
            // Arrange
            var ediContent = "AK9*A*5*5*5~";
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.Equal("Accepted", result.Status);
            Assert.Equal(0, result.Partial);
        }

        [Fact]
        public void Summary_WithOnlyRejected_ShouldHaveRejectedStatus()
        {
            // Arrange
            var ediContent = "AK9*A*5*5*0~IK5*R~IK5*R~";
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.Equal("Rejected", result.Status);
            Assert.Equal(0, result.Partial);
        }

        [Fact]
        public void Summary_WithBothAcceptedAndRejected_ShouldHavePartialStatus()
        {
            // Arrange
            var ediContent = "AK9*A*5*5*3~IK5*R~";
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.Equal("Partial", result.Status);
            Assert.Equal(1, result.Partial);
        }

        [Fact]
        public void Summary_WithNoResults_ShouldHaveUnknownStatus()
        {
            // Arrange
            var ediContent = "";
            var fileName = "test.999";
            var partner = "TestPartner";

            // Act
            var result = EDI999Reader.Parse(ediContent, fileName, partner);

            // Assert
            Assert.Equal("Unknown", result.Status);
        }

        #endregion
    }
}

using BillingService.Domain.Services.Billing.EDI;
using EdiFabric.Core.Model.Edi.X12;
using System;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.EDI
{
    public class SegmentBuildersTest
    {
        [Fact]
        public void BuildIsa_Should_Create_Valid_ISA()
        {
            // Arrange
            var controlNumber = "123";
            var securityInfo = "SEC";
            var senderId = "SENDER";
            var receiverId = "RECEIVER";
            var testIndicator = "T";
            var ackRequested = "1";

            // Act
            var isa = SegmentBuilders.BuildIsa(
                controlNumber,
                securityInfo,
                senderId,
                receiverId,
                testIndicator,
                ackRequested);

            // Assert
            Assert.NotNull(isa);
            Assert.Equal("00", isa.AuthorizationInformationQualifier_1);
            Assert.Equal("00", isa.SecurityInformationQualifier_3);
            Assert.Equal(testIndicator, isa.UsageIndicator_15);
            Assert.Equal(controlNumber.PadLeft(9, '0'), isa.InterchangeControlNumber_13);
        }

        [Fact]
        public void BuildGs_Should_Create_Valid_GS()
        {
            // Arrange
            var controlNumber = "456";
            var customerId = "CUSTOMER";
            var receiverId = "RECEIVER";
            var version = "005010X279A1";

            // Act
            var gs = SegmentBuilders.BuildGs(
                controlNumber,
                customerId,
                receiverId,
                version);

            // Assert
            Assert.NotNull(gs);
            Assert.Equal("HC", gs.CodeIdentifyingInformationType_1);
            Assert.Equal(customerId, gs.SenderIDCode_2);
            Assert.Equal(receiverId, gs.ReceiverIDCode_3);
            Assert.Equal(controlNumber.PadLeft(9, '0'), gs.GroupControlNumber_6);
            Assert.Equal(version, gs.VersionAndRelease_8);
        }

        [Fact]
        public void BuildIsa270_Should_Create_Valid_ISA()
        {
            // Arrange
            var controlNumber = "12345";
            var securityInfo = "SECURITY";
            var senderId = "SENDER";
            var receiverId = "RECEIVER";
            var testIndicator = "T";

            // Act
            var isa = SegmentBuilders270.BuildIsa(
                controlNumber,
                securityInfo,
                senderId,
                receiverId,
                testIndicator);

            // Assert
            Assert.NotNull(isa);
            Assert.Equal("00", isa.AuthorizationInformationQualifier_1);
            Assert.Equal("00501", isa.InterchangeControlVersionNumber_12);
            Assert.Equal("000012345", isa.InterchangeControlNumber_13);
            Assert.Equal("T", isa.UsageIndicator_15);
        }

        [Fact]
        public void BuildIsa270_Should_Set_Production_Mode_When_Not_Test()
        {
            // Arrange
            var controlNumber = "999";
            var securityInfo = "SEC";
            var senderId = "SENDER";
            var receiverId = "RECEIVER";

            // Act
            var isa = SegmentBuilders270.BuildIsa(
                controlNumber,
                securityInfo,
                senderId,
                receiverId,
                "P");

            // Assert
            Assert.Equal("P", isa.UsageIndicator_15);
        }

        [Fact]
        public void BuildIsa270_Should_Throw_Exception_When_ControlNumber_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                SegmentBuilders270.BuildIsa( null,"SEC","SENDER","RECEIVER","T"));
        }

        [Fact]
        public void BuildIsa270_Should_Throw_Exception_When_ControlNumber_Has_No_Digits()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                SegmentBuilders270.BuildIsa("ABC","SEC","SENDER","RECEIVER","T"));
        }

        [Fact]
        public void BuildGs270_Should_Create_Valid_GS()
        {
            // Arrange
            var groupControlNumber = "123";
            var senderCode = "SENDER";
            var receiverCode = "RECEIVER";
            var version = "005010X279A1";

            // Act
            var gs = SegmentBuilders270.BuildGs(
                groupControlNumber,
                senderCode,
                receiverCode,
                version);

            // Assert
            Assert.NotNull(gs);
            Assert.Equal("HS", gs.CodeIdentifyingInformationType_1);
            Assert.Equal(senderCode, gs.SenderIDCode_2);
            Assert.Equal(receiverCode, gs.ReceiverIDCode_3);
            Assert.Equal(groupControlNumber, gs.GroupControlNumber_6);
            Assert.Equal(version, gs.VersionAndRelease_8);
        }

        [Fact]
        public void BuildGs270_Should_Throw_Exception_When_GroupControlNumber_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                SegmentBuilders270.BuildGs(null,"SENDER","RECEIVER","005010X279A1"));
        }

        [Fact]
        public void BuildIsa_Should_Handle_Null_AckRequested()
        {
            // Arrange
            var isa = SegmentBuilders.BuildIsa("1","SEC","SEND","RECV","T",null);

            // Assert
            Assert.NotNull(isa);
            Assert.Null(isa.AcknowledgementRequested_14);
        }

        [Fact]
        public void BuildIsa270_Should_Handle_Null_SecurityInfo()
        {
            // Arrange
            var isa = SegmentBuilders270.BuildIsa("123",null,"SEND","RECV","T");

            // Assert
            Assert.NotNull(isa);
            Assert.Equal(10, isa.SecurityInformation_4.Length);
        }

        [Fact]
        public void BuildIsa270_Should_Handle_Null_SenderQualifier_ReceiverQualifier()
        {
            // Arrange
            var isa = SegmentBuilders270.BuildIsa("123","SEC","SEND","RECV","T",null,null);

            // Assert
            Assert.NotNull(isa);
            Assert.Equal("ZZ", isa.SenderIDQualifier_5);
            Assert.Equal("ZZ", isa.ReceiverIDQualifier_7);
        }

        [Fact]
        public void BuildIsa270_Should_Default_AckRequested_When_Null()
        {
            // Arrange
            var isa = SegmentBuilders270.BuildIsa("123","SEC","SEND","RECV","T","ZZ","ZZ",null);

            // Assert
            Assert.Equal("0", isa.AcknowledgementRequested_14);
        }

        [Fact]
        public void BuildIsa270_Should_Handle_ComponentSeparator_Reflection_Path()
        {
            // Arrange
            var isa = SegmentBuilders270.BuildIsa("123","SEC","SEND","RECV","T","ZZ","ZZ","1",'^',':');

            // Assert
            Assert.NotNull(isa);
        }

        [Fact]
        public void BuildIsa_Should_Pad_Fields_Correctly()
        {
            // Arrange
            var isa = SegmentBuilders.BuildIsa("1","SEC","SEND","RECV","T");

            // Assert
            Assert.Equal(10, isa.SecurityInformation_4.Length);
            Assert.Equal(15, isa.InterchangeSenderID_6.Length);
            Assert.Equal(15, isa.InterchangeReceiverID_8.Length);
        }

        [Fact]
        public void BuildGs_Should_Set_Date_And_Time()
        {
            // Arrange
            var gs = SegmentBuilders.BuildGs("1","SEND","RECV","00501");

            // Assert
            Assert.NotNull(gs.Date_4);
            Assert.NotNull(gs.Time_5);
        }

        [Fact]
        public void BuildGs270_Should_Set_Date_Time()
        {
            var gs = SegmentBuilders270.BuildGs("1","SEND","RECV","00501");

            Assert.NotNull(gs.Date_4);
            Assert.NotNull(gs.Time_5);
        }
    }
}
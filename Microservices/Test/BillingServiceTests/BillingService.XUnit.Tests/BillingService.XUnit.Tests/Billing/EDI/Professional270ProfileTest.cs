using BillingService.Domain.Services.Billing.EDI;
using EdiFabric.Core.Model.Edi.X12;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.EDI
{
    public class Professional270ProfileTest
    {
        [Fact]
        public void Constructor_Should_Set_ReceiverIds_Correctly()
        {
            // Arrange
            var isaReceiverId = "ISA_RECEIVER";
            var gsReceiverId = "GS_RECEIVER";

            // Act
            var profile = new Professional270Profile(isaReceiverId, gsReceiverId);

            // Assert
            Assert.Equal("005010X279A1", profile.GsVersion);
            Assert.Equal(isaReceiverId, profile.IsaReceiverId);
            Assert.Equal(gsReceiverId, profile.GsReceiverId);
        }

        [Fact]
        public void BuildIsa_Should_Return_ISA_Object()
        {
            // Arrange
            var profile = new Professional270Profile("ISA_RECEIVER", "GS_RECEIVER");

            var groupControlNumber = "0001";
            var securityInfo = "SECURITY";
            var submitterId = "SUBMITTER";
            var isaReceiverId = "RECEIVER";
            var testMode = "T";

            // Act
            ISA result = profile.BuildIsa(
                groupControlNumber,
                securityInfo,
                submitterId,
                isaReceiverId,
                testMode);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ISA>(result);
        }

        [Fact]
        public void BuildGs_Should_Return_GS_Object()
        {
            // Arrange
            var profile = new Professional270Profile("ISA_RECEIVER", "GS_RECEIVER");

            var groupControlNumber = "0001";
            var customerId = "CUSTOMER";
            var gsReceiverId = "RECEIVER";
            var gsVersion = "005010X279A1";

            // Act
            GS result = profile.BuildGs(
                groupControlNumber,
                customerId,
                gsReceiverId,
                gsVersion);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<GS>(result);
        }

        [Fact]
        public void Properties_Should_Return_Correct_Values()
        {
            // Arrange
            var profile = new Professional270Profile("ISA123", "GS456");

            // Assert
            Assert.Equal("ISA123", profile.IsaReceiverId);
            Assert.Equal("GS456", profile.GsReceiverId);
            Assert.Equal("005010X279A1", profile.GsVersion);
        }
    }
}
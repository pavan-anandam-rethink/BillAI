using System;
using System.Collections.Generic;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Utils;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.EDI
{
    public class EDI277DetailedReportRendererTest
    {
        #region Render Tests - Basic Functionality

        [Fact]
        public void Render_WithEmptyClaimsList_ShouldReturnEmptyString()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>();
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Render_WhenAllClaimsAreFiltered_ShouldReturnEmptyString()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM001" },
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM002" }
            };
            var claimIds = new List<string> { "CLM001", "CLM002" }; // All claims filtered

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Render_WithUnfilteredClaims_ShouldReturnReport()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary 
                { 
                    ClaimTrnNumber = "CLM001",
                    PatientName = "John Doe",
                    Status = "Rejected",
                    StcCode = "A1:0",
                    StcDescriptions = new List<string> { "Claim rejected" },
                    ActionRequired = "Resubmit claim"
                }
            };
            var claimIds = new List<string>(); // No filter

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("CLM001", result);
            Assert.Contains("John Doe", result);
        }

        #endregion

        #region Render Tests - Header Content

        [Fact]
        public void Render_ShouldContainReportHeader()
        {
            // Arrange
            var report = CreateReportWithOneClaim();
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("277CA Detailed Error Log", result);
            Assert.Contains("==========================================================", result);
        }

        [Fact]
        public void Render_ShouldContainSenderAndReceiver()
        {
            // Arrange
            var report = CreateReportWithOneClaim();
            report.Sender = "TestSender";
            report.Receiver = "TestReceiver";
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("Sender: TestSender", result);
            Assert.Contains("Receiver: TestReceiver", result);
        }

        [Fact]
        public void Render_ShouldContainFormattedDate()
        {
            // Arrange
            var report = CreateReportWithOneClaim();
            report.ReportDate = new DateTime(2024, 1, 15, 10, 30, 45);
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("Date: 2024-01-15 10:30:45", result);
        }

        [Fact]
        public void Render_WithTrnReferenceNumber_ShouldDisplayIt()
        {
            // Arrange
            var report = CreateReportWithOneClaim();
            report.TrnReferenceNumber = "TRN123456";
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("TRN Reference Number (Report Level): TRN123456", result);
        }

        [Fact]
        public void Render_WithEmptyTrnReferenceNumber_ShouldDisplayNotFound()
        {
            // Arrange
            var report = CreateReportWithOneClaim();
            report.TrnReferenceNumber = "";
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("TRN Reference Number (Report Level): [Not Found]", result);
        }

        [Fact]
        public void Render_WithBhtNumber_ShouldDisplayIt()
        {
            // Arrange
            var report = CreateReportWithOneClaim();
            report.BhtNumber = "BHT789";
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("BHT Number: BHT789", result);
        }

        [Fact]
        public void Render_WithEmptyBhtNumber_ShouldDisplayNotFound()
        {
            // Arrange
            var report = CreateReportWithOneClaim();
            report.BhtNumber = "";
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("BHT Number: [Not Found]", result);
        }

        #endregion

        #region Render Tests - Claim Details

        [Fact]
        public void Render_ShouldDisplayClaimTrnNumber()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLAIM12345" }
            };
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("Claim TRN #: CLAIM12345", result);
        }

        [Fact]
        public void Render_WithEmptyClaimTrnNumber_ShouldDisplayNotFound()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary { ClaimTrnNumber = "" }
            };
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("Claim TRN #: [Not Found]", result);
        }

        [Fact]
        public void Render_ShouldDisplayPatientName()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM001", PatientName = "Jane Smith" }
            };
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("Patient Name: Jane Smith", result);
        }

        [Fact]
        public void Render_WithNullPatientName_ShouldDisplayNotFound()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM001", PatientName = null }
            };
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("Patient Name: [Not Found]", result);
        }

        [Fact]
        public void Render_ShouldDisplayClaimStatus()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM001", Status = "Pending" }
            };
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("Status: Pending", result);
        }

        [Fact]
        public void Render_ShouldDisplayStcCode()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM001", StcCode = "A1:20:21" }
            };
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("STC Code: A1:20:21", result);
        }

        [Fact]
        public void Render_WithMultipleStcDescriptions_ShouldDisplayAll()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary 
                { 
                    ClaimTrnNumber = "CLM001", 
                    StcDescriptions = new List<string> { "Description 1", "Description 2", "Description 3" }
                }
            };
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("  - Description 1", result);
            Assert.Contains("  - Description 2", result);
            Assert.Contains("  - Description 3", result);
        }

        [Fact]
        public void Render_WithNullStcDescriptions_ShouldDisplayNotFound()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM001", StcDescriptions = null }
            };
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("  - [Not Found]", result);
        }

        [Fact]
        public void Render_WithEmptyStcDescriptions_ShouldDisplayNotFound()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM001", StcDescriptions = new List<string>() }
            };
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("  - [Not Found]", result);
        }

        [Fact]
        public void Render_ShouldDisplayActionRequired()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM001", ActionRequired = "Please resubmit" }
            };
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("Action Required: Please resubmit", result);
        }

        [Fact]
        public void Render_WithEmptyActionRequired_ShouldDisplayNone()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM001", ActionRequired = "" }
            };
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("Action Required: None", result);
        }

        #endregion

        #region Render Tests - Filtering

        [Fact]
        public void Render_ShouldExcludeClaimsInFilterList()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM001", PatientName = "John" },
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM002", PatientName = "Jane" },
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM003", PatientName = "Bob" }
            };
            var claimIds = new List<string> { "CLM001", "CLM003" }; // Filter out CLM001 and CLM003

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.DoesNotContain("CLM001", result);
            Assert.Contains("CLM002", result);
            Assert.DoesNotContain("CLM003", result);
        }

        [Fact]
        public void Render_WithPartialFilter_ShouldOnlyShowUnfilteredClaims()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM001" },
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM002" }
            };
            var claimIds = new List<string> { "CLM001" };

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.DoesNotContain("CLM001", result);
            Assert.Contains("CLM002", result);
        }

        #endregion

        #region Render Tests - Multiple Claims

        [Fact]
        public void Render_WithMultipleClaims_ShouldRenderAllUnfiltered()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM001", PatientName = "Patient One" },
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM002", PatientName = "Patient Two" },
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM003", PatientName = "Patient Three" }
            };
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("Patient One", result);
            Assert.Contains("Patient Two", result);
            Assert.Contains("Patient Three", result);
        }

        [Fact]
        public void Render_WithMultipleClaims_ShouldIncludeSeparatorBetweenClaims()
        {
            // Arrange
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM001" },
                new EDI277DetailedReportSummary { ClaimTrnNumber = "CLM002" }
            };
            var claimIds = new List<string>();

            // Act
            var result = EDI277DetailedReportRenderer.Render(report, claimIds);

            // Assert
            Assert.Contains("----------------------------------------------------------", result);
        }

        #endregion

        #region Helper Methods

        private EDI277CADetailedReport CreateBasicReport()
        {
            return new EDI277CADetailedReport
            {
                Sender = "Sender",
                Receiver = "Receiver",
                TrnReferenceNumber = "TRN123",
                BhtNumber = "BHT456",
                ReportDate = DateTime.Now,
                Claims = new List<EDI277DetailedReportSummary>()
            };
        }

        private EDI277CADetailedReport CreateReportWithOneClaim()
        {
            var report = CreateBasicReport();
            report.Claims = new List<EDI277DetailedReportSummary>
            {
                new EDI277DetailedReportSummary
                {
                    ClaimTrnNumber = "CLM001",
                    PatientName = "Test Patient",
                    Status = "Pending",
                    StcCode = "A1:0",
                    StcDescriptions = new List<string> { "Test description" },
                    ActionRequired = "None"
                }
            };
            return report;
        }

        #endregion
    }
}

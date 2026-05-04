using Billing.FolderStructure.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billing.FolderStructure.Tests.Utils
{
    public class EDI277DetailedReportReaderTest
    {
        [Fact]
        public void Parse_WithValidEDI_ShouldParseCompleteReport()
        {
            string edi =
            "BHT*0085*08*BHT123~" +
            "NM1*AY*2*SenderName****~" +
            "NM1*41*2*ReceiverName****~" +
            "TRN*1*REPORTTRN~" +
            "NM1*QC*1*JohnDoe****~" +
            "TRN*2*CLAIMTRN1~" +
            "STC*A1:19*20240101*WQ*1000*EXTRA*Accepted Claim~" +
            "REF*F8*CCN12345~";

            var result = EDI277DetailedReportReader.Parse(edi);

            Assert.Equal("BHT123", result.BhtNumber);
            Assert.Equal("SenderName", result.Sender);
            Assert.Equal("ReceiverName", result.Receiver);
            Assert.Equal("REPORTTRN", result.TrnReferenceNumber);

            var claim = Assert.Single(result.Claims);
            Assert.Equal("JohnDoe", claim.PatientName);
            Assert.Equal("CLAIMTRN1", claim.ClaimTrnNumber);
            Assert.Equal("CCN12345", claim.ClaimControlNumber);
            Assert.Equal("Accepted", claim.Status);
        }

        [Fact]
        public void Parse_WithRejectedSTC_ShouldMarkRejected()
        {
            string edi =
            "TRN*1*REPORTTRN~" +
            "NM1*QC*1*JaneDoe****~" +
            "TRN*2*TRN2~" +
            "STC*R5:20*20240101*WQ*1000*EXTRA*Missing Info~";

            var result = EDI277DetailedReportReader.Parse(edi);
            var claim = Assert.Single(result.Claims);

            Assert.Equal("Rejected", claim.Status);
            Assert.Equal("EXTRA Missing Info", claim.ActionRequired);
        }


        [Fact]
        public void Parse_WhenPatientNameBeforeTrn_ShouldAssignCorrectly()
        {
            string edi =
            "TRN*1*REPORTTRN~" +
            "NM1*QC*1*PreTrnPatient****~" +
            "TRN*2*TRN3~" +
            "STC*A1:19*20240101*WQ*1000*EXTRA*OK~";

            var result = EDI277DetailedReportReader.Parse(edi);
            var claim = Assert.Single(result.Claims);

            Assert.Equal("PreTrnPatient", claim.PatientName);
        }

        [Fact]
        public void Parse_WithMultipleClaims_ShouldHandleSeparately()
        {
            string edi =
            "TRN*1*REPORTTRN~" +
            "NM1*QC*1*PatientOne****~" +
            "TRN*2*TRN1~" +
            "STC*A1:19*20240101*WQ*1000*EXTRA*OK~" +
            "NM1*QC*1*PatientTwo****~" +
            "TRN*2*TRN2~" +
            "STC*R5:20*20240101*WQ*1000*EXTRA*Error~";

            var result = EDI277DetailedReportReader.Parse(edi);

            Assert.Equal(2, result.Claims.Count);
        }

        [Fact]
        public void Parse_WithDuplicateSTC_ShouldNotDuplicateDescriptions()
        {
            string edi =
            "TRN*1*REPORTTRN~" +
            "NM1*QC*1*John****~" +
            "TRN*2*TRN100~" +
            "STC*A1:19*20240101*WQ*1000*EXTRA*OK~" +
            "STC*A1:19*20240101*WQ*1000*EXTRA*OK~";

            var result = EDI277DetailedReportReader.Parse(edi);
            var claim = Assert.Single(result.Claims);

            Assert.Single(claim.StcDescriptions);
        }

        [Fact]
        public void Parse_WithREFQualifiers_ShouldCaptureClaimControlNumber()
        {
            string edi =
            "TRN*1*REPORTTRN~" +
            "NM1*QC*1*John****~" +
            "TRN*2*TRN200~" +
            "STC*A1:19*20240101*WQ*1000*EXTRA*OK~" +
            "REF*D9*CCN-D9~" +
            "REF*TJ*CCN-TJ~";

            var result = EDI277DetailedReportReader.Parse(edi);
            var claim = Assert.Single(result.Claims);

            Assert.Equal("CCN-TJ", claim.ClaimControlNumber);
        }

        [Fact]
        public void Parse_WithMissingSegments_ShouldNotCrash()
        {
            string edi = @"BHT*0085*08*ONLYBHT~";
            var result = EDI277DetailedReportReader.Parse(edi);
            Assert.Empty(result.Claims);
            Assert.Equal("ONLYBHT", result.BhtNumber);
            }

        [Fact]
        public void Parse_WhenNoSTC_ShouldNotAddClaim()
        {
            string edi = @"
			TRN*1*REPORTTRN~
			NM1*QC*1*NoSTCPatient~
			TRN*2*TRN300~";
            
            var result = EDI277DetailedReportReader.Parse(edi);
            Assert.Empty(result.Claims);
            }
        }
    }

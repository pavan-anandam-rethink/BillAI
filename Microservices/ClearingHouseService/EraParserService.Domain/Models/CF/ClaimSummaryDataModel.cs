using System;
using System.Collections.Generic;

namespace EraParserService.Domain.Models.CF
{
    public class ClaimSummaryDataModel
    {
        public int BillingId { get; set; }
        public int SubmitterId { get; set; }
        public string SubmitterName { get; set; }
        public DateTime BillingDate { get; set; }
        public DateTime ProcessingDate { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public string ChangeHealthcareAssignedInputFileId { get; set; }
        public string TestingIndicator { get; set; }
        public int CPID { get; set; }
        public string CPIDStateCode { get; set; }
        public string CarrierName { get; set; }
        public string ClientFormId { get; set; }
        public string SubmitterAssignedClaimId { get; set; }
        public string ChangeHealthcareAssignedClaimId { get; set; }
        public int TransactionCount { get; set; }
        public int SupplementalClaimsCount { get; set; }
        public string DistributionCode { get; set; }
        public string PatientId { get; set; }
        public string PatientFirstName { get; set; }
        public string PatientMiddleInitial { get; set; }
        public string PatientLastName { get; set; }
        public DateTime ClaimFromDate { get; set; }
        public DateTime ClaimToDate { get; set; }
        public decimal ClaimAmount { get; set; }
        public string InstitutionalTypeOfBill { get; set; }
        public string FederalTaxId { get; set; }
        public string SiteId { get; set; }
        public string ClaimFileIndicator { get; set; }
        public string ErrorFlag { get; set; }

        public List<RejectErrorModel> RejectErrors { get; set; } = new List<RejectErrorModel>();
    }
}

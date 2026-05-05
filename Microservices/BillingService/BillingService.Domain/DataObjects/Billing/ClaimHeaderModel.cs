using System;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.DataObjects.Billing
{
    [ExcludeFromCodeCoverage]
    public class ClaimHeaderModel
    {
        public int Id { get; set; }
        public string ClaimNumber { get; set; }
        public string PatientName { get; set; }
        public string FunderName { get; set; }
        public int ChildProfileId { get; set; }
        public int ChildProfileAuthorizationId { get; set; }
        public int ChildProfileFunderId { get; set; }
        public int LocationCodeId { get; set; }
        public int? FunderId { get; set; }
        public int? LastBilledFunderId { get; set; }
        public string RenderingProviderName { get; set; }
        public int RenderingProviderId { get; set; }
        public string PlaceOfService { get; set; }
        public DateTime DateOfServiceStart { get; set; }
        public DateTime DateOfServiceEnd { get; set; }
        public string AuthorizationNumber { get; set; }
        public decimal? BilledAmount { get; set; }
        public decimal? ExpectedAmount { get; set; }
        public decimal? PaymentAmount { get; set; }
        public decimal? AdjustmentAmount { get; set; }
        public decimal? PatientResponsibilityAmount { get; set; }
        public decimal? BalanceAmount { get; set; }
        public bool IsManual { get; set; } = false;
        public DateTime BilledDate { get; set; }
        public int ValidationAlertsCount { get; set; }
        public int ValidationErrorsCount { get; set; }
        public int AssigneeId { get; set; }
        public string AssigneeName { get; set; }
        public int Status { get; set; }
        public string ClaimStatusName { get; set; }
        public int SubmissionStatusId { get; set; }
        public int SubmissionTypeId { get; set; }
        public int ErrorsCount { get; set; }
        public int ResponseCount { get; set; }
        public int WarningsCount { get; set; }
        public int TotalCount { get; set; }
        public bool HasNote { get; set; }
        public int CMSPagesCount { get; set; }
        public int? PrimaryFunderId { get; set; }
        public int? SecondaryFunderId { get; set; }
        public bool IsSecondaryPayerAvailable { get; set; }
        public string ReasonCodes { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public int? FlagReasonTransactionId { get; set; }
        public int? ReasonId { get; set; }
        public bool IsTestAccount { get; set; } = false;
        public bool UseNewClaimProcessing { get; set; } = false;
        public bool IsClientDeleted { get; set; } = false;
        public string? ClaimSubmissionIdentifier { get; set; }
    }
}
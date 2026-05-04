using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Enums.Billing;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ClaimVersionEntity : BasePersistEntity, IAuditedEntity
    {
        public Guid Identifier { get; set; }

        public string ClaimIdentifier { get; set; }
        public int ClaimId { get; set; }
        public int AccountInfoId { get; set; }
        public int MemberId { get; set; }

        [Column("claimStatusId")]
        public ClaimStatus Status { get; set; }

        // Client Info
        public string ClientName { get; set; }
        public string ResponsibleParty { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string DiagnosisCodes { get; set; }
        public string AuthorizationNumber { get; set; }

        // Charge Detail Summary
        public decimal BalanceAmount { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal BilledAmount { get; set; }
        public decimal PatientResponsibilityAmount { get; set; }

        // Providers
        public string RenderingProvider { get; set; }
        public string BillingProvider { get; set; }
        public string ReferringProvider { get; set; }
        public string ServiceProvider { get; set; }

        public string PlaceOfService { get; set; }
        public string BenefitAssignment { get; set; }
        public int SubmissionReason { get; set; }
        public string AuthorizedReleaseOfInfo { get; set; }
        public string AuthorizePayment { get; set; }
        public string SubmissionCode { get; set; }
        public string OriginalClaim { get; set; }
        public string Note { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual ClaimHistoryEntity ClaimHistory { get; set; }
    }
}

using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ClaimEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("providerBillingCodeId")]
        public int? ProviderBillingCodeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int AccountInfoId { get; set; }
        public int ChildProfileId { get; set; }
        [Column("tblMemberId")]
        public int MemberId { get; set; }
        [Column("hcLocationCodeId")]
        public int LocationCodeId { get; set; }
        [Column("hcChildProfileAuthorizationId")]
        public int? AuthorizationId { get; set; }
        public string AuthorizationNumber { get; set; }
        public string ClaimIdentifier { get; set; }
        [Column("hcProviderLocationId")]
        public int? ProviderLocationId { get; set; }
        //[Obsolete("PrimaryFunderId is deprecated. LEGACY USE ONLY!!! Use ClaimSubmission instead.")]
        public int PrimaryFunderId { get; set; }
        //[Obsolete("SecondaryFunderId is deprecated. LEGACY USE ONLY!!! Use ClaimSubmission instead.")]
        public int? SecondaryFunderId { get; set; }
        //[Obsolete("TertiaryFunderId is deprecated. LEGACY USE ONLY!!! Use ClaimSubmission instead.")]
        public int? TertiaryFunderId { get; set; }
        //TODO is this prop?
        public int? BillTo { get; set; }
        [Column("hcClaimStatusId")]
        public ClaimStatus ClaimStatus { get; set; }
        public DateTime? billedDate { get; set; }
        public string Note { get; set; }
        public bool? IsAppointmentDeleted { get; set; }
        public string ToLocation { get; set; }
        public int? ToLocationId { get; set; }
        public bool IsFlagged { get; set; }
        public int? RenderingStaffMemberId { get; set; }
        public int? RenderingProviderTypeId { get; set; }
        [Column("referringProviderId")]
        public int? ChildProfileReferringProviderId { get; set; }
        public int? ServiceLocationId { get; set; }
        public int? AuthorizedPaymentConfirmationTypeId { get; set; }
        public int? ReleaseOfInformationConfirmationTypeId { get; set; }
        public int? BenefitAssignmentId { get; set; }
        public string OriginalClaim { get; set; }
        public ClaimFrequencyType? FrequencyTypeId { get; set; }
        public int? LastBilledFunderId { get; set; }
        public int? ClientFunderId { get; set; }
        public int? ClientFunderServiceLineId { get; set; }
        [NotMapped]
        public string RenderingProviderNPI { get; set; }
        [NotMapped]
        public virtual FunderDetails ClientFunder { get; set; }
        public bool IsSecondaryPayerAvailable { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public bool? isPrivatePayClaim { get; set; }

        [NotMapped]
        public ProviderLocations ProviderLocation { get; set; }
        //[NotMapped]
        //public virtual MemberEntity Member { get; set; }
        //[NotMapped]
        //public virtual MemberEntity ModifiedByMember { get; set; }
        [NotMapped]
        public LocationCodesModel LocationCode { get; set; }
        //[Obsolete("PrimaryFunder is deprecated. LEGACY USE ONLY!!! Use ClaimSubmission instead.")]
        //[NotMapped]
        //public virtual FunderEntity PrimaryFunder { get; set; }
        //[Obsolete("SecondaryFunder is deprecated. LEGACY USE ONLY!!! Use ClaimSubmission instead.")]
        //[NotMapped]
        //public virtual FunderEntity SecondaryFunder { get; set; }
        //[Obsolete("TertiaryFunder is deprecated. LEGACY USE ONLY!!! Use ClaimSubmission instead.")]
        //[NotMapped]
        //public virtual FunderEntity TertiaryFunder { get; set; }
        [NotMapped]
        public virtual ChildProfileEntityModel ChildProfile { get; set; }
        [NotMapped]
        public AccountInfoEntityModel AccountInfo { get; set; }
        [NotMapped]
        public virtual RethinkAccountMember RenderingStaffMember { get; set; }
        [NotMapped]
        public virtual ClientAuthorization ChildProfileAuthorization { get; set; }
        [NotMapped]
        public virtual clientReferringProviders ReferringProvider { get; set; }
        [NotMapped]
        public virtual ProviderLocations ServiceLocation { get; set; }
        [NotMapped]
        public virtual ProviderLocations ServiceFacilityLocation { get; set; }

        public virtual ICollection<ClaimAppointmentLinkEntity> ClaimAppointmentLinks { get; set; } = new List<ClaimAppointmentLinkEntity>();
        public virtual ICollection<ClaimHistoryEntity> ClaimHistory { get; set; }

        public virtual ICollection<PaymentClaimEntity> PaymentClaims { get; set; }

        public virtual ICollection<ClaimChargeEntryEntity> ClaimChargeEntries { get; set; }
        public virtual ICollection<ClaimWriteOffEntity> ClaimWriteOffs { get; set; }

        public virtual ICollection<ClaimDiagnosisCodeEntity> ClaimDiagnosisCodes { get; set; }

        public ICollection<ClaimValidationErrorEntity> ClaimValidationErrors { get; set; }
        public ICollection<ClaimSubmissionEntity> ClaimSubmissions { get; set; }
        public ICollection<ClaimBillingProviderEntity> ClaimBillingProviders { get; set; }
        public int EntityTypeId => 5;
        public int AssigneeId { get; set; }
    }
}
using BillingService.Domain.Models;
using BillingService.Domain.Models.PaymentPosting;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.Billing;
using System;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing.ChangeTracking
{
    public interface IClaimChangeTrackingService
    {
        void Initialize(IdWithUserInfo claimUserInfo, ClaimAction action, ClaimHistoryAction historyAction, DateTime actionDate);
        void TrackChanges(ClaimEntity claim, UpdateClaimDetailsModel saveModel);
        void TrackChangesForCharges(ClaimChargeEntryEntity charge, UpdateBillingClaimDetailsModel saveModel);
        void TrackChangesForModifiers(ClaimChargeEntryEntity charge, UpdateChargeModifiersModel saveModel);
        void TrackAttachmentsChanges(ClaimAttachmentEntity attachment, RenameAttachmentModelWithUserInfo saveModel);
        Task SaveChangesAsync();
        Task SaveChangesAsync(string? ImpersonationUserName);

    }
}

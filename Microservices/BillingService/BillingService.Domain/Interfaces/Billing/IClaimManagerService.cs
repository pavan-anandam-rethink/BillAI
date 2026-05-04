using BillingService.Domain.Models;
using Rethink.Services.Common.Dtos.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Models.Claim;
using System;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IClaimManagerService
    {
        Task<ClaimEntity> InitializeClaim(int memberId,
                                          int accountInfoId,
                                          int childProfileId,
                                          int lastBilledFunderId,
                                          DateTime startDate,
                                          DateTime endDate);
        Task<int> SubmitInitialClaim(int claimId,
                                     int submittingMemberId,
                                     ClaimDocumentType documentType, // 837P, HCFA1500, etc.
                                     ResponsibilitySequenceType responsibilitySequence = ResponsibilitySequenceType.Primary);

        Task<int> SubmitClaimRebill(int claimId,
                                    int submittingMemberId,
                                    ClaimFrequencyType frequencyType);    // the rebill type: Original, Corrected, Void
        Task<int> SubmitClaimTransfer(int claimId,
                                      int submittingMemberId,
                                      ClaimFrequencyType frequencyType,    // if this is an Original, Corrected, Void transfer
                                      ClaimDocumentType documentType,
                                      int? secondaryFunderId = null,
                                      string controlNumber = null,
                                      bool IsRebillPostSecondaryBilling = false);     // the new document type 837P, HCFA1500, etc.

        Task<string> GenerateEdi(ClearingHouseClaimModel claimModelDto);
        Task<string> Generate270Edi(Eligibility270DTO billingEligibilityDTO);


        Task UpdateClaimSubmissionStatusAsync(int id, int memberId, ClaimSubmissionStatus status, bool commitImmediately = true);

        Task UpdateClaimStatusAsync(int id, ClaimStatus status, int memberId, bool commitImmediately = true, bool isBilledDateUpdate = false);

        Task<ClaimHFCAModel> LookupHCFAClaimDetails(int memberId, int accountInfoId, int claimId);

        Task<ClaimHFCAModel> CreateHCFAClaim(int memberId,
                                                         int accountInfoId,
                                                         int claimId,
                                                         ClaimFrequencyType frequencyType,
                                                         ClaimSubmissionType submissionType,
                                                         ResponsibilitySequenceType responsibilitySequence);

        Task<ClaimEntity> GetFullClaim(int claimId);

        Task<ChildProfileInfo> GetPatientInfoById(int patientId, int accountInfoId);
    }
}

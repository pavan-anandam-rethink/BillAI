using BillingService.Domain.DataObjects.Base;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.Funders;
using BillingService.Domain.Models.PaymentPosting;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientFunderModel = BillingService.Domain.Models.Funders.ClientFunderModel;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IClaimService
    {
        Task<IQueryable<BillingClaimDetailsModel>> GetClaimChargesForAccountAsync(GetBillingClaimDetailsModel model);
        Task<ActionResponse> RemoveBillingClaimDetailAsync(RemoveBillingClaimDetailsModel model);


        Task<List<int>> GetIdsForAccountAsync(int accountInfoId);

        Task<ActionResponse> GetClaimByIdentifierAsync(string claimIdentifier, int accountInfoId);

        Task<List<ClaimDropdownModel>> GetAccountClaimByIdOrPatientNameAsync(ClaimSearchModel model);

        Task<bool> IsDiagnosisServiceLineHasActiveClaims(int clienId, int diagnosisCodeId);
        Task<List<ClientDiagnosisServiceLine>> GetDiagnosisServiceLineUsedByClaims(int clienId, int diagnosisCodeId);

        Task<ClaimHeaderModelResponseModel> GetClaimHeadersAsync(ClaimGetRequestSortFilterWithUserInfo model);

        Task<List<ClaimFilterOptionModel>> GetClaimPatientsAsync(ClaimFilterGetModel model);
        Task<List<ClaimFilterOptionModel>> GetClaimFundersAsync(ClaimFilterGetModel model);
        Task<List<ClaimFilterOptionModel>> GetClaimRenderingProvidersAsync(ClaimFilterGetModel model);
        Task<List<ClaimFilterOptionModel>> GetClaimTabStatusesAsync(ClaimFilterGetModel model);
        Task<List<ClaimFilterOptionModel>> GetClaimIdentifiersAsync(ClaimFilterGetModel model);
        Task<ClaimDetailsModel> GetClaimDetailsAsync(IdWithUserInfo model, ClaimEntity claimEntity);
        Task<List<ClaimHFCAModel>> GetHFCAClaimDetailsAsync(IdsWithUserInfo model);
        Task<MemberViewSettingEntity> SaveSelectedColumnsAsync(int accountInfoId, int memberId, List<string> selectedColumns);
        Task<MemberViewSettingEntity> GetMemberViewSettingsAsync(int memberId);
        Task<List<ClaimErrorAlertViewModel>> GetClaimErrorsAndAlertsAsync(int claimId);
        Task<ClaimErrorsSourcesModel> GetErrorsSourcesAsync();
        Task<ClaimErrorsCodesModel> GetErrorsCodesAsync();
        Task<int> SaveClaimAsync(ClaimSaveModelWithUserInfo model);
        Task<List<ClaimApprovalResponseModel>> ApproveClaimsAsync(int accountInfoId, int memberId, int[] claimsIds);
        Task<int[]> UnapproveClaimsAsync(int accountInfoId, int memberId, int[] claimsIds);
        Task<int[]> FlagClaimsAsync(int accountInfoId, int memberId, int[] claimsIds, string? impersonationUserName = null);
        Task<int[]> UnflagClaimsAsync(int accountInfoId, int memberId, int[] claimsIds, string? impersonationUserName = null);
        Task<List<ClaimDeleteResultModel>> DeleteClaimsAsync(int accountInfoId, int memberId, int[] claimIds, string? impersonationUserName = null);
        Task<List<ServiceLineAppointmentModel>> GetClaimLineAppointmentsAsync(int accountInfoId, int serviceLineId);
        Task<int[]> MarkBilledClaimsAsync(int accountInfoId, int memberId, int[] claimsIds);
        Task<List<string>> SubmitClaimsAsync(ClaimsSubmitModel model);
        Task<int> ClaimProviderLocationUsageCountAsync(int providerLocationId);
        Task<int> ClaimReferringProviderUsageCountAsync(int providerId);
        Task<int> ClaimStaffAsRendingProviderUsageCountAsync(int staffId);
        Task<List<string>> VoidClaimsAsync(int accountInfoId, int memberId, ClaimsVoidModel model, int? clearingHouseId);
        Task<List<string>> CompleteSelectedClaimsAsync(int[] claimsToCompleteIds, int accountInfoId, int memberId);
        Task<List<string>> RebillClaimsAsync(int accountInfoId, int memberId, ClaimsRebillModel claimsToRebill, int? clearingHouseId);
        Task<List<string>> SecondaryBillingRebillClaimsAsync(SecondaryBillingClaimsRebillModel claimsToRebill);
        Task<ClaimNextFundersAndControlNumberModel> GetClaimBillNextFundersAndControlNumberAsync(int accountInfoId, int memberId, int claimId);
        Task<bool> HasFunderBilledClaimsAsync(ClientFunderModel model);

        Task SetEditAuthWarningAsync(AuthorizationModifiedModel authorizationId);
        Task<List<AuthorizationBuitData>> CheckIsAuthUsedByClaimAsync(int authorizationId);
        Task<bool> PopagateProvidersClaimDataAsync(PropagatingProvidersClaimDataModel model, int accountInfoId);
        Task<List<BasicOption>> GetClaimRenderingProviders(int accountInfoId);
        Task<List<BasicOption>> GetClaimReferringProviders(int claimId, int accountInfoId);
        Task<List<ProviderLocations>> GetClaimProviderLocations(int accountInfoId);
        Task<List<ClientFunderWithClaimModel>> IsFunderHasActiveClaimsAsync(IsClientFundersInUseModel model);
        Task ValidateClaimDataAsync(ClaimValidationModel model);
        Task ValidateClaimsOnFunderChangedAsync(int funderId, int clientFunderId, DateTime funderModifiedDate, int memberId);
        Task<bool> UpdateClaimAsync(UpdateDetails updateDetails);
        Task<bool> UpdateClaimDetailsAsync(UpdateClaimDetailsModel model, int accountInfoId, int memberId, bool isValidateRequired = false);
        Task<List<BillingClaimDetailsModel>> UpdateBillingClaimDetailsAsync(UpdateBillingClaimDetailsListModel model, int memberId, bool isValidateRequired = false, bool saveChanges = false);
        Task<List<CarcCodeResponseModel>> GetAllCarcCodes();
        Task<List<BillingClaimDetailsModel>> UpdateBillingClaimAsync(UpdateBillingClaimDetailsListModel model, int memberId, bool isValidateRequired = false);
        Task<List<AuthRenderingProviderType>> GetRenderingProviders(int accountInfoId);
        Task<bool> UpdateClaimsStatusAsync(UpdateClaimRequestModel model);
        Task<List<BaseNameOption>> GetStaffLocations(ClaimFilterGetModel requestModel);
        Task SubmitClaimsToServiceBusAsync(ClaimsSubmitModel requestModel);
        Task SubmitClaimsToServiceBusTopicAsync(IdsWithUserInfo model);
        Task<List<ClaimFlagReasonModel>> GetClaimFlagReasonsAsync(int accountInfoId);
        Task<int[]> FlagClaimsAsync(int accountInfoId, int memberId, int[] claimIds, int[] reasonIds, string? notes, int? claimReasonTransactionId = null, string? impersonationUserName = null);
        Task<bool> AssignClaimsAsync(int[] claimIds, int assigneeId, int memberId);
        Task<List<LocationBillingProviderDto>> GetLatestBillingProvidersAsync(int accountInfoId);
        Task<List<StateDto>> GetStatesAsync();
        Task<ClaimBillingProviderOtherDto> GetBillingProviderDetailsIdAsync(int claimId);
        Task<List<ExternalCodeResponseModel>> GetAllExternalCodes();
    }
}

using BillingService.Domain.Models;
using BillingService.Domain.Models.BillingSettings;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.BillingSettings
{
    public interface IBillingSettingsService
    {
        Task<List<ClaimFilingIndicatorModel>> GetClaimFilingIndicators();
        Task SetBillingFunderSettings(BillingFunderSettingRequestModel model);
        Task<BillingFunderSettingResponseModel> GetBillingFunderSettings(BillingFunderListRequestModel model);

        Task<BillingFunderSettingAPIResponse> DeleteFunderSetting(int id);
        Task<List<FeatureStatusDto>> GetFeaturesForAccountAsync(int accountId);
        Task<BillingSettingInformationModel> GetBillingSettingInformationAsync(int accountId);
        Task<BillingSettingInformationModel> GetDefaultBillingFromMainLocationAsync(int accountId); 
        Task<ActionResponse> SaveBillingSettingInformationAsync(SaveBillingSettingRequest request, int memberId);

        Task<BillingFunderIdRequestModel> GetBillingFunderIdsSettingAsync(int funderId, int accountInfoId);
    }
}

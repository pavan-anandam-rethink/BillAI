using BillingService.Domain.Models.Claims;
using ClearingHouseService.Web.Service;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.EligibilityRequest;

namespace ClearingHouseService.Web.Interface
{
    public interface ICommon
    {
        Task<(bool success, string result)> GenerateEDIData(ClearingHouseClaimModel claimModelDto);
        Task<(bool success, string result)> UploadfileToBlobStorage(ClaimUploadModelWithUserInfo filesWithUserInfo);
        Task<ClearingHouseDetailsModel> GetclearinghouseNameById(int clearinghouseId);
        Task<(bool success, string result)> UploadSFTPfilesToBlobStorage(DownloadSftpDataModel fileData);
        Task<bool> ReapplyPRAdjustmentAfterSecondaryBilling(int claimId);
        Task<(bool success, string result)> Generate270EDIData(Eligibility270Request eligibility270Request);
        
    }
}

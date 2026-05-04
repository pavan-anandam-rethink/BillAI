using BillingService.Domain.Models.Claims;
using IdentityModel.OidcClient;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.EligibilityRequest;

namespace ClearingHouseService.Web.Interface
{
    public interface IClearingHouseProcessor
    {
        public Task<(bool success, string result)> GenerateEDI(ClearingHouseClaimModel claimModelDto);
        Task<(bool success, string result)> UploadfileToBlobStorage(ClaimUploadModelWithUserInfo filesWithUserInfo);
        Task<(bool success, string result)> UploadSFTPfilesToBlobStorage(DownloadSftpDataModel fileStreams);
    }

    public interface IClearingHouseProcessorFor270Edi
    {
        public Task<(bool success, string result)> Generate270EDIData(Eligibility270Request eligibility270Request);
    }
}

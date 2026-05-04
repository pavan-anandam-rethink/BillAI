using ClearingHouseService.Web.Models;
namespace ClearingHouseService.Web.Service
{
    public interface IEdiUploadService
    {
        Task<OperationResult> ProcessClaimAsync(int ClaimId, string EdiData, int clearinghouseId);
        Task<ClearinghouseCredentialValidationResponse> ValidateAllClearinghousesAsync();
    }
}

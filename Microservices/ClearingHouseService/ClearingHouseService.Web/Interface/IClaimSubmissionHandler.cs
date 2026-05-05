using ClearingHouseService.Web.Service;

namespace ClearingHouseService.Web.Interface
{
    public interface IClaimSubmissionHandler
    {
        Task HandleUploadResultAsync(int claimId, OperationResult result);
    }
}
